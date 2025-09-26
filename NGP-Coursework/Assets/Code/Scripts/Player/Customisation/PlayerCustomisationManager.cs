using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCustomisationManager : NetworkBehaviour
{
    [SerializeField] private PlayerCustomisationOptionsDatabase _optionsDatabase;
    [SerializeField] private PlayerCustomisationUI _playerCustomisationUI;
    private NetworkList<PlayerCustomisationState> _players;


    [Header("Player Lobby GFX Instances")]
    [SerializeField] private PlayerCustomisation _playerLobbyPrefab;
    private Dictionary<ulong, PlayerCustomisation> _playerLobbyInstances;

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
        _players = new NetworkList<PlayerCustomisationState>();
        _playerLobbyInstances = new Dictionary<ulong, PlayerCustomisation>();
    }


    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            _playerCustomisationUI.Setup(this, NetworkManager.Singleton.LocalClientId, _optionsDatabase);

            _players.OnListChanged += HandlePlayersStateChanged;

            // Ensure that we're accounting for other already existing players.
            for(int i = 0; i < _players.Count; ++i)
            {
                AddPlayerInstance(_players[i].ClientID);
            }
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            // Ensure that we're accounting for already connected clients.
            foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                HandleClientConnected(client.ClientId);
            }
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            _players.OnListChanged -= HandlePlayersStateChanged;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }


    private void HandleClientConnected(ulong clientID)
    {
        // Ensure we've not already added this client.
        for(int i = 0; i < _players.Count; ++i)
            if (_players[i].ClientID == clientID)
                return;

        // Add the client.
        _players.Add(new PlayerCustomisationState(clientID));
        Debug.Log("Players Count: " + _players.Count);
    }
    private void HandleClientDisconnected(ulong clientID)
    {
        // Remove the player who just left from our '_players' list.
        for(int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == clientID)
            {
                _players.RemoveAt(i);
                break;
            }
        }
    }



#region Changing Selected Elements

#region Frame

    public void SelectNextFrame() => SelectFrameServerRpc(isIncrement: transform);
    public void SelectPreviousFrame() => SelectFrameServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectFrameServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for(int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].FrameIndex + (isIncrement ? 1 : -1), _optionsDatabase.FrameDatas.Length);
                _players[i] = _players[i].NewWithFrameIndex(newValue);
            }
        }
    }

#endregion

#region Leg

    public void SelectNextLeg() => SelectLegServerRpc(isIncrement: true);
    public void SelectPreviousLeg() => SelectLegServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectLegServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].LegIndex + (isIncrement ? 1 : -1), _optionsDatabase.LegDatas.Length);
                _players[i] = _players[i].NewWithLegIndex(newValue);
            }
        }
    }

#endregion

#region Weapons

    // Primary.
    public void SelectNextPrimaryWeapon() => SelectPrimaryWeaponServerRpc(isIncrement: true);
    public void SelectPreviousPrimaryWeapon() => SelectPrimaryWeaponServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectPrimaryWeaponServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].PrimaryWeaponIndex + (isIncrement ? 1 : -1), _optionsDatabase.WeaponDatas.Length);
                _players[i] = _players[i].NewWithPrimaryWeaponIndex(newValue);
            }
        }
    }


    // Secondary.
    public void SelectNextSecondaryWeapon() => SelectSecondaryWeaponServerRpc(isIncrement: true);
    public void SelectPreviousSecondaryWeapon() => SelectSecondaryWeaponServerRpc(isIncrement: false);
    
    [ServerRpc(RequireOwnership = false)]
    private void SelectSecondaryWeaponServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].SecondaryWeaponIndex + (isIncrement ? 1 : -1), _optionsDatabase.WeaponDatas.Length);
                _players[i] = _players[i].NewWithSecondaryWeaponIndex(newValue);
            }
        }
    }


    // Tertiary.
    public void SelectNextTertiaryWeapon() => SelectTertiaryWeaponServerRpc(isIncrement: true);
    public void SelectPreviousTertiaryWeapon() => SelectTertiaryWeaponServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectTertiaryWeaponServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].TertiaryWeaponIndex + (isIncrement ? 1 : -1), _optionsDatabase.WeaponDatas.Length);
                _players[i] = _players[i].NewWithTertiaryWeaponIndex(newValue);
            }
        }
    }

