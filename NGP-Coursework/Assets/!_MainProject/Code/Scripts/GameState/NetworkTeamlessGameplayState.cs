using System;
using System.Collections.Generic;
using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Common data and RPCs for the Gameplay states that don't include Teams.
    /// </summary>
    public class NetworkTeamlessGameplayState : NetworkGameplayState
    {
        public NetworkList<PlayerGameData> PlayerData { get; private set; } = new NetworkList<PlayerGameData>(); // Also includes players that have left the game? OR have score in SessionPlayerData (But what about states that don't use scores?)?
        private Dictionary<int, PlayerGameData> _playerIndexToDataDict = new Dictionary<int, PlayerGameData>();
        public PlayerGameData GetPlayerData(ulong clientId) => PlayerData[GetPlayerIndex(clientId)];


        public override void OnNetworkSpawn()
        {
            PlayerData.OnListChanged += OnPlayerDataChanged;
        }
        public override void OnNetworkDespawn()
        {
            if (PlayerData != null)
                PlayerData.OnListChanged -= OnPlayerDataChanged;
        }


        public override void Initialise(ulong[] clientIds)
        {
            for (int i = 0; i < clientIds.Length; ++i)
            {
                int playerIndex = GetPlayerIndex(clientIds[i]);
                PlayerData.Add(new PlayerGameData(clientIds[i], playerIndex));
                // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.
            }
        }
        public override void AddPlayer(ulong clientId)
        {
            int playerIndex = GetPlayerIndex(clientId);
            PlayerData.Add(new PlayerGameData(clientId, playerIndex));
            // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.
        }


        /// <summary>
        ///     Called when the PlayerData NetworkList is changed.<br/>
        ///     Handles caching of values into the '_playerIndexToDataDict' Dictionary so that we can more easily retrieve and edit values for specific players.
        /// </summary>
        /// <param name="changeEvent"></param>
        private void OnPlayerDataChanged(NetworkListEvent<PlayerGameData> changeEvent)
        {
            Debug.Log($"Player Data Changed. Type: {changeEvent.Type}");

            // Update our cached value.
            switch (changeEvent.Type)
            {
                // New Entry.
                case NetworkListEvent<PlayerGameData>.EventType.Add:
                case NetworkListEvent<PlayerGameData>.EventType.Insert:
                    {
                        PlayerGameData playerData = changeEvent.Value;
                        playerData.ListIndex = changeEvent.Index;  // Allows for easier retrieval when editing (Mainly on the Server).
                        _playerIndexToDataDict.Add(changeEvent.Value.PlayerIndex, playerData);
                        break;
                    }

                // Entry Changed.
                case NetworkListEvent<PlayerGameData>.EventType.Value:
                    {
                        PlayerGameData playerData = changeEvent.Value;
                        playerData.ListIndex = changeEvent.Index;  // Allows for easier retrieval when editing (Mainly on the Server).
                        _playerIndexToDataDict[changeEvent.Value.PlayerIndex] = playerData;
                        break;
                    }

                // Removal.
                case NetworkListEvent<PlayerGameData>.EventType.Remove:
                case NetworkListEvent<PlayerGameData>.EventType.RemoveAt:
                    {
                        _playerIndexToDataDict.Remove(changeEvent.Value.PlayerIndex);
                        return;
                    }

                // Other.    
                case NetworkListEvent<PlayerGameData>.EventType.Clear:
                    {
                        _playerIndexToDataDict.Clear();
                        return;
                    }
                case NetworkListEvent<PlayerGameData>.EventType.Full:
                    {
                        for (int i = 0; i < PlayerData.Count; ++i)
                        {
                            PlayerGameData playerData = PlayerData[i];
                            playerData.ListIndex = i;  // Allows for easier retrieval when editing (Mainly on the Server).

                            if (!_playerIndexToDataDict.TryAdd(playerData.PlayerIndex, playerData))
                            {
                                _playerIndexToDataDict[playerData.PlayerIndex] = playerData;
                            }
                        }
                        break;
                    }
            }
        }


        public override void SavePersistentData(ref PersistentGameState persistentGameState)
        {
            // Create & populate the PostGameData array.
            PostGameData[] postGameData = new PostGameData[PlayerData.Count];
            for(int i = 0; i < PlayerData.Count; ++i)
                postGameData[i] = PlayerData[i].ToPostGameData();
            

            // Set the PostGameData.
            persistentGameState.GameData = postGameData;
            persistentGameState.UseTeams = false;
        }


        public override void IncrementScore(ServerCharacter serverCharacter)
        {
            if (serverCharacter.GetComponent<Gameplay.GameplayObjects.Players.Player>() == null)
                return; // We cannot currently increase the score of non-players.
            

            // Increment the score of the corresponding player data.
            // Retrieve the character's PlayerID.
            int playerIndex = GetPlayerIndex(serverCharacter.OwnerClientId);

            // Retrieve the Team Data for editing.
            int index = GetListIndexForPlayerIndex(playerIndex);
            PlayerGameData playerData = PlayerData[index];

            // Increment Score.
            playerData.Score += 1;

            // Apply our changes.
            PlayerData[index] = playerData;
            Debug.Log($"Player '{serverCharacter.CharacterName}' - New Score: {PlayerData[index].Score}");
        }
        /// <summary>
        ///     Try to get the PlayerData index of the PlayerGameData with the passed PlayerIndex.
        /// </summary>
        private int GetListIndexForPlayerIndex(int playerIndex)
        {
            if (_playerIndexToDataDict.ContainsKey(playerIndex))
            {
                int index = _playerIndexToDataDict[playerIndex].ListIndex;

                if (index == -1)
                    throw new System.Exception($"The Cached Player with Index {playerIndex} has an invalid ListPosition value");

                return index;
            }

            throw new System.Exception($"No Player Cached with Index {playerIndex}");
        }



        public struct PlayerGameData : INetworkSerializable, IEquatable<PlayerGameData>
        {
            public ulong ClientId;
            public int PlayerIndex;
            public int Score;

            [System.NonSerialized] public int ListIndex; // A non-serialized, non-synced int representing this data's position in the PlayerData array. Used on the server for easier retrieval of data.
            //[System.NonSerialized] public ServerCharacter Character;


            public PlayerGameData(ulong clientId, int playerIndex) : this(clientId, playerIndex, -1) { }
            public PlayerGameData(ulong clientId, int playerIndex, int listIndex)
            {
                this.ClientId = clientId;
                this.PlayerIndex = playerIndex;
                this.Score = 0;
                this.ListIndex = listIndex;
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref PlayerIndex);
                serializer.SerializeValue(ref Score);
            }
            public bool Equals(PlayerGameData other) => (this.ClientId, this.PlayerIndex, this.Score) == (other.ClientId, other.PlayerIndex, other.Score);


            public PostGameData ToPostGameData() => new PostGameData()
            {
                Index = PlayerIndex,
                Score = Score,
            };
        }
    }
}