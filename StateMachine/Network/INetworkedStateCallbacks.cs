using Photon.Realtime;

namespace AEB.StateMachine
{
    /// <summary>
    /// Global Callback interface for ownership changes. These callbacks will fire for changes to ANY StateView that changes.
    /// Consider using IOnPhotonViewControllerChange for callbacks from a specific StateView.
    /// </summary>
    public interface INetworkedStateOwnershipCallbacks<EState> where EState : System.Enum
    {
        /// <summary>
        /// Called when another player requests ownership of a StateView. 
        /// This method is called on all clients, so you should check if (targetView.IsMine) or (targetView.Owner == PhotonNetwork.LocalPlayer) 
        /// to determine if a response such as targetView.TransferOwnership(requestingPlayer) should be given.
        /// </summary>
        /// <param name="targetView">The StateView for which ownership is being requested.</param>
        /// <param name="requestingPlayer">The player who is requesting ownership.</param>
        void OnOwnershipRequest(StateView<EState> targetView, Player requestingPlayer);

        /// <summary>
        /// Called when ownership of a StateView is transferred to another player.
        /// </summary>
        /// <param name="targetView">The StateView for which ownership has changed.</param>
        /// <param name="newOwner">The player who is the new owner.</param>
        /// <param name="previousOwner">The player who was the previous owner (or null, if there was no previous owner).</param>
        void OnOwnershipTransfered(StateView<EState> targetView, Player previousOwner);

        /// <summary>
        /// Called when an ownership request fails for objects with the "takeover" setting.
        /// </summary>
        /// <remarks>
        /// Each request asks to take ownership from a specific controlling player. This can fail if another player
        /// took over ownership briefly before the request arrived.
        /// </remarks>
        /// <param name="targetView">The StateView for which the ownership transfer failed.</param>
        /// <param name="senderOfFailedRequest">The player who sent the ownership request that failed.</param>
        void OnOwnershipTransferFailed(StateView<EState> targetView, Player senderOfFailedRequest);
    }
}
