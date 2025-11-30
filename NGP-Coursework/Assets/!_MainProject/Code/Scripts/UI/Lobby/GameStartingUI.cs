using System.Collections;
using Gameplay.GameState;
using TMPro;
using UnityEngine;

namespace Gameplay.UI.Lobby
{
    /// <summary>
    ///     A UI element that shows the remaining time until a game starts.
    /// </summary>
    public class GameStartingUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timeRemainingText;
        private const string TIME_REMAINING_UNFORMATTED_TEXT = "<size=56><b>Prepare for Drop</b></size>\nGame Is Starting\n{0}s";
        private const string TIMER_COMPLETE_TEXT = "<size=56><b>Prepare for Drop</b></size>\nGame Is Starting\nNOW";


        private void Update()
        {
            if (ClientPreGameLobbyState.LobbyClosedEstimatedTime <= 0.0f)
                return;

            float timeRemaining = ClientPreGameLobbyState.LobbyClosedEstimatedTime - Time.time;
            _timeRemainingText.text = timeRemaining > 0.0f ? string.Format(TIME_REMAINING_UNFORMATTED_TEXT, Mathf.CeilToInt(timeRemaining)) : TIMER_COMPLETE_TEXT;

        }
    }
}