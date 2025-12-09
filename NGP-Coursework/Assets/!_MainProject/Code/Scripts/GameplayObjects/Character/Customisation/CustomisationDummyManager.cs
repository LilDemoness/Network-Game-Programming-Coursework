using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Netcode.ConnectionManagement;
using Gameplay.GameplayObjects.Players;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    public class CustomisationDummyManager : MonoBehaviour
    {
        [SerializeField] private PersistentPlayerRuntimeCollection _persistentPlayerRuntimeCollection;


        [Header("Player Lobby GFX Instances")]
        [SerializeField] private PlayerCustomisationDisplay _playerDummyPrefab;
        private Dictionary<ulong, PlayerCustomisationDisplay> _playerDummyInstances = new Dictionary<ulong, PlayerCustomisationDisplay>();

        [Space(5)]
        [SerializeField] private bool _onlyShowLocalClient = false;
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
            _persistentPlayerRuntimeCollection.ItemAdded += PersistentPlayerCollection_ItemAdded;
            _persistentPlayerRuntimeCollection.ItemRemoved += PersistentPlayerCollection_Removed;

            foreach (PersistentPlayer persistentPlayer in _persistentPlayerRuntimeCollection.Items)
            {
                AddPlayerInstance(persistentPlayer.OwnerClientId, persistentPlayer.NetworkBuildState.BuildDataReference);
            }

        }
        private void OnDestroy()
        {
            _persistentPlayerRuntimeCollection.ItemAdded -= PersistentPlayerCollection_ItemAdded;
            _persistentPlayerRuntimeCollection.ItemRemoved -= PersistentPlayerCollection_Removed;
        }


        private void PersistentPlayerCollection_ItemAdded(PersistentPlayer persistentPlayer) => AddPlayerInstance(persistentPlayer.OwnerClientId, persistentPlayer.NetworkBuildState.BuildDataReference);
        private void PersistentPlayerCollection_Removed(PersistentPlayer persistentPlayer) => RemovePlayerInstance(persistentPlayer.OwnerClientId);


        /// <summary>
        ///     Add a CustomisationDummy instance for the client with the specified clientId.
        /// </summary>
        /// <param name="clientIDToAdd"></param>
        /// <param name="initialBuild"></param>
        /// <exception cref="System.Exception"></exception>
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
                if (_onlyShowLocalClient)
                    return; // We aren't wanting to add non-local clients.

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
                {
                    Debug.LogWarning($"More players have tried to join that there are spawn positions\nNot Spawning Graphic for Client {clientIDToAdd}");
                    return;
                }
            }

            // Mark the spawn position as occupied.
            lobbySpawnPosition.IsOccupied = true;
            lobbySpawnPosition.OccupyingClientID = clientIDToAdd;


            // Add the client's GFX Instance (Updated here for the first time only as the CustomisationDisplay is created after the event call is triggered, and so doesn't receive it otherwise).
            PlayerCustomisationDisplay clientGFXInstance = Instantiate<PlayerCustomisationDisplay>(_playerDummyPrefab, lobbySpawnPosition.SpawnPosition, worldPositionStays: false);
            clientGFXInstance.Setup(clientIDToAdd, initialBuild);
            _playerDummyInstances.Add(clientIDToAdd, clientGFXInstance);
        }
        /// <summary>
        ///     Remove a CustomisationDummy instance corresponding to the clientId to remove.
        /// </summary>
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


        /// <summary>
        ///     Try to update the CustomisationDummy corresponding to the client with the given id, if one matches.
        /// </summary>
        public void UpdateCustomisationDummy(ulong clientId, BuildData buildData)
        {
            if (_playerDummyInstances.TryGetValue(clientId, out PlayerCustomisationDisplay playerCustomisationDisplay))
                playerCustomisationDisplay.UpdateDummy(buildData);
        }
    }
}