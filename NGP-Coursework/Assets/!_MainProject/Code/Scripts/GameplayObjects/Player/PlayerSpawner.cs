using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private PlayerManager _playerPrefab;
    public static event System.Action<ulong, Gameplay.GameplayObjects.Character.Customisation.Data.BuildData> OnPlayerCustomisationFinalised;


    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        // This is the server.
        // Setup our players, notifying clients once each has been spawned.
        foreach (var kvp in PlayerCustomisationManager_Server.Instance.PlayerBuilds)
        {
            Vector3 spawnPosition = Vector3.right * Mathf.Floor(kvp.Key) * 2.0f;

            PlayerManager playerInstance = Instantiate<PlayerManager>(_playerPrefab, spawnPosition, Quaternion.identity);

            playerInstance.GetComponent<Gameplay.GameplayObjects.Character.ServerCharacter>().BuildData = kvp.Value;
            playerInstance.NetworkObject.SpawnAsPlayerObject(kvp.Key);

            SetupPlayerClientRpc(kvp.Key, kvp.Value);
        }
    }

    [ClientRpc]
    private void SetupPlayerClientRpc(ulong clientID, BuildData buildData)
    {
        PlayerManager playerInstance = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerManager>();
        playerInstance.SetBuild(buildData.ActiveFrameIndex, buildData.ActiveSlottableIndicies);
        OnPlayerCustomisationFinalised?.Invoke(clientID, buildData);
    }
}
