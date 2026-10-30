using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtifactOfParallel
{
    /// <summary>
    /// Leaves the vanilla scene director alone and, once it has spawned everything, splits the private
    /// objects between players so each owns about an equal share of their combined director value.
    /// </summary>
    internal static class LootAssigner
    {
        private struct Spawned
        {
            public GameObject obj;
            public int value;
        }

        private struct Fixed
        {
            public GameObject obj;
            public InteractableSpawnCard card;
        }

        private static readonly List<Spawned> privateObjects = new List<Spawned>();
        private static readonly Dictionary<string, List<GameObject>> itemBoxes = new Dictionary<string, List<GameObject>>();
        private static readonly SortedDictionary<string, (int count, int value)> sharedCounts = new SortedDictionary<string, (int count, int value)>();
        private static readonly SortedDictionary<string, (int count, int value)> privateCounts = new SortedDictionary<string, (int count, int value)>();
        private static int sharedValue;
        private static bool populating;

        public static void Init()
        {
            SceneDirector.onPrePopulateSceneServer += OnPrePopulate;
            SceneDirector.onSpawnInteractableServer += OnSpawnInteractable;
            SceneDirector.onPostPopulateSceneServer += OnPostPopulate;
            On.RoR2.DirectorCore.TrySpawnObject += DirectorCore_TrySpawnObject;
        }

        private static void OnPrePopulate(SceneDirector director)
        {
            privateObjects.Clear();
            itemBoxes.Clear();
            sharedCounts.Clear();
            privateCounts.Clear();
            sharedValue = 0;
            populating = true;
        }

        private static void OnSpawnInteractable(SceneDirector director, GameObject obj, DirectorCard card)
        {
            if (!obj || card == null)
            {
                return;
            }
            bool isPrivate = PrivatePrefabs.IsPrivate(card.GetSpawnCard()) && obj.GetComponent<ParallelOwner>();
            if (DebugLog.Enabled)
            {
                CountSpawn(isPrivate && ParallelArtifact.Enabled ? privateCounts : sharedCounts, card);
            }
            if (!ParallelArtifact.Enabled)
            {
                return;
            }
            if (isPrivate)
            {
                privateObjects.Add(new Spawned { obj = obj, value = card.cost });
            }
            else
            {
                sharedValue += card.cost;
            }
        }

        // Item boxes are spawned straight through DirectorCore, one per item holder, in holder order. Failed spawns
        // are kept as null so box k still lines up with holder k.
        private static GameObject DirectorCore_TrySpawnObject(On.RoR2.DirectorCore.orig_TrySpawnObject orig, DirectorCore self, DirectorSpawnRequest request)
        {
            GameObject result = orig(self, request);
            if (populating && request != null && PrivatePrefabs.IsItemBox(request.spawnCard))
            {
                string name = request.spawnCard.name;
                if (!itemBoxes.TryGetValue(name, out List<GameObject> list))
                {
                    itemBoxes[name] = list = new List<GameObject>();
                }
                list.Add(result);
            }
            return result;
        }

        private static void OnPostPopulate(SceneDirector director)
        {
            populating = false;
            List<Spawned> objects = privateObjects.Where(spawned => spawned.obj).ToList();
            privateObjects.Clear();
            LogSpawnCounts();
            if (!ParallelArtifact.Enabled)
            {
                return;
            }

            DebugLog.Info($"Settings: {ModConfig.Summary()}");
            List<NetworkUser> players = NetworkUser.readOnlyInstancesList.Where(user => user && user.master).ToList();
            int privateValue = objects.Sum(spawned => spawned.value);
            var log = new StringBuilder();
            log.Append($"Stage {SceneCatalog.mostRecentSceneDef?.cachedName}: players={players.Count} shared={sharedValue} private={privateValue}");            if (players.Count <= 1)
            {
                Log(log.Append(" (not split: fewer than 2 players)"));
                return;
            }

            var rng = new Xoroshiro128Plus(Run.instance.seed ^ (ulong)(Run.instance.stageClearCount + 1) * 0x9E3779B97F4A7C15UL);
            Shuffle(players, rng);

            List<Fixed> fixedObjects = FindFixedObjects();
            int fixedValue = fixedObjects.Sum(item => item.card.directorCreditCost);
            float target = (float)(privateValue + fixedValue) / players.Count;
            bool copyFixed = fixedObjects.Count > 0 && fixedValue <= target;
            if (!copyFixed)
            {
                target = (float)privateValue / players.Count;
            }
            log.Append($" fixed={fixedValue} ({fixedObjects.Count} objects, {(fixedObjects.Count == 0 ? "none" : copyFixed ? "copied" : "shared")})");
            log.Append($" target={target:0.#}");

            int[] assigned = new int[players.Count];
            int[] counts = new int[players.Count];
            if (copyFixed)
            {
                CopyFixedObjects(fixedObjects, players);
                for (int i = 0; i < players.Count; i++)
                {
                    assigned[i] = fixedValue;
                    counts[i] = fixedObjects.Count;
                }
            }

            Shuffle(objects, rng);
            objects = objects.OrderByDescending(spawned => spawned.value).ToList();
            int removedValue = 0;
            int removedCount = 0;
            var received = new List<string>[players.Count];
            for (int i = 0; i < received.Length; i++)
            {
                received[i] = new List<string>();
            }
            var removedNames = new List<string>();
            foreach (Spawned spawned in objects)
            {
                int lowest = 0;
                for (int i = 1; i < assigned.Length; i++)
                {
                    if (assigned[i] < assigned[lowest])
                    {
                        lowest = i;
                    }
                }
                if (assigned[lowest] + spawned.value <= target + spawned.value * 0.5f)
                {
                    spawned.obj.GetComponent<ParallelOwner>().SetOwner(players[lowest]);
                    assigned[lowest] += spawned.value;
                    counts[lowest]++;
                    received[lowest].Add(ShortName(spawned));
                }
                else
                {
                    removedValue += spawned.value;
                    removedCount++;
                    removedNames.Add(ShortName(spawned));
                    NetworkServer.Destroy(spawned.obj);
                }
            }

            for (int i = 0; i < players.Count; i++)
            {
                log.Append($"\n  {players[i].userName}: value={assigned[i]} objects={counts[i]} [{string.Join(", ", received[i])}]");
            }
            log.Append($"\n  removed: value={removedValue} objects={removedCount} [{string.Join(", ", removedNames)}]");
            AssignItemBoxes(log);
            Log(log);
        }

        /// <summary>Active, unopened stage-placed objects of a private type.</summary>
        private static List<Fixed> FindFixedObjects()
        {
            var result = new List<Fixed>();
            foreach (PurchaseInteraction purchase in Object.FindObjectsOfType<PurchaseInteraction>())
            {
                if (!purchase.available || purchase.GetComponent<ParallelOwner>())
                {
                    continue;
                }
                NetworkIdentity identity = purchase.GetComponent<NetworkIdentity>();
                if (!identity || identity.sceneId.IsEmpty())
                {
                    continue;
                }
                InteractableSpawnCard card = PrivatePrefabs.FindCardForSceneObject(purchase);
                if (card)
                {
                    result.Add(new Fixed { obj = purchase.gameObject, card = card });
                }
            }
            return result;
        }

        /// <summary>
        /// Scene objects can't carry <see cref="ParallelOwner"/>, so each one is replaced by one spawned copy per player.
        /// </summary>
        private static void CopyFixedObjects(List<Fixed> fixedObjects, List<NetworkUser> players)
        {
            foreach (Fixed item in fixedObjects)
            {
                Transform original = item.obj.transform;
                foreach (NetworkUser player in players)
                {
                    GameObject copy = Object.Instantiate(item.card.prefab, original.position, original.rotation);
                    copy.GetComponent<ParallelOwner>().SetOwner(player);
                    NetworkServer.Spawn(copy);
                }
                NetworkIdentity identity = item.obj.GetComponent<NetworkIdentity>();
                if (identity && !identity.netId.IsEmpty())
                {
                    NetworkServer.Destroy(item.obj);
                }
                else
                {
                    Object.Destroy(item.obj);
                }
            }
        }

        private static void AssignItemBoxes(StringBuilder log)
        {
            if (!ModConfig.PrivateItemBoxes.Value)
            {
                return;
            }
            AssignItemBoxes(log, "iscLockbox", RoR2Content.Items.TreasureCache);
            AssignItemBoxes(log, "iscLockboxVoid", DLC1Content.Items.TreasureCacheVoid);
            AssignItemBoxes(log, "iscFreeChest", DLC1Content.Items.FreeChest);
        }

        private static void AssignItemBoxes(StringBuilder log, string cardName, ItemDef item)
        {
            if (!item || !itemBoxes.TryGetValue(cardName, out List<GameObject> boxes))
            {
                return;
            }
            List<CharacterMaster> holders = CharacterMaster.readOnlyInstancesList
                .Where(master => master.inventory && master.inventory.GetItemCountEffective(item) > 0)
                .ToList();
            for (int i = 0; i < boxes.Count; i++)
            {
                GameObject box = boxes[i];
                if (!box || !box.TryGetComponent(out ParallelOwner owner))
                {
                    continue;
                }
                NetworkUser user = i < holders.Count ? PlayerFor(holders[i]) : null;
                if (user)
                {
                    owner.SetOwner(user);
                }
                log.Append($"\n  {cardName}: {(user ? user.userName : "shared")}");
            }
        }

        private static NetworkUser PlayerFor(CharacterMaster master)
        {
            if (master.playerCharacterMasterController)
            {
                return master.playerCharacterMasterController.networkUser;
            }
            CharacterMaster leader = master.minionOwnership ? master.minionOwnership.ownerMaster : null;
            return leader && leader.playerCharacterMasterController ? leader.playerCharacterMasterController.networkUser : null;
        }

        private static void Shuffle<T>(List<T> list, Xoroshiro128Plus rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.RangeInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static string ShortName(Spawned spawned) => $"{spawned.obj.name.Replace("(Clone)", "")} {spawned.value}";

        private static void CountSpawn(SortedDictionary<string, (int count, int value)> counts, DirectorCard card)
        {
            SpawnCard spawnCard = card.GetSpawnCard();
            string name = spawnCard ? spawnCard.name : "unknown";
            counts.TryGetValue(name, out (int count, int value) entry);
            counts[name] = (entry.count + 1, entry.value + card.cost);
        }

        /// <summary>What the vanilla director spawned this stage, before any split. Compare with the artifact off.</summary>
        private static void LogSpawnCounts()
        {
            if (!DebugLog.Enabled)
            {
                return;
            }
            var log = new StringBuilder();
            log.Append($"Director spawns on {SceneCatalog.mostRecentSceneDef?.cachedName} (artifact {(ParallelArtifact.Enabled ? "on" : "off")}, " +
                $"{NetworkUser.readOnlyInstancesList.Count(user => user && user.master)} players, before the split):");
            AppendCounts(log, "shared", sharedCounts);
            AppendCounts(log, "private", privateCounts);
            DebugLog.Info(log.ToString());
        }

        private static void AppendCounts(StringBuilder log, string label, SortedDictionary<string, (int count, int value)> counts)
        {
            log.Append($"\n  {label}: {counts.Values.Sum(entry => entry.count)} objects, value {counts.Values.Sum(entry => entry.value)}");
            foreach (KeyValuePair<string, (int count, int value)> pair in counts)
            {
                log.Append($"\n    {pair.Key} x{pair.Value.count} (value {pair.Value.value})");
            }
        }

        private static void Log(StringBuilder text)
        {
            DebugLog.Info(text.ToString());
        }
    }
}
