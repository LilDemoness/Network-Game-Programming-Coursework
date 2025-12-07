using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UI.Debugging
{
    /// <summary>
    ///     Displays data from a player that will be useful for debugging (E.g. Health, Heat, Speed, ect)
    /// </summary>
    public class PlayerStateUI : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _healthText;
        private string _healthFormattingString;

        [SerializeField] private TMP_Text _heatText;
        private string _heatFormattingString;

        [SerializeField] private TMP_Text _currentSpeedText;


        private void Awake()
        {
            Player.OnLocalPlayerSet += Player_OnLocalPlayerSet;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                // No use on non-clients.
                Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
                this.enabled = false;
                return;
            }
        }
        private void Player_OnLocalPlayerSet()
        {
            // Subscribe to change events.
            Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent.OnHealthChanged += OnHealthChanged;
            Player.LocalClientInstance.ServerCharacter.CurrentHeat.OnValueChanged += OnHeatChanged;

            // Ensure that late joiners receive the initial state.
            OnHealthChanged(0.0f, Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent.GetCurrentHealth());
            OnHeatChanged(0.0f, Player.LocalClientInstance.ServerCharacter.CurrentHeat.Value);
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe to change events.
            if (Player.LocalClientInstance != null)
            {
                Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent.OnHealthChanged -= OnHealthChanged;
                Player.LocalClientInstance.ServerCharacter.CurrentHeat.OnValueChanged -= OnHeatChanged;
            }
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
        }


        private void OnHealthChanged(float _, float newHealth) => SetHealthText(newHealth, Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent.MaxHealth);
        private void OnHeatChanged(float _, float newValue) => SetHeatText(newValue, Player.LocalClientInstance.ServerCharacter.MaxHeat);


        private void SetHealthText(float currentHealth, float maxHealth)
        {
            _healthFormattingString = GetFormattingString(Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent.MaxHealth); // Call when 'MaxHealth' is changed.
            _healthText.text = string.Concat("Health: ", currentHealth.ToString(_healthFormattingString), "/", maxHealth.ToString(_healthFormattingString));
        }
        private void SetHeatText(float currentHeat, float maxHeat)
        {
            _heatFormattingString = GetFormattingString(Player.LocalClientInstance.ServerCharacter.MaxHeat); // Call when 'MaxHeat' is changed.
            _heatText.text = string.Concat("Heat: ", currentHeat.ToString(_heatFormattingString), "/", maxHeat.ToString(_heatFormattingString));
        }
        private string CreateSpeedString(float currentSpeed) => string.Concat("Speed: ", (Mathf.Round(currentSpeed / 10.0f) * 10.0f).ToString(), Units.SPEED_UNITS);

        private string GetFormattingString(float value)
        {
            int significantFigureCount = CalculateSignificantFigureCount(value) + 1;
            return new string('0', significantFigureCount);
        }
        // Source: 'https://stackoverflow.com/questions/374316/round-a-double-to-x-significant-figures/374470#374470'.
        int CalculateSignificantFigureCount(float value)
        {
            float scale = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(Mathf.Abs(value))) + 1);
            return Mathf.RoundToInt(value * Mathf.Floor(value / scale));
        }
    }
}