using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AEB.StateMachine
{
    /// <summary>
    /// Represents a networked state machine that synchronizes state changes across the network.
    /// It extends the basic state machine functionality to work in a multiplayer environment.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public abstract class NetworkedStateMachine<EState> : StateMachine<EState> where EState : Enum
    {
        #region Variables

        protected PhotonView photonView;
        protected List<MethodInfo> stateRPCMethods = new List<MethodInfo>();
        protected Dictionary<int, StateView<EState>> stateViews = new Dictionary<int, StateView<EState>>();
        protected Dictionary<GameObject, NetworkedStateObject<EState>> objectToStateViewMap = new();

        #endregion

        #region Events 

        /// <summary>
        /// Event triggered when ownership of a StateView is requested.
        /// </summary>
        event Action<StateView<EState>, Player> OnOwnershipRequestEv;

        /// <summary>
        /// Event triggered when ownership of a StateView is transferred.
        /// </summary>
        event Action<StateView<EState>, Player> OnOwnershipTransferedEv;

        /// <summary>
        /// Event triggered when ownership transfer of a StateView fails.
        /// </summary>
        event Action<StateView<EState>, Player> OnOwnershipTransferFailedEv;

        /// <summary>
        /// Action to hook when the state view is ready.
        /// </summary>
        internal Action<int, StateView<EState>> OnHookStateReady;

        #endregion

        #region Properties

        public PhotonView View => photonView;

        #endregion

        #region Unity

        protected override void Awake()
        {
            base.Awake();
            photonView = GetComponent<PhotonView>();
            CollectStateRPCs();
        }

        protected override void Update()
        {
            if (!photonView.IsMine) return;

            base.Update();
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine) return;

            base.OnTriggerEnter(other);
        }

        protected override void OnTriggerStay(Collider other)
        {
            if (!photonView.IsMine) return;

            base.OnTriggerStay(other);
        }

        protected override void OnTriggerExit(Collider other)
        {
            if (!photonView.IsMine) return;

            base.OnTriggerExit(other);
        }

        #endregion

        #region Public

        //--------------------------- State Management ---------------------------

        /// <summary>
        /// Invokes a state RPC method by name.
        /// </summary>
        /// <remarks>
        /// Ensure the method has [StateRPC] attribute.
        /// </remarks>
        /// <param name="methodName">The name of the method to invoke. This method should have the [StateRPC] attribute.</param>
        /// /// <param name="rpcTarget">The target on which the RPC method is to be invoked.</param>
        public void StateRPC(string methodName, RpcTarget rpcTarget, params object[] parameters)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                int methodIndex = GetStateRPCMethodIndexByName(methodName);

                if (methodIndex != -1)
                    photonView.RPC(nameof(RPC_InvokeStateMethod), rpcTarget, methodIndex, parameters);
                else
                    Debug.LogError($"Method not found: {methodName}");
            }
            else
            {
                MethodInfo methodToInvoke = GetStateRPCMethodByName(methodName);
                if (currentState == null || methodToInvoke.DeclaringType != currentState.GetType()) return;

                try { methodToInvoke?.Invoke(currentState, parameters); }
                catch (Exception ex) { Debug.LogError($"Error invoking method: {methodToInvoke.Name}, Error: {ex.Message}"); }
            }

        }

        /// <summary>
        /// Queues a new state to be set on the state machine and synchronizes this change across the network.
        /// </summary>
        /// <param name="stateKey">The new state to queue.</param>
        /// <param name="savePrevious">Indicates whether to save the current state before changing to the new state.</param>
        public override void QueueNewState(EState stateKey, bool savePrevious = true)
        {
            QueueNewState(stateKey, savePrevious, RpcTarget.All);
        }

        /// <summary>
        /// Queues a new state to be set on the state machine and allows specifying the network targets for synchronization.
        /// </summary>
        /// <param name="stateKey">The new state to queue.</param>
        /// <param name="savePrevious">Indicates whether to save the current state before changing to the new state.</param>
        /// <param name="rpcTarget">The network targets to synchronize the state change.</param>
        public virtual void QueueNewState(EState stateKey, bool savePrevious, RpcTarget rpcTarget = RpcTarget.All)
        {
            if (CheckOnlineStatus())
            {
                if (photonView.IsMine)
                    photonView.RPC(nameof(RPC_QueueNewState), rpcTarget, ConvertStateToByte(stateKey), savePrevious);
            }
            else base.QueueNewState(stateKey, savePrevious);
        }


        /// <summary>
        /// Immediately changes the state of the state machine and synchronizes this change across the network.
        /// </summary>
        /// <param name="stateKey">The new state to set.</param>
        public override void ForceChangeState(EState stateKey)
        {
            ForceChangeState(stateKey, RpcTarget.All);
        }

        /// <summary>
        /// Immediately changes the state of the state machine and allows specifying the network targets for synchronization.
        /// </summary>
        /// <param name="stateKey">The new state to set.</param>
        /// <param name="rpcTarget">The network targets to synchronize the state change.</param>
        public virtual void ForceChangeState(EState stateKey, RpcTarget rpcTarget = RpcTarget.All)
        {
            if (CheckOnlineStatus())
            {
                if (photonView.IsMine)
                    photonView.RPC(nameof(RPC_ForceChangeState), rpcTarget, ConvertStateToByte(stateKey));
            }
            else base.ForceChangeState(stateKey);
        }

        //----------------------------------------------------------------------------

        //--------------------------- Ownership Management ---------------------------

        /// <summary>
        /// Adds a callback target for state ownership events.
        /// </summary>
        /// <param name="stateOwnershipCallback">The callback target to add.</param>
        public void AddCallbackTarget(INetworkedStateOwnershipCallbacks<EState> stateOwnershipCallback)
        {
            if (stateOwnershipCallback == null) return;

            OnOwnershipRequestEv += stateOwnershipCallback.OnOwnershipRequest;
            OnOwnershipTransferedEv += stateOwnershipCallback.OnOwnershipTransfered;
            OnOwnershipTransferFailedEv += stateOwnershipCallback.OnOwnershipTransferFailed;
        }

        /// <summary>
        /// Removes a callback target for state ownership events.
        /// </summary>
        /// <param name="stateOwnershipCallback">The callback target to remove.</param>
        public void RemoveCallbackTarget(INetworkedStateOwnershipCallbacks<EState> stateOwnershipCallback)
        {
            if (stateOwnershipCallback == null) return;

            OnOwnershipRequestEv -= stateOwnershipCallback.OnOwnershipRequest;
            OnOwnershipTransferedEv -= stateOwnershipCallback.OnOwnershipTransfered;
            OnOwnershipTransferFailedEv -= stateOwnershipCallback.OnOwnershipTransferFailed;
        }

        /// <summary>
        /// Requests ownership of a StateView.
        /// </summary>
        /// <param name="viewId">The ID of the StateView.</param>
        /// <param name="requesterId">The ID of the requester.</param>
        public virtual void RequestOwnership(int viewId, int requesterId)
        {
            StateView<EState> requestedView = GetStateView(viewId);

            switch (requestedView.OwnershipTransfer)
            {
                case OwnershipOption.Takeover:
                    int currentPvOwnerId = requestedView.OwnerActorNr;
                    if (viewId == currentPvOwnerId || (viewId == 0 && currentPvOwnerId == photonView.Owner.ActorNumber) || currentPvOwnerId == 0)
                        photonView.RPC(nameof(RPC_TransferOwnership), RpcTarget.AllBuffered, viewId, requesterId);
                    else
                        photonView.RPC(nameof(RPC_OwnershipEventTrigger), RpcTarget.AllBuffered, GetOwnershipEventId(OnOwnershipTransferFailedEv), viewId, requesterId);
                    break;

                case OwnershipOption.Request:
                    photonView.RPC(nameof(RPC_OwnershipEventTrigger), RpcTarget.AllBuffered, GetOwnershipEventId(OnOwnershipRequestEv), viewId, requesterId);
                    break;

                default:
                    Debug.LogWarning("Ownership mode == " + (requestedView.OwnershipTransfer) + ". Ignoring request.");
                    break;
            }
        }

        /// <summary>
        /// Transfers ownership of a StateView to a specified player.
        /// </summary>
        /// <param name="viewId">The view ID of the StateView.</param>
        /// <param name="playerID">The ID of the player to transfer ownership to.</param>
        public virtual void TransferOwnership(int viewId, int playerID)
        {
            photonView.RPC(nameof(RPC_TransferOwnership), RpcTarget.AllBuffered, viewId, playerID);
        }

        // TODO: The demand and registration logic isn't perfect and could be improved with a smarter approach.
        // Since this is a NetworkedStateMachine, all states work simultaneously across clients without issues.
        // However, problems may arise if a player disconnects and reconnects,
        // or if the StateMachine initialization is delayed for some clients. Consider improving these areas.

        /// <summary>
        /// Requests a StateView to be registered on LocalClient.
        /// </summary>
        /// <param name="uniqueId">The unique ID for the StateView.</param>
        public void DemandStateView(int uniqueId)
        {
            if (!PhotonNetwork.IsConnected)
                return;

            foreach (var obj in objectToStateViewMap.Keys)
                if (GetUniqueID(obj) == uniqueId) return; // You may want to invoke HookEvent, why any client demands StateView but it is already exist ? Investigate if happens.

            photonView.RPC(nameof(RPC_RespondToStateViewDemand), photonView.Owner, uniqueId, photonView.Controller.ActorNumber);
        }

        /// <summary>
        /// Cleans up a StateView locally by removing it from the state views dictionary.
        /// </summary>
        /// <param name="stateView">The StateView to be cleaned up.</param>
        public virtual void LocalCleanStateView(StateView<EState> stateView)
        {
            if (!stateViews.ContainsKey(stateView.ViewID)) return;

            stateViews.Remove(stateView.ViewID);
        }

        /// <summary>
        /// Retrieves a StateView by its view ID.
        /// </summary>
        /// <param name="viewId">The view ID of the StateView.</param>
        /// <returns>The StateView with the specified view ID, or null if not found.</returns>
        public virtual StateView<EState> GetStateView(int viewId)
        {
            StateView<EState> result = null;
            stateViews.TryGetValue(viewId, out result);
            return result;
        }

        /// <summary>
        /// Gets a unique ID for a GameObject based on its hierarchy path.
        /// </summary>
        /// <param name="gameObject">The GameObject to get the unique ID for.</param>
        /// <returns>The unique ID for the GameObject.</returns>
        public int GetUniqueID(GameObject gameObject)
        {
            string path = gameObject.name;
            Transform current = gameObject.transform;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(path));
                return BitConverter.ToInt32(hash, 0);
            }
        }

        /// <summary>
        /// Checks if the provided view reference belongs to the local client.
        /// </summary>
        /// <param name="viewReferance">The reference to the view object to check ownership for.</param>
        /// <returns>True if the view reference belongs to the local client, otherwise false.</returns>
        /// <remarks>
        /// If the client is not connected to PhotonNetwork or not in a room, the method returns true by default.
        /// If the view reference is not found in the object-to-state-view map, the method returns false.
        /// </remarks>
        public virtual bool IsMine(GameObject viewReferance)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return true;

            if (!objectToStateViewMap.ContainsKey(viewReferance)) return false;

            return objectToStateViewMap[viewReferance].StateView.IsMine;
        }

        //----------------------------------------------------------------------------

        #endregion

        #region Protected

        //----------------------------- State Management -----------------------------

        /// <summary>
        /// Transitions to a new state and synchronizes this change across the network.
        /// </summary>
        protected override void TransitionToState()
        {
            if (CheckOnlineStatus())
            {
                if(photonView.IsMine)
                    photonView.RPC(nameof(RPC_TransitionToState), RpcTarget.All);
            }
            else base.TransitionToState();
        }

        //----------------------------------------------------------------------------

        //--------------------------- Ownership Management ---------------------------

        /// <summary>
        /// Retrieves an ownership event action based on the event ID.
        /// </summary>
        /// <param name="eventId">The ID of the ownership event.</param>
        /// <returns>The corresponding ownership event action.</returns>
        protected virtual Action<StateView<EState>, Player> GetOwnershipEvent(byte eventId)
        {
            switch (eventId)
            {
                case 0:
                    return OnOwnershipRequestEv;
                case 1:
                    return OnOwnershipTransferedEv;
                case 2:
                    return OnOwnershipTransferFailedEv;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Retrieves the event ID for a given ownership event action.
        /// </summary>
        /// <param name="ownershipEvent">The ownership event action.</param>
        /// <returns>The event ID corresponding to the ownership event action.</returns>
        protected virtual byte GetOwnershipEventId(Action<StateView<EState>, Player> ownershipEvent)
        {
            if (ownershipEvent == OnOwnershipRequestEv)
                return 0;
            if (ownershipEvent == OnOwnershipTransferedEv)
                return 1;
            if (ownershipEvent == OnOwnershipTransferFailedEv)
                return 2;

            return 255;
        }

        //----------------------------------------------------------------------------

        #endregion

        #region RPCs

        // IMPORTANT REMARK:
        // Due to a limitation in PhotonNetwork's PunRPC method discovery mechanism, methods already declared in this base class with the [PunRPC] attribute can not be detected if they are not also declared in the subclass.
        // Photon is unable to find [PunRPC] attributed methods in the base class alone. Therefore, it's crucial to override these methods in any subclass derived from this base class and include the [PunRPC] attribute in those overrides.
        
        //----------------------------- State Management -----------------------------
        [PunRPC]
        protected virtual void RPC_QueueNewState(byte stateKeyByte, bool savePrevious)
        {
            base.QueueNewState(ConvertByteToState(stateKeyByte), savePrevious);
        }

        [PunRPC]
        protected virtual void RPC_ForceChangeState(byte stateKeyByte)
        {
            base.ForceChangeState(ConvertByteToState(stateKeyByte));
        }

        [PunRPC]
        protected virtual void RPC_TransitionToState()
        {
            base.TransitionToState();
        }

        [PunRPC]
        protected virtual void RPC_InvokeStateMethod(int methodIndex, params object[] parameters)
        {
            MethodInfo methodToInvoke = GetStateRPCMethodByIndex(methodIndex);

            // This system is designed with the consideration that sometimes active states might differ across clients, even this is a NetworkedStateMachine.
            // Therefore, if the current state doesn't match the state where the method belongs, we don't invoke the method.
            // However, if there is a scenario where you need to invoke an RPC event even though the active state is different, consider improving the architecture.
            if (currentState == null || methodToInvoke.DeclaringType != currentState.GetType()) return;

            //Debug.Log($"Invoking method: {methodToInvoke.Name} with parameters: {string.Join(", ", parameters.Select(p => p?.ToString() ?? "null"))}");

            methodToInvoke?.Invoke(currentState, parameters);
        }


        //----------------------------------------------------------------------------

        //--------------------------- Ownership Management ---------------------------

        [PunRPC]
        protected virtual void RPC_RespondToStateViewDemand(int uniqueId, int ownerId, PhotonMessageInfo info)
        {
            NetworkedStateObject<EState> stateObject = GetStateObject(uniqueId);

            if (stateObject != null)
                photonView.RPC(nameof(RPC_RegisterStateView), info.Sender, uniqueId, stateObject.StateView.ViewID, stateObject.StateView.Owner.ActorNumber);
            else
            {
                RPC_RegisterStateView(uniqueId, GetViewID(), photonView.Controller.ActorNumber);
                NetworkedStateObject<EState> newStateObject = GetStateObject(uniqueId);
                photonView.RPC(nameof(RPC_RegisterStateView), info.Sender, uniqueId, newStateObject.StateView.ViewID, newStateObject.StateView.Owner.ActorNumber);
            }
        }

        [PunRPC]
        protected virtual void RPC_RegisterStateView(int uniqueId, int viewId, int ownerId)
        {
            if (GetStateObject(uniqueId) != null) return;

            StateView<EState> stateView = new StateView<EState>(this);
            stateView.ViewID = viewId;
            stateViews.Add(viewId, stateView);
            stateView.OwnerActorNr = ownerId;
            stateView.ControllerActorNr = ownerId;
            OnHookStateReady?.Invoke(uniqueId, stateView);
        }


        [PunRPC]
        protected virtual void RPC_OwnershipEventTrigger(byte eventId, int viewId, int requesterId)
        {
            StateView<EState> stateView = GetStateView(viewId);
            Player newPlayer = PhotonNetwork.CurrentRoom.GetPlayer(requesterId);
            Player prevOwner = stateView.Owner;

            Action<StateView<EState>, Player> ownershipEvent = GetOwnershipEvent(eventId);
            if (ownershipEvent != null)
                ownershipEvent.Invoke(stateView, newPlayer);
        }


        [PunRPC]
        protected virtual void RPC_TransferOwnership(int viewId, int newOwnerId, PhotonMessageInfo info)
        {
            Player sender = info.Sender;
            Player newPlayer = PhotonNetwork.CurrentRoom.GetPlayer(newOwnerId);
            StateView<EState> requestedView = GetStateView(viewId);

            if (requestedView.OwnershipTransfer == OwnershipOption.Takeover ||
               (requestedView.OwnershipTransfer == OwnershipOption.Request && (sender == requestedView.Controller || sender == requestedView.Owner)))
            {
                Player prevOwner = requestedView.Owner;

                requestedView.OwnerActorNr = newOwnerId;
                requestedView.ControllerActorNr = newOwnerId;

                if (OnOwnershipTransferedEv != null)
                    OnOwnershipTransferedEv(requestedView, prevOwner);
            }
        }

        //----------------------------------------------------------------------------

        #endregion

        #region Private

        bool CheckOnlineStatus()
        {
            return photonView && PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        }

        void CollectStateRPCs()
        {
            stateRPCMethods.Clear();

            try
            {
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
                    if (type.IsClass && type.IsSubclassOf(typeof(BaseState<EState>)))
                        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            if (method.GetCustomAttribute(typeof(StateRPC)) != null)
                                stateRPCMethods.Add(method);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error collecting StateRPC methods: {ex.Message}");
            }
        }

        byte ConvertStateToByte(EState state)
        {
            try { return Convert.ToByte(state); }
            catch (OverflowException)
            {
                Debug.LogError("Enum value is too large to convert to byte. Ensure the EState enum is derived from 'byte' (e.g., 'public enum EState : byte { ... }')");
                return 0;
            }
        }

        EState ConvertByteToState(byte stateKeyByte)
        {
            try { return (EState)Enum.ToObject(typeof(EState), stateKeyByte); }
            catch (ArgumentException)
            {
                Debug.LogError($"Invalid byte value received: {stateKeyByte}. Cannot convert to {typeof(EState)}");
                return default;
            }
        }

        int GetStateRPCMethodIndexByName(string methodName)
        {
            var method = stateRPCMethods.FirstOrDefault(m => m.Name == methodName);
            return method != null ? stateRPCMethods.IndexOf(method) : -1;
        }

        MethodInfo GetStateRPCMethodByIndex(int index)
        {
            if (index >= 0 && index < stateRPCMethods.Count)
                return stateRPCMethods[index];
            else
            {
                Debug.LogError($"Invalid method index: {index}");
                return null;
            }
        }

        MethodInfo GetStateRPCMethodByName(string methodName)
        {
            return stateRPCMethods.FirstOrDefault(m => m.Name == methodName);
        }

        int GetViewID()
        {
            if (stateViews.Count == 0) return 1;

            var keys = stateViews.Keys.ToList();
            keys.Sort();

            int lowestAvailable = 1;
            foreach (int key in keys)
            {
                if (key == lowestAvailable)
                    lowestAvailable++;
                else
                    break;
            }

            return lowestAvailable;
        }

        NetworkedStateObject<EState> GetStateObject(int uniqueId)
        {
            foreach (var obj in objectToStateViewMap.Keys)
                if (GetUniqueID(obj) == uniqueId)
                    return objectToStateViewMap[obj];

            return null;
        }
        #endregion
    }
}

/// <summary>
/// Attribute to mark methods that are remote procedure calls (RPCs) related to state changes in NetworkedStateMachine.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class StateRPC : Attribute
{
    // You can add additional properties or methods here if needed
}

/// <summary>
/// Options to define how Ownership Transfer is handled per StateView.
/// </summary>
/// <remarks>
/// This setting affects how RequestOwnership and TransferOwnership work at runtime.
/// </remarks>
public enum OwnershipOption
{
    /// <summary>
    /// Ownership is fixed. Instantiated objects stick with their creator, room objects always belong to the Master Client.
    /// </summary>
    Fixed,
    /// <summary>
    /// Ownership can be taken away from the current owner who can't object.
    /// </summary>
    Takeover,
    /// <summary>
    /// Ownership can be requested with PhotonView.RequestOwnership but the current owner has to agree to give up ownership.
    /// </summary>
    /// <remarks>The current owner has to implement IPunCallbacks.OnOwnershipRequest to react to the ownership request.</remarks>
    Request
}