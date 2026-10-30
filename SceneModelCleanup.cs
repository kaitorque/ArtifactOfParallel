using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtifactOfParallel
{
    /// <summary>
    /// Client only. The host replaces stage-placed private objects with one copy per player, but UNet only deactivates
    /// a removed scene object on clients. Its model was already detached by <see cref="ModelLocator"/>, so it stayed
    /// behind as a closed chest nobody could open. Keeps such models active only while their object is.
    /// </summary>
    internal static class SceneModelCleanup
    {
        public static void Init()
        {
            On.RoR2.ModelLocator.Start += ModelLocator_Start;
        }

        private static void ModelLocator_Start(On.RoR2.ModelLocator.orig_Start orig, ModelLocator self)
        {
            orig(self);
            if (NetworkServer.active || !ParallelArtifact.Enabled)
            {
                return;
            }
            Transform model = self.modelTransform;
            if (!model || model.IsChildOf(self.transform))
            {
                return;
            }
            if (!self.TryGetComponent(out NetworkIdentity identity) || identity.sceneId.IsEmpty()
                || !self.TryGetComponent(out PurchaseInteraction purchase) || !PrivatePrefabs.IsPrivateType(purchase))
            {
                return;
            }
            self.gameObject.AddComponent<DetachedModelFollower>().model = model.gameObject;
        }
    }

    internal class DetachedModelFollower : MonoBehaviour
    {
        public GameObject model;

        private void OnEnable()
        {
            if (model)
            {
                model.SetActive(true);
            }
        }

        private void OnDisable()
        {
            // Not while the stage unloads; the model goes with it.
            if (!model || !gameObject.scene.isLoaded)
            {
                return;
            }
            model.SetActive(false);
            DebugLog.Info($"Scene object: hid the leftover model of removed {name} at {transform.position}");
        }
    }
}
