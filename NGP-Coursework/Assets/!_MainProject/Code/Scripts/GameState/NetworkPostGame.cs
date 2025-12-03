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
    ///     Synced data & RPCs for the Post-Game State
    /// </summary>
    public class NetworkPostGame : NetworkBehaviour
    {
        public NetworkVariable<bool> UseTeams = new NetworkVariable<bool>();
        public NetworkList<PostGameData> PostGameData = new NetworkList<PostGameData>();

        private bool _hasSet = false;
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
            UseTeams.Value = _persistentGameState.UseTeams;
            for (int i = 0; i < _persistentGameState.GameData.Length; i++)
            {
                PostGameData.Add(_persistentGameState.GameData[i]);
            }

            _hasSet = true;
            NotifyClientsOfScoresSetRpc();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && _persistentGameState != null)
                SetValues();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void NotifyClientsOfScoresSetRpc() => OnScoresSet?.Invoke();
    }
}