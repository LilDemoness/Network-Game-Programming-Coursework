using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using UI.Customisation;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    /// <summary>
    ///     Handles the processing, storing, and eventual relaying of player customisation data when in the customisation menu.
    /// </summary>
    public class PlayerCustomisationManager : NetworkBehaviour
    {
        [SerializeField] private CustomisationOptionsDatabase _optionsDatabase;
        [SerializeField] private PlayerCustomisationUI _playerCustomisationUI;
        private PlayerCustomisationState _localPlayerState;
        private Dictionary<ulong, PlayerCustomisationState> _otherPlayerStates;


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


        public static event System.Action<ulong, PlayerCustomisationState> OnPlayerCustomisationStateChanged;
        public static event System.Action<ulong, PlayerCustomisationState> OnPlayerCustomisationFinalised;


        private void Awake()
        {
            _otherPlayerStates = new Dictionary<ulong, PlayerCustomisationState>();
            _playerLobbyInstances = new Dictionary<ulong, PlayerCustomisationDisplay>();
        }


        [Rpc(SendTo.Server)]
        public void AlterPlayerStateServerRpc(PlayerCustomisationState customisationState, RpcParams rpcParams = default)
        {
            AlterPlayerStateClientRpc(customisationState);
        }
        [Rpc(SendTo.ClientsAndHost)]
        public void AlterPlayerStateClientRpc(PlayerCustomisationState customisationState)
        {
            AlterPlayerState(ref customisationState);
        }
        private void AlterPlayerState(ref PlayerCustomisationState customisationState)
        {
            if (customisationState.ClientID == this.OwnerClientId)
            {
                _localPlayerState = customisationState;
                HandleLocalPlayerStateChanged();
            }
            else
            {
                // Update the corresponding other player state.
                if (!_otherPlayerStates.TryAdd(customisationState.ClientID, customisationState))
                    _otherPlayerStates[customisationState.ClientID] = customisationState;

                // Update the displayed graphics.
                HandlePlayersStateChanged(customisationState);
            }
        }


        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                _playerCustomisationUI.Setup(this, NetworkManager.Singleton.LocalClientId, _optionsDatabase);

                // Ensure that we're accounting for other already existing players.
                foreach(var clientID in _otherPlayerStates.Keys)
                {
                    AddPlayerInstance(clientID);
                }
            }

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += Server_HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += Server_HandleClientDisconnected;

                // Ensure that we're accounting for already connected clients.
                foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    Server_HandleClientConnected(client.ClientId);
                }
            }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= Server_HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= Server_HandleClientDisconnected;
            }
        }


        private void Server_HandleClientConnected(ulong clientID)
        {
            // Ensure we've not already added this client.
            if (_otherPlayerStates.ContainsKey(clientID))
                return;

            // Add the client.
            _otherPlayerStates.Add(clientID, _optionsDatabase.GetDefaultState(clientID));
            Debug.Log("Players Count: " + _otherPlayerStates.Count);

            // Notify the Clients of the new player.
            HandleClientConnectedClientRpc(_otherPlayerStates[clientID]);
        }
        private void Server_HandleClientDisconnected(ulong clientID)
        {
            // Remove the player who just left from our dictionary.
            _otherPlayerStates.Remove(clientID);

            // Notify the Clients of the disconnect.
            HandleClientDisconnectClientRpc(clientID);
        }



    #region Changing Selected Elements

    #region Frame

        public void SelectNextFrame() => SelectFrameServerRpc(isIncrement: transform);
        public void SelectPreviousFrame() => SelectFrameServerRpc(isIncrement: false);

        [ServerRpc(RequireOwnership = false)]
        private void SelectFrameServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
        {
            if (!_otherPlayerStates.TryGetValue(serverRpcParams.Receive.SenderClientId, out PlayerCustomisationState customisationState))
                throw new System.Exception();

            int newValue = Loop(customisationState.FrameIndex + (isIncrement ? 1 : -1), _optionsDatabase.FrameDatas.Length);
            _otherPlayerStates[serverRpcParams.Receive.SenderClientId] = customisationState.NewWithFrameIndex(newValue);
            AlterPlayerStateClientRpc(_otherPlayerStates[serverRpcParams.Receive.SenderClientId]);
        }

    #endregion

    #region Leg

        public void SelectNextLeg() => SelectLegServerRpc(isIncrement: true);
        public void SelectPreviousLeg() => SelectLegServerRpc(isIncrement: false);
        [ServerRpc(RequireOwnership = false)]
        private void SelectLegServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
        {
            if (!_otherPlayerStates.TryGetValue(serverRpcParams.Receive.SenderClientId, out PlayerCustomisationState customisationState))
                throw new System.Exception();

            int newValue = Loop(customisationState.LegIndex + (isIncrement ? 1 : -1), _optionsDatabase.LegDatas.Length);
            _otherPlayerStates[serverRpcParams.Receive.SenderClientId] = customisationState.NewWithLegIndex(newValue);
            AlterPlayerStateClientRpc(_otherPlayerStates[serverRpcParams.Receive.SenderClientId]);
        }

    #endregion

    #region Slottable Data (Weapons & Abilities)

        public void SelectNextSlottableData(SlotIndex slotIndex) => SelectSlottableDataServerRpc(slotIndex, isIncrement: true);
        public void SelectPreviousSlottableData(SlotIndex slotIndex) => SelectSlottableDataServerRpc(slotIndex, isIncrement: false);

        [ServerRpc(RequireOwnership = false)]
        private void SelectSlottableDataServerRpc(SlotIndex slotIndex, bool isIncrement, ServerRpcParams serverRpcParams = default)
        {
            if (!_otherPlayerStates.TryGetValue(serverRpcParams.Receive.SenderClientId, out PlayerCustomisationState customisationState))
                throw new System.Exception();

            int minValue = slotIndex == SlotIndex.Ability ? _optionsDatabase.WeaponSlotDatas.Length : 0;
            int maxValue = slotIndex == SlotIndex.Ability ? _optionsDatabase.WeaponSlotDatas.Length + _optionsDatabase.AbilitySlotDatas.Length : _optionsDatabase.WeaponSlotDatas.Length;
            int newValue = Loop(customisationState.GetSlottableDataIndexForSlot(slotIndex) + (isIncrement ? 1 : -1), minValue, maxValue);

            _otherPlayerStates[serverRpcParams.Receive.SenderClientId] = customisationState.NewWithSlottableDataValue(slotIndex, newValue);
            AlterPlayerStateClientRpc(_otherPlayerStates[serverRpcParams.Receive.SenderClientId]);
        }

    #endregion

        private int Loop(int value, int maxValueExclusive) => Loop(value, 0, maxValueExclusive);
        private int Loop(int value, int minValueInclusive, int maxValueExclusive)
        {
            if (value >= maxValueExclusive)
                return minValueInclusive;
            else if (value < minValueInclusive)
                return maxValueExclusive - 1;
            else
                return value;
        }

        #endregion


        private void HandleLocalPlayerStateChanged()
        {
            OnPlayerCustomisationStateChanged?.Invoke(_localPlayerState.ClientID, _localPlayerState);
        }
        private void HandlePlayersStateChanged(PlayerCustomisationState newState)
        {
            OnPlayerCustomisationStateChanged?.Invoke(newState.ClientID, newState);
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void HandleClientConnectedClientRpc(PlayerCustomisationState initialState)
        {
            AddPlayerInstance(initialState.ClientID);
            AlterPlayerState(ref initialState);
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void HandleClientDisconnectClientRpc(ulong clientID)
        {
            RemovePlayerInstance(clientID);
        }


        private void RemovePlayerInstance(ulong clientIDToRemove)
        {
            // Allow this client's lobby spawn position can be reused.
            for(int i = 0; i < _playerLobbyGFXSpawnPositions.Length; ++i)
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
        private void AddPlayerInstance(ulong clientIDToAdd)
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
                for(int i = 1; i < _playerLobbyGFXSpawnPositions.Length; ++i)
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


            // Add the client's GFX Instance (Not updated here, instead updated later via the 'OnPlayerCustomisationStateChanged' call in 'HandlePlayersStateChanged').
            PlayerCustomisationDisplay clientGFXInstance = Instantiate<PlayerCustomisationDisplay>(_playerLobbyPrefab, lobbySpawnPosition.SpawnPosition, worldPositionStays: false);
            clientGFXInstance.Setup(clientIDToAdd);
            _playerLobbyInstances.Add(clientIDToAdd, clientGFXInstance);
        }


        public void ToggleReady()
        {
            if (_otherPlayerStates.ContainsKey(NetworkManager.LocalClientId))
            {
                // Toggle our ready state on the server.
                if (_otherPlayerStates[NetworkManager.LocalClientId].IsReady)
                    SetPlayerNotReadyServerRpc();
                else
                    SetPlayerReadyServerRpc();
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            // Update the triggering client as ready.
            if (_otherPlayerStates.ContainsKey(NetworkManager.LocalClientId))
            {
                // Mark this player as ready.
                _otherPlayerStates[NetworkManager.LocalClientId] = _otherPlayerStates[NetworkManager.LocalClientId].NewWithIsReady(true);
            }


            // Check if all players are ready.
            foreach(var kvp in _otherPlayerStates)
            {
                if (!kvp.Value.IsReady)
                {
                    // This player isn't ready. Not all players are ready.
                    Debug.Log($"Player {kvp.Key} is not ready");
                    return;
                }
            }


            // Set the player's data for loading into new scenes?
            foreach (var clientID in _otherPlayerStates.Keys)
            {
                BuildData playerBuildData = new BuildData(
                    activeFrame:            _otherPlayerStates[clientID].FrameIndex,
                    activeLeg:              _otherPlayerStates[clientID].LegIndex,
                    activeSlottableIndicies:_otherPlayerStates[clientID].SlottableDataIndicies
                );

                ServerManager.Instance.SetBuild(clientID, playerBuildData);
            }


            ServerManager.Instance.StartGame();
            FinaliseCustomisationClientRpc();   // Temp (Will be removed once we have scene management in, as we will be changing scene once everyone is ready and the host starts the game).
        }
        [ClientRpc]
        private void FinaliseCustomisationClientRpc()
        {
            foreach(var clientID in _otherPlayerStates.Keys)
            {
                OnPlayerCustomisationFinalised?.Invoke(clientID, _otherPlayerStates[clientID]);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerNotReadyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            // Update the triggering client as ready.
            if (_otherPlayerStates.ContainsKey(serverRpcParams.Receive.SenderClientId))
            {
                _otherPlayerStates[serverRpcParams.Receive.SenderClientId] = _otherPlayerStates[serverRpcParams.Receive.SenderClientId].NewWithIsReady(false);
            }
        }
    }
}