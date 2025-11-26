using System;
using UnityEngine;
using Infrastructure;
using VContainer;
using VContainer.Unity;
using Unity.Services.Multiplayer;
using System.Threading.Tasks;
using static UnityEngine.Analytics.AnalyticsSessionInfo;

namespace UnityServices.Sessions
{
    /// <summary>
    ///     An abstraction layer between direct calls into the Multiplayer Services SDK and the outcomes we actually want.
    /// </summary>
    public class MultiplayerServicesFacade : IDisposable, IStartable
    {
        [Inject]
        private LifetimeScope _parentScope;
        [Inject]
        private UpdateRunner _updateRunner;
        [Inject]
        private LocalSession _localSession;
        [Inject]
        private LocalSessionUser _localUser;
        [Inject]
        IPublisher<UnityServiceErrorMessage> _unityServiceErrorMessagePublisher;
        [Inject]
        IPublisher<SessionListFetchedMessage> _sessionListFetchedPublisher;


        private LifetimeScope _serviceScope;
        MultiplayerServicesInterface _multiplayerServicesInterface;

        private RateLimitCooldown _rateLimitQuery;
        private RateLimitCooldown _rateLimitJoin;
        private RateLimitCooldown _rateLimitQuickJoin;
        private RateLimitCooldown _rateLimitHost;

        public ISession CurrentUnitySession { get; private set; }
        private bool _isTracking;


        public void Start()
        {
            _serviceScope = _parentScope.CreateChild(builder => {
                builder.Register<MultiplayerServicesInterface>(Lifetime.Singleton);
            });

            _multiplayerServicesInterface = _serviceScope.Container.Resolve<MultiplayerServicesInterface>();
            
            //See 'https://docs.unity.com/ugs/manual/lobby/manual/rate-limits'.
            _rateLimitQuery = new RateLimitCooldown(1.0f);
            _rateLimitJoin = new RateLimitCooldown(1.0f);
            _rateLimitQuickJoin = new RateLimitCooldown(1.0f);
            _rateLimitHost = new RateLimitCooldown(3.0f);
        }
        public void Dispose()
        {
            EndTracking();
            if (_serviceScope != null)
            {
                _serviceScope.Dispose();
            }
        }


        public void SetRemoteSession(ISession session)
        {
            CurrentUnitySession = session;
            _localSession.ApplyRemoteData(session);
        }

        /// <summary>
        ///     Initiates tracking of the joined session's events.
        ///     The host also starts sending heartbeat pings here.
        /// </summary>
        public void BeginTracking()
        {
            if (_isTracking)
                return; // We're already tracking.

            _isTracking = true;
            SubscribeToJoinedSession();
        }
        /// <summary>
        ///     Ends tracking of a joined session's events and leaves or deletes the session.
        ///     The host also stops sending heartbeat pings here.
        /// </summary>
        public void EndTracking()
        {
            if (_isTracking)
            {
                _isTracking = false;
            }

            if (CurrentUnitySession != null)
            {
                UnsubscribeFromJoinedSession();
                if (_localUser.IsHost)
                {
                    DeleteSessionAsync();
                }
                else
                {
                    LeaveSessionAsync();
                }
            }
        }


        /// <summary>
        ///     Attempt to create a new session and then join it.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryCreateSessionAsync(string sessionName, int maxPlayers, bool isPrivate)
        {
            if (!_rateLimitHost.CanCall)
            {
                Debug.LogWarning("CreateSession hit the rate limit.");
                return (false, null);
            }

            // Attempt to create the session through the MultiplayerServicesInterface.
            try
            {
                ISession session = await _multiplayerServicesInterface.CreateSession(
                    sessionName,
                    maxPlayers,
                    isPrivate,
                    _localUser.GetDataForUnityServices(),
                    null);
                return (true, session);
            }
            catch (System.Exception e)
            {
                PublishError(e);
            }

            // Failed to create session.
            return (false, null);
        }

        /// <summary>
        ///     Attempt to join an existing session with a join code.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryJoinSessionByCodeAsync(string sessionCode)
        {
            if (!_rateLimitJoin.CanCall)
            {
                Debug.LogWarning("JoinSessionByCode() hit the rate limit.");
                return (false, null);
            }
            if (string.IsNullOrEmpty(sessionCode))
            {
                Debug.LogWarning("Cannot join a session without a join code.");
                return (false, null);
            }

            Debug.Log($"Joining session with join code: {sessionCode}");

            // Attempt to join the session.
            try
            {
                ISession session = await _multiplayerServicesInterface.JoinSessionByCode(sessionCode, _localUser.GetDataForUnityServices());
                return (true, session);
            }
            catch (System.Exception e)
            {
                PublishError(e);
            }

            // Failed to join session.
            return (false, null);
        }


        /// <summary>
        ///     Attempt to join an existing session by name.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryJoinSessionByNameAsync(string sessionName)
        {
            if (!_rateLimitJoin.CanCall)
            {
                Debug.LogWarning("JoinSessionByName() hit the rate limit.");
                return (false, null);
            }
            if (string.IsNullOrEmpty(sessionName))
            {
                Debug.LogWarning("Cannot join a session without a session name.");
                return (false, null);
            }

            Debug.Log($"Joining session with name {sessionName}");

            // Attempt to join the session.
            try
            {
                ISession session = await _multiplayerServicesInterface.JoinSessionById(sessionName, _localUser.GetDataForUnityServices());
                return (true, session);
            }
            catch (Exception e)
            {
                PublishError(e);
            }

            // Failed to join the session.
            return (false, null);
        }


        /// <summary>
        ///     Attempt to join the first available session that matches the filtered OnlineMode.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryQuickJoinSessionAsync()
        {
            if (!_rateLimitJoin.CanCall)
            {
                Debug.LogWarning("QuickJoinSession() hit the rate limit.");
                return (false, null);
            }

            // Attempt to join the first available session.
            try
            {
                var session = await _multiplayerServicesInterface.QuickJoinSession(_localUser.GetDataForUnityServices());
                return (true, session);
            }
            catch (Exception e)
            {
                PublishError(e);
            }

            // Failed to join the session.
            return (false, null);
        }



        private void ResetSession()
        {
            CurrentUnitySession = null;
            _localUser?.ResetState();
            _localSession?.Reset(_localUser);

            // We don't need to disconnect Netcode as it should already be handled by Netcode's callback to disconnect.
        }

        private void SubscribeToJoinedSession()
        {
            CurrentUnitySession.Changed                     += OnSessionChanged;
            CurrentUnitySession.StateChanged                += OnSessionStateChanged;
            CurrentUnitySession.Deleted                     += OnSessionDeleted;
            CurrentUnitySession.PlayerJoined                += OnPlayerJoined;
            CurrentUnitySession.PlayerHasLeft               += OnPlayerHasLeft;
            CurrentUnitySession.RemovedFromSession          += OnRemovedFromSession;
            CurrentUnitySession.PlayerPropertiesChanged     += OnPlayerPropertiesChanged;
            CurrentUnitySession.SessionPropertiesChanged    += OnSessionPropertiesChanged;
        }
        private void UnsubscribeFromJoinedSession()
        {
            CurrentUnitySession.Changed                     -= OnSessionChanged;
            CurrentUnitySession.StateChanged                -= OnSessionStateChanged;
            CurrentUnitySession.Deleted                     -= OnSessionDeleted;
            CurrentUnitySession.PlayerJoined                -= OnPlayerJoined;
            CurrentUnitySession.PlayerHasLeft               -= OnPlayerHasLeft;
            CurrentUnitySession.RemovedFromSession          -= OnRemovedFromSession;
            CurrentUnitySession.PlayerPropertiesChanged     -= OnPlayerPropertiesChanged;
            CurrentUnitySession.SessionPropertiesChanged    -= OnSessionPropertiesChanged;
        }


        private void OnSessionChanged()
        {
            _localSession.ApplyRemoteData(CurrentUnitySession);

            // If we are a client, check if the host is still in session.
            if (!_localUser.IsHost)
            {
                foreach(var sessionUser in _localSession.SessionUsers)
                {
                    if (sessionUser.Value.IsHost)
                    {
                        // The host is still in the session.
                        return;
                    }
                }

                // The host has disconnected.
                _unityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Host left the session", "Disconnecting.", UnityServiceErrorMessage.Service.Session));
                EndTracking();

                // We don't need to disconnect Netcode as it should already be handled by Netcode's callback to disconnect.
            }
        }

        private void OnSessionStateChanged(SessionState sessionState)
        {
            switch (sessionState)
            {
                case SessionState.None: break;
                case SessionState.Connected:
                    Debug.Log("Session state changed: Session connected.");
                    break;
                case SessionState.Disconnected:
                    Debug.Log("Session state changed: Session disconnected.");
                    break;
                case SessionState.Deleted:
                    Debug.Log("Session state changed: Session deleted.");
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(SessionState), sessionState, null);
            }
        }

