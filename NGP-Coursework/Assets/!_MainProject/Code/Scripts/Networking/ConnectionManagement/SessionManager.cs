using System.Collections.Generic;
using UnityEngine;

namespace Netcode.ConnectionManagement
{
    public interface ISessionPlayerData
    {
        bool IsConnected { get; set; }
        ulong ClientID { get; set; }
        
        void Reinitialise();
    }


    /// <summary>
    ///     This class uses a unique player ID to bind a player to a session.<br/>
    ///     Once that player connects to a host, the host associates the current ClientID to the player's unique ID.<br/>
    ///     If the player disconnects and reconnects to the same host, the session is preserved.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    // Note: We current have no way to ensure that the same player is using the same player ID as before.
    public class SessionManager<T> where T : struct, ISessionPlayerData
    {
        private SessionManager()
        {
            _clientData = new Dictionary<string, T>();
            _clientIDToPlayerId = new Dictionary<ulong, string>();
        }


        private static SessionManager<T> s_instance;
        public static SessionManager<T> Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new SessionManager<T>();

                return s_instance;
            }
        }


        /// <summary>
        ///     Dictionary to convert PlayerID to the data for a given client.
        /// </summary>
        private Dictionary<string, T> _clientData;

        /// <summary>
        ///     Dictionary to convert PlayerID to PlayerData.
        /// </summary>
        private Dictionary<ulong, string> _clientIDToPlayerId;


        private bool _hasSessionStarted;


        /// <summary>
        ///     Called when a Client successfully connects to the server.
        /// </summary>
        public static event System.EventHandler<PlayerConnectionEventArgs> OnClientConnected;
        /// <summary>
        ///     Called when a Client disconnects from the server.
        /// </summary>
        public static event System.Action<ulong> OnClientDisconnect;


        /// <summary>
        ///     Handles client disconnect.
        /// </summary>
        public void DisconnectClient(ulong clientId)
        {
            if (_hasSessionStarted)
            {
                // Mark the client as diconnected, but retain their data in case they reconnect.
                if (_clientIDToPlayerId.TryGetValue(clientId, out string playerId))
                {
                    T? playerData = GetPlayerData(playerId);
                    if (playerData != null && playerData.Value.ClientID == clientId)
                    {
                        // Player Data exists for this client. Mark the player as disconnected.
                        T clientData = _clientData[playerId];
                        clientData.IsConnected = false;
                        _clientData[playerId] = clientData;
                    }
                }
            }
            else
            {
                // The session hasn't started, so discard any player data and remove connections.
                if (_clientIDToPlayerId.TryGetValue(clientId, out var playerId))
                {
                    _clientIDToPlayerId.Remove(clientId);

                    T? playerData = GetPlayerData(playerId);
                    if (playerData != null && playerData.Value.ClientID == clientId)
                    {
                        _clientData.Remove(playerId);
                    }
                }
            }

            OnClientDisconnect?.Invoke(clientId);
        }


        /// <summary> </summary>
        /// <param name="playerId"> The Player ID that is unique to this client and persists across multiple logins from the same client.</param>
        /// <returns> True if a player with this ID is already connected.</returns>
        public bool IsDuplicateConnection(string playerId) => _clientData.ContainsKey(playerId) && _clientData[playerId].IsConnected;


        /// <summary>
        ///     Adds a connecting player's session data if it is a new connection, or updates their session data in case of a reconnection.
        /// </summary>
        /// <param name="clientId"> The Client ID that Netcode assigned to the client upon login. Doesn't persist between multiple logins of the same client.</param>
        /// <param name="playerId"> The Player ID that is unique to this client and persists across multiple logins from the same client.</param>
        /// <param name="sessionPlayerData"> The player's initial data.</param>
        public void SetupConnectingPlayerSessionData(ulong clientId, string playerId, T sessionPlayerData)
        {
            bool isReconnecting = false;

            // Test for a duplicate connection.
            if (IsDuplicateConnection(playerId))
            {
                Debug.LogError($"Player ID '{playerId}' already exists. This is a duplicate connection. Rejecting this session data.");
                return;
            }

            // The player is a valid connection.

            // Check if a previously disconnected client existed with the same playerId.
            if (_clientData.ContainsKey(playerId))
            {
                if (!_clientData[playerId].IsConnected)
                {
                    // This connecting client has the same player ID as a disconnected client, so this is a reconnection.
                    isReconnecting = true;
                }
            }

            if (isReconnecting)
            {
                // Reconnecting. Give data from the old player to the new one.
                sessionPlayerData = _clientData[playerId];
                sessionPlayerData.ClientID = clientId;
                sessionPlayerData.IsConnected = true;
            }

            // Populate our dictionaries with the SessionPlayerData.
            _clientIDToPlayerId[clientId] = playerId;
            _clientData[playerId] = sessionPlayerData;


            // Notify listeners that a player has joined.
            OnClientConnected?.Invoke(this, new PlayerConnectionEventArgs(clientId, isReconnecting, sessionPlayerData));
        }



        /// <summary> </summary>
        /// <param name="clientId"> The ID of the client whose data is requested.</param>
        /// <returns> The Player Id matching the given client ID.</returns>
        public string GetPlayerId(ulong clientId)
        {
            if (_clientIDToPlayerId.TryGetValue(clientId, out string playerId))
            {
                return playerId;
            }

            Debug.Log($"No client player ID found mapped to the given client ID: {clientId}");
            return null;
        }


        /// <summary> </summary>
        /// <param name="clientId"> The ID of the client whose data is requested.</param>
        /// <returns> The Player data struct matching the given ID.</returns>
        public T? GetPlayerData(ulong clientId)
        {
            // Check if we have a playerId matching the clientId given.
            string playerId = GetPlayerId(clientId);
            if (playerId != null)
            {
                // We have a player ID for the given client ID. Return our Player Data (Null if we have none).
                return GetPlayerData(playerId);
            }

            Debug.Log($"No client player ID found mapped to the given client ID: {clientId}");
            return null;
        }

        /// <summary> </summary>
        /// <param name="playerId"> The Player ID of the client whose data is requested.</param>
        /// <returns> The Player data struct matching the given ID.</returns>
        public T? GetPlayerData(string playerId)
        {
            if (_clientData.TryGetValue(playerId, out T data))
            {
                // We found our data. Return it.
                return data;
            }

            Debug.Log($"No PlayerData of matching player ID found: {playerId}");
            return null;
        }


        /// <summary>
        ///     Updates player data.
        /// </summary>
        /// <param name="clientID"> The ID of the client whose data will be updated.</param>
        /// <param name="sessionPlayerData"> The new Player Data to override the old.</param>
        public void SetPlayerData(ulong clientID, T sessionPlayerData)
        {
            if (_clientIDToPlayerId.TryGetValue(clientID, out string playerId))
            {
                // We should have data for this client. Update it.
                Debug.Log("Set Player Data");
                _clientData[playerId] = sessionPlayerData;
            }
            else
            {
                Debug.LogError($"No client player Id found mapped to the given client ID: {clientID}");
            }
        }


        /// <summary>
        ///     Marks the current session as started.
        /// </summary>
        /// <remarks> From this point on, we retain the data of disconnected players.</remarks>
        public void OnSessionStarted()
        {
            _hasSessionStarted = true;
        }


        /// <summary>
        ///     Reinitialises session data from the connected players, and clears the data from disconnected players so that if they reconnect in the next game they are treated as new players.
        /// </summary>
        public void OnSessionEnded()
        {
            ClearDisconnectedPlayersData();
            ReinitialisePlayersData();
            _hasSessionStarted = false;
        }


        /// <summary>
        ///     Resest all our runtime states so that thye can be reinitialised when starting a new server.
        /// </summary>
        public void OnServerEnded()
        {
            _clientData.Clear();
            _clientIDToPlayerId.Clear();
            _hasSessionStarted = false;
        }


        /// <summary>
        ///     Reinitialise the data of all players.
        /// </summary>
        private void ReinitialisePlayersData()
        {
            foreach(var id in _clientIDToPlayerId.Keys)
            {
                string playerId = _clientIDToPlayerId[id];
                T sessionPlayerData = _clientData[playerId];
                sessionPlayerData.Reinitialise();
                _clientData[playerId] = sessionPlayerData;
            }
        }
        /// <summary>
        ///     Clear the data of all disconnected players.
        /// </summary>
        private void ClearDisconnectedPlayersData()
        {
            // Find the ClientIDs of all disconnected players.
            List<ulong> idsToClear = new List<ulong>();
            foreach (var id in _clientIDToPlayerId.Keys)
            {
                T? data = GetPlayerData(id);
                if (data is { IsConnected: false })
                {
                    // This player has disconnected.
                    idsToClear.Add(id);
                }
            }

            // Remove all disconnected players.
            foreach(ulong id in idsToClear)
            {
                string playerId = _clientIDToPlayerId[id];
                T? playerData = GetPlayerData(playerId);
                if (playerData != null && playerData.Value.ClientID == id)
                {
                    // The disconnected player has valid data. Remove their data.
                    _clientData.Remove(playerId);
                }

                // Remove the ID connection.
                _clientIDToPlayerId.Remove(id);
            }
        }



        public class PlayerConnectionEventArgs : System.EventArgs
        {
            public readonly ulong ClientId;
            public readonly bool IsReconnect;
            public readonly T SessionPlayerData;


            private PlayerConnectionEventArgs() { }
            public PlayerConnectionEventArgs(ulong clientId, bool isReconnect, T sessionPlayerData)
            {
                this.ClientId = clientId;
                this.IsReconnect = isReconnect;
                this.SessionPlayerData = sessionPlayerData;
            }
        }
    }
}