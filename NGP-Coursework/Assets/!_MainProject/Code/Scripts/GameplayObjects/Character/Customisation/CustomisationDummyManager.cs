using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    public class CustomisationDummyManager : MonoBehaviour
    {
        [Header("Player Lobby GFX Instances")]
        [SerializeField] private PlayerCustomisationDisplay _playerLobbyPrefab;
        private Dictionary<ulong, PlayerCustomisationDisplay> _playerLobbyInstances;

        [SerializeField] private LobbySpawnPositions[] _playerLobbyGFXSpawnPositions; // Replace with spawning in a circle?
        [System.Serializable]
        public class LobbySpawnPositions
        {
            public Transform SpawnPosition;
            public bool IsOccupied;
            public ulong OccupyingClientID;
        }


        private void Awake()
        {
            _playerLobbyInstances = new Dictionary<ulong, PlayerCustomisationDisplay>();

            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged += HandlePlayerStateChanged;
            PlayerCustomisationManager.OnPlayerDisconnected += HandlePlayerDisconnected;
        }
        private void OnDestroy()
        {
            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged -= HandlePlayerStateChanged;
            PlayerCustomisationManager.OnPlayerDisconnected -= HandlePlayerDisconnected;
        }


        private void HandlePlayerStateChanged(ulong clientID, BuildData buildData)
        {
            if (_playerLobbyInstances.ContainsKey(clientID))
            {
                Debug.Log("Contains Key");
            }
            else
            {
                AddPlayerInstance(clientID, buildData);
            }
        }
        private void HandlePlayerDisconnected(ulong clientID) => RemovePlayerInstance(clientID);


        private void AddPlayerInstance(ulong clientIDToAdd, BuildData initialBuild)
        {
            // Get our desired spawn position.
            LobbySpawnPositions lobbySpawnPosition = null;
            if (clientIDToAdd == NetworkManager.Singleton.LocalClientId)
            {
                // We are adding the local client, so we want to put them at spawn position 0.
                lobbySpawnPosition = _playerLobbyGFXSpawnPositions[0];
            }
            else
            {
                // We are not adding the local client, so put them in the first available spawn position.
                for (int i = 1; i < _playerLobbyGFXSpawnPositions.Length; ++i)
                {
                    if (!_playerLobbyGFXSpawnPositions[i].IsOccupied)
                    {
                        // This spawn position is unoccupied. Spawn the other client here.
                        lobbySpawnPosition = _playerLobbyGFXSpawnPositions[i];
                        break;
                    }
                }

                if (lobbySpawnPosition == null)
                    throw new System.Exception("More players have tried to join that there are spawn positions");
            }

            // Mark the spawn position as occupied.
            lobbySpawnPosition.IsOccupied = true;
            lobbySpawnPosition.OccupyingClientID = clientIDToAdd;


            // Add the client's GFX Instance (Updated here for the first time only as the CustomisationDisplay is created after the event call is triggered, and so doesn't receive it otherwise).
            PlayerCustomisationDisplay clientGFXInstance = Instantiate<PlayerCustomisationDisplay>(_playerLobbyPrefab, lobbySpawnPosition.SpawnPosition, worldPositionStays: false);
            clientGFXInstance.Setup(clientIDToAdd, initialBuild);
            _playerLobbyInstances.Add(clientIDToAdd, clientGFXInstance);
        }
        private void RemovePlayerInstance(ulong clientIDToRemove)
        {
            // Allow this client's lobby spawn position can be reused.
            for (int i = 0; i < _playerLobbyGFXSpawnPositions.Length; ++i)
            {
                if (_playerLobbyGFXSpawnPositions[i].OccupyingClientID == clientIDToRemove)
                {
                    _playerLobbyGFXSpawnPositions[i].IsOccupied = false;
                    _playerLobbyGFXSpawnPositions[i].OccupyingClientID = default;
                }
            }

            // Remove the GFX Instance.
            if (_playerLobbyInstances.Remove(clientIDToRemove, out PlayerCustomisationDisplay customisationInstance))
            {
                Destroy(customisationInstance.gameObject);
            }
        }
    }
}