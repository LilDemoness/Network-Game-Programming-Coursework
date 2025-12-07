using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Common data and RPCs for the FFA Gameplay state.
    /// </summary>
    public class NetworkFFAGameplayState : NetworkGameplayState
    {
        private List<ServerPlayerGameData> _serverPlayerData;
        private Dictionary<ServerCharacter, ServerPlayerGameData> _characterToDataIndex = new();

        public NetworkList<PlayerGameData> PlayerData { get; private set; } = new NetworkList<PlayerGameData>(); // Also includes players that have left the game? OR have score in SessionPlayerData (But what about states that don't use scores?)?
        public List<int> SortedDataIndicies = new List<int>();
        
        private Dictionary<int, PlayerGameData> _playerIndexToDataDict = new Dictionary<int, PlayerGameData>();
        public PlayerGameData GetPlayerData(ulong clientId) => PlayerData[GetPlayerIndex(clientId)];

        public int GetActualDataCount() => SortedDataIndicies.Count;
        public PlayerGameData GetSortedData(int position) => PlayerData[SortedDataIndicies[position]];


        [Inject]
        private void Initialise(ServerFreeForAllState serverFreeForAllState)
        {
            PlayerGameData.DeathsCountAsLostPoints = serverFreeForAllState.DeathsCountAsLostPoints;
        }


        public override void OnNetworkSpawn()
        {
            PlayerData.OnListChanged += OnPlayerDataChanged;
            if (PlayerData.Count > 0)
            {
                // The NetworkList is already populated, meaning that we are likely a rejoining client.
                // Reinitialise the SortedDataIndicies Array as the NetworkList was populated before our 'OnListChanged' subscription.
                ReinitialiseSortedArray();
            }
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
                PlayerData.Add(new PlayerGameData(playerIndex, playerCharacter.CharacterName));
                // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.

                // Create the server data.
                ServerPlayerGameData data = ServerPlayerGameData.NewDataForPlayer(playerCharacter, PlayerData.Count - 1);

                // Cache the server data.
                _serverPlayerData.Add(data);
                _characterToDataIndex.Add(playerCharacter, data);
            }

        }
        // Server-only.
        public override void AddNPC(ServerCharacter npcCharacter)
        {
            PlayerData.Add(new PlayerGameData(-1, npcCharacter.CharacterName));
            // Note: Adding to the index->data dictionary is handed through the 'OnListChanged' event subscription.

            // Create the server data.
            ServerPlayerGameData data = ServerPlayerGameData.NewDataForNPC(npcCharacter, PlayerData.Count - 1);

            // Cache the server data.
            _serverPlayerData.Add(data);
            _characterToDataIndex.Add(npcCharacter, data);
        }


        // Server-only.
        public override void OnPlayerLeft(ulong clientId)
        {
            StartCoroutine(RemoveNullKeysAfterFrame());

            for (int i = 0; i < _serverPlayerData.Count; ++i)
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
        private IEnumerator RemoveNullKeysAfterFrame()
        {
            yield return null;  // Wait a frame to allow the ServerCharacter to be destroyed and its reference set to null.

            foreach(var kvp in _characterToDataIndex)
                if (kvp.Key == null)
                {
                    PlayerGameData data = PlayerData[kvp.Value.PlayerGameDataListIndex];
                    data.IsInGame = false;
                    PlayerData[kvp.Value.PlayerGameDataListIndex] = data;
                }

            _characterToDataIndex.RemoveNullKeys();
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
                _characterToDataIndex.Add(newServerCharacter, _serverPlayerData[i]);

                PlayerGameData data = PlayerData[_serverPlayerData[i].PlayerGameDataListIndex];
                data.IsInGame = true;
                PlayerData[_serverPlayerData[i].PlayerGameDataListIndex] = data;
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

                        // Add to and then re-sort our sorted indicies list.
                        SortedDataIndicies.Add(changeEvent.Index);
                        SortUp(SortedDataIndicies.Count - 1);
                        break;
                    }

                // Entry Changed.
                case NetworkListEvent<PlayerGameData>.EventType.Value:
                    {
                        PlayerGameData playerData = changeEvent.Value;
                        playerData.ListIndex = changeEvent.Index;  // Allows for easier retrieval when editing (Mainly on the Server).

                        if (changeEvent.Value.PlayerIndex != -1)
                            _playerIndexToDataDict[changeEvent.Value.PlayerIndex] = playerData;

                        if (!changeEvent.Value.IsInGame)
                        {
                            // This entity is no longer in the game. Don't include them in our sorted data.
                            SortedDataIndicies.Remove(changeEvent.Index);
                        }
                        else if (!changeEvent.PreviousValue.IsInGame && !SortedDataIndicies.Contains(changeEvent.Index))
                        //else if (!SortedDataIndicies.Contains(changeEvent.Index))
                        {
                            // This entity wasn't in the game, but now it is (Note: Using a 'Contains' check to ensure that we don't duplicate this client's entry).
                            // Re-add them to the Sorted Data.
                            SortedDataIndicies.Add(changeEvent.Index);
                            SortUp(SortedDataIndicies.Count - 1);
                        } else
                        {
                            // The entity was in the game and still is, they just changed another piece of data.
                            // Re-sort our sorted indicies list.
                            SortUp(changeEvent.Index);
                        }
                        
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

                        // As every value may have changed, we should fully re-create and sort our sorted indicies list.
                        ReinitialiseSortedArray();
                        break;
                    }
            }
        }


        private void ReinitialiseSortedArray()
        {
            SortedDataIndicies = new List<int>(PlayerData.Count);
            for (int i = 0; i < PlayerData.Count; ++i)
                SortedDataIndicies.Add(i);

            SortFullSortedArray();
        }
        private void SortFullSortedArray()
        {
            // BubbleSort.
            bool performedSwap;
            do
            {
                performedSwap = false;
                for (int i = 1; i < PlayerData.Count; ++i)
                {
                    if (FirstExceedsSecond(i, i - 1))
                    {
                        // Swap the two indicies.
                        SwapIndicies(i, i - 1);
                        performedSwap = true;
                    }
                }
            } while (performedSwap);
        }
        private void SortUp(int changedIndex)
        {
            for(int i = changedIndex; i >= 1; --i)
            {
                if (FirstExceedsSecond(i, i - 1))
                {
                    Debug.Log($"Sort: {PlayerData[SortedDataIndicies[i]].PlayerIndex} | {PlayerData[SortedDataIndicies[i - 1]].PlayerIndex}");
                    SwapIndicies(i, i - 1);
                }
            }
        }
        private bool FirstExceedsSecond(int firstIndex, int secondIndex) => PlayerData[SortedDataIndicies[firstIndex]].Score > PlayerData[SortedDataIndicies[secondIndex]].Score;
        private void SwapIndicies(int firstIndex, int secondIndex)
        {
            int tempIndex = SortedDataIndicies[firstIndex];
            SortedDataIndicies[firstIndex] = SortedDataIndicies[secondIndex];
            SortedDataIndicies[secondIndex] = tempIndex;
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
            // Find our character's PlayerData NetworkList index.
            int listIndex = _characterToDataIndex[serverCharacter].PlayerGameDataListIndex;

            // Retrieve our PlayerGameData (Required as it's a struct, so we need to set externally then re-add to the list).
            PlayerGameData data = PlayerData[listIndex];

            // Increment Score.
            data.Kills += 1;

            // Apply our changes.
            PlayerData[listIndex] = data;
            Debug.Log($"Player '{serverCharacter.CharacterName}' - New Kills: {PlayerData[listIndex].Kills}");
        }
        public void OnCharacterDied(ServerCharacter serverCharacter)
        {
            // Find our character's PlayerData NetworkList index.
            int listIndex = _characterToDataIndex[serverCharacter].PlayerGameDataListIndex;

            // Retrieve our PlayerGameData (Required as it's a struct, so we need to set externally then re-add to the list).
            PlayerGameData data = PlayerData[listIndex];

            // Increment our Deaths.
            data.Deaths += 1;

            // Apply our changes.
            PlayerData[listIndex] = data;
            Debug.Log($"Player '{serverCharacter.CharacterName}' - New Deaths: {PlayerData[listIndex].Deaths}");
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
                kills:              playerGameData.Kills,
                deaths:             playerGameData.Deaths,
                name:               ServerCharacter.FixedCharacterName,
                frameIndex:         ServerCharacter.BuildDataReference.ActiveFrameIndex,
                slottableIndicies:  ServerCharacter.BuildDataReference.ActiveSlottableIndicies
            );
        }
        public struct PlayerGameData : INetworkSerializable, IEquatable<PlayerGameData>
        {
            public static bool DeathsCountAsLostPoints { get; set; }

            public int PlayerIndex;
            public FixedPlayerName Name;
            public bool IsInGame;

            public int Score => DeathsCountAsLostPoints ? (Kills - Deaths) : Kills;
            public int Kills;
            public int Deaths;

            [field: System.NonSerialized] public int ListIndex { get; set; } // A non-serialized, non-synced int representing this data's position in the PlayerData array. Used on the server for easier retrieval of data.


            public PlayerGameData(int playerIndex, FixedPlayerName name) : this(playerIndex, name, -1) { }
            public PlayerGameData(int playerIndex, FixedPlayerName name, int listIndex)
            {
                this.PlayerIndex = playerIndex;
                this.Name = name;
                this.IsInGame = true;

                this.Kills = 0;
                this.Deaths = 0;

                this.ListIndex = listIndex;
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PlayerIndex);
                serializer.SerializeValue(ref Name);
                serializer.SerializeValue(ref IsInGame);
                serializer.SerializeValue(ref Kills);
                serializer.SerializeValue(ref Deaths);
            }
            public bool Equals(PlayerGameData othr)
                => (this.PlayerIndex, this.Name, this.IsInGame, this.Kills, this.Deaths)
                == (othr.PlayerIndex, othr.Name, othr.IsInGame, othr.Kills, othr.Deaths);
        }
    }
}