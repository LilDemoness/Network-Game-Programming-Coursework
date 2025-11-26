using System.Collections;
using Infrastructure;
using UnityEngine;
using VContainer;

namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     A connection state corresponding to a client attempting to reconnect to a server.</br>
    ///     It will try to reconnect a number of times defined by <see cref="ConnectionManager.MaxReconnectAttempts"/> property.<br/>
    ///     If it succeeds, it will transition to the <see cref="ClientConnectedState"/>, otherwise it will transition ot the <see cref="OfflineState"/>.<br/>
    ///     If given a disconnect reason first, depending on the reason given, we may not try to reconnect again and will instead transition directly ot the <see cref="OfflineState"/>.
    /// </summary>
    public class ClientReconnectingState : ClientConnectingState
    {
        [Inject]
        IPublisher<ReconnectMessage> ReconnectMessagePublisher;

        Coroutine _reconnectCoroutine;
        private int _reconnectionAttemptsMade;

        const float TIME_BEFORE_FIRST_ATTEMPT = 1.0f;
        const float TIME_BETWEEN_ATTEMPTS = 5.0f;


        public override void Enter()
        {
            _reconnectionAttemptsMade = 0;
            _reconnectCoroutine = ConnectionManager.StartCoroutine(ReconnectCoroutine());
        }
        public override void Exit()
        {
            // Stop reconnection attempts.
            if (_reconnectCoroutine != null)
            {
                ConnectionManager.StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }

            ReconnectMessagePublisher.Publish(new ReconnectMessage(ConnectionManager.MaxReconnectAttempts, ConnectionManager.MaxReconnectAttempts));
        }


        public override void OnClientConnected(ulong _)
        {
            ConnectionManager.ChangeState(ConnectionManager.ClientConnected);
        }
        public override void OnClientDisconnect(ulong _)
        {
            string disconnectReason = ConnectionManager.NetworkManager.DisconnectReason;
            if (_reconnectionAttemptsMade < ConnectionManager.MaxReconnectAttempts)
            {
                // We've made at least 1 reconnect attempt.
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    // No disconnection reason given.
                    _reconnectCoroutine = ConnectionManager.StartCoroutine(ReconnectCoroutine());
                }
                else
                {
                    // Disconnection reason given.
                    ConnectStatus connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                    ConnectStatusPublisher.Publish(connectStatus);
                    switch (connectStatus)
                    {
                        // Invalid connection attempts. Don't attempt to reconnect.
                        case ConnectStatus.UserRequestedDisconnect:
                        case ConnectStatus.HostEndedSession:
                        case ConnectStatus.ServerFull:
                        case ConnectStatus.IncompatibleBuildType:
                            ConnectionManager.ChangeState(ConnectionManager.Offline);
                            break;
                        // Valid connection attempt. Attempt to reconnect.
                        default:
                            _reconnectCoroutine = ConnectionManager.StartCoroutine(ReconnectCoroutine());
                            break;
                    }
                }
            }
            else
            {
                // We haven't made any reconnection attempts.
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    // No disconnection reason given.
                    ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
                }
                else
                {
                    // Disconnection reason given.
                    ConnectStatus connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                    ConnectStatusPublisher.Publish(connectStatus);
                }

                ConnectionManager.ChangeState(ConnectionManager.Offline);
            }
        }


        private IEnumerator ReconnectCoroutine()
        {
            // If we don't manage to connect on our first attempt, wait some time before trying again so that
            //  if the issue causing the disconnect is temporary, it has time to fix itself before we try again.
            // Here we are using a simple fixed cooldown, but we could use exponential backoff instead to wait
            //  longer between each failed attempt. See 'https://en.wikipedia.org/wiki/Exponential_backoff'
            if (_reconnectionAttemptsMade > 0)
            {
                yield return new WaitForSeconds(TIME_BETWEEN_ATTEMPTS);
            }

            Debug.Log("Lost connection to host, trying to reconnect...");

            // Shutdown our NetworkManager to prepare for reconnect.
            ConnectionManager.NetworkManager.Shutdown();

            // Wait until we have completed shutting down.
            yield return new WaitWhile(() => ConnectionManager.NetworkManager.ShutdownInProgress);
            Debug.Log($"Reconnecting attempt {_reconnectionAttemptsMade + 1}/{ConnectionManager.MaxReconnectAttempts}...");
            ReconnectMessagePublisher.Publish(new ReconnectMessage(_reconnectionAttemptsMade, ConnectionManager.MaxReconnectAttempts));


            // If this is our first attempt, wait some time before attempting to reconnect to give the services some time to update.
            //  For example, if we are in a session and the host shuts down unexpectedly, this should give enough time for the session
            //  to be properly deleted so that we don't try to connect to an empty session.
            if (_reconnectionAttemptsMade == 0)
            {
                yield return new WaitForSeconds(TIME_BEFORE_FIRST_ATTEMPT);
            }

            // Perform our reconnection attempt.
            ++_reconnectionAttemptsMade;
            var reconnectingSetupTask = ConnectionMethod.SetupClientReconnectionAsync();
            yield return new WaitUntil(() => reconnectingSetupTask.IsCompleted);


            // Determine if we successfully connected or not.
            if (!reconnectingSetupTask.IsFaulted && reconnectingSetupTask.Result.Success)
            {
                // Successful reconnection attempt.
                // If this connection attempt fails, the OnClientDisconnect callback will be invoked by Netcode.
                ConnectClientAsync();
            }
            else
            {
                // Failed reconnection attempt.
                if (!reconnectingSetupTask.Result.ShouldTryAgain)
                {
                    // We don't want to try again. Set our number of attempts to the max so that none new are made.
                    _reconnectionAttemptsMade = ConnectionManager.MaxReconnectAttempts;
                }

                // Calling OnClientDisconnect() to mark this attempt as failed and either start a new one or
                //  give up and reutrn to the Offline state.
                OnClientDisconnect(0);
            }
        }
    }
}