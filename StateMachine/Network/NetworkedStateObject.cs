using Photon.Pun;
using Photon.Realtime;
using Sirenix.OdinInspector;
using SOLA.Photon;
using System;
using UnityEngine;

namespace AEB.StateMachine
{
    public class NetworkedStateObject<EState> : MonoBehaviour, INetworked where EState : Enum
    {
        public StateView<EState> StateView { get; private set; }

        public virtual NetworkedStateObject<EState> Construct(StateView<EState> stateView) {
            PhotonView = stateView.stateMachine.View;
            LastRequestTime = PhotonNetwork.Time;
            StateView = stateView;

            NetworkedObjectsManager.Instance.CacheMe(gameObject);
            return this;
        }

        #region Properties

        /// <summary>
        /// Gets the PhotonView component of StateMachine system.
        /// </summary>
        public PhotonView PhotonView { get; set; }

        /// <summary>
        /// Determines if ownership transfer is allowed based on the current state.
        /// </summary>
        public bool IsOwnershipTransferable { get => StateView.AmOwner || StateView.Owner == null; }

        /// <summary>
        /// Determines if ownership request is allowed based on the current state.
        /// </summary>
        public bool IsOwnershipRequestable { get => !StateView.AmOwner && PhotonNetwork.Time - LastRequestTime > 3f; }

        /// <summary>
        /// Gets or sets the last time ownership was requested.
        /// </summary>
        public double LastRequestTime { get; set; }

        /// <summary>
        /// Who currently demands the ownership.
        /// </summary>
        public Player Demander { get; set; }

#if UNITY_EDITOR
        [ReadOnly, ShowInInspector]
        public bool IsMine => StateView == null ? false : StateView.IsMine;

        [ReadOnly, ShowInInspector]
        public string Controller => (StateView == null || StateView.Controller == null) ? "null" : StateView.Controller.NickName.ToString();

        [ReadOnly, ShowInInspector]
        public string Owner => (StateView == null || StateView.Owner == null) ? "null" : StateView.Owner.NickName.ToString();
#endif

        #endregion

        #region Public

        /// <summary>
        /// Requests ownership of the object.
        /// </summary>
        public void RequestOwnership()
        {
            if (StateView.Owner == null || !StateView.IsMine)
                if (StateView.OwnershipTransfer == OwnershipOption.Request)
                    StateView.RequestOwnership();
                else
                    StateView.TransferOwnership(PhotonNetwork.LocalPlayer);
            LastRequestTime = PhotonNetwork.Time;
        }

        /// <summary>
        /// Transfers ownership of the object to a specified player.
        /// </summary>
        /// <param name="playerID">The ID of the player to transfer ownership to.</param>
        public void TransferOwnership(int playerID)
        {
            StateView.TransferOwnership(playerID);
        }

        #endregion
    }
}
