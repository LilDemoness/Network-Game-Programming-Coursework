using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace UnityServices.Sessions
{
    /// <summary>
    ///     A wrapper for all the interactions with the Sessions API.
    /// </summary>
    public class MultiplayerServicesInterface
    {
        private const int MAX_SESSIONS_TO_SHOW = 16;    // If more are necessary, consider retrieving paginated results or using filters.
        private const int MAX_PLAYERS = 8;

        private readonly List<FilterOption> _filterOptions;
        private readonly List<SortOption> _sortOptions;


        public MultiplayerServicesInterface()
        {
            // Filter for open sessions only.
            _filterOptions = new List<FilterOption>
            {
                new(FilterField.AvailableSlots, "0", FilterOperation.Greater),
            };

            // Order by newest first.
            _sortOptions = new List<SortOption>()
            {
                new(SortOrder.Descending, SortField.CreationTime),
            };
        }


        /// <summary>
        ///     Create a session with the given parameters.
        /// </summary>
        public async Task<ISession> CreateSession(string sessionName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerProperty> playerProperties, Dictionary<string, SessionProperty> sessionProperties)
        {
            SessionOptions sessionOptions = new SessionOptions
            {
                Name = sessionName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                IsLocked = false,
                PlayerProperties = playerProperties,
                SessionProperties = sessionProperties,
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
        }

        /// <summary>
        ///     Join the session with the given code.
        /// </summary>
        public async Task<ISession> JoinSessionByCode(string sessionCode, Dictionary<string, PlayerProperty> localUserData)
        {
            JoinSessionOptions joinSessionOptions = new JoinSessionOptions
            {
                PlayerProperties = localUserData,
            };
            return await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode, joinSessionOptions);
        }

        /// <summary>
        ///     Join the session with the given id.
        /// </summary>
        public async Task<ISession> JoinSessionById(string sessionId, Dictionary<string, PlayerProperty> localUserData)
        {
            JoinSessionOptions joinSessionOptions = new JoinSessionOptions
            {
                PlayerProperties = localUserData,
            };
            return await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, joinSessionOptions);
        }

        /// <summary>
        ///     Join the first available session.
        /// </summary>
        public async Task<ISession> QuickJoinSession(Dictionary<string, PlayerProperty> localUserData)
        {
            QuickJoinOptions quickJoinOptions = new QuickJoinOptions
            {
                Filters = _filterOptions,
                CreateSession = true,   // Create a matching session if no matching session was found.
            };

            SessionOptions sessionOptions = new SessionOptions
            {
                MaxPlayers = MAX_PLAYERS,
                PlayerProperties = localUserData,
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
        }


        /// <summary>
        ///     Retrieve a list of all active sessions.
        /// </summary>
        public async Task<QuerySessionsResults> QuerySessions() => await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
        /// <summary>
        ///     Attempt to reconnect to the session with the corresponding sessionId.
        /// </summary>
        public async Task<ISession> ReconnectToSession(string sessionId) => await MultiplayerService.Instance.ReconnectToSessionAsync(sessionId);

        /// <summary>
        ///     Retrieve a list of all sessions, filtered and sorted by the active filters & sort mode.
        /// </summary>
        public async Task<QuerySessionsResults> QueryAllSessions()
        {
            QuerySessionsOptions querySessionsOptions = new QuerySessionsOptions
            {
                Count = MAX_SESSIONS_TO_SHOW,
                FilterOptions = _filterOptions,
                SortOptions = _sortOptions,
            };

            return await MultiplayerService.Instance.QuerySessionsAsync(querySessionsOptions);
        }
    }
}