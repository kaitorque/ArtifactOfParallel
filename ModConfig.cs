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
        public static ConfigEntry<bool> PrivatePrinters { get; private set; }
        public static ConfigEntry<bool> PrivateScrappers { get; private set; }
        public static ConfigEntry<bool> PrivateDrones { get; private set; }
        public static ConfigEntry<bool> LogSpawns { get; private set; }

        public static void Init(ConfigFile config)
        {
            PrivateChests = config.Bind("Private", "Chests", true,
                "Small, large, legendary and category chests are split between players.");
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
            PrivatePrinters = config.Bind("Private", "Printers", false,
                "3D printers are split between players.");
            PrivateScrappers = config.Bind("Private", "Scrappers", false,
                "Scrappers are split between players.");
            PrivateDrones = config.Bind("Private", "Drones", false,
                "Broken drones and turrets are split between players.");

            LogSpawns = config.Bind("Debug", "LogSpawns", false,
                "Log each stage's loot budget and who got which private object.");
        }
    }
}
