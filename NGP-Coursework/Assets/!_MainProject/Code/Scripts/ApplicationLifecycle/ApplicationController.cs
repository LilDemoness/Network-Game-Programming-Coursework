using System;
using System.Collections;
using ApplicationLifecycle.Messages;
using Gameplay.GameState;
using Gameplay.Messages;
using Infrastructure;
using Netcode.ConnectionManagement;
using SceneLoading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServices;
using UnityServices.Auth;
using UnityServices.Sessions;
using Utils;
using VContainer;
using VContainer.Unity;

namespace ApplicationLifecycle
{
    /// <summary>
    ///     An entry point to the application where we bind all common dependencies to the root DI scope.
    /// </summary>
    public class ApplicationController : LifetimeScope
    {
        [SerializeField] private UpdateRunner _updateRunner;
        [SerializeField] private ConnectionManager _connectionManager;
        [SerializeField] private NetworkManager _networkManager;

        private LocalSession _localSession;
        private MultiplayerServicesFacade _multiplayerServicesFacade;

        private IDisposable _subscriptions;


        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(_updateRunner);
            builder.RegisterComponent(_connectionManager);
            builder.RegisterComponent(_networkManager);


            // The following singletons represent the local representations of the Session that we're in and the user that we are.
            //  They can persist longer than the lifetime of the UI in the MainMenu where we set up the sessions that we create or join.
            builder.Register<LocalSessionUser>(Lifetime.Singleton);
            builder.Register<LocalSession>(Lifetime.Singleton);

            builder.Register<ProfileManager>(Lifetime.Singleton);

            builder.Register<PersistentGameState>(Lifetime.Singleton);


            // These message channels are essential and persist for the lifetime of the Session and Relay services.
            // Registering as an instance to provent code stripping on iOS.
            builder.RegisterInstance(new MessageChannel<QuitApplicationMessage>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<UnityServiceErrorMessage>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<ConnectStatus>()).AsImplementedInterfaces();

            // These message channels are essential and persist for the lifetime of the Session and Relay services.
            //  They are networked so that the clients can subscribe to those messages that are published by the server.
            builder.RegisterComponent(new NetworkedMessageChannel<LifeStateChangedEventMessage>()).AsImplementedInterfaces();
            builder.RegisterComponent(new NetworkedMessageChannel<ConnectionEventMessage>()).AsImplementedInterfaces();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterComponent(new NetworkedMessageChannel<CheatUsedMessage>()).AsImplementedInterfaces();
#endif


            // This message channel is essential and persists for the lifetime of the Session and Relay services.
            builder.RegisterInstance(new MessageChannel<ReconnectMessage>()).AsImplementedInterfaces();

            // Buffered message channels hold the latest received message in the buffer and pass to any new subscribers.
            builder.RegisterInstance(new BufferedMessageChannel<SessionListFetchedMessage>()).AsImplementedInterfaces();

            // All the Session service stuff, bound here so that it persists through scene loads.
            builder.Register<AuthenticationServiceFacade>(Lifetime.Singleton);  // A manager entity that allows us to do anonymous authentication with Unity Services.

            // MultiplayerSrevicesFacade is registered as an entrypoint because it wants a callback after the container is build to do its initialisation.
            builder.RegisterEntryPoint<MultiplayerServicesFacade>(Lifetime.Singleton).AsSelf();
        }


        private void Start()
        {
            _localSession = Container.Resolve<LocalSession>();
            _multiplayerServicesFacade = Container.Resolve<MultiplayerServicesFacade>();

            ISubscriber<QuitApplicationMessage> quitApplicationSubscriber = Container.Resolve<ISubscriber<QuitApplicationMessage>>();

            DisposableGroup subscriptionHandles = new DisposableGroup();
            subscriptionHandles.Add(quitApplicationSubscriber.Subscribe(QuitGame));
            _subscriptions = subscriptionHandles;

            Application.wantsToQuit += OnWantToQuit;
            DontDestroyOnLoad(this.gameObject);
            DontDestroyOnLoad(_updateRunner.gameObject);
            Application.targetFrameRate = 120;

            SceneLoader.Instance.LoadNonGameplayScene(SceneLoader.NonGameplayScene.MainMenu, false);
        }

        protected override void OnDestroy()
        {
            if (_subscriptions != null)
                _subscriptions.Dispose();

            if (_multiplayerServicesFacade != null)
                _multiplayerServicesFacade.EndTracking();

            base.OnDestroy();
        }


        /// <summary>
        ///     In builds, if we are in a Session and try to send a Leave request on application quit, it won't do through if we're quitting on the same frame.
        ///     This coroutine briefly delays the quit so that our leave request can happen (We don't need to wait for the result though).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            // We want to quit even if there is an issue, so if anything happens when trying to leave just log the error and carry on.
            try
            {
                _multiplayerServicesFacade.EndTracking();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }

            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            Application.wantsToQuit -= OnWantToQuit;

            bool canInstantlyQuit = _localSession != null && string.IsNullOrEmpty(_localSession.SessionID);
            if (!canInstantlyQuit)
            {
                // We cannot instantly quit as we need to first leave our session.
                StartCoroutine(LeaveBeforeQuit());
            }

            return canInstantlyQuit;
        }

        private void QuitGame(QuitApplicationMessage msg)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}