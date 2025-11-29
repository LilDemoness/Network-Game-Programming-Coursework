using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityServices.Sessions;
using Utils;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     ConnectionMethod contains all the setup needed to setup Netcode for GameObjects to be ready to start a connection, either host or client side.<br/>
    ///     Override this abstract class to add a new transport or way of connecting.
    /// </summary>
    public abstract class ConnectionMethodBase
    {
        protected ConnectionManager ConnectionManager;
        private readonly ProfileManager _profileManager;
        protected readonly string PlayerName;


        /// <summary>
        ///     Setup the host connection prior to starting the NetworkManager.
        /// </summary>
        public abstract void SetupHostConnection();

        /// <summary>
        ///     Setup the client connection prior to starting the NetworkManager.
        /// </summary>
        public abstract void SetupClientConnection();


        /// <summary>
        ///     Setup the client for reconnection prior to reconnecting.
        /// </summary>
        /// <returns>
        ///     Success: True if succeeded in setting up a reconnection, false if we failed.
        ///     ShouldTryAgain: True if we should try again after failing, false if not.
        /// </returns>
        public abstract Task<(bool Success, bool ShouldTryAgain)> SetupClientReconnectionAsync();


        public ConnectionMethodBase(ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
        {
            this.ConnectionManager = connectionManager;
            this._profileManager = profileManager;
            this.PlayerName = playerName;
        }

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            string payload = JsonUtility.ToJson(new ConnectionPayload
            {
                PlayerId = playerId,
                PlayerName = playerName,
                IsDebug = Debug.isDebugBuild,
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }


        // Using authentication, this makes sure your session is associated with our account and not your device.
        // This means that you could reconnect from a different device for example.
        // A playerId is also a bit more permanent than player prefs. In a browser, for example, player prefs can be cleared as easily as cookies.
        // The forked flow here is for debug purposes and to make UGS optional, allowing you to connect without having a UGS account.
        // It is recommended to investgiate our own initialisation and IsSigned flows to see if we need to do those checks on our own.
        // The option of offline access is presented for debug purposes, but we may want to show an error popup and ask our player to connect to the internet.
        protected string GetPlayerId()
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + _profileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + _profileManager.Profile;
        }
    }


    /// <summary>
    ///     Simple IP connection setup with UTP.
    /// </summary>
    public class ConnectionMethodIP : ConnectionMethodBase
    {
        private string _ipAddress;
        private ushort _port;


        public ConnectionMethodIP(string ip, ushort port, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            _ipAddress = ip;
            _port = port;
            //ConnectionManager = connectionManager;
        }


        public override void SetupClientConnection()
        {
            SetConnectionPayload(GetPlayerId(), PlayerName);
            UnityTransport unityTransport = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            unityTransport.SetConnectionData(_ipAddress, _port);
        }

        public override Task<(bool Success, bool ShouldTryAgain)> SetupClientReconnectionAsync()
        {
            // Nothing to do here.
            return Task.FromResult((true, true));
        }

        public override void SetupHostConnection()
        {
            SetConnectionPayload(GetPlayerId(), PlayerName);    // Need to set connection payload for the host as well, as the host is a client too.
            UnityTransport unityTransport = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            unityTransport.SetConnectionData(_ipAddress, _port);
        }
    }

    /// <summary>
    ///     UTP's Relay connection setup using the Session integration.
    /// </summary>
    public class ConnectionMethodRelay : ConnectionMethodBase
    {
        MultiplayerServicesFacade _multiplayerServicesFacade;


        public ConnectionMethodRelay(MultiplayerServicesFacade multiplayerServicesFacade, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            _multiplayerServicesFacade = multiplayerServicesFacade;
            //ConnectionManager = connectionManager;
        }

        public override void SetupClientConnection()
        {
            SetConnectionPayload(GetPlayerId(), PlayerName);
        }

        public override async Task<(bool Success, bool ShouldTryAgain)> SetupClientReconnectionAsync()
        {
            if (_multiplayerServicesFacade.CurrentUnitySession == null)
            {
                Debug.Log("Session does not exist anymore. Stopping reconnection attempts.");
                return (false, false);
            }

            // When using Session with Relay, if a user is disconnected from the Relay server, the server will notify the Session service
            //  and mark the user as disconnected, but will not remove them from the Session. They then have some time to attempt to
            //  reconnect (Defined by the "Disconnect removal time" parameter on the dashboard), after which they will be removed from the Session completely.
            // See 'https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/join-session#Reconnect_to_a_session'.
            ISession session = await _multiplayerServicesFacade.ReconnectToSessionAsync();
            bool success = session != null;

            Debug.Log(success ? "Successfully reconnected to Session." : "Failed to reconnect to Session.");
            return (success, true);
        }

        public override void SetupHostConnection()
        {
            Debug.Log("Setting up Unity Relay host");
            SetConnectionPayload(GetPlayerId(), PlayerName);    // Need to set connection payload for the host as well, as the host is a client too.
        }
    }
}