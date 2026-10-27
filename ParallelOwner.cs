using System.Collections.Generic;
using RoR2;
using RoR2.Hologram;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtifactOfParallel
{
    /// <summary>
    /// Makes an interactable private to one player. Added to private-type prefabs at startup so server and
    /// clients have the same NetworkBehaviour layout; without an owner it behaves like vanilla.
    /// </summary>
    public class ParallelOwner : NetworkBehaviour
    {
        private const uint OwnerDirtyBit = 1u;

        private NetworkUserId ownerId;
        private bool hasOwner;
        private NetworkInstanceId ownerNetId = NetworkInstanceId.Invalid;
        private bool isPrivate;

        private const float RescanInterval = 0.25f;

        private readonly HashSet<Behaviour> hiddenBehaviours = new HashSet<Behaviour>();
        private readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>();
        private readonly HashSet<Collider> hiddenColliders = new HashSet<Collider>();
        private bool locallyHidden;
        private float rescanTimer;

        private static readonly List<ParallelOwner> instances = new List<ParallelOwner>();

        public static IReadOnlyList<ParallelOwner> Instances => instances;

        public bool IsPrivate => isPrivate;

        public bool HasOwner => hasOwner;

        public NetworkUserId OwnerId => ownerId;

        public NetworkInstanceId OwnerNetId => ownerNetId;

        private void OnEnable()
        {
            instances.Add(this);
        }

        private void OnDisable()
        {
            instances.Remove(this);
        }

        /// <summary>Server only. Can be called before or after the object is network-spawned.</summary>
        public void SetOwner(NetworkUser owner)
        {
            hasOwner = owner;
            ownerId = owner ? owner.id : default;
            ApplyState(hasOwner, owner ? owner.netId : NetworkInstanceId.Invalid);

            if (TryGetComponent(out MultiShopController multishop))
            {
                var terminals = multishop.terminalGameObjectsList;
                for (int i = 0; i < terminals.Length; i++)
                {
                    if (terminals[i] && terminals[i].TryGetComponent(out ParallelOwner terminalOwner))
                    {
                        terminalOwner.SetOwner(owner);
                    }
                }
            }
        }

        /// <summary>
        /// Server only. Makes an owned object shared (owner gone or dead) or private again, and follows the owner's
        /// current <see cref="NetworkUser"/> after a reconnect. Opened objects are left as they are.
        /// </summary>
        public void ServerUpdateState(NetworkUser currentOwner, bool shared)
        {
            if (!hasOwner || IsUsed())
            {
                return;
            }
            bool newPrivate = !shared && currentOwner;
            NetworkInstanceId newNetId = currentOwner ? currentOwner.netId : NetworkInstanceId.Invalid;
            if (newPrivate != isPrivate || newNetId != ownerNetId)
            {
                ApplyState(newPrivate, newNetId);
            }
        }

        private void ApplyState(bool newPrivate, NetworkInstanceId newOwnerNetId)
        {
            isPrivate = newPrivate;
            ownerNetId = newOwnerNetId;
            if (isServer)
            {
                SetDirtyBit(OwnerDirtyBit);
                GetComponent<NetworkIdentity>().RebuildObservers(initialize: false);
            }
            RefreshLocalVisibility();
        }

        private bool IsUsed()
        {
            if (TryGetComponent(out PurchaseInteraction purchase))
            {
                return !purchase.available;
            }
            if (TryGetComponent(out MultiShopController multishop))
            {
                var terminals = multishop.terminalGameObjectsList;
                for (int i = 0; i < terminals.Length; i++)
                {
                    if (terminals[i] && terminals[i].TryGetComponent(out PurchaseInteraction terminal) && terminal.available)
                    {
                        return false;
                    }
                }
                return terminals.Length > 0;
            }
            return false;
        }

        public bool IsOwnedBy(NetworkUser user)
        {
            if (!user)
            {
                return false;
            }
            return NetworkServer.active ? hasOwner && user.id.Equals(ownerId) : user.netId == ownerNetId;
        }

        public bool CanBeUsedBy(Interactor activator)
        {
            return CanBeUsedBy(UserOf(activator ? activator.GetComponent<CharacterBody>() : null));
        }

        public bool CanBeUsedBy(NetworkUser user)
        {
            return !isPrivate || IsOwnedBy(user);
        }

        /// <summary>The player controlling this body, including their Remote Operation drone.</summary>
        public static NetworkUser UserOf(CharacterBody body)
        {
            CharacterMaster master = body ? body.master : null;
            PlayerCharacterMasterController player = master ? master.playerCharacterMasterController : null;
            return player ? player.networkUser : null;
        }

        public bool IsVisibleToLocalPlayer()
        {
            if (!isPrivate)
            {
                return true;
            }
            foreach (NetworkUser user in NetworkUser.readOnlyLocalPlayersList)
            {
                if (IsOwnedBy(user))
                {
                    return true;
                }
            }
            return false;
        }

        public string Describe()
        {
            string ownerName = "none";
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                if (IsOwnedBy(user))
                {
                    ownerName = user.userName;
                }
            }
            int visibleRenderers = 0;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled && !renderer.forceRenderingOff)
                {
                    visibleRenderers++;
                }
            }
            NetworkIdentity identity = GetComponent<NetworkIdentity>();
            string observers = NetworkServer.active && identity && identity.observers != null ? identity.observers.Count.ToString() : "-";
            return $"{name} netId={netId} private={isPrivate} owner={ownerName} ownerNetId={ownerNetId} " +
                $"visibleToLocal={IsVisibleToLocalPlayer()} hidden={locallyHidden} renderersOn={visibleRenderers} observers={observers} pos={transform.position}";
        }

        public override bool OnCheckObserver(NetworkConnection conn)
        {
            return !isPrivate || IsOwnerConnection(conn);
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            AddObservers(NetworkServer.connections, observers);
            AddObservers(NetworkServer.localConnections, observers);
            return true;
        }

        public override void OnSetLocalVisibility(bool vis)
        {
            RefreshLocalVisibility();
        }

        private void AddObservers(IList<NetworkConnection> connections, HashSet<NetworkConnection> observers)
        {
            foreach (NetworkConnection conn in connections)
            {
                if (conn != null && conn.isReady && OnCheckObserver(conn))
                {
                    observers.Add(conn);
                }
            }
        }

        private bool IsOwnerConnection(NetworkConnection conn)
        {
            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                if (user.connectionToClient == conn && IsOwnedBy(user))
                {
                    return true;
                }
            }
            return false;
        }

        private void Start()
        {
            RefreshLocalVisibility();
        }

        /// <summary>
        /// The host has every player's objects, so it hides the ones its local player can't see.
        /// UNet's own host visibility callback only fires in some cases, so this doesn't rely on it.
        /// </summary>
        public void RefreshLocalVisibility()
        {
            bool hide = NetworkUser.readOnlyLocalPlayersList.Count > 0 && !IsVisibleToLocalPlayer();
            if (hide == locallyHidden)
            {
                return;
            }
            locallyHidden = hide;
            if (hide)
            {
                Hide();
            }
            else
            {
                Show();
            }
            foreach (PingIndicator ping in PingIndicator.instancesList.ToArray())
            {
                if (ping && ping.pingTarget && ping.pingTarget.GetComponentInParent<ParallelOwner>() == this)
                {
                    ping.RebuildPing();
                }
            }
            ParallelApi.RaiseLocalVisibilityChanged(gameObject, !hide);
        }

        // On clients, objects appear and disappear through the network when they're shared or returned. The ones
        // present when the stage loads are found by other mods' own stage-start scans.
        private static bool IsMidStage => Run.instance && Stage.instance && Stage.instance.entryTime.timeSince > 2f;

        public override void OnStartClient()
        {
            if (!NetworkServer.active && IsMidStage)
            {
                ParallelApi.RaiseLocalVisibilityChanged(gameObject, true);
            }
        }

        public override void OnNetworkDestroy()
        {
            if (!NetworkServer.active && IsMidStage)
            {
                ParallelApi.RaiseLocalVisibilityChanged(gameObject, false);
            }
        }

        public bool IsLocallyHidden => locallyHidden;

        private void LateUpdate()
        {
            if (!locallyHidden)
            {
                return;
            }
            // Pickup displays, effects and lights get added after spawn, so look for new ones while hidden.
            rescanTimer -= Time.deltaTime;
            if (rescanTimer <= 0f)
            {
                rescanTimer = RescanInterval;
                Hide();
            }
            // Drifter's grab and throw (SpecialObjectAttributes) switch renderers, colliders and behaviours like the
            // price hologram back on, so keep everything hidden applied every frame.
            foreach (Renderer renderer in hiddenRenderers)
            {
                if (renderer && !renderer.forceRenderingOff)
                {
                    renderer.forceRenderingOff = true;
                }
            }
            foreach (Collider collider in hiddenColliders)
            {
                if (collider && collider.enabled)
                {
                    collider.enabled = false;
                }
            }
            foreach (Behaviour behaviour in hiddenBehaviours)
            {
                if (behaviour && behaviour.enabled)
                {
                    behaviour.enabled = false;
                    if (behaviour is HologramProjector projector && projector.hologramContentInstance)
                    {
                        Destroy(projector.hologramContentInstance);
                        projector.hologramContentInstance = null;
                    }
                }
            }
        }

        // Renderers are hidden with forceRenderingOff because DistanceLOD toggles Renderer.enabled on chests.
        // Safe to call repeatedly; only components not hidden yet are touched.
        private void Hide()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (hiddenRenderers.Add(renderer))
                {
                    renderer.forceRenderingOff = true;
                }
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (collider.enabled && hiddenColliders.Add(collider))
                {
                    collider.enabled = false;
                }
            }
            foreach (Light light in GetComponentsInChildren<Light>())
            {
                if (light.enabled)
                {
                    hiddenBehaviours.Add(light);
                    light.enabled = false;
                }
            }
            foreach (Highlight highlight in GetComponentsInChildren<Highlight>())
            {
                highlight.isOn = false;
                if (highlight.enabled && hiddenBehaviours.Add(highlight))
                {
                    highlight.enabled = false;
                }
            }
            // The price hologram is built on demand for whichever player is nearest, so it has to be stopped too.
            foreach (HologramProjector projector in GetComponentsInChildren<HologramProjector>())
            {
                if (projector.hologramContentInstance)
                {
                    Destroy(projector.hologramContentInstance);
                    projector.hologramContentInstance = null;
                }
                if (projector.enabled && hiddenBehaviours.Add(projector))
                {
                    projector.enabled = false;
                }
            }
        }

        private void Show()
        {
            foreach (Renderer renderer in hiddenRenderers)
            {
                if (renderer)
                {
                    renderer.forceRenderingOff = false;
                }
            }
            foreach (Collider collider in hiddenColliders)
            {
                if (collider)
                {
                    collider.enabled = true;
                }
            }
            foreach (Behaviour behaviour in hiddenBehaviours)
            {
                if (behaviour)
                {
                    behaviour.enabled = true;
                }
            }
            hiddenRenderers.Clear();
            hiddenColliders.Clear();
            hiddenBehaviours.Clear();
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                WriteState(writer);
                return true;
            }
            uint dirty = syncVarDirtyBits;
            writer.WritePackedUInt32(dirty);
            if (dirty != 0)
            {
                WriteState(writer);
            }
            return dirty != 0;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState || reader.ReadPackedUInt32() != 0)
            {
                ownerNetId = reader.ReadNetworkId();
                isPrivate = reader.ReadBoolean();
                RefreshLocalVisibility();
            }
        }

        private void WriteState(NetworkWriter writer)
        {
            writer.Write(ownerNetId);
            writer.Write(isPrivate);
        }
    }
}
