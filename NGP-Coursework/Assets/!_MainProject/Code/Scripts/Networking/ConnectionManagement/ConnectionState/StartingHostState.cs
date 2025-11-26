using Unity.Netcode;
using UnityEngine;
using UnityServices.Sessions;
using VContainer;


namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     A connection state corresponding to a host starting up.<br/>
    ///     Starts the host when entering the state.<br/>
    ///     If successful, transitions to the <see cref="HostingState"/>, otherwise transitions back to the <see cref="OfflineState"/>.
    /// </summary>
    public class StartingHostState : OnlineState
    {
        [Inject]
        private MultiplayerServicesFacade _multiplayerServicesFacade;
        [Inject]
        private LocalSession _localSession;
        private ConnectionMethodBase _connectionMethod;


        public StartingHostState Configure(ConnectionMethodBase baseConnectionMethod)
        {
            this._connectionMethod = baseConnectionMethod;
            return this;    // Fluent interface.
        }


        public override void Enter()
        {
            StartHost();
        }
        public override void Exit() { }


        public override void OnServerStarted()
        {
            ConnectStatusPublisher.Publish(ConnectStatus.Success);
            ConnectionManager.ChangeState(ConnectionManager.Hosting);
        }

        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            byte[] connectionData = request.Payload;
            ulong clientId = request.ClientNetworkId;

            // This is called when starting as a host, before the end of the StartHost() call. In that case, we simply approve ourselves.
            if (clientId == ConnectionManager.NetworkManager.LocalClientId)
            {
                // Retrieve our Connection Payload.
                string payload = System.Text.Encoding.UTF8.GetString(connectionData);
                ConnectionPayload connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.PlayerId, new SessionPlayerData(clientId, connectionPayload.PlayerName, isConnected: true));

                // Connection approval will create a player object for us.
                response.Approved = true;
                response.CreatePlayerObject = true;
            }
        }

        public override void OnServerStopped()
        {
            StartHostFailed();
        }


        private void StartHost()
        {
            try
            {
                _connectionMethod.SetupHostConnection();

                if (_connectionMethod is ConnectionMethodIP)
                {
                    // NetworkManager's StartHost() will launch everything for us.
                    if (!ConnectionManager.NetworkManager.StartHost())
                    {
                        StartHostFailed();
                    }
                }
            }
            catch (System.Exception)
            {
                StartHostFailed();
                throw;
            }
        }
        private void StartHostFailed()
        {
            ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
            ConnectionManager.ChangeState(ConnectionManager.Offline);
        }
    }
}