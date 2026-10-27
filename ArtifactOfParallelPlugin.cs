using BepInEx;
using BepInEx.Logging;
using R2API.Utils;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ArtifactOfParallel
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.bepis.r2api.content_management")]
    [BepInDependency("com.bepis.r2api.language")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class ArtifactOfParallelPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "kaitorque.ArtifactOfParallel";
        public const string PluginName = "ArtifactOfParallel";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            ModConfig.Init(Config);
            ParallelArtifact.Register();
            PrivatePrefabs.Init();
            InteractionHooks.Init();
        }
    }
}
