using System.Collections.Generic;
using RoR2;

namespace ArtifactOfParallel
{
    /// <summary>Logging behind the Debug.Logging setting. Check <see cref="Enabled"/> before building messages in hot paths.</summary>
    internal static class DebugLog
    {
        private static readonly HashSet<string> logged = new HashSet<string>();

        public static bool Enabled => ModConfig.DebugLogging.Value;

        public static void Init()
        {
            Stage.onStageStartGlobal += stage => logged.Clear();
        }

        public static void Info(string message)
        {
            if (Enabled)
            {
                ArtifactOfParallelPlugin.Log.LogInfo(message);
            }
        }

        /// <summary>Logs a message once per stage for the given key, for checks that run every frame.</summary>
        public static void Once(string key, string message)
        {
            if (Enabled && logged.Add(key))
            {
                ArtifactOfParallelPlugin.Log.LogInfo(message);
            }
        }
    }
}