#endregion

#region Ability

    public void SelectNextAbility() => SelectAbilityServerRpc(isIncrement: true);
    public void SelectPreviousAbility() => SelectAbilityServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectAbilityServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].AbilityIndex + (isIncrement ? 1 : -1), _optionsDatabase.AbilityDatas.Length);
                _players[i] = _players[i].NewWithAbilityIndex(newValue);
            }
        }
    }

#endregion

    private int Loop(int value, int maxValueExclusive)
    {
        if (value >= maxValueExclusive)
            return 0;
        else if (value < 0)
            return maxValueExclusive - 1;
        else
            return value;
    }

    #endregion


    private void HandlePlayersStateChanged(NetworkListEvent<PlayerCustomisationState> changeEvent)
    {
        // Handle player joining/leaving.
        switch(changeEvent.Type)
        {
            case NetworkListEvent<PlayerCustomisationState>.EventType.Add:
                AddPlayerInstance(changeEvent.Value.ClientID);
                break;
            case NetworkListEvent<PlayerCustomisationState>.EventType.Remove:
            case NetworkListEvent<PlayerCustomisationState>.EventType.RemoveAt:
            case NetworkListEvent<PlayerCustomisationState>.EventType.Clear:
                RemovePlayerInstance(changeEvent.Value.ClientID);
                break;
        }

        // Handle any changes in the player's build.
        for (int i = 0; i < _players.Count; ++i)
        {
            OnPlayerCustomisationStateChanged?.Invoke(_players[i].ClientID, _players[i]);
        }
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
        if (_playerLobbyInstances.Remove(clientIDToRemove, out PlayerCustomisation customisationInstance))
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
        PlayerCustomisation clientGFXInstance = Instantiate<PlayerCustomisation>(_playerLobbyPrefab, lobbySpawnPosition.SpawnPosition, worldPositionStays: false);
        clientGFXInstance.Setup(clientIDToAdd);
        _playerLobbyInstances.Add(clientIDToAdd, clientGFXInstance);
    }


    public void ToggleReady()
    {
        for(int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID != NetworkManager.Singleton.LocalClientId)
                continue;

            // Toggle our ready state on the server.
            if (_players[i].IsReady)
                SetPlayerNotReadyServerRpc();
            else
                SetPlayerReadyServerRpc();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Update the triggering client as ready.
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID != serverRpcParams.Receive.SenderClientId)
                continue;

            // Mark this player as ready.
            _players[i] = _players[i].NewWithIsReady(true);
        }


        // Check if all players are ready.
        for(int i = 0; i < _players.Count; ++i)
        {
            if (!_players[i].IsReady)
            {
                // This player isn't ready. Not all players are ready.
                Debug.Log($"Player {_players[i].ClientID} is not ready");
                return;
            }
        }


        // Set the player's data for loading into new scenes?
        foreach (var player in _players)
        {
            BuildData playerBuildData = new BuildData(
                activeFrame:            _optionsDatabase.FrameDatas[player.FrameIndex],
                activeLeg:              _optionsDatabase.LegDatas[player.LegIndex],
                activePrimaryWeapon:    _optionsDatabase.WeaponDatas[player.PrimaryWeaponIndex],
                activeSecondaryWeapon:  _optionsDatabase.WeaponDatas[player.SecondaryWeaponIndex],
                activeTertiaryWeapon:   _optionsDatabase.WeaponDatas[player.TertiaryWeaponIndex],
                activeAbility:          _optionsDatabase.AbilityDatas[player.AbilityIndex]);

            ServerManager.Instance.SetBuild(player.ClientID, playerBuildData);
        }


        ServerManager.Instance.StartGame();
        FinaliseCustomisationClientRpc();   // Temp (Will be removed once we have scene management in, as we will be changing scene once everyone is ready and the host starts the game).
    }
    [ClientRpc]
    private void FinaliseCustomisationClientRpc()
    {
        for(int i = 0; i < _players.Count; ++i)
        {
            OnPlayerCustomisationFinalised?.Invoke(_players[i].ClientID, _players[i]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNotReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Update the triggering client as ready.
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID != serverRpcParams.Receive.SenderClientId)
                continue;

            _players[i] = _players[i].NewWithIsReady(false);
        }
    }
}