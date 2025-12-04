using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Synced data & RPCs for the Post-Game State after a FFA match.
    /// </summary>
    public class NetworkPostFFAGame : NetworkBehaviour
    {
        [SerializeField] private PersistentPlayerRuntimeCollection _persistentPlayerCollection;

        public FFAPostGameData[] PostGameData;
        public int ThisClientDataIndex { get; set; }

        private PersistentGameState _persistentGameState;
        public event System.Action OnScoresSet;



        [Inject]
        private void Configure(PersistentGameState persistentGameState)
        {
            if (!NetworkManager.IsServer)
                return; // Only perform on the server.

            // Set Data based on persistent state data.
            _persistentGameState = persistentGameState;
            if (IsSpawned)
                SetValues();
        }

        private void SetValues()
        {
            // Get the data from the Persistent Game State.
            FFAPersistentData persistentData = _persistentGameState.GetContainer<FFAPersistentData>();

            // Organise the data based on scores.
            persistentData.GameData = persistentData.GameData.OrderByDescending(t => t.Kills).ToArray();

            // Send our sorted data to clients.
            NotifyClientsOfScoresSetRpc(persistentData.GameData, FFAPostGameData.CountDeathsAsNegativePoints);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && _persistentGameState != null)
                SetValues();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void NotifyClientsOfScoresSetRpc(FFAPostGameData[] data, bool deathsCountAsPenalty)
        {
            PostGameData = data;
            FFAPostGameData.CountDeathsAsNegativePoints = deathsCountAsPenalty;

            if (!_persistentPlayerCollection.TryGetPlayer(NetworkManager.Singleton.LocalClientId, out PersistentPlayer persistentPlayer))
                throw new System.Exception($"No PersistentPlayer found for this Client (Id: {NetworkManager.Singleton.LocalClientId})");

            int playerIndex = persistentPlayer.PlayerNumber;
            ThisClientDataIndex = -1;
            for(int i = 0; i < PostGameData.Length; ++i)
            {
                if (PostGameData[i].PlayerIndex == playerIndex)
                {
                    ThisClientDataIndex = i;
                    break;
                }
            }
            if (ThisClientDataIndex == -1)
                throw new System.Exception($"No PostGameData found for this Client (Id: {NetworkManager.Singleton.LocalClientId})");

            OnScoresSet?.Invoke();
        }
    }
}