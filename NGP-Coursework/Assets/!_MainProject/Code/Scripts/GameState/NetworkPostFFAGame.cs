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
        public FFAPostGameData[] PostGameData;

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
            FFAPersistentData persistentData = _persistentGameState.GetContainer<FFAPersistentData>();
            NotifyClientsOfScoresSetRpc(persistentData.GameData);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && _persistentGameState != null)
                SetValues();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void NotifyClientsOfScoresSetRpc(FFAPostGameData[] data)
        {
            PostGameData = data;
            OnScoresSet?.Invoke();
        }
    }
}