using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Netcode.ConnectionManagement
{
    public struct SessionPlayerData : ISessionPlayerData
    {
        public string PlayerName { get; set; }
        public int PlayerNumber { get; set; }
        public int TeamIndex { get; set; }

        public Vector3 PlayerPosition { get; set; }
        public Quaternion PlayerRotation { get; set; }

        public BuildData BuildData { get; set; }

        public float CurrentHealth { get; set; }
        public bool HasCharacterSpawned { get; set; }


        public SessionPlayerData(ulong clientID, string name, BuildData buildData = default, float currentHealth = 0.0f, bool isConnected = false, bool hasCharacterSpawned = false)
        {
            this.ClientID = clientID;

            this.PlayerName = name;
            this.PlayerNumber = -1;
            this.TeamIndex = -1;

            this.PlayerPosition = Vector3.zero;
            this.PlayerRotation = Quaternion.identity;

            this.BuildData = buildData;
            this.CurrentHealth = currentHealth;
            this.IsConnected = isConnected;
            this.HasCharacterSpawned = hasCharacterSpawned;
        }


        #region Interface Implementation

        public bool IsConnected { get; set; }
        public ulong ClientID { get; set; }


        public void Reinitialise()
        {
            HasCharacterSpawned = false;
        }

        #endregion
    }
}