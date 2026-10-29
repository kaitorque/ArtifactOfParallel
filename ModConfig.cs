using BepInEx.Configuration;

namespace ArtifactOfParallel
{
    internal static class ModConfig
    {
        public static ConfigEntry<bool> PrivateChests { get; private set; }
        public static ConfigEntry<bool> PrivateEquipmentBarrels { get; private set; }
        public static ConfigEntry<bool> PrivateLunarPods { get; private set; }
        public static ConfigEntry<bool> PrivateVoidCradles { get; private set; }
        public static ConfigEntry<bool> PrivateShrineOfChance { get; private set; }
        public static ConfigEntry<bool> PrivateMultishops { get; private set; }
        public static ConfigEntry<bool> PrivateItemBoxes { get; private set; }
        public static ConfigEntry<bool> DebugLogging { get; private set; }

        public static void Init(ConfigFile config)
        {
            PrivateChests = config.Bind("Private", "Chests", true,
                "Small, large, legendary, category and adaptive chests are split between players.");
            PrivateEquipmentBarrels = config.Bind("Private", "EquipmentBarrels", true,
                "Equipment barrels are split between players.");
            PrivateLunarPods = config.Bind("Private", "LunarPods", true,
                "Lunar pods are split between players.");
            PrivateVoidCradles = config.Bind("Private", "VoidCradles", true,
                "Void cradles and void potentials are split between players.");
            PrivateShrineOfChance = config.Bind("Private", "ShrineOfChance", true,
                "Shrines of Chance are split between players.");
            PrivateMultishops = config.Bind("Private", "Multishops", true,
                "Multishop terminals are split between players.");
            PrivateItemBoxes = config.Bind("Private", "ItemBoxes", true,
                "Rusted Key lockboxes, Encrusted Key caches and Shipping Request Form chests belong to the player whose item spawned them.");

            DebugLogging = config.Bind("Debug", "Logging", false,
                "Log each stage's loot split, ownership changes, and interaction, Drifter, ping and hiding checks. For testing.");
        }

        public static string Summary() =>
            $"Chests={PrivateChests.Value} EquipmentBarrels={PrivateEquipmentBarrels.Value} LunarPods={PrivateLunarPods.Value} " +
            $"VoidCradles={PrivateVoidCradles.Value} ShrineOfChance={PrivateShrineOfChance.Value} Multishops={PrivateMultishops.Value} " +
            $"ItemBoxes={PrivateItemBoxes.Value}";
    }
}
