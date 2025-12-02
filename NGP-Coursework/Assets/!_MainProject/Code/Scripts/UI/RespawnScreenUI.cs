using UnityEngine;
using TMPro;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;

namespace UI
{
    public class RespawnScreenUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _killerNameText;

        [SerializeField] private TMP_Text _respawnTimeRemainingText;
        private float _respawnTimeRemaining;
        private const string DEFAULT_KILLER_NAME = "SERVER";


        private void Awake()
        {
            Player.OnLocalPlayerDeath += Player_OnLocalPlayerDeath;
            Player.OnLocalPlayerRevived += Player_OnLocalPlayerRevived;

            Hide();
        }
        private void OnDestroy()
        {
            Player.OnLocalPlayerDeath -= Player_OnLocalPlayerDeath;
            Player.OnLocalPlayerRevived -= Player_OnLocalPlayerRevived;
        }

        private void Player_OnLocalPlayerDeath(object sender, Player.PlayerDeathEventArgs e) => Show(e.Inflicter, Gameplay.GameState.ServerFreeForAllState.GetRespawnDelay());
        private void Player_OnLocalPlayerRevived(object sender, System.EventArgs e)
        {
            Debug.Log("Player Revived");
            Hide();
        }



        public void Show(ServerCharacter killer, float timeToRespawn)
        {
            Debug.Log("Show");

            // Killer Name.
            _killerNameText.text = killer != null ? killer.CharacterName : DEFAULT_KILLER_NAME;

            // Time Remaining.
            this._respawnTimeRemaining = timeToRespawn;
            _respawnTimeRemainingText.text = _respawnTimeRemaining.ToString("0");

            // Show the UI.
            gameObject.SetActive(true);
        }
        public void Hide() => gameObject.SetActive(false);


        private void Update()
        {
            _respawnTimeRemaining -= Time.deltaTime;
            _respawnTimeRemainingText.text = _respawnTimeRemaining.ToString("0");
        }
    }
}