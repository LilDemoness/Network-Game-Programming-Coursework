using Gameplay.GameState;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    ///     A class representing a single row of a Leaderboard.<br/>
    ///     Handles updating the row's information to match the stats of the given player.
    /// </summary>
    public class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _placeText;
        [SerializeField] private TextMeshProUGUI _playerNameText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _killsCountText;
        [SerializeField] private TextMeshProUGUI _deathsCountText;


        public void SetPlace(int placeNumber) => _placeText.text = placeNumber.ToString();
        public void SetPlaceWithSuffix(int placeNumber) =>  _placeText.text = ConvertToStringWithSuffix(placeNumber);
        private string ConvertToStringWithSuffix(int number)    // Returns a string of the number with its suffix/ordinal indicator.
        {
            if (Mathf.Floor(number / 10) == 1)
            {
                // Handle exception: All teens are -th.
                return number + "th";
            }
            else
            {
                return number + (GetFirstDigit(number) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th",
                });
            }


            // From: 'https://stackoverflow.com/a/701621'.
            int GetFirstDigit(int i)
            {
                if (i >= 100000000) i /= 100000000;
                if (i >= 10000) i /= 10000;
                if (i >= 100) i /= 100;
                if (i >= 10) i /= 10;
                return i;
            }
        }
        
        
        public void SetInformation(string playerName, int score, int killsCount, int deathsCount)
        {
            // Set Name.
            _playerNameText.text = playerName;


            // Set Stats.
            if (_scoreText)
                _scoreText.text = score.ToString();
            if (_killsCountText)
                _killsCountText.text = killsCount.ToString();
            if (_deathsCountText)
                _deathsCountText.text = deathsCount.ToString();
        }
    }
}