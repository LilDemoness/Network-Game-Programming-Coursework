using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;

namespace GameState
{
    public class GameManager : NetworkSingleton<GameManager>
    {
        [Header("Player Spawning")]
        [SerializeField] private Player _playerPrefab;


        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            // This is the server.
            // Setup our players, notifying clients once each has been spawned.
            foreach (var kvp in PlayerCustomisationManager_Server.Instance.PlayerBuilds)
            {
                Vector3 spawnPosition = Vector3.right * Mathf.Floor(kvp.Key) * 2.0f;

                // Create & Spawn the Player Instance.
                Player playerInstance = Instantiate<Player>(_playerPrefab, spawnPosition, Quaternion.identity);
                playerInstance.NetworkObject.SpawnAsPlayerObject(kvp.Key);
                playerInstance.GetComponent<ServerCharacter>().BuildData.Value = kvp.Value.GetBuildDataState();
            }
        }
    }
}