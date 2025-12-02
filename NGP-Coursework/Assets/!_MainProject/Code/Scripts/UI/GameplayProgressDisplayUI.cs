using Gameplay.GameState;
using TMPro;
using UnityEngine;
using VContainer;

namespace Gameplay.UI
{
    public class GameplayProgressDisplayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gameTimeRemainingText;

        [Inject]
        private NetworkGameplayState _networkGameplayState;


        private void Update()
        {
            _gameTimeRemainingText.text = GetMinutesSecondsString(_networkGameplayState.RemainingMatchTimeEstimate);
        }

        private string GetMinutesSecondsString(float timeInSeconds)
        {
            float minutes = Mathf.FloorToInt(timeInSeconds / 60.0f);
            float seconds = Mathf.Max(timeInSeconds - (minutes * 60.0f), 0);
            return minutes + ":" + seconds.ToString("00");
        }
    }
}