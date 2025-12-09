using UnityEngine;
using TMPro;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using System.Collections;
using Unity.Netcode;

namespace UI
{
    /// <summary>
    ///     UI Element Representing the Respawn Screen for a Player.
    /// </summary>
    public class RespawnScreenUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _killerNameText;

        [SerializeField] private TMP_Text _respawnTimeRemainingText;
        private float _respawnElapsedTime;
        private float _respawnTimeRemaining => (_respawnElapsedTime - Time.time);
        private const string DEFAULT_KILLER_NAME = "SERVER";


        [Space(10)]
        [SerializeField] private NonNavigableButton _customisationButton;


        private void Awake()
        {
            Gameplay.GameState.NetworkGameplayState.OnLocalPlayerRespawnStarted += NetworkGameplayState_OnLocalPlayerRespawnStarted;
            Player.OnLocalPlayerDeath += Player_OnLocalPlayerDeath;
            Player.OnLocalPlayerRevived += Player_OnLocalPlayerRevived;

            Hide(); // Start Hidden.
        }
        private void OnDestroy()
        {
            Gameplay.GameState.NetworkGameplayState.OnLocalPlayerRespawnStarted -= NetworkGameplayState_OnLocalPlayerRespawnStarted;
            Player.OnLocalPlayerDeath -= Player_OnLocalPlayerDeath;
            Player.OnLocalPlayerRevived -= Player_OnLocalPlayerRevived;
        }

        private void Player_OnLocalPlayerDeath(object sender, Player.PlayerDeathEventArgs e)
        {
            // Only set the killer name when notified of the death on the local player as the required timings are sent through the NetworkGameplayState instead, but that doesn't have killer information.
            SetKillerName(e.Inflicter);
        }
        private void NetworkGameplayState_OnLocalPlayerRespawnStarted(float respawnDelay)
            => Show(Time.time + respawnDelay - (NetworkManager.Singleton.LocalTime.TimeAsFloat - NetworkManager.Singleton.ServerTime.TimeAsFloat) / 2.0f);

        private void Player_OnLocalPlayerRevived(object sender, System.EventArgs e)
        {
            // Hide the Respawn Screen UI when the local player is revived.
            Hide();
        }



        private void SetKillerName(ServerCharacter killer) => _killerNameText.text = killer != null ? killer.CharacterName : DEFAULT_KILLER_NAME;
        
        public void Show(ServerCharacter killer, float timeToRespawn)
        {
            // Killer Name.
            SetKillerName(killer);

            Show(timeToRespawn);
        }
        private void Show(float timeToRespawn)
        {
            // Time Remaining.
            this._respawnElapsedTime = timeToRespawn;
            _respawnTimeRemainingText.text = _respawnTimeRemaining.ToString("0");

            // Show the UI.
            gameObject.SetActive(true);

            // Try to reduce accidental input on the Customisation Button.
            StartCoroutine(HandleCustomisationButtonInteractability());
        }

        public void Hide() => gameObject.SetActive(false);

        // Ensures that the Customisation Button isn't interactable for a short duration after opening to reduce accidental input.
        private IEnumerator HandleCustomisationButtonInteractability()
        {
            _customisationButton.IsInteractable = false;
            yield return new WaitForSeconds(0.5f);
            _customisationButton.IsInteractable = true;
        }


        private void Update()
        {
            _respawnTimeRemainingText.text = _respawnTimeRemaining.ToString("0");
        }
    }
}