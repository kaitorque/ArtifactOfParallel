using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.Projectile;
using RoR2.UI;
using UnityEngine;

namespace ArtifactOfParallel
{
    internal static class InteractionHooks
    {
        private static Hook isTargetableHook;

        // Set while the server checks a Drifter's grab or Tinker, so isTargetable can judge by that player.
        private static NetworkUser drifterActor;
        private static bool hasDrifterActor;

        public static void Init()
        {
            On.RoR2.PurchaseInteraction.GetInteractability += PurchaseInteraction_GetInteractability;
            On.RoR2.DrifterBagController.AssignPassenger += DrifterBagController_AssignPassenger;
            On.RoR2.Projectile.TinkerProjectile.TransmuteTargetObject += TinkerProjectile_TransmuteTargetObject;
            On.RoR2.UI.PingIndicator.RebuildPing += PingIndicator_RebuildPing;
            isTargetableHook = new Hook(
                typeof(SpecialObjectAttributes).GetProperty(nameof(SpecialObjectAttributes.isTargetable)).GetGetMethod(),
                new Func<Func<SpecialObjectAttributes, bool>, SpecialObjectAttributes, bool>(SpecialObjectAttributes_isTargetable));
        }

        private static Interactability PurchaseInteraction_GetInteractability(
            On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.TryGetComponent(out ParallelOwner owner))
            {
                if (!owner.CanBeUsedBy(activator))
                {
                    return Interactability.Disabled;
                }
                // The owner's Remote Operation drone may open their own private objects.
                if (owner.IsPrivate && activator && activator.isRemoteOp && !self.allowRemoteOpInteraction)
                {
                    self.allowRemoteOpInteraction = true;
                    try
                    {
                        return orig(self, activator);
                    }
                    finally
                    {
                        self.allowRemoteOpInteraction = false;
                    }
                }
            }
            return orig(self, activator);
        }

        /// <summary>
        /// A ping outlines its target's renderers even when they're hidden, which would show a private object's
        /// shape on the host. Keep the ping marker but drop the outline, like on clients that don't have the object.
        /// </summary>
        private static void PingIndicator_RebuildPing(On.RoR2.UI.PingIndicator.orig_RebuildPing orig, PingIndicator self)
        {
            orig(self);
            ParallelOwner owner = self.pingTarget ? self.pingTarget.GetComponentInParent<ParallelOwner>() : null;
            if (owner && owner.IsLocallyHidden && PingTargetRenderers.GetValue(self) is List<Renderer> renderers)
            {
                renderers.Clear();
            }
        }

        private static readonly FieldInfo PingTargetRenderers =
            typeof(PingIndicator).GetField("pingTargetRenderers", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Drifter's grab, smash and Tinker all check isTargetable. On the Drifter player's own game that means
        /// "visible here"; during the server's checks it means "usable by that Drifter".
        /// </summary>
        private static bool SpecialObjectAttributes_isTargetable(Func<SpecialObjectAttributes, bool> orig, SpecialObjectAttributes self)
        {
            bool result = orig(self);
            if (!result || !self.TryGetComponent(out ParallelOwner owner) || !owner.IsPrivate)
            {
                return result;
            }
            return hasDrifterActor ? owner.CanBeUsedBy(drifterActor) : owner.IsVisibleToLocalPlayer();
        }

        private static void DrifterBagController_AssignPassenger(
            On.RoR2.DrifterBagController.orig_AssignPassenger orig, DrifterBagController self, GameObject passengerObject)
        {
            using (new DrifterScope(self.GetComponent<CharacterBody>()))
            {
                orig(self, passengerObject);
            }
        }

        private static void TinkerProjectile_TransmuteTargetObject(
            On.RoR2.Projectile.TinkerProjectile.orig_TransmuteTargetObject orig, TinkerProjectile self, GameObject targetObject)
        {
            GameObject ownerObject = self.TryGetComponent(out ProjectileController projectile) ? projectile.owner : null;
            using (new DrifterScope(ownerObject ? ownerObject.GetComponent<CharacterBody>() : null))
            {
                orig(self, targetObject);
            }
        }

        private readonly struct DrifterScope : IDisposable
        {
            public DrifterScope(CharacterBody body)
            {
                drifterActor = ParallelOwner.UserOf(body);
                hasDrifterActor = true;
            }

            public void Dispose()
            {
                drifterActor = null;
                hasDrifterActor = false;
            }
        }
    }
}
