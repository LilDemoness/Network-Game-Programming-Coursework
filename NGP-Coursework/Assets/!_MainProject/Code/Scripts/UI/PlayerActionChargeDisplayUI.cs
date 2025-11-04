using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions;

namespace UI.Actions
{
    public class PlayerActionChargeDisplayUI : MonoBehaviour
    {
        private static Dictionary<SlotIndex, List<PlayerActionChargeDisplayUI>> s_slotIndexToUIDictionary = new Dictionary<SlotIndex, List<PlayerActionChargeDisplayUI>>();
        [SerializeField] private SlotIndex _slotIndex = SlotIndex.Unset;


        [Header("UI References")]
        [SerializeField] private Image _chargeRadialImage;

        private System.Func<float> CalculateChargePercentageFunc;
        private float _currentChargePercentage, _targetChargePercentage;
        private float _chargeTransitionRate = 0.0f;


        static PlayerActionChargeDisplayUI()
        {
            Action.OnClientStartedCharging += Action_ClientStartedCharging;
            Action.OnClientStoppedCharging += Action_ClientStoppedCharging;
            Action.OnClientResetCharging += Action_ClientResetChargeDisplay;
        }
        /// <summary>
        ///     Called when any client starts charging any action.
        /// </summary>
        private static void Action_ClientStartedCharging(object sender, Action.StartedChargingEventArgs e)
        {
            if (e.Client.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                return; // Not the local client.

            if (!s_slotIndexToUIDictionary.TryGetValue((SlotIndex)e.SlotIndex, out List<PlayerActionChargeDisplayUI> chargeUIElements))
                throw new System.ArgumentException($"No instances of {nameof(PlayerActionChargeDisplayUI)} exist for the Weapon Slot {(SlotIndex)e.SlotIndex}");


            // Valid call. Update the charge UI.
            foreach (PlayerActionChargeDisplayUI chargeUI in chargeUIElements)
                chargeUI.CalculateChargePercentageFunc = () => CalculateChargePercentage_StartedCharging(e.ChargeStartedTime, e.MaxChargeDuration);
        }
        private static float CalculateChargePercentage_StartedCharging(float chargeStartTime, float maxChargeDuration)
            => Mathf.Clamp01((NetworkManager.Singleton.ServerTime.TimeAsFloat - chargeStartTime) / maxChargeDuration);

        /// <summary>
        ///     Called when any client stops charging any action.
        /// </summary>
        private static void Action_ClientStoppedCharging(object sender, Action.StoppedChargingEventArgs e)
        {
            if (e.Client.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                return; // Not the local client.

            if (!s_slotIndexToUIDictionary.TryGetValue((SlotIndex)e.SlotIndex, out List<PlayerActionChargeDisplayUI> chargeUIElements))
                throw new System.ArgumentException($"No instances of {nameof(PlayerActionChargeDisplayUI)} exist for the Weapon Slot {(SlotIndex)e.SlotIndex}");


            // Valid call. Update the charge UI.
            foreach (PlayerActionChargeDisplayUI chargeUI in chargeUIElements)
                chargeUI.CalculateChargePercentageFunc = () => CalculateChargePercentage_StoppedCharging(e.ChargeFullyDepletedTime, e.MaxChargeDepletionTime);
        }
        private static float CalculateChargePercentage_StoppedCharging(float chargeFullyDepletedTime, float maxChargeDepletionTime)
            => maxChargeDepletionTime > 0.0f ? Mathf.Clamp01((chargeFullyDepletedTime - NetworkManager.Singleton.ServerTime.TimeAsFloat) / maxChargeDepletionTime) : 0.0f;

        /// <summary>
        ///     Called when any client resets the charge percentage on any action.
        /// </summary>
        private static void Action_ClientResetChargeDisplay(object sender, Action.ResetChargingEventArgs e)
        {
            if (e.Client.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                return; // Not the local client.

            if (!s_slotIndexToUIDictionary.TryGetValue((SlotIndex)e.SlotIndex, out List<PlayerActionChargeDisplayUI> chargeUIElements))
                throw new System.ArgumentException($"No instances of {nameof(PlayerActionChargeDisplayUI)} exist for the Weapon Slot {(SlotIndex)e.SlotIndex}");

            // Valid call. Update the charge UI.
            foreach (PlayerActionChargeDisplayUI chargeUI in chargeUIElements)
            {
                // Prepare to reset our UI towards 0 via a lerp.
                chargeUI._currentChargePercentage = e.TimeToReset > 0.0f ? e.CurrentChargePercentage : 0.0f;
                chargeUI._targetChargePercentage = 0.0f;
                chargeUI.CalculateChargePercentageFunc = null;

                // Set our lerp rate based on how long we want it to take to fully reset.
                chargeUI._chargeTransitionRate = 1.0f / e.TimeToReset;
            }
        }



        private void Awake()
        {
            if (_slotIndex == SlotIndex.Unset)
            {
                Debug.LogError($"Error: {this.name} has an unset Slot Index", this);
                return;
            }

            // Add ourselves (Or create then add ourselves) to the instances of weapon UI for this slot.
            if (s_slotIndexToUIDictionary.TryGetValue(_slotIndex, out List<PlayerActionChargeDisplayUI> chargeUIElements))
                chargeUIElements.Add(this);
            else
                s_slotIndexToUIDictionary.Add(_slotIndex, new List<PlayerActionChargeDisplayUI>() { this });


            // Build Change Event (Enable/Disable State of this UI element).
            PlayerSpawner.OnPlayerCustomisationFinalised += PlayerSpawner_OnPlayerCustomisationFinalised;
        }
        private void OnDestroy()
        {
            PlayerSpawner.OnPlayerCustomisationFinalised -= PlayerSpawner_OnPlayerCustomisationFinalised;
        }

        private void PlayerSpawner_OnPlayerCustomisationFinalised(ulong clientID, Gameplay.GameplayObjects.Character.Customisation.Data.BuildData buildData)
        {
            if (clientID != NetworkManager.Singleton.LocalClientId)
                return;

            if (buildData.GetFrameData().AttachmentPoints.Length < (int)_slotIndex)
            {
                this.gameObject.SetActive(false);
            }
            else
            {
                this.gameObject.SetActive(buildData.GetSlottableData(_slotIndex).AssociatedAction.CanCharge);
            }
        }

        private void LateUpdate()
        {
            if (NetworkManager.Singleton == null)
                return;

            // Update our target percentage.
            if (CalculateChargePercentageFunc != null)
                _targetChargePercentage = CalculateChargePercentageFunc();

            // Handle our transition between current & target percentages.
            if (_chargeTransitionRate > 0.0f)
            {
                // Lerp to our target.
                _currentChargePercentage = Mathf.MoveTowards(_currentChargePercentage, _targetChargePercentage, _chargeTransitionRate * Time.deltaTime);

                // If we've reached our target, stop lerping.
                if (_currentChargePercentage == _targetChargePercentage)
                    _chargeTransitionRate = 0.0f;
            }
            else
            {
                // Instant set (No lerp).
                _currentChargePercentage = _targetChargePercentage;
            }


            // Update our UI.
            //_chargePercentageText.text = (_currentChargePercentage * 100.0f).ToString("00.0") + "%";
            _chargeRadialImage.fillAmount = _currentChargePercentage;
        }
    }
}