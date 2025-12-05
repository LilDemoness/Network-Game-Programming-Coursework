using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Players;
using Gameplay.GameState;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>
    ///     Temporary UI to display the local player's score.
    /// </summary>
    public class LocalPlayerScoreUI : NetworkBehaviour
    {
        [SerializeField] private PersistentPlayerRuntimeCollection _persistentPlayerCollection;

        [SerializeField] private TextMeshProUGUI _currentScoreText;
        [SerializeField] private NetworkFFAGameplayState _gameplayState;


        public override void OnNetworkSpawn()
        {
            _gameplayState.PlayerData.OnListChanged += PlayerData_OnListChanged;
        }
        public override void OnNetworkDespawn()
        {
            _gameplayState.PlayerData.OnListChanged -= PlayerData_OnListChanged;
        }

        private void PlayerData_OnListChanged(NetworkListEvent<NetworkFFAGameplayState.PlayerGameData> changeEvent)
        {
            if (!_persistentPlayerCollection.TryGetPlayer(changeEvent.Value.PlayerIndex, out PersistentPlayer persistentPlayer))
                return; // No PersistentPlayer (Not a Player).
            
            if (persistentPlayer.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                return; // Not the local player.
            

            // Is the local player.
            // Set text.
            _currentScoreText.text = changeEvent.Value.Kills.ToString();
        }
    }
}