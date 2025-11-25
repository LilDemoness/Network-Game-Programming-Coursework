using UnityEngine;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     Base class representing an online connection state.
    /// </summary>
    public abstract class OnlineState : ConnectionState
    {
        public override void OnUserRequestedShutdown()
        {
            // This behaviour will be the same for every online state.
            ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            ConnectionManager.ChangeState(ConnectionManager.Offline);
        }

        public override void OnTransportFailure()
        {
            // This behaviour will be the same for every online state.
            ConnectionManager.ChangeState(ConnectionManager.Offline);
        }
    }
}