using UnityEngine;
using UnityServices.Sessions;
using VContainer;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     A Connection State corresponding to a connected client.<br/>
    ///     When being disconnected, transitions to the <see cref="ClientReconnectingState"/> if no reason is given, or to the <see cref="OfflineState"/> otherwise.
    /// </summary>
    public class ClientConnectedState : OnlineState
    {
        [Inject]
        protected MultiplayerServicesFacade MultiplayerServicesFacade;


        public override void Enter()
        {
            if (MultiplayerServicesFacade.CurrentUnitySession != null)
            {
                MultiplayerServicesFacade.BeginTracking();
            }
        }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            string disconnectReason = ConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason) || disconnectReason == "Disconnected due to host shutting down.")
            {
                // Disconnected due to an unknown reason OR an unexpected host shutdown. Attempt a reconnect.
                ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
                ConnectionManager.ChangeState(ConnectionManager.ClientReconnecting);
            }
            else
            {
                // Disconnected due to a known reason.
                ConnectStatus connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                ConnectStatusPublisher.Publish(connectStatus);
                ConnectionManager.ChangeState(ConnectionManager.Offline);
            }
        }
    }
}