using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    // Player Ready States.
    private Dictionary<ulong, PlayerLobbyState> _playerStates = new Dictionary<ulong, PlayerLobbyState>();


    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            //this.enabled = false;
            return;
        }

        NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
    }


    private void NetworkManager_OnClientConnectedCallback(ulong clientID) => _playerStates.Add(clientID, new PlayerLobbyState() { ClientID = clientID, IsReady = false });
    private void NetworkManager_OnClientDisconnectCallback(ulong clientID) => _playerStates.Remove(clientID);
    

    public void ToggleReady()
    {
        // Toggle our ready state on the server.
        TogglePlayerReadyServerRpc(NetworkManager.LocalClientId);
    }
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void TogglePlayerReadyServerRpc(ulong clientID)
    {
        if (_playerStates.ContainsKey(clientID))
        {
            if (_playerStates[clientID].IsReady)
                SetPlayerNotReadyServerRpc(clientID);
            else
                SetPlayerReadyServerRpc(clientID);
        }
        else
            throw new System.Exception("A player is trying to ready that we didn't receive a connection request for");
    }
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ulong clientID)
    {
        // Update the triggering client as ready.
        if (_playerStates.ContainsKey(clientID))
        {
            // Mark this player as ready.
            _playerStates[clientID] = _playerStates[clientID].NewWithIsReady(true);
        }


        // Check if all players are ready.
        foreach (var kvp in _playerStates)
        {
            if (!kvp.Value.IsReady)
            {
                // This player isn't ready. Not all players are ready.
                Debug.Log($"Player {kvp.Key} is not ready");
                return;
            }
        }


        // Set the player's data for loading into new scenes?


        ServerManager.Instance.StartGame();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SetPlayerNotReadyServerRpc(ulong clientID)
    {
        // Update the triggering client as ready.
        if (_playerStates.ContainsKey(clientID))
        {
            _playerStates[clientID] = _playerStates[clientID].NewWithIsReady(false);
        }
    }
}
