using Gameplay.GameState;
using TMPro;
using UnityEngine;

namespace UI.PostGame
{
    /// <summary>
    ///     A class representing a single row of a Leaderboard.<br/>
    ///     Handles updating the row's information to match the stats of the given player.
    /// </summary>
    public class PostGameLeaderboardRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _placeText;
        [SerializeField] private TextMeshProUGUI _playerNameText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _killsCountText;
        [SerializeField] private TextMeshProUGUI _deathsCountText;


        public void SetPlace(int placeNumber) => _placeText.text = placeNumber.ToString();

        public void SetInformation(string playerName, int killsCount, int deathsCount)
        {
            // Set Name.
            _playerNameText.text = playerName;


            // Set Stats.
            if (_killsCountText)
                _killsCountText.text = killsCount.ToString();
            if (_deathsCountText)
                _deathsCountText.text = deathsCount.ToString();
        }
    }
}