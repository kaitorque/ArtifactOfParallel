using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtifactOfParallel
{
    /// <summary>
    /// Server only. Shares an owner's unopened objects while they're disconnected or dead, and makes them private
    /// again when they reconnect, revive or take over a Remote Operation drone.
    /// </summary>
    internal static class OwnershipTracker
    {
        private const float Interval = 0.25f;
        // Body swaps and revives briefly leave a player without a body; only share after this long.
        private const float DeathDelay = 1f;

        private static readonly HashSet<NetworkUserId> aliveThisStage = new HashSet<NetworkUserId>();
        private static readonly Dictionary<NetworkUserId, float> deadSince = new Dictionary<NetworkUserId, float>();
        private static readonly Dictionary<NetworkUserId, bool> lastShared = new Dictionary<NetworkUserId, bool>();
        private static readonly Dictionary<NetworkUserId, (NetworkUser user, bool shared)> states =
            new Dictionary<NetworkUserId, (NetworkUser user, bool shared)>();
        private static readonly Dictionary<NetworkUserId, string> names = new Dictionary<NetworkUserId, string>();
        private static readonly List<ParallelOwner> buffer = new List<ParallelOwner>();
        private static float timer;

        public static void Init()
        {
            SceneDirector.onPrePopulateSceneServer += director =>
            {
                aliveThisStage.Clear();
                deadSince.Clear();
                lastShared.Clear();
            };
        }

        public static void FixedUpdate()
        {
            timer -= Time.fixedDeltaTime;
            if (timer > 0f)
            {
                return;
            }
            timer = Interval;
            if (!NetworkServer.active || !ParallelArtifact.Enabled || ParallelOwner.Instances.Count == 0)
            {
                return;
            }

            states.Clear();
            buffer.Clear();
            buffer.AddRange(ParallelOwner.Instances);
            foreach (ParallelOwner owner in buffer)
            {
                if (!owner || !owner.HasOwner)
                {
                    continue;
                }
                NetworkUserId id = owner.OwnerId;
                if (!states.TryGetValue(id, out (NetworkUser user, bool shared) state))
                {
                    state = Evaluate(id);
                    states[id] = state;
                    LogChange(id, state);
                }
                owner.ServerUpdateState(state.user, state.shared);
            }
        }

        private static (NetworkUser user, bool shared) Evaluate(NetworkUserId id)
        {
            NetworkUser user = null;
            foreach (NetworkUser candidate in NetworkUser.readOnlyInstancesList)
            {
                if (candidate && candidate.id.Equals(id))
                {
                    user = candidate;
                    break;
                }
            }
            if (!user)
            {
                return (null, true);
            }
            names[id] = user.userName;

            CharacterMaster master = user.master;
            CharacterBody body = master ? master.GetBody() : null;
            if (body && body.healthComponent && body.healthComponent.alive)
            {
                aliveThisStage.Add(id);
                deadSince.Remove(id);
                return (user, false);
            }
            // Bodies spawn after the stage is assigned; a player who hasn't spawned yet isn't dead.
            if (!aliveThisStage.Contains(id))
            {
                return (user, false);
            }
            if (!deadSince.TryGetValue(id, out float since))
            {
                deadSince[id] = since = Time.time;
            }
            return (user, Time.time - since >= DeathDelay);
        }

        private static void LogChange(NetworkUserId id, (NetworkUser user, bool shared) state)
        {
            if (lastShared.TryGetValue(id, out bool wasShared) && wasShared == state.shared)
            {
                return;
            }
            lastShared[id] = state.shared;
            if (DebugLog.Enabled && (wasShared || state.shared))
            {
                string name = state.user ? state.user.userName : names.TryGetValue(id, out string known) ? known : "unknown player";
                string reason = !state.user ? "disconnected" : state.shared ? "dead" : "back";
                ArtifactOfParallelPlugin.Log.LogInfo($"Owner {name} {reason}: objects now {(state.shared ? "shared" : "private")}");
            }
        }
    }
}
