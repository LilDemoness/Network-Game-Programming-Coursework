using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Netcode.ConnectionManagement
{
    public struct SessionPlayerData : ISessionPlayerData
    {
        public string PlayerName;
        public int PlayerNumber;

        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;

        public BuildDataState BuildData;
        public float CurrentHealth;
        public bool HasCharacterSpawned;


        public SessionPlayerData(ulong clientID, string name, BuildDataState buildData = default, float currentHealth = 0.0f, bool isConnected = false, bool hasCharacterSpawned = false)
        {
            this.ClientID = clientID;

            this.PlayerName = name;
            this.PlayerNumber = -1;

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