using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtifactOfParallel
{
    /// <summary>
    /// Adds <see cref="ParallelOwner"/> to private-type prefabs. Runs on every player at startup, so the
    /// NetworkBehaviour layout matches between server and clients.
    /// </summary>
    internal static class PrivatePrefabs
    {
        public const string SmallChest = "RoR2/Base/Chest1/iscChest1.asset";

        private static readonly string[] SpawnCards =
        {
            SmallChest,
        };

        public static void Init()
        {
            foreach (string key in SpawnCards)
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
            }
        }

        public static InteractableSpawnCard Load(string key)
        {
            return Addressables.LoadAssetAsync<InteractableSpawnCard>(key).WaitForCompletion();
        }
    }
}