        private void OnSessionDeleted()
        {
            Debug.Log("Session deleted.");
            ResetSession();
            EndTracking();
        }

        private void OnPlayerJoined(string playerId) => Debug.Log($"Player joined: {playerId}");
        private void OnPlayerHasLeft(string playerId) => Debug.Log($"Player left: {playerId}");

        private void OnRemovedFromSession()
        {
            Debug.Log("Removed from Session");
            ResetSession();
            EndTracking();
        }

        private void OnPlayerPropertiesChanged() => Debug.Log("Player properties changed.");
        private void OnSessionPropertiesChanged() => Debug.Log("Session properties changed.");


        /// <summary>
        ///     Used for getting the list of all active sessions without needing full info for each.
        /// </summary>
        public async Task RetrieveAndPublishSessionListAsync()
        {
            if (!_rateLimitQuery.CanCall)
            {
                Debug.LogWarning("Retrieving the session list hit the rate limit. Will try again soon...");
                return;
            }

            try
            {
                var queryResults = await _multiplayerServicesInterface.QuerySessions();
                _sessionListFetchedPublisher.Publish(new SessionListFetchedMessage(queryResults.Sessions));
            }
            catch (System.Exception e)
            {
                PublishError(e);
            }
        }

        /// <summary>
        ///     Attempt to reconnect to the current session.
        /// </summary>
        public async Task<ISession> ReconnectToSessionAsync()
        {
            try
            {
                return await _multiplayerServicesInterface.ReconnectToSession(_localSession.SessionID);
            }
            catch (System.Exception e)
            {
                PublishError(e, true);
            }

            return null;
        }


        /// <summary>
        ///     Attempt to leave a session.
        /// </summary>
        private async void LeaveSessionAsync()
        {
            try
            {
                await CurrentUnitySession.LeaveAsync();
            }
            catch (System.Exception e)
            {
                PublishError(e, true);
            }
            finally
            {
                ResetSession();
            }
        }

        /// <summary>
        ///     Attempt to remove a player from the session.<br/>
        ///     Should only work if called from the host.
        /// </summary>
        /// <param name="uasId"></param>
        public async void RemovePlayerFromSessionAsync(string uasId)
        {
            if (!_localUser.IsHost)
            {
                Debug.LogError("Only the host can remove other players from the session.");
                return;
            }

            try
            {
                await CurrentUnitySession.AsHost().RemovePlayerAsync(uasId);
            }
            catch (System.Exception e)
            {
                PublishError(e);
            }
        }

        /// <summary>
        ///     Attempt to delete the session.<br/>
        ///     Should only work if called form the host.
        /// </summary>
        private async void DeleteSessionAsync()
        {
            if (!_localUser.IsHost)
            {
                Debug.LogError("Only the host can delete a session.");
                return;
            }

            try
            {
                await CurrentUnitySession.AsHost().DeleteAsync();
            }
            catch (System.Exception e)
            {
                PublishError(e);
            }
            finally
            {
                ResetSession();
            }
        }


        /// <summary>
        ///     A helper function to publish an exception correctly.
        /// </summary>
        private void PublishError(System.Exception e, bool checkIfDeleted = false)
        {
            if (e is not AggregateException aggregateException)
            {
                // The exception was not an aggregate exception.
                _unityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Session Error", e.Message, UnityServiceErrorMessage.Service.Session, e));
                return;
            }

            // The exception was an aggregate exception.
            if (aggregateException.InnerException is not SessionException sessionException)
            {
                // The inner exception of the aggregate was not a session exception.
                _unityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Session Error", e.Message, UnityServiceErrorMessage.Service.Session, e));
                return;
            }
            // The error was a session exception.

            // If the session is not found and we are not the host, then the session has already been deleted and we don't need to publish the error.
            if (checkIfDeleted)
            {
                if (sessionException.Error == SessionError.SessionNotFound && !_localUser.IsHost)
                {
                    return;
                }
            }

            if (sessionException.Error == SessionError.RateLimitExceeded)
            {
                // We exceeded our rate limit. Prevent further joining for a short duration.
                _rateLimitJoin.PutOnCooldown();
                return;
            }

            // Publish the message.
            string reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})";    // Session error type, then HTTP error type.
            _unityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Session Error", reason, UnityServiceErrorMessage.Service.Session, e));
        }
    }
}