using UnityEngine;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     A connection state corresponding to when a client is attempting to connect to a server.<br/>
    ///     Starts when the client is entering.<br/>
    ///     If successful, transitions to the <see cref="ClientConnectedState"/>, otherwise it transitions to the <see cref="OnlineState"/>.
    /// </summary>
    public class ClientConnectingState : OnlineState
    {
        protected ConnectionMethodBase ConnectionMethod;

        public ClientConnectingState Configure(ConnectionMethodBase baseConnectionMethod)
        {
            ConnectionMethod = baseConnectionMethod;
            return this;    // Fluent Interface.
        }

        public override void Enter()
        {
#pragma warning disable 4014
            ConnectClientAsync();
#pragma warning restore 4014
        }
        public override void Exit() { }

        public override void OnClientConnected(ulong _)
        {
            ConnectStatusPublisher.Publish(ConnectStatus.Success);
            ConnectionManager.ChangeState(ConnectionManager.ClientConnected);
        }
        public override void OnClientDisconnect(ulong _)
        {
            // Our client ID is always going to be ours for this.
            StartingClientFailed();
        }

        private void StartingClientFailed()
        {
            string disconnectReason = ConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                // Unknown failure reason.
                ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
            }
            else
            {
                // Known disconnect reason. Parse and publish.
                ConnectStatus connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                ConnectStatusPublisher.Publish(connectStatus);
            }

            ConnectionManager.ChangeState(ConnectionManager.Offline);
        }


        internal void ConnectClientAsync()
        {
            try
            {
                ConnectionMethod.SetupClientConnection();

                if (ConnectionMethod is ConnectionMethodIP)
                {
                    if (!ConnectionManager.NetworkManager.StartClient())
                    {
                        throw new System.Exception("NetworkManager StartClient failed");
                    }
                }
            }
            catch (System.Exception e)
            {
                // Log our exception and handle our failure.
                Debug.LogError("Error connecting client, see following exception");
                Debug.LogException(e);
                StartingClientFailed();
                throw;
            }
        }
    }
}