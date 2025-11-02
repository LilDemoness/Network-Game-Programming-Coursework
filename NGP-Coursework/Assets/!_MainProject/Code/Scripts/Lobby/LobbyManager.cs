using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class LobbyManager : NetworkSingleton<LobbyManager>
{
    // Player Ready States.
    private NetworkList<PlayerLobbyState> _playerStates = new NetworkList<PlayerLobbyState>();

    public static event System.Action<ulong> OnClientIsReady;
    public static event System.Action<ulong> OnClientNotReady;


    protected override void Awake()
    {
        base.Awake();
        _playerStates.OnListChanged += OnPlayerStatesChanged;
    }
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            foreach(var player in _playerStates)
            {
                if (player.IsReady)
                    OnClientIsReady?.Invoke(player.ClientID);
                else
                    OnClientNotReady?.Invoke(player.ClientID);
            }
        }

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }

    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        _playerStates.OnListChanged -= OnPlayerStatesChanged;
    }


    private void OnPlayerStatesChanged(NetworkListEvent<PlayerLobbyState> changeEvent)
    {
        if (changeEvent.Value.IsReady)
            OnClientIsReady?.Invoke(changeEvent.Value.ClientID);
        else
            OnClientNotReady?.Invoke(changeEvent.Value.ClientID);
    }


    private void NetworkManager_OnClientConnectedCallback(ulong clientID) => _playerStates.Add(new PlayerLobbyState() { ClientID = clientID, IsReady = false });
    private void NetworkManager_OnClientDisconnectCallback(ulong clientID)
    {
        for(int i = 0; i < _playerStates.Count; ++i)
        {
            if (_playerStates[i].ClientID == clientID)
            {
                _playerStates.RemoveAt(i);
                return;
            }
        }
    }
    

    public void ToggleReady()
    {
        // Toggle our ready state on the server.
        TogglePlayerReadyServerRpc(NetworkManager.LocalClientId);
    }
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void TogglePlayerReadyServerRpc(ulong clientID)
    {
        if (TryGetIndexForId(clientID, out int playerStateIndex))
        {
            if (_playerStates[playerStateIndex].IsReady)
            {
                SetPlayerNotReadyServerRpc(clientID);
            }
            else
            {
                SetPlayerReadyServerRpc(clientID);
            }
        }
        else
            throw new System.Exception("A player is trying to ready that we didn't receive a connection request for");
    }
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ulong clientID)
    {
        // Update the triggering client as ready.
        if (TryGetIndexForId(clientID, out int playerStateIndex))
        {
            // Mark this player as ready.
            _playerStates[playerStateIndex] = _playerStates[playerStateIndex].NewWithIsReady(true);
        }

        //SetPlayerReadyClientRpc(clientID);


        // Check if all players are ready.
        foreach (PlayerLobbyState lobbyState in _playerStates)
        {
            if (!lobbyState.IsReady)
            {
                // This player isn't ready. Not all players are ready.
                Debug.Log($"Player {lobbyState.ClientID} is not ready");
                return;
            }
        }


        // Set the player's data for loading into new scenes?


        ServerManager.Instance.StartGame();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SetPlayerReadyClientRpc(ulong clientID) => OnClientIsReady?.Invoke(clientID);


    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SetPlayerNotReadyServerRpc(ulong clientID)
    {
        // Update the triggering client as not ready.
        if (TryGetIndexForId(clientID, out int playerStateIndex))
        {
            _playerStates[playerStateIndex] = _playerStates[playerStateIndex].NewWithIsReady(false);
        }

        //SetPlayerNotReadyClientRpc(clientID);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SetPlayerNotReadyClientRpc(ulong clientID) => OnClientNotReady?.Invoke(clientID);


    private int GetIndexForId(ulong clientID)
    {
        for(int i = 0; i < _playerStates.Count; ++i)
        {
            if (_playerStates[i].ClientID == clientID)
            {
                return i;
            }
        }

        throw new System.ArgumentException($"Invalid Client ID: No '_playerState' element matches the ClientID {clientID}");
    }
    private bool TryGetIndexForId(ulong clientID, out int index)
    {
        for (int i = 0; i < _playerStates.Count; ++i)
        {
            if (_playerStates[i].ClientID == clientID)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }
}
