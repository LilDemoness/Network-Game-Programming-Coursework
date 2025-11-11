using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private PlayerManager _playerPrefab;
        public static event System.Action<ulong, BuildData> OnPlayerCustomisationFinalised;


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

                playerInstance.NetworkObject.SpawnAsPlayerObject(kvp.Key);
                playerInstance.GetComponent<ServerCharacter>().BuildData.Value = kvp.Value;

                SetupPlayerClientRpc(kvp.Key, kvp.Value);
            }
        }

        [ClientRpc]
        private void SetupPlayerClientRpc(ulong clientID, BuildData buildData)
        {
            PlayerManager playerInstance = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerManager>();
            playerInstance.SetBuild(clientID, buildData);
            OnPlayerCustomisationFinalised?.Invoke(clientID, buildData);
        }
    }
}