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

        private static readonly List<Spawned> privateObjects = new List<Spawned>();
        private static int sharedValue;

        public static void Init()
        {
            SceneDirector.onPrePopulateSceneServer += OnPrePopulate;
            SceneDirector.onSpawnInteractableServer += OnSpawnInteractable;
            SceneDirector.onPostPopulateSceneServer += OnPostPopulate;
        }

        private static void OnPrePopulate(SceneDirector director)
        {
            privateObjects.Clear();
            sharedValue = 0;
        }

        private static void OnSpawnInteractable(SceneDirector director, GameObject obj, DirectorCard card)
        {
            if (!ParallelArtifact.Enabled || !obj || card == null)
            {
                return;
            }
            if (PrivatePrefabs.IsPrivate(card.GetSpawnCard()) && obj.GetComponent<ParallelOwner>())
            {
                privateObjects.Add(new Spawned { obj = obj, value = card.cost });
            }
            else
            {
                sharedValue += card.cost;
            }
        }

        private static void OnPostPopulate(SceneDirector director)
        {
            if (!ParallelArtifact.Enabled)
            {
                privateObjects.Clear();
                return;
            }

            List<NetworkUser> players = NetworkUser.readOnlyInstancesList.Where(user => user && user.master).ToList();
            List<Spawned> objects = privateObjects.Where(spawned => spawned.obj).ToList();
            int privateValue = objects.Sum(spawned => spawned.value);
            privateObjects.Clear();
            if (players.Count <= 1)
            {
                Log(players.Count, privateValue, privateValue, null, null, null, 0, 0);
                return;
            }

            var rng = new Xoroshiro128Plus(Run.instance.seed ^ (ulong)(Run.instance.stageClearCount + 1) * 0x9E3779B97F4A7C15UL);
            Shuffle(players, rng);
            Shuffle(objects, rng);
            objects = objects.OrderByDescending(spawned => spawned.value).ToList();

            float target = (float)privateValue / players.Count;
            int[] assigned = new int[players.Count];
            int[] counts = new int[players.Count];
            int removedValue = 0;
            int removedCount = 0;

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
                }
                else
                {
                    removedValue += spawned.value;
                    removedCount++;
                    NetworkServer.Destroy(spawned.obj);
                }
            }

            Log(players.Count, privateValue, target, players, assigned, counts, removedValue, removedCount);
        }

        private static void Shuffle<T>(List<T> list, Xoroshiro128Plus rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.RangeInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void Log(int playerCount, int privateValue, float target, List<NetworkUser> players,
            int[] assigned, int[] counts, int removedValue, int removedCount)
        {
            if (!ModConfig.LogSpawns.Value)
            {
                return;
            }
            var text = new StringBuilder();
            text.Append($"Stage {SceneCatalog.mostRecentSceneDef?.cachedName}: players={playerCount} ");
            text.Append($"shared={sharedValue} private={privateValue} total={sharedValue + privateValue} target={target:0.#}");
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    text.Append($"\n  {players[i].userName}: value={assigned[i]} objects={counts[i]}");
                }
                text.Append($"\n  removed: value={removedValue} objects={removedCount}");
            }
            else
            {
                text.Append(" (not split: fewer than 2 players)");
            }
            ArtifactOfParallelPlugin.Log.LogInfo(text.ToString());
        }
    }
}
