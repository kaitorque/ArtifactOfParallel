using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using RiskOfOptions;
using RiskOfOptions.Options;

namespace ArtifactOfParallel
{
    /// <summary>Optional in-game settings page. Only touched when Risk Of Options is installed.</summary>
    internal static class RiskOfOptionsCompat
    {
        public const string Guid = "com.rune580.riskofoptions";

        public static void TryInit()
        {
            if (Chainloader.PluginInfos.ContainsKey(Guid))
            {
                Init();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void Init()
        {
            ModSettingsManager.SetModDescription(
                "Splits chests, shrines and shops between players. The host's settings apply, read at the start of each stage.");
#pragma warning disable CS0618
            if (ParallelArtifact.Def && ParallelArtifact.Def.smallIconSelectedSprite)
            {
                ModSettingsManager.SetModIcon(ParallelArtifact.Def.smallIconSelectedSprite);
            }
#pragma warning restore CS0618

            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.PrivateChests));
            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.PrivateEquipmentBarrels));
            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.PrivateLunarPods));
            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.PrivateVoidCradles));
            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.PrivateShrineOfChance));
            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.PrivateMultishops));
            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.PrivateItemBoxes));
            ModSettingsManager.AddOption(new CheckBoxOption(ModConfig.DebugLogging));
            DebugLog.Info("Risk Of Options settings page added.");
        }
    }
}
