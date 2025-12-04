using System.Collections.Generic;
using Gameplay.GameState;
using TMPro;
using UnityEngine;
using VContainer;

namespace UI.PostGame
{
    public class PostGameLeaderboard : MonoBehaviour
    {
        [SerializeField] private Transform _leaderboardValuesContainer;
        [SerializeField] private GameObject _leaderboardRowPrefab;
        private List<GameObject> _leaderboardRowInstances;

        [SerializeField] private TextMeshProUGUI _tempText;


        //[Inject]
        [SerializeField] private NetworkPostFFAGame _networkPostGame;


        private void Awake()
        {
            _networkPostGame.OnScoresSet += InitialiseUI;
        }
        private void OnDestroy()
        {
            if (_networkPostGame != null)
                _networkPostGame.OnScoresSet -= InitialiseUI;
        }


        private void InitialiseUI()
        {
            /*
            ----- Teams -----
            Team Name | Score | Players
            
            ----- No Teams -----
            Player Name | Score | Deaths?
            
            */

            string displayString = "";
            for(int i = 0; i < _networkPostGame.PostGameData.Length; ++i)
            {
                displayString += $"{_networkPostGame.PostGameData[i].PlayerIndex}: {_networkPostGame.PostGameData[i].Score}\n";
            }

            _tempText.text = displayString;
        }
    }
}