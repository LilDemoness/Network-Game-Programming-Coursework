using Infrastructure;
using Netcode.ConnectionManagement;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

namespace Gameplay.UI.MainMenu
{
    /// <summary>
    ///     A UI Window that shows while a client is attempting to connect to a server. {Confirm]
    /// </summary>
    public class IPConnectionWindow : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _titleText;

        [Inject]
        private IPUIMediator _ipUIMediator;
        private ISubscriber<ConnectStatus> _connectStatusSubscriber;

        [Inject]
        private void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSubscriber)
        {
            this._connectStatusSubscriber = connectStatusSubscriber;
            _connectStatusSubscriber.Subscribe(OnConnectStatusMessage);
        }


        private void Awake()
        {
            Hide();
        }
        private void OnDestroy()
        {
            if (_connectStatusSubscriber != null)
                _connectStatusSubscriber.Unsubscribe(OnConnectStatusMessage);
        }


        private void OnConnectStatusMessage(ConnectStatus connectStatus)
        {
            CancelConnectionWindow();
            _ipUIMediator.DisableSignInSpinner();
        }

        private void Show()
        {
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;
        }
        private void Hide()
        {
            _canvasGroup.alpha = 0.0f;
            _canvasGroup.blocksRaycasts = false;
        }


        /// <summary>
        ///     Show the Connection Window and update its text to match the remaining max connection time.
        /// </summary>
        /// <remarks> Hides itself upon reaching an estimate time of 0s.</remarks>
        public void ShowConnectionWindow()
        {
            void OnTimeElapsed()
            {
                Hide();
                _ipUIMediator.DisableSignInSpinner();
            }

            UnityTransport utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            int maxConnectAttempts = utp.MaxConnectAttempts;
            int connectTimeoutMS = utp.ConnectTimeoutMS;
            StartCoroutine(DisplayUTPConnectionDuration(maxConnectAttempts, connectTimeoutMS, OnTimeElapsed));

            Show();
        }

        /// <summary>
        ///     Hide the Connection Window and stop its text from updating.
        /// </summary>
        public void CancelConnectionWindow()
        {
            Hide();
            StopAllCoroutines();
        }


        /// <summary>
        ///     Display the UTP connection duration in seconds, triggering <paramref name="endAction"/> when it reaches 0s.
        /// </summary>
        private IEnumerator DisplayUTPConnectionDuration(int maxReconnectAttempts, int connectTimeoutMS, System.Action endAction)
        {
            float connectionDuration = maxReconnectAttempts * (connectTimeoutMS / 1000.0f);
            int seconds = Mathf.CeilToInt(connectionDuration);

            while(seconds > 0)
            {
                _titleText.text = $"Connecting...\n{seconds}";
                yield return new WaitForSeconds(1.0f);
                --seconds;
            }
            _titleText.text = "Connecting...";

            endAction();
        }


        // Invoked by UI cancel button.
        public void OnCancelJoinButtonPressed()
        {
            CancelConnectionWindow();
            _ipUIMediator.JoiningWindowCancelled();
        }
    }
}