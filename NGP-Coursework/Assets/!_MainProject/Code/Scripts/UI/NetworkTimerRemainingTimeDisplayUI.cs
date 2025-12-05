using TMPro;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.UI
{
    public class NetworkTimerRemainingTimeDisplayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gameTimeRemainingText;
        [SerializeField] private bool _alwaysShowMinutes = true;

        [Inject]
        private NetworkTimer _networkTimer;


        private void Update()
        {
            if (_networkTimer != null)
                _gameTimeRemainingText.text = GetMinutesSecondsString(_networkTimer.RemainingMatchTimeEstimate);
        }

        private string GetMinutesSecondsString(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60.0f);
            float seconds = Mathf.Max(timeInSeconds - (minutes * 60.0f), 0);
            if (_alwaysShowMinutes || minutes != 0)
                return minutes + ":" + seconds.ToString("00");
            else
                return seconds.ToString("0");
        }
    }
}