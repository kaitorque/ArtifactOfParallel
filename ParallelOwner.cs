using System.Collections.Generic;
using RoR2;
using RoR2.Hologram;
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

        private readonly List<Behaviour> hiddenBehaviours = new List<Behaviour>();
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<Collider> hiddenColliders = new List<Collider>();
        private bool locallyHidden;

        public bool IsPrivate => isPrivate;

        public NetworkInstanceId OwnerNetId => ownerNetId;

        /// <summary>Server only. Can be called before or after the object is network-spawned.</summary>
        public void SetOwner(NetworkUser owner)
        {
            hasOwner = owner;
            ownerId = owner ? owner.id : default;
            isPrivate = hasOwner;
            ownerNetId = owner ? owner.netId : NetworkInstanceId.Invalid;
            if (isServer)
            {
                SetDirtyBit(OwnerDirtyBit);
                GetComponent<NetworkIdentity>().RebuildObservers(initialize: false);
            }
            RefreshLocalVisibility();
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
            if (!isPrivate)
            {
                return true;
            }
            CharacterBody body = activator ? activator.GetComponent<CharacterBody>() : null;
            CharacterMaster master = body ? body.master : null;
            PlayerCharacterMasterController player = master ? master.playerCharacterMasterController : null;
            return player && IsOwnedBy(player.networkUser);
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
        }

        private void LateUpdate()
        {
            if (!locallyHidden)
            {
                return;
            }
            // Other systems (Drifter's bag) also toggle forceRenderingOff, so keep it applied while hidden.
            foreach (Renderer renderer in hiddenRenderers)
            {
                if (renderer && !renderer.forceRenderingOff)
                {
                    renderer.forceRenderingOff = true;
                }
            }
        }

        // Renderers are hidden with forceRenderingOff because DistanceLOD toggles Renderer.enabled on chests.
        private void Hide()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.forceRenderingOff = true;
                hiddenRenderers.Add(renderer);
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (collider.enabled)
                {
                    collider.enabled = false;
                    hiddenColliders.Add(collider);
                }
            }
            foreach (Highlight highlight in GetComponentsInChildren<Highlight>())
            {
                highlight.isOn = false;
                if (highlight.enabled)
                {
                    highlight.enabled = false;
                    hiddenBehaviours.Add(highlight);
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
                if (projector.enabled)
                {
                    projector.enabled = false;
                    hiddenBehaviours.Add(projector);
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
