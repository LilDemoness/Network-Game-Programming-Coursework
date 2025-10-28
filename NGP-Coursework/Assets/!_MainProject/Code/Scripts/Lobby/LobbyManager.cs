using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    // Player Ready States.
    private Dictionary<ulong, PlayerLobbyState> _playerStates = new Dictionary<ulong, PlayerLobbyState>();


    public void ToggleReady()
    {
        // Toggle our ready state on the server.
        if (_playerStates.ContainsKey(NetworkManager.Singleton.LocalClientId))
        {
            if (_playerStates[NetworkManager.Singleton.LocalClientId].IsReady)
                SetPlayerNotReadyServerRpc();
            else
                SetPlayerReadyServerRpc();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Update the triggering client as ready.
        if (_playerStates.ContainsKey(serverRpcParams.Receive.SenderClientId))
        {
            // Mark this player as ready.
            _playerStates[serverRpcParams.Receive.SenderClientId] = _playerStates[serverRpcParams.Receive.SenderClientId].NewWithIsReady(true);
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

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNotReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Update the triggering client as ready.
        if (_playerStates.ContainsKey(serverRpcParams.Receive.SenderClientId))
        {
            _playerStates[serverRpcParams.Receive.SenderClientId] = _playerStates[serverRpcParams.Receive.SenderClientId].NewWithIsReady(false);
        }
    }
}
