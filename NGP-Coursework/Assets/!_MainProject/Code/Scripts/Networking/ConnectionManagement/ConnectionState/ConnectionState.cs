using Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     Base class representing a connection state.
    /// </summary>
    public abstract class ConnectionState
    {
        [Inject]
        protected ConnectionManager ConnectionManager;

        [Inject]
        protected IPublisher<ConnectStatus> ConnectStatusPublisher;

        public abstract void Enter();
        public abstract void Exit();

        public virtual void OnClientConnected(ulong clientId) { }
        public virtual void OnClientDisconnect(ulong clientId) { }

        public virtual void OnServerStarted() { }
        public virtual void OnServerStopped() { }

        public virtual void StartClientIP(string playerName, string ipAddress, int port) { }
        public virtual void StartClientSession(string playerName) { }

        public virtual void StartHostIP(string playerName, string ipAddress, int port) { }
        public virtual void StartHostSession(string playerName) { }

        public virtual void OnUserRequestedShutdown() { }

        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) { }

        public virtual void OnTransportFailure() { }
    }
}