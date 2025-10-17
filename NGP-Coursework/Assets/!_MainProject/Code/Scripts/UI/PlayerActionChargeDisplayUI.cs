using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Gameplay.GameplayObjects;

public class PlayerActionChargeDisplayUI : MonoBehaviour
{
    private static Dictionary<WeaponSlotIndex, PlayerActionChargeDisplayUI> s_weaponSlotToUIDictionary = new Dictionary<WeaponSlotIndex, PlayerActionChargeDisplayUI>();
    [SerializeField] private WeaponSlotIndex _slotIndex = WeaponSlotIndex.Unset;


    [Header("UI References")]
    [SerializeField] private Image _chargeRadialImage;
    [SerializeField] private TMP_Text _chargePercentageText;


    private System.Func<float> CalculateChargePercentageFunc;
    private float _currentChargePercentage, _targetChargePercentage;
    private float _chargeTransitionRate = 0.0f;


    public static void StartedCharging(int slotIndex, float chargeStartedTime, float maxChargeDuration)
    {
        if (!s_weaponSlotToUIDictionary.TryGetValue((WeaponSlotIndex)slotIndex, out PlayerActionChargeDisplayUI chargeUI))
            throw new System.ArgumentException($"No instances of {nameof(PlayerActionChargeDisplayUI)} exist for the Weapon Slot {(WeaponSlotIndex)slotIndex}");
        
        chargeUI.CalculateChargePercentageFunc = () => CalculateChargePercentage_StartedCharging(chargeStartedTime, maxChargeDuration);
    }
    private static float CalculateChargePercentage_StartedCharging(float chargeStartTime, float maxChargeDuration)
        => Mathf.Clamp01((NetworkManager.Singleton.ServerTime.TimeAsFloat - chargeStartTime) / maxChargeDuration);

    public static void StoppedCharging(int slotIndex, float chargeDepletedTime, float maxChargeDepletionTime)
    {
        if (!s_weaponSlotToUIDictionary.TryGetValue((WeaponSlotIndex)slotIndex, out PlayerActionChargeDisplayUI chargeUI))
            throw new System.ArgumentException($"No instances of {nameof(PlayerActionChargeDisplayUI)} exist for the Weapon Slot {(WeaponSlotIndex)slotIndex}");

        chargeUI.CalculateChargePercentageFunc = () => CalculateChargePercentage_StoppedCharging(chargeDepletedTime, maxChargeDepletionTime);
    }
    private static float CalculateChargePercentage_StoppedCharging(float chargeDepletedTime, float maxChargeDepletionTime)
        => maxChargeDepletionTime > 0.0f ? Mathf.Clamp01((chargeDepletedTime - NetworkManager.Singleton.ServerTime.TimeAsFloat) / maxChargeDepletionTime) : 0.0f;

    public static void ResetChargeDisplay(int slotIndex, float currentChargePercentage, float timeToReset)
    {
        if (!s_weaponSlotToUIDictionary.TryGetValue((WeaponSlotIndex)slotIndex, out PlayerActionChargeDisplayUI chargeUI))
            throw new System.ArgumentException($"No instances of {nameof(PlayerActionChargeDisplayUI)} exist for the Weapon Slot {(WeaponSlotIndex)slotIndex}");

        // Prepare to reset our UI towards 0 via a lerp.
        chargeUI._currentChargePercentage = timeToReset > 0.0f ? currentChargePercentage : 0.0f;
        chargeUI._targetChargePercentage = 0.0f;
        chargeUI.CalculateChargePercentageFunc = null;

        // Set our lerp rate based on how long we want it to take to fully reset.
        chargeUI._chargeTransitionRate = 1.0f / timeToReset;
        
    }



    private void Awake()
    {
        if (_slotIndex == WeaponSlotIndex.Unset)
        {
            Debug.LogError($"Error: {this.name} has an unset Slot Index", this);  
            return;
        }

        if (!s_weaponSlotToUIDictionary.TryAdd(_slotIndex, this))
        {
            Debug.LogError($"Error: You are trying to initialise a {nameof(PlayerActionChargeDisplayUI)} for slot index {_slotIndex} when one already exists.\nExisting: {s_weaponSlotToUIDictionary[_slotIndex]}\nDestroying: {this.name}");  
            Destroy(this.gameObject);
            return;
        }

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

        if (buildData.GetFrameData().WeaponSlotCount < (int)_slotIndex)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            bool shouldBeEnabled = _slotIndex switch
            {
                WeaponSlotIndex.Primary => buildData.GetPrimaryWeaponData().AssociatedAction.CanCharge,
                WeaponSlotIndex.Secondary => buildData.GetSecondaryWeaponData().AssociatedAction.CanCharge,
                WeaponSlotIndex.Tertiary => buildData.GetTertiaryWeaponData().AssociatedAction.CanCharge,
                _ => false
            };
            this.gameObject.SetActive(shouldBeEnabled);
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
        _chargePercentageText.text = (_currentChargePercentage * 100.0f).ToString("00.0") + "%";
        _chargeRadialImage.fillAmount = _currentChargePercentage;
    }
}
