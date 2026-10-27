using System;
using UnityEngine;

namespace ArtifactOfParallel
{
    /// <summary>
    /// For other mods (e.g. MapChests) that highlight or map interactables. Reference this mod softly and check
    /// <c>Chainloader.PluginInfos.ContainsKey("kaitorque.ArtifactOfParallel")</c> before calling.
    /// </summary>
    public static class ParallelApi
    {
        /// <summary>
        /// True if the local player should see this object: it isn't private, it's owned by the local player, or
        /// it's currently shared. Also true for objects this mod doesn't manage.
        /// </summary>
        public static bool IsVisibleToLocalPlayer(GameObject obj)
        {
            if (!obj)
            {
                return false;
            }
            ParallelOwner owner = obj.GetComponentInParent<ParallelOwner>();
            return !owner || !owner.IsLocallyHidden;
        }

        /// <summary>
        /// Raised on host and clients when a private object becomes visible (true) or hidden (false) for the local
        /// player: shared on death or disconnect, returned on revival or reconnect, or network-spawned or destroyed on
        /// a client. May be raised for objects already known, so handle it idempotently.
        /// </summary>
        public static event Action<GameObject, bool> LocalVisibilityChanged;

        internal static void RaiseLocalVisibilityChanged(GameObject obj, bool visible)
        {
            if (DebugLog.Enabled)
            {
                int handlers = LocalVisibilityChanged?.GetInvocationList().Length ?? 0;
                DebugLog.Info($"ParallelApi: {(obj ? obj.name : "null")} visible={visible}, {handlers} listeners");
            }
            if (LocalVisibilityChanged == null)
            {
                return;
            }
            try
            {
                LocalVisibilityChanged(obj, visible);
            }
            catch (Exception e)
            {
                ArtifactOfParallelPlugin.Log.LogError($"ParallelApi.LocalVisibilityChanged handler failed: {e}");
            }
        }
    }
}
