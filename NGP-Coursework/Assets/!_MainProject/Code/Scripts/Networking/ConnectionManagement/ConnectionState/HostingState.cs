using Infrastructure;
using SceneLoading;
using Unity.Netcode;
using UnityEngine;
using UnityServices.Sessions;
using VContainer;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     A connection stats corresponding to a listending host.<br/>
    ///     Handles incoming client connections.<br/>
    ///     When shtting down or being timed out, transitions to the <see cref="OfflineState"/>.
    /// </summary>
    public class HostingState : OnlineState
    {
        [Inject]
        private MultiplayerServicesFacade _multiplayerServicesFacade;
        [Inject]
        private IPublisher<ConnectionEventMessage> _connectionEventPublisher;

        // Used in ApprovalCheck. This is intended as a light bit of protection against DOS attacks that rely on sending large buffers of garbage.
        private const int MAX_CONNETION_PAYLOAD = 1024;


        public override void Enter()
        {
            SceneLoader.Instance.LoadNonGameplayScene(SceneLoader.NonGameplayScene.Lobby, true);

            if (_multiplayerServicesFacade.CurrentUnitySession != null)
            {
                _multiplayerServicesFacade.BeginTracking();
            }
        }
        public override void Exit()
        {
            SessionManager<SessionPlayerData>.Instance.OnServerEnded();
        }


        public override void OnClientConnected(ulong clientId)
        {
            SessionPlayerData? playerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
            if (playerData.HasValue)
            {
                // Successful connection and SessionData setup.
                _connectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.Success, PlayerName = playerData.Value.PlayerName });
            }
            else
            {
                // This shouldn't occur since Player Data is assigned during connection approval, so we should be notified if this occurs.
                Debug.LogError($"No player data associated with the client {clientId}");
                string reason = JsonUtility.ToJson(ConnectStatus.GenericDisconnect);
                ConnectionManager.NetworkManager.DisconnectClient(clientId, reason);
            }
        }
        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == ConnectionManager.NetworkManager.LocalClientId)
                return;

            string playerId = SessionManager<SessionPlayerData>.Instance.GetPlayerId(clientId);
            if (playerId != null)
            {
                // Notify for handling of player disconnect.
                SessionPlayerData? sessionData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(playerId);
                if (sessionData.HasValue)
                {
                    _connectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.GenericDisconnect, PlayerName = sessionData.Value.PlayerName });
                }
                SessionManager<SessionPlayerData>.Instance.DisconnectClient(clientId);
            }
        }


        public override void OnUserRequestedShutdown()
        {
            // Cache our disconnection reason.
            string reason = JsonUtility.ToJson(ConnectStatus.HostEndedSession);

            // Disconnect all clients.
            for(int i = ConnectionManager.NetworkManager.ConnectedClientsIds.Count - 1; i >= 0; --i)
            {
                ulong id = ConnectionManager.NetworkManager.ConnectedClientsIds[i];
                if (id != ConnectionManager.NetworkManager.LocalClientId)
                {
                    // Not the host. Disconnect this client.
                    ConnectionManager.NetworkManager.DisconnectClient(id, reason);
                }
            }

            // Go offline.
            ConnectionManager.ChangeState(ConnectionManager.Offline);
        }

        public override void OnServerStopped()
        {
            ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
            ConnectionManager.ChangeState(ConnectionManager.Offline);
        }


        /// <summary>
        ///     This logic plugs into the <see cref="NetworkManager.ConnectionApprovalResponse"/> and runs every time a client connects to us.<br/>
        ///     The complementary logic that runs when the client starts its connection can be found in <see cref="ClientConnectingState"/>.
        /// </summary>
        /// <remarks>
        ///     See: 'https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs' for further implementation suggestions.
        /// </remarks>
        /// <param name="request"> The initial request contains, among other things, binary data passed into StartClient. In this case, this is the client's GUID, which is a unique identifier for their install of the game that persists across app restarts.</param>
        /// <param name="response"> Our response to the approval process. In the case of connection refusal with a custom return message, we delay using the 'Pending' field.</param>
        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            byte[] connectionData = request.Payload;
            ulong clientId = request.ClientNetworkId;

            if (connectionData.Length > MAX_CONNETION_PAYLOAD)
            {
                // If connectioNData is too large, deny immediately to avoid wasting time on the server.
                //  This is intended as a light bit of protection against DOS attacks that rely on sending large buffers of garbage.
                response.Approved = false;
                return;
            }


            string payload = System.Text.Encoding.UTF8.GetString(connectionData);
            ConnectionPayload connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);
            ConnectStatus gameReturnStatus = GetConnectStatus(connectionPayload);

            if (gameReturnStatus == ConnectStatus.Success)
            {
                // Create our session data.
                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.PlayerId, new SessionPlayerData(clientId, connectionPayload.PlayerName, isConnected: true));

                // Connection approval will create our player object for us.
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Position = Vector3.zero;
                response.Rotation = Quaternion.identity;
                return;
            }

            // Approval failed.
            response.Approved = false;
            response.Reason = JsonUtility.ToJson(gameReturnStatus);
            if (_multiplayerServicesFacade.CurrentUnitySession != null)
            {
                _multiplayerServicesFacade.RemovePlayerFromSessionAsync(connectionPayload.PlayerId);
            }
        }

        private ConnectStatus GetConnectStatus(ConnectionPayload connectionPayload)
        {
            if (ConnectionManager.NetworkManager.ConnectedClientsIds.Count >= ConnectionManager.MaxConnectedPlayers)
            {
                return ConnectStatus.ServerFull;
            }

            if (connectionPayload.IsDebug != Debug.isDebugBuild)
            {
                return ConnectStatus.IncompatibleBuildType;
            }

            return SessionManager<SessionPlayerData>.Instance.IsDuplicateConnection(connectionPayload.PlayerId) ? ConnectStatus.DuplicateLoginAttempt : ConnectStatus.Success;
        }
    }
}