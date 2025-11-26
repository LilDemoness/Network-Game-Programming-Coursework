using System.Collections.Generic;
using Unity.Services.Multiplayer;

namespace UnityServices.Sessions
{
    /// <summary>
    ///     Data for a local session user instance.<br/>
    ///     This will update data and is observed to know when to push local user changes to the entire session.
    /// </summary>
    [System.Serializable]
    public class LocalSessionUser
    {
        public struct UserData
        {
            public bool IsHost { get; set; }
            public string DisplayName { get; set; }
            public string ID { get; set; }

            public UserData(bool isHost, string displayName, string id)
            {
                this.IsHost = isHost;
                this.DisplayName = displayName;
                this.ID = id;
            }
        }

        private UserData _userData;

        public event System.Action<LocalSessionUser> changed;

        public LocalSessionUser()
        {
            _userData = new UserData(isHost: false, displayName: null, id: null);
        }

        public void ResetState()
        {
            _userData = new UserData(false, _userData.DisplayName, _userData.ID);
        }


        /// <summary>
        ///     Used for limiting costly OnChanged actions to just the members which actually changed.
        /// </summary>
        [System.Flags]
        public enum UserMembers
        {
            IsHost = 1 << 0,
            DisplayName = 1 << 1,
            ID = 1 << 2,
        }
        private UserMembers _lastChanged;


        public bool IsHost
        {
            get => _userData.IsHost;
            set
            {
                if (_userData.IsHost == value)
                    return; // Data already matches.

                _userData.IsHost = value;
                _lastChanged = UserMembers.IsHost;
                OnChanged();
            }
        }
        public string DisplayName
        {
            get => _userData.DisplayName;
            set
            {
                if (_userData.DisplayName == value)
                    return; // Data already matches.

                _userData.DisplayName = value;
                _lastChanged = UserMembers.DisplayName;
                OnChanged();
            }
        }
        public string ID
        {
            get => _userData.ID;
            set
            {
                if (_userData.ID == value)
                    return; // Data already matches.

                _userData.ID = value;
                _lastChanged = UserMembers.ID;
                OnChanged();
            }
        }


        public void CopyDataFrom(LocalSessionUser session)
        {
            UserData data = session._userData;
            int lastChanged =   // Set the flags for the members that will be changed.
                (_userData.IsHost == data.IsHost ? 0 : (int)UserMembers.IsHost) |
                (_userData.DisplayName == data.DisplayName ? 0 : (int)UserMembers.DisplayName) |
                (_userData.ID == data.ID ? 0 : (int)UserMembers.ID);

            if (lastChanged == 0)   // Ensure we've actually changed anything.
            {
                return;
            }

            _userData = data;
            _lastChanged = (UserMembers)lastChanged;

            OnChanged();
        }

        /// <summary>
        ///     Notify listeners of a change in data.
        /// </summary>
        private void OnChanged() => changed?.Invoke(this);


        public Dictionary<string, PlayerProperty> GetDataForUnityServices() =>
            new()
            {
                { "DisplayName", new PlayerProperty(DisplayName, VisibilityPropertyOptions.Member) },
            };
    }
}