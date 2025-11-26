using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Multiplayer;

namespace UnityServices.Sessions
{
    /// <summary>
    ///     A local wrapper around a session's remote data, with additional functionality
    ///     for providing that data to UI elements and tracking local player objects.
    /// </summary>
    [System.Serializable]
    public sealed class LocalSession
    {
        private Dictionary<string, LocalSessionUser> _sessionUsers = new();
        public Dictionary<string, LocalSessionUser> SessionUsers => _sessionUsers;

        private SessionData _data;


        public event System.Action<LocalSession> changed;

        public string SessionID
        {
            get => _data.SessionID;
            set
            {
                _data.SessionID = value;
                OnChanged();
            }
        }
        public string SessionCode
        {
            get => _data.SessionCode;
            set
            {
                _data.SessionCode = value;
                OnChanged();
            }
        }
        public string RelayJoinCode
        {
            get => _data.RelayJoinCode;
            set
            {
                _data.RelayJoinCode = value;
                OnChanged();
            }
        }


        public struct SessionData
        {
            public string SessionID { get; set; }
            public string SessionCode { get; set; }
            public string RelayJoinCode { get; set; }
            public string SessionName { get; set; }
            public bool IsPrivate { get; set; }
            public int MaxPlayerCount { get; set; }


            public SessionData(SessionData existing)
            {
                SessionID = existing.SessionID;
                SessionCode = existing.SessionCode;
                RelayJoinCode = existing.RelayJoinCode;
                SessionName = existing.SessionName;
                IsPrivate = existing.IsPrivate;
                MaxPlayerCount = existing.MaxPlayerCount;
            }

            public SessionData(string sessionCode)
            {
                SessionID = null;
                SessionCode = sessionCode;
                RelayJoinCode = null;
                SessionName = null;
                IsPrivate = false;
                MaxPlayerCount = -1;
            }
        }


        /// <summary>
        ///     Add a user to the session if they aren't already within.
        /// </summary>
        public void AddUser(LocalSessionUser user)
        {
            if (_sessionUsers.ContainsKey(user.ID))
                return; // The user is already in the game.
            
            DoAddUser(user);
            OnChanged();
        }
        /// <summary>
        ///     Add a user to the session.<br/>
        ///     Doesn't notify listeners of the change.
        /// </summary>
        private void DoAddUser(LocalSessionUser user)
        {
            _sessionUsers.Add(user.ID, user);
            user.changed += OnChangedUser;
        }

        /// <summary>
        ///     Remove a user from the session.
        /// </summary>
        public void RemoveUser(LocalSessionUser user)
        {
            DoRemoveUser(user);
            OnChanged();
        }
        /// <summary>
        ///     Perform user existance check & remove them if they exist.<br/>
        ///     Doesn't notify listeners of the change.
        /// </summary>
        private void DoRemoveUser(LocalSessionUser user)
        {
            if (!_sessionUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player '{user.DisplayName}({user.ID})' does not exist in session: {SessionID}");
                return;
            }

            _sessionUsers.Remove(user.ID);
            user.changed -= OnChangedUser;
        }

        private void OnChangedUser(LocalSessionUser user) => OnChanged();
        /// <summary>
        ///     Notify listeners that we have been changed.
        /// </summary>
        private void OnChanged() => changed?.Invoke(this);


        /// <summary>
        ///     Sets our data to match the passed data.
        /// </summary>
        public void CopyDataFrom(SessionData data, Dictionary<string, LocalSessionUser> currentUsers)
        {
            _data = data;

            if (currentUsers == null)
            {
                _sessionUsers = new Dictionary<string, LocalSessionUser>();
            }
            else
            {
                // Remove all users no longer in the session.
                List<LocalSessionUser> toRemove = new List<LocalSessionUser>();
                foreach(var oldUser in _sessionUsers)
                {
                    if (currentUsers.ContainsKey(oldUser.Key))
                    {
                        oldUser.Value.CopyDataFrom(currentUsers[oldUser.Key]);
                    }
                    else
                    {
                        toRemove.Add(oldUser.Value);
                    }
                }
                // Perform the user removal.
                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                // Add in the new users.
                foreach(var currentUser in currentUsers)
                {
                    if (!_sessionUsers.ContainsKey(currentUser.Key))
                    {
                        // The user is new to the session. Add them.
                        DoAddUser(currentUser.Value);
                    }
                }
            }

            // Notify listeners of our change.
            OnChanged();
        }


        public Dictionary<string, SessionProperty> GetDataForUnityServices() =>
            new()
            {
                { "RelayJoinCode", new SessionProperty(RelayJoinCode) }
            };

        public void ApplyRemoteData(ISession session)
        {
            // Setup our SessionData.
            SessionData info = new SessionData();   // Technically this is largely redundant after the first assignment, but it doesn't cause any harm to assign it again.
            info.SessionID = session.Id;
            info.SessionName = session.Name;
            info.MaxPlayerCount = session.MaxPlayers;
            info.SessionCode = session.Code;
            info.IsPrivate = session.IsPrivate;

            if (session.Properties != null)
            {
                // By providing the RelayCode through the session's properties with member visibility, we ensure that a client is
                //  connected to the session before they can attempt a relay connection, preventing issues between them.
                info.RelayJoinCode = session.Properties.TryGetValue("RelayJoinCode", out var property) ? property.Value : null;
            }
            else
            {
                info.RelayJoinCode = null;
            }

            Dictionary<string, LocalSessionUser> localSessionUsers = new Dictionary<string, LocalSessionUser>();
            foreach(var player in session.Players)
            {
                if (player.Properties != null)
                {
                    if (localSessionUsers.ContainsKey(player.Id))
                    {
                        localSessionUsers.Add(player.Id, localSessionUsers[player.Id]);
                        continue;
                    }
                }

                // If the player isn't connected to Relay, get the most recent data that the session knows.
                //  (If we haven't seen this player yet, a new local representation of the player will already have been added by the LocalSession).
                LocalSessionUser incomingData = new LocalSessionUser
                {
                    IsHost = session.Host.Equals(player.Id),
                    DisplayName = player.Properties != null && player.Properties.TryGetValue("DisplayName", out var property) ? property.Value : default,
                    ID = player.Id,
                };

                localSessionUsers.Add(incomingData.ID, incomingData);
            }

            CopyDataFrom(info, localSessionUsers);
        }

        /// <summary>
        ///     Reset the data of the passed <see cref="LocalSessionUser"/>.
        /// </summary>
        public void Reset(LocalSessionUser localUser)
        {
            CopyDataFrom(new SessionData(), new Dictionary<string, LocalSessionUser>());
            AddUser(localUser);
        }
    }
}