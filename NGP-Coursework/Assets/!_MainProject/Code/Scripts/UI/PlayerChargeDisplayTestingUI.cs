using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerChargeDisplayTestingUI : Singleton<PlayerChargeDisplayTestingUI>
{
    [SerializeField] private Image _chargeRadialImage;
    [SerializeField] private TMP_Text _chargePercentageText;


    private bool _isCharging;

    private float _chargeStartTime;
    private float _maxChargeDuration;

    private float _chargeDepletedTime;
    private float _maxChargeDepletionTime;


    public static void StartedCharging(float chargeStartedTime, float maxChargeDuration)
    {
        Instance._isCharging = true;

        Instance._chargeStartTime = chargeStartedTime;
        Instance._maxChargeDuration = maxChargeDuration;
    }
    public static void StoppedCharging(float chargeDepletedTime, float maxChargeDepletionTime)
    {
        Instance._isCharging = false;

        Instance._chargeDepletedTime = chargeDepletedTime;
        Instance._maxChargeDepletionTime = maxChargeDepletionTime;
    }


    private void LateUpdate()
    {
        if (NetworkManager.Singleton == null)
            return;

        float currentTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        float chargePercentage = _isCharging
            ? Mathf.Clamp01((currentTime - _chargeStartTime) / _maxChargeDuration)
            : _maxChargeDepletionTime > 0.0f ? Mathf.Max((_chargeDepletedTime - currentTime) / _maxChargeDepletionTime, 0.0f) : 0.0f;


        _chargePercentageText.text = (chargePercentage * 100.0f).ToString("00.0") + "%";
        _chargeRadialImage.fillAmount = chargePercentage;
    }
}
