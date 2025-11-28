using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Netcode.ConnectionManagement;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    public class CustomisationDummyManager : MonoBehaviour
    {
        [Header("Player Lobby GFX Instances")]
        [SerializeField] private PlayerCustomisationDisplay _playerDummyPrefab;
        private Dictionary<ulong, PlayerCustomisationDisplay> _playerDummyInstances;

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
            _playerDummyInstances = new Dictionary<ulong, PlayerCustomisationDisplay>();

            SessionManager<SessionPlayerData>.OnClientConnected += SessionManager_OnClientConnected;
            SessionManager<SessionPlayerData>.OnClientDisconnect += SessionManager_OnClientDisconnect;
        }
        private void OnDestroy()
        {
            SessionManager<SessionPlayerData>.OnClientConnected -= SessionManager_OnClientConnected;
            SessionManager<SessionPlayerData>.OnClientDisconnect -= SessionManager_OnClientDisconnect;
        }


        private void SessionManager_OnClientConnected(object sender, SessionManager<SessionPlayerData>.PlayerConnectionEventArgs args) => HandlePlayerConnected(args.ClientId, new BuildDataReference(args.SessionPlayerData.BuildData));
        private void SessionManager_OnClientDisconnect(ulong clientId) => HandlePlayerDisconnected(clientId);

        private void HandlePlayerConnected(ulong clientId, BuildDataReference initialBuild)
        {
            if (_playerDummyInstances.ContainsKey(clientId))
            {
                Debug.Log("Contains Key");
            }
            else
            {
                AddPlayerInstance(clientId, initialBuild);
            }
        }
        private void HandlePlayerDisconnected(ulong clientId) => RemovePlayerInstance(clientId);


        public void AddNewPlayer(ulong clientId) => AddPlayerInstance(clientId, new BuildDataReference());
        private void AddPlayerInstance(ulong clientIDToAdd, BuildDataReference initialBuild)
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
            PlayerCustomisationDisplay clientGFXInstance = Instantiate<PlayerCustomisationDisplay>(_playerDummyPrefab, lobbySpawnPosition.SpawnPosition, worldPositionStays: false);
            clientGFXInstance.Setup(clientIDToAdd, initialBuild);
            _playerDummyInstances.Add(clientIDToAdd, clientGFXInstance);
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
            if (_playerDummyInstances.Remove(clientIDToRemove, out PlayerCustomisationDisplay customisationInstance))
            {
                Destroy(customisationInstance.gameObject);
            }
        }


        public void UpdateCustomisationDummy(ulong clientId, BuildDataReference buildData)
        {
            if (_playerDummyInstances.TryGetValue(clientId, out PlayerCustomisationDisplay playerCustomisationDisplay))
            {
                playerCustomisationDisplay.UpdateDummy(buildData);
            }
        }
    }
}