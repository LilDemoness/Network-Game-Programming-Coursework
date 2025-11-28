using Gameplay.UI.Popups;
using Infrastructure;
using Netcode.ConnectionManagement;
using UnityEngine;
using VContainer;

namespace Gameplay.UI
{
    /// <summary>
    ///     Subscribes to connection status messages to display them through the popup panel.
    /// </summary>
    public class ConnectionStatusMessageUIManager : MonoBehaviour
    {
        private DisposableGroup _subscriptions;
        private PopupPanel _currentReconnectPopup;

        [Inject]
        private void InjectDependenciesAndInitialize(ISubscriber<ConnectStatus> connectStatusSubscriber, ISubscriber<ReconnectMessage> reconnectMessageSubscriber)
        {
            _subscriptions = new DisposableGroup();
            _subscriptions.Add(connectStatusSubscriber.Subscribe(OnConnectStatus));
            _subscriptions.Add(reconnectMessageSubscriber.Subscribe(OnReconnectMessage));
        }


        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
        private void OnDestroy()
        {
            if (_subscriptions != null)
                _subscriptions.Dispose();
        }


        private void OnConnectStatus(ConnectStatus status)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;  // No required message.
                case ConnectStatus.ServerFull:
                    PopupManager.ShowPopupPanel("Connection Failed", "The Host is full and cannot accept any additional connections.");
                    break;
                case ConnectStatus.Success:
                    break;
                case ConnectStatus.DuplicateLoginAttempt:
                    PopupManager.ShowPopupPanel("Connection Failed", "You have logged in elsewhere using the same account. If you still want to connect, select a different profile by using the 'Change Profile' button.");
                    break;
                case ConnectStatus.IncompatibleBuildType:
                    PopupManager.ShowPopupPanel("Connection Failed", "Server and client builds are not compatible. You cannot connect a release build to a development build or an in-editor session.");
                    break;
                case ConnectStatus.GenericDisconnect:
                    PopupManager.ShowPopupPanel("Disconnected From Host", "The connection to the host was lost.");
                    break;
                case ConnectStatus.HostEndedSession:
                    PopupManager.ShowPopupPanel("Disconnected From Host", "The host has ended the game session.");
                    break;
                case ConnectStatus.Reconnecting:
                    break;
                case ConnectStatus.StartHostFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting host failed.");
                    break;
                case ConnectStatus.StartClientFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting client failed.");
                    break;
                default: throw new System.NotImplementedException($"No connect message defined for the Connect Status {status.ToString()}");
            }
        }

        private void OnReconnectMessage(ReconnectMessage message)
        {
            if (message.CurrentAttempt == message.MaxAttempt)
            {
                // We've made the maximum reconnection attempts. Close the panel.
                CloseReconnectPopup();
            }
            else if (_currentReconnectPopup != null)
            {
                // Popup panel is already open.
                _currentReconnectPopup.SetupPopupPanel("Connection lost", $"Attempting to reconnect...\nAttempt {message.CurrentAttempt + 1}/{message.MaxAttempt}", closeableByUser: false);
            }
            else
            {
                // Popup panel isn't open. Open one and cache it for the next reconnect popup.
                _currentReconnectPopup = PopupManager.ShowPopupPanel("Connection lost", $"Attempting to reconnect...\nAttempt {message.CurrentAttempt + 1}/{message.MaxAttempt}", closeableByUser: false);
            }
        }


        private void CloseReconnectPopup()
        {
            if (_currentReconnectPopup == null)
                return; // No open popup.

            _currentReconnectPopup.Hide();
            _currentReconnectPopup = null;
        }
    }
}