using System;
using Photon.Pun;
using Photon.Realtime;

namespace AEB.StateMachine
{
    // TODO: This script's event system is currently not functioning.
    // The commented-out events need to be handled with a more complex structure that ensures they are fired for all clients.
    // If you need one of these events in the future, don't forget to implement this part.

    /// <summary>
    /// Represents a view for a state in a networked state machine.
    /// </summary>
    /// <typeparam name="EState">The type of the state enumeration.</typeparam>
    public class StateView<EState> where EState : Enum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateView{EState}"/> class.
        /// </summary>
        /// <param name="networkedStateMachine">The networked state machine associated with this view.</param>
        public StateView(NetworkedStateMachine<EState> networkedStateMachine)
        {
            stateMachine = networkedStateMachine;
        }

        #region Fields and Properties

        /// <summary>
        /// Gets the state machine associated with this view.
        /// </summary>
        public NetworkedStateMachine<EState> stateMachine { get; private set; }

        protected int viewId = 0;

        /// <summary>
        /// Gets or sets the view ID of this state view.
        /// </summary>
        public int ViewID
        {
            get => viewId;
            set => viewId = value;
        }

        /// <summary>
        /// Gets or sets the actor number of the owner.
        /// </summary>
        public int OwnerActorNr
        {
            get { return Owner.ActorNumber; }
            set
            {
                if (value != 0 && Owner?.ActorNumber == value)
                    return;

                Player prevOwner = Owner;
                Owner = PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.GetPlayer(value, true);
                //OnOwnerChange?.Invoke(prevOwner, Owner);
            }
        }

        /// <summary>
        /// Gets or sets the actor number of the controller.
        /// </summary>
        public int ControllerActorNr
        {
            get { return Controller.ActorNumber; }
            set
            {
                Player prevController = this.Controller;

                Controller = PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.GetPlayer(value, true);
                if (Controller != null && Controller.IsInactive)
                    Controller = PhotonNetwork.MasterClient;

                //OnControllerChange?.Invoke(prevController, Controller);
            }
        }

        /// <summary>
        /// Gets the owner of this state view.
        /// </summary>
        public Player Owner { get; private set; }

        /// <summary>
        /// Gets the controller of this state view.
        /// </summary>
        public Player Controller { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the local player is the controller.
        /// </summary>
        public bool IsMine => Controller != null && Controller.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

        /// <summary>
        /// Gets a value indicating whether the local player is the owner.
        /// </summary>
        public bool AmOwner => Owner != null && Owner.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

        /// <summary>
        /// Gets a value indicating whether the local player is the controller.
        /// </summary>
        public bool AmController => IsMine;

        /// <summary>
        /// Gets or sets the ownership transfer option for this state view.
        /// </summary>
        public OwnershipOption OwnershipTransfer = OwnershipOption.Takeover;

        #endregion

        #region Events

        //public event Action<Player, Player> OnOwnerChange;
        //public event Action<Player, Player> OnControllerChange;
        //public event Action<StateView<EState>, Player> OnOwnershipRequest;
        //public event Action<StateView<EState>> OnPreNetDestroy;

        #endregion

        #region Public

        /// <summary>
        /// Requests ownership of this state view for the local player.
        /// </summary>
        public void RequestOwnership()
        {
            if (OwnershipTransfer == OwnershipOption.Fixed) return;

            stateMachine.RequestOwnership(ViewID, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        /// <summary>
        /// Transfers ownership of this state view to the specified player.
        /// </summary>
        /// <param name="newOwner">The new owner of this state view.</param>
        public void TransferOwnership(Player newOwner)
        {
            if (newOwner == null) return;
            TransferOwnership(newOwner.ActorNumber);
        }

        /// <summary>
        /// Transfers ownership of this state view to the specified player by their actor number.
        /// </summary>
        /// <param name="newOwnerId">The actor number of the new owner.</param>
        public void TransferOwnership(int newOwnerId)
        {
            if (OwnershipTransfer == OwnershipOption.Takeover || (OwnershipTransfer == OwnershipOption.Request && AmController))
                stateMachine.TransferOwnership(ViewID, newOwnerId);
        }

        /// <summary>
        /// Destroys this state view and cleans it from the state machine.
        /// </summary>
        public void Destroy()
        {
            stateMachine.LocalCleanStateView(this);
            //OnPreNetDestroy?.Invoke(this);
        }

        #endregion
    }
}
