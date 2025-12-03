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
        [SerializeField] private TextMeshProUGUI _currentScoreText;
        [SerializeField] private NetworkTeamlessGameplayState _gameplayState;


        public override void OnNetworkSpawn()
        {
            _gameplayState.PlayerData.OnListChanged += PlayerData_OnListChanged;
        }
        public override void OnNetworkDespawn()
        {
            _gameplayState.PlayerData.OnListChanged -= PlayerData_OnListChanged;
        }

        private void PlayerData_OnListChanged(NetworkListEvent<NetworkTeamlessGameplayState.PlayerGameData> changeEvent)
        {
            if (changeEvent.Value.ClientId != NetworkManager.Singleton.LocalClientId)
                return;

            _currentScoreText.text = changeEvent.Value.Score.ToString();
        }
    }
}