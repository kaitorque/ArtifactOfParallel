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
        EquipmentBarrel,
        LunarPod,
        VoidCradle,
        ShrineOfChance,
        Multishop,
        ItemBox,
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
            ("RoR2/Base/CasinoChest/iscCasinoChest.asset", PrivateType.Chest),
            ("RoR2/Base/EquipmentBarrel/iscEquipmentBarrel.asset", PrivateType.EquipmentBarrel),
            ("RoR2/Base/LunarChest/iscLunarChest.asset", PrivateType.LunarPod),
            ("RoR2/DLC1/VoidChest/iscVoidChest.asset", PrivateType.VoidCradle),
            ("RoR2/DLC1/VoidChest/iscVoidChestSacrificeOn.asset", PrivateType.VoidCradle),
            ("RoR2/DLC1/VoidTriple/iscVoidTriple.asset", PrivateType.VoidCradle),
            ("RoR2/Base/ShrineChance/iscShrineChance.asset", PrivateType.ShrineOfChance),
            ("RoR2/Base/ShrineChance/iscShrineChanceSandy.asset", PrivateType.ShrineOfChance),
            ("RoR2/Base/ShrineChance/iscShrineChanceSnowy.asset", PrivateType.ShrineOfChance),
            ("RoR2/Base/TripleShop/iscTripleShop.asset", PrivateType.Multishop),
            ("RoR2/Base/TripleShopLarge/iscTripleShopLarge.asset", PrivateType.Multishop),
            ("RoR2/Base/TripleShopEquipment/iscTripleShopEquipment.asset", PrivateType.Multishop),
            ("RoR2/Junk/TreasureCache/iscLockbox.asset", PrivateType.ItemBox),
            ("RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset", PrivateType.ItemBox),
            ("RoR2/DLC1/FreeChest/iscFreeChest.asset", PrivateType.ItemBox),
        };

        private static readonly Dictionary<SpawnCard, PrivateType> typeByCard = new Dictionary<SpawnCard, PrivateType>();
        private static readonly Dictionary<string, InteractableSpawnCard> cardByNameToken = new Dictionary<string, InteractableSpawnCard>();

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
                AddOwner(card.prefab);
                if (card.prefab.TryGetComponent(out MultiShopController multishop) && multishop.terminalPrefab)
                {
                    AddOwner(multishop.terminalPrefab);
                }
                typeByCard[card] = type;
                if (type != PrivateType.ItemBox && card.prefab.TryGetComponent(out PurchaseInteraction purchase)
                    && !string.IsNullOrEmpty(purchase.displayNameToken) && !cardByNameToken.ContainsKey(purchase.displayNameToken))
                {
                    cardByNameToken[purchase.displayNameToken] = card;
                }
            }
        }

        private static void AddOwner(GameObject prefab)
        {
            if (!prefab.GetComponent<ParallelOwner>())
            {
                prefab.AddComponent<ParallelOwner>();
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

        public static bool IsItemBox(SpawnCard card)
        {
            return card && typeByCard.TryGetValue(card, out PrivateType type) && type == PrivateType.ItemBox;
        }

        /// <summary>
        /// Matches a stage-placed object to the private spawn card it's a copy of, by its display name.
        /// </summary>
        public static InteractableSpawnCard FindCardForSceneObject(PurchaseInteraction purchase)
        {
            if (purchase && !string.IsNullOrEmpty(purchase.displayNameToken)
                && cardByNameToken.TryGetValue(purchase.displayNameToken, out InteractableSpawnCard card) && IsPrivate(card))
            {
                return card;
            }
            return null;
        }

        private static bool IsEnabled(PrivateType type)
        {
            ConfigEntry<bool> entry = type switch
            {
                PrivateType.Chest => ModConfig.PrivateChests,
                PrivateType.EquipmentBarrel => ModConfig.PrivateEquipmentBarrels,
                PrivateType.LunarPod => ModConfig.PrivateLunarPods,
                PrivateType.VoidCradle => ModConfig.PrivateVoidCradles,
                PrivateType.ShrineOfChance => ModConfig.PrivateShrineOfChance,
                PrivateType.Multishop => ModConfig.PrivateMultishops,
                PrivateType.ItemBox => ModConfig.PrivateItemBoxes,
                _ => null,
            };
            return entry != null && entry.Value;
        }
    }
}
