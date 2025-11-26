using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VContainer;

namespace Netcode.ConnectionManagement
{
    public enum ConnectStatus
    {
        Undefined = 0,
        Success,                    // Client Logged in Successfully. May also be a successful reconnect.
        ServerFull,                 // Can't join as the server is at capacity.
        DuplicateLoginAttempt,      // Logged in on another client, so we cannot connect with this one.
        UserRequestedDisconnect,    // Intentional disconnect triggered by the user.
        GenericDisconnect,          // Server disconnected with no specific reason given.
        Reconnecting,               // Client lost connection and is attempting to reconnect.
        IncompatibleBuildType,      // The client's build type is incompatible with the server.
        HostEndedSession,           // The Host intentionally ended the session.
        StartHostFailed,            // Server failed to bind.
        StartClientFailed,          // Failed to connect to server and/or invalid network endpoint.
    }


    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            this.CurrentAttempt = currentAttempt;
            this.MaxAttempt = maxAttempt;
        }
    }

    public struct ConnectionEventMessage : INetworkSerializeByMemcpy
    {
        public ConnectStatus ConnectStatus;
        public FixedPlayerName PlayerName;
    }

    [System.Serializable]
    public class ConnectionPayload
    {
        public string PlayerId;
        public string PlayerName;
        public bool IsDebug;
    }


    /// <summary>
    ///     This State Machine handles connection through the NetworkManager.<br/>
    ///     It is responsible for listening to the NetworkManager callbacks and other outsdie calls and redirecting them to the current ConnectionState object.
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        private ConnectionState _currentState;

        [Inject]
        private NetworkManager _networkManager;
        public NetworkManager NetworkManager => _networkManager;


        [SerializeField] private int _maxReconnectAttempts = 2;
        public int MaxReconnectAttempts => _maxReconnectAttempts;


        [Inject]
        private IObjectResolver _resolver;

        public int MaxConnectedPlayers = 8;

        internal readonly OfflineState Offline = new OfflineState();
        internal readonly ClientConnectingState ClientConnecting = new ClientConnectingState();
        internal readonly ClientConnectedState ClientConnected = new ClientConnectedState();
        internal readonly ClientReconnectingState ClientReconnecting = new ClientReconnectingState();
        internal readonly StartingHostState StartingHost = new StartingHostState();
        internal readonly HostingState Hosting = new HostingState();


        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            List<ConnectionState> states = new() { Offline, ClientConnecting, ClientConnected, ClientReconnecting, StartingHost, Hosting };
            foreach(var connectionState in states)
            {
                _resolver.Inject(connectionState);
            }

            _currentState = Offline;

            // Subscribe to NetworkManager events.
            NetworkManager.OnConnectionEvent += OnConnectionEvent;
            NetworkManager.OnServerStarted += OnServerStarted;
            NetworkManager.OnServerStopped += OnServerStopped;
            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
        }
        private void OnDestroy()
        {
            // Unsubscribe to NetworkManager events.
            NetworkManager.OnConnectionEvent -= OnConnectionEvent;
            NetworkManager.OnServerStarted -= OnServerStarted;
            NetworkManager.OnServerStopped -= OnServerStopped;
            NetworkManager.OnTransportFailure -= OnTransportFailure;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck;
        }


        internal void ChangeState(ConnectionState nextState)
        {
            Debug.Log($"{name}: Changed connection state from {_currentState.GetType().Name} to {nextState.GetType().Name}.");

            if (_currentState != null)
            {
                _currentState.Exit();
            }
            _currentState = nextState;
            _currentState.Enter();
        }


        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            switch (connectionEventData.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    _currentState.OnClientConnected(connectionEventData.ClientId);
                    break;
                case ConnectionEvent.ClientDisconnected:
                    _currentState.OnClientDisconnect(connectionEventData.ClientId);
                    break;
            }
        }
        private void OnServerStarted() => _currentState.OnServerStarted();
        private void OnServerStopped(bool _) => _currentState.OnServerStopped();
        private void OnTransportFailure() => _currentState.OnTransportFailure();
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) => _currentState.ApprovalCheck(request, response);


        public void StartClientSession(string playerName) => _currentState.StartClientSession(playerName);
        public void StartClientIP(string playerName, string ipAddress, int port) => _currentState.StartClientIP(playerName, ipAddress, port);
        public void StartHostSession(string playerName) => _currentState.StartHostSession(playerName);
        public void StartHostIP(string playerName, string ipAddress, int port) => _currentState.StartHostIP(playerName, ipAddress, port);


        public void RequestShutdown() => _currentState.OnUserRequestedShutdown();
    }
}