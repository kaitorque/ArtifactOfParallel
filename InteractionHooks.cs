using RoR2;

namespace ArtifactOfParallel
{
    internal static class InteractionHooks
    {
        public static void Init()
        {
            On.RoR2.PurchaseInteraction.GetInteractability += PurchaseInteraction_GetInteractability;
        }

        private static Interactability PurchaseInteraction_GetInteractability(
            On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.TryGetComponent(out ParallelOwner owner) && !owner.CanBeUsedBy(activator))
            {
                return Interactability.Disabled;
            }
            return orig(self, activator);
        }
    }
}
