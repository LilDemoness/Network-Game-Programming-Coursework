using Gameplay.Configuration;
using Infrastructure;
using Netcode.ConnectionManagement;
using TMPro;
using Unity.Services.Multiplayer;
using Unity.Services.Core;
using UnityEngine;
using UnityServices.Auth;
using UnityServices.Sessions;
using VContainer;

namespace Gameplay.UI.MainMenu.Session
{
    public class SessionUIMediator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private SessionJoiningUI _sessionJoiningUI;
        [SerializeField] private SessionCreationUI _sessionCreationUI;

        //[SerializeField] private UITinter _joinToggleHighlight;
        //[SerializeField] private UITinter _joinToggleTabBlocker;
        //[SerializeField] private UITinter _createToggleHighlight;
        //[SerializeField] private UITinter _createToggleTabBlocker;

        [SerializeField] private TextMeshProUGUI _playerNameLabel;
        [SerializeField] private GameObject _loadingSpinner;


        private AuthenticationServiceFacade _authenticationServiceFacade;
        private MultiplayerServicesFacade _multiplayerServicesFacade;
        private LocalSessionUser _localUser;
        private LocalSession _localSession;
        private NameGenerationData _nameGenerationData;
        private ConnectionManager _connectionManager;
        private ISubscriber<ConnectStatus> _connectStatusSubscriber;

        private const string DEFAULT_SESSION_NAME = "no-name";
        private const int MAX_PLAYERS = 8;

        private ISession _session;


        [Inject]
        private void InjectDependenciesAndInitialise(
            AuthenticationServiceFacade authenticationServiceFacade,
            MultiplayerServicesFacade multiplayerServicesFacade,
            LocalSessionUser localSessionUser,
            LocalSession localSession,
            NameGenerationData nameGenerationData,
            ISubscriber<ConnectStatus> connectStatusSubscriber,
            ConnectionManager connectionManager)
        {
            this._authenticationServiceFacade = authenticationServiceFacade;
            this._multiplayerServicesFacade = multiplayerServicesFacade;
            this._localUser = localSessionUser;
            this._localSession = localSession;
            this._nameGenerationData = nameGenerationData;
            this._connectStatusSubscriber = connectStatusSubscriber;
            this._connectionManager = connectionManager;
            RegenerateName();

            _connectStatusSubscriber.Subscribe(OnConnectStatus);
        }

        private void OnConnectStatus(ConnectStatus status)
        {
            if (status is ConnectStatus.GenericDisconnect or ConnectStatus.StartClientFailed)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        private void OnDestroy()
        {
            _connectStatusSubscriber?.Unsubscribe(OnConnectStatus);
        }


        // Multiplayer Services SDK calls done from UI.
        public async void CreateSessionRequest(string sessionName, bool isPrivate)
        {
            // Before sending a request, populate an empty session name, if necessary.
            if (string.IsNullOrEmpty(sessionName))
            {
                sessionName = DEFAULT_SESSION_NAME;
            }

            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
            }

            _connectionManager.StartHostSession(_localUser.DisplayName);
            var result = await _multiplayerServicesFacade.TryCreateSessionAsync(sessionName, MAX_PLAYERS, isPrivate);

            HandleSessionJoinResult(result);
        }

        public async void QuerySessionRequest(bool blockUI)
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
                return; // Services are uninitialised.

            if (blockUI)
                BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorised = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (blockUI && !playerIsAuthorised)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            await _multiplayerServicesFacade.RetrieveAndPublishSessionListAsync();

            if (blockUI)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void JoinSessionWithCodeRequest(string sessionCode)
        {
            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            _connectionManager.StartClientSession(_localUser.DisplayName);

            var result = await _multiplayerServicesFacade.TryJoinSessionByCodeAsync(sessionCode);

            HandleSessionJoinResult(result);
        }

        public async void JoinSessionRequest(ISessionInfo sessionInfo)
        {
            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            _connectionManager.StartClientSession(_localUser.DisplayName);

            var result = await _multiplayerServicesFacade.TryJoinSessionByNameAsync(sessionInfo.Id);

            HandleSessionJoinResult(result);
        }

        public async void QuickJoinRequest()
        {
            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            _connectionManager.StartHostSession(_localUser.DisplayName);

            var result = await _multiplayerServicesFacade.TryQuickJoinSessionAsync();

            HandleSessionJoinResult(result);
        }

        private void HandleSessionJoinResult((bool Success, ISession Session) result)
        {
            if (result.Success)
            {
                OnJoinedSession(result.Session);
            }
            else
            {
                _connectionManager.RequestShutdown();
                UnblockUIAfterLoadingIsComplete();
            }
        }

        private void OnJoinedSession(ISession remoteSession)
        {
            _multiplayerServicesFacade.SetRemoteSession(remoteSession);

            Debug.Log($"Joined session with ID: {_localSession.SessionID}");

            _connectionManager.StartClientSession(_localUser.DisplayName);
        }



        public void Show()
        {
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;
        }
        public void Hide()
        {
            _canvasGroup.alpha = 0.0f;
            _canvasGroup.blocksRaycasts = false;
            _sessionCreationUI.Hide();
            _sessionJoiningUI.Hide();
        }

        public void OpenJoinSessionUI()
        {
            _sessionJoiningUI.Show();
            _sessionCreationUI.Hide();

            //_joinToggleHighlight.SetToColour(1);
            //_joinToggleTabBlocker.SetToColour(1);
            //_createToggleHighlight.SetToColour(0);
            //_createToggleTabBlocker.SetToColour(0);
        }
        public void OpenCreateSessionUI()
        {
            _sessionJoiningUI.Hide();
            _sessionCreationUI.Show();

            //_joinToggleHighlight.SetToColour(0);
            //_joinToggleTabBlocker.SetToColour(0);
            //_createToggleHighlight.SetToColour(1);
            //_createToggleTabBlocker.SetToColour(1);
        }


        public void RegenerateName()
        {
            _localUser.DisplayName = _nameGenerationData.GenerateRandomName();
            _playerNameLabel.text = _localUser.DisplayName;
        }

        private void BlockUIWhileLoadingIsInProgress()
        {
            _canvasGroup.interactable = false;
            _loadingSpinner.SetActive(true);
        }
        private void UnblockUIAfterLoadingIsComplete()
        {
            // This callback can happen after we've already switched to a different scene,
            //  so we should check in case thje canbas group is null.
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _loadingSpinner.SetActive(false);
            }
        }
    }
}