using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtifactOfParallel
{
    internal static class TestCommands
    {
        private const float Spacing = 3f;

        [ConCommand(commandName = "parallel_test_chests", flags = ConVarFlags.ExecuteOnServer,
            helpText = "Spawns one small chest per player in a row in front of you. Each player should see only their own.")]
        private static void SpawnTestChests(ConCommandArgs args)
        {
            CharacterBody body = args.senderBody;
            if (!NetworkServer.active || !body)
            {
                Debug.Log("parallel_test_chests: needs a living character on the host's run.");
                return;
            }

            InteractableSpawnCard card = PrivatePrefabs.Load(PrivatePrefabs.SmallChest);
            if (!card || !card.prefab || !card.prefab.GetComponent<ParallelOwner>())
            {
                Debug.Log("parallel_test_chests: small chest spawn card failed to load; see the startup log.");
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(body.inputBank ? body.inputBank.aimDirection : body.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 center = body.footPosition + forward * 6f;
            Quaternion rotation = Quaternion.LookRotation(-forward, Vector3.up);

            int total = 0;
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                if (user.master)
                {
                    total++;
                }
            }

            int count = 0;
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                if (!user.master)
                {
                    continue;
                }
                Vector3 position = center + right * ((count - (total - 1) * 0.5f) * Spacing);
                if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, LayerIndex.world.mask))
                {
                    position = hit.point;
                }
                GameObject chest = Object.Instantiate(card.prefab, position, rotation);
                chest.GetComponent<ParallelOwner>().SetOwner(user);
                NetworkServer.Spawn(chest);
                count++;
                ArtifactOfParallelPlugin.Log.LogInfo($"Test chest for {user.userName} at {position}");
            }
            Debug.Log($"parallel_test_chests: spawned {count} chests.");
        }

        [ConCommand(commandName = "parallel_debug",
            helpText = "Lists the private objects that exist on this game, their owner and whether they're hidden here.")]
        private static void DebugList(ConCommandArgs args)
        {
            ParallelOwner[] owners = Object.FindObjectsOfType<ParallelOwner>();
            ArtifactOfParallelPlugin.Log.LogInfo($"parallel_debug: server={NetworkServer.active} client={NetworkClient.active} objects={owners.Length}");
            foreach (ParallelOwner owner in owners)
            {
                ArtifactOfParallelPlugin.Log.LogInfo("  " + owner.Describe());
            }
            Debug.Log($"parallel_debug: {owners.Length} private-type objects here; details in the BepInEx log.");
        }
    }
}
