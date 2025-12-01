using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using System.Collections;

namespace GameState
{
    public class GameManager : NetworkSingleton<GameManager>
    {
        [Header("Player Spawning")]
        [SerializeField] private Player _playerPrefab;


        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                // Not the server, return.
                this.enabled = false;
                return;
            }
            // This is the server.

            // Subscribe to events.
            Player.OnPlayerDeath += Player_OnPlayerDeath;

            // Setup our players, notifying clients once each has been spawned.
            throw new System.NotImplementedException("Player Spawning");
            /*foreach (var kvp in PlayerCustomisationManager_Server.Instance.PlayerBuilds)
            {
                Vector3 spawnPosition = Vector3.right * Mathf.Floor(kvp.Key) * 2.0f;

                // Create & Spawn the Player Instance.
                Player playerInstance = Instantiate<Player>(_playerPrefab, spawnPosition, Quaternion.identity);
                playerInstance.NetworkObject.SpawnAsPlayerObject(kvp.Key);
                playerInstance.GetComponent<ServerCharacter>().BuildData.Value = kvp.Value.GetBuildDataState();
            }*/
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe from events.
            Player.OnPlayerDeath -= Player_OnPlayerDeath;
        }

        private void Player_OnPlayerDeath(object sender, Player.PlayerDeathEventArgs e)
        {
            TempRespawn((sender as Player));
        }


        private void TempRespawn(Player player)
        {
            StartCoroutine(RespawnAfterDelay(player, 3.0f, Vector3.zero));
        }
        private IEnumerator RespawnAfterDelay(Player player, float delay, Vector3 respawnPosition)
        {
            yield return new WaitForSeconds(delay);
            //player.PerformRespawn(respawnPosition);
        }

        public float GetRespawnTimeEstimate() => 3.0f;
    }
}