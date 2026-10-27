using System.Collections.Generic;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtifactOfParallel
{
    internal enum PrivateType
    {
        Chest,
    }

    /// <summary>
    /// Adds <see cref="ParallelOwner"/> to private-type prefabs. Runs on every player at startup, so the
    /// NetworkBehaviour layout matches between server and clients.
    /// </summary>
    internal static class PrivatePrefabs
    {
        public const string SmallChest = "RoR2/Base/Chest1/iscChest1.asset";

        private static readonly (string key, PrivateType type)[] SpawnCards =
        {
            (SmallChest, PrivateType.Chest),
            ("RoR2/Base/Chest1StealthedVariant/iscChest1Stealthed.asset", PrivateType.Chest),
            ("RoR2/Base/Chest2/iscChest2.asset", PrivateType.Chest),
            ("RoR2/Base/GoldChest/iscGoldChest.asset", PrivateType.Chest),
            ("RoR2/Base/CategoryChest/iscCategoryChestDamage.asset", PrivateType.Chest),
            ("RoR2/Base/CategoryChest/iscCategoryChestHealing.asset", PrivateType.Chest),
            ("RoR2/Base/CategoryChest/iscCategoryChestUtility.asset", PrivateType.Chest),
            ("RoR2/DLC1/CategoryChest2/iscCategoryChest2Damage.asset", PrivateType.Chest),
            ("RoR2/DLC1/CategoryChest2/iscCategoryChest2Healing.asset", PrivateType.Chest),
            ("RoR2/DLC1/CategoryChest2/iscCategoryChest2Utility.asset", PrivateType.Chest),
        };

        private static readonly Dictionary<SpawnCard, PrivateType> typeByCard = new Dictionary<SpawnCard, PrivateType>();

        public static void Init()
        {
            foreach ((string key, PrivateType type) in SpawnCards)
            {
                InteractableSpawnCard card = Addressables.LoadAssetAsync<InteractableSpawnCard>(key).WaitForCompletion();
                if (!card || !card.prefab)
                {
                    ArtifactOfParallelPlugin.Log.LogError($"Spawn card {key} not found.");
                    continue;
                }
                if (!card.prefab.GetComponent<ParallelOwner>())
                {
                    card.prefab.AddComponent<ParallelOwner>();
                }
                typeByCard[card] = type;
            }
        }

        public static InteractableSpawnCard Load(string key)
        {
            return Addressables.LoadAssetAsync<InteractableSpawnCard>(key).WaitForCompletion();
        }

        /// <summary>True if objects from this card are split between players under the current config.</summary>
        public static bool IsPrivate(SpawnCard card)
        {
            return card && typeByCard.TryGetValue(card, out PrivateType type) && IsEnabled(type);
        }

        private static bool IsEnabled(PrivateType type)
        {
            ConfigEntry<bool> entry = type switch
            {
                PrivateType.Chest => ModConfig.PrivateChests,
                _ => null,
            };
            return entry != null && entry.Value;
        }
    }
}
