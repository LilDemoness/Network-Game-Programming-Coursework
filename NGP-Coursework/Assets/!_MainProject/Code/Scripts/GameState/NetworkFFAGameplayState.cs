using System;
using System.Collections.Generic;
using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Common data and RPCs for the FFA Gameplay state.
    /// </summary>
    public class NetworkFFAGameplayState : NetworkGameplayState
    {
        private List<ServerPlayerGameData> _serverPlayerData;

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


        // Server-only.
        public override void Initialise(ServerCharacter[] playerCharacters, ServerCharacter[] npcCharacters)
        {
            _serverPlayerData = new List<ServerPlayerGameData>();

            // Add Players.
            for (int i = 0; i < playerCharacters.Length; ++i)
                AddPlayer(playerCharacters[i]);

            // Add NPCs.
            if (npcCharacters != null)
                for (int i = 0; i < npcCharacters.Length; ++i)
                    AddNPC(npcCharacters[i]);
        }
        // Server-only.
        public override void AddPlayer(ServerCharacter playerCharacter)
        {
            int playerIndex = GetPlayerIndex(playerCharacter.OwnerClientId);
            if (_playerIndexToDataDict.ContainsKey(playerIndex))
            {
                // Rejoining Player.
                OnPlayerReconnected(playerIndex, playerCharacter);
            }
            else
            {
                // New Player.
                PlayerData.Add(new PlayerGameData(playerIndex));
                // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.

                _serverPlayerData.Add(ServerPlayerGameData.NewDataForPlayer(playerCharacter, PlayerData.Count - 1));
            }

        }
        // Server-only.
        public override void AddNPC(ServerCharacter npcCharacter)
        {
            PlayerData.Add(new PlayerGameData(-1));
            // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.

            _serverPlayerData.Add(ServerPlayerGameData.NewDataForNPC(npcCharacter, PlayerData.Count - 1));
        }


        // Server-only.
        public override void OnPlayerLeft(ulong clientId)
        {
            for(int i = 0; i < _serverPlayerData.Count; ++i)
            {
                if (!_serverPlayerData[i].IsPlayer)
                    continue;   // Not a player, so can't have been disconnected.
                if (!_serverPlayerData[i].IsConnected)
                    continue;   // Disconnected player.
                if (_serverPlayerData[i].ClientId != clientId)
                    continue;   // Incorrect player.

                _serverPlayerData[i].OnCorrespondingPlayerLeft();
                return;
            }
        }
        // Server-only.
        public override void OnPlayerReconnected(int playerIndex, ServerCharacter newServerCharacter)
        {
            // Note: We need to use PlayerIndex rather than ClientId as the PlayerIndex doesn't change between connections.
            for(int i = 0; i < _serverPlayerData.Count; ++i)
            {
                if (!_serverPlayerData[i].IsPlayer)
                    continue;   // Not a player, so can't have been disconnected & reconnected.
                if (PlayerData[_serverPlayerData[i].PlayerGameDataListIndex].PlayerIndex != playerIndex)
                    continue;   // Incorrect player.

                // We found the desired player. Update their cached data where neccessary.
                _serverPlayerData[i].OnCorrepsondingPlayerRejoined(newServerCharacter);
                return;
            }
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

                        if (changeEvent.Value.PlayerIndex != -1)
                            _playerIndexToDataDict.Add(changeEvent.Value.PlayerIndex, playerData);
                        break;
                    }

                // Entry Changed.
                case NetworkListEvent<PlayerGameData>.EventType.Value:
                    {
                        PlayerGameData playerData = changeEvent.Value;
                        playerData.ListIndex = changeEvent.Index;  // Allows for easier retrieval when editing (Mainly on the Server).

                        if (changeEvent.Value.PlayerIndex != -1)
                            _playerIndexToDataDict[changeEvent.Value.PlayerIndex] = playerData;
                        break;
                    }

                // Removal.
                case NetworkListEvent<PlayerGameData>.EventType.Remove:
                case NetworkListEvent<PlayerGameData>.EventType.RemoveAt:
                    {
                        if (changeEvent.Value.PlayerIndex != -1)
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

                            if (playerData.PlayerIndex != -1)
                            {
                                if (!_playerIndexToDataDict.TryAdd(playerData.PlayerIndex, playerData))
                                {
                                    _playerIndexToDataDict[playerData.PlayerIndex] = playerData;
                                }
                            }
                        }
                        break;
                    }
            }
        }


        public override void SavePersistentData(ref PersistentGameState persistentGameState)
        {
            // Create the Data Container.
            FFAPersistentData persistentData = new();

            // Create & populate the PostGameData array.
            List<FFAPostGameData> postGameData = new List<FFAPostGameData>(PlayerData.Count);
            for(int i = 0; i < _serverPlayerData.Count; ++i)
            {
                if (_serverPlayerData[i].IsConnected)   // Only process connected players/npcs.
                    postGameData.Add(_serverPlayerData[i].ToPostGameData(PlayerData[_serverPlayerData[i].PlayerGameDataListIndex]));
            }
            
            // Set the PersistentData.
            persistentData.GameData = postGameData.ToArray();

            // Add the PersistentData to the PersistentGameState
            persistentGameState.SetContainer<FFAPersistentData>(persistentData);
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



        public class ServerPlayerGameData  // Contains additional information needed when passing to the PostGameState, but that doesn't need to be synced during the game.
        {
            public bool IsPlayer;
            public bool IsConnected;
            public ulong ClientId;   // Only used for players.

            public ServerCharacter ServerCharacter;
            public int PlayerGameDataListIndex;


            public static ServerPlayerGameData NewDataForPlayer(ServerCharacter serverCharacter, int playerGameDataListIndex) => new ServerPlayerGameData(true, serverCharacter.OwnerClientId, serverCharacter, playerGameDataListIndex);
            public static ServerPlayerGameData NewDataForNPC(ServerCharacter serverCharacter, int playerGameDataListIndex) => new ServerPlayerGameData(false, default, serverCharacter, playerGameDataListIndex);
            private ServerPlayerGameData() { }
            public ServerPlayerGameData(bool isPlayer, ulong clientId, ServerCharacter serverCharacter, int playerGameDataListIndex)
            {
                this.IsPlayer = isPlayer;
                this.IsConnected = true;
                this.ClientId = clientId;

                this.ServerCharacter = serverCharacter;
                this.PlayerGameDataListIndex = playerGameDataListIndex;
            }


            public void OnCorrespondingPlayerLeft() => IsConnected = false;
            public void OnCorrepsondingPlayerRejoined(ServerCharacter serverCharacter)
            {
                this.IsConnected = true;
                this.ClientId = serverCharacter.OwnerClientId;
                this.ServerCharacter = serverCharacter;
            }

            public FFAPostGameData ToPostGameData(PlayerGameData playerGameData) => new FFAPostGameData(
                playerIndex:        playerGameData.PlayerIndex,
                score:              playerGameData.Score,
                name:               ServerCharacter.FixedCharacterName,
                frameIndex:         ServerCharacter.BuildDataReference.ActiveFrameIndex,
                slottableIndicies:  ServerCharacter.BuildDataReference.ActiveSlottableIndicies
            );
        }
        public struct PlayerGameData : INetworkSerializable, IEquatable<PlayerGameData>
        {
            public int PlayerIndex;
            public int Score;

            [field: System.NonSerialized] public int ListIndex { get; set; } // A non-serialized, non-synced int representing this data's position in the PlayerData array. Used on the server for easier retrieval of data.


            public PlayerGameData(int playerIndex) : this(playerIndex, -1) { }
            public PlayerGameData(int playerIndex, int listIndex)
            {
                this.PlayerIndex = playerIndex;
                this.Score = 0;
                this.ListIndex = listIndex;
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PlayerIndex);
                serializer.SerializeValue(ref Score);
            }
            public bool Equals(PlayerGameData other) => (this.PlayerIndex, this.Score) == (other.PlayerIndex, other.Score);
        }
    }
}