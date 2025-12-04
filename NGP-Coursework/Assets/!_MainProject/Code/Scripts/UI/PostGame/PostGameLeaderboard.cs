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
        [SerializeField] private PostGameLeaderboardRow _leaderboardRowPrefab;
        private List<PostGameLeaderboardRow> _leaderboardRowInstances = new List<PostGameLeaderboardRow>();

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
            int currentInstancesCount = _leaderboardRowInstances.Count;
            for (int i = 0; i < _networkPostGame.PostGameData.Length; ++i)
            {
                if (i >= currentInstancesCount)
                {
                    // Create a new row.
                    PostGameLeaderboardRow leaderboardRow = Instantiate<PostGameLeaderboardRow>(_leaderboardRowPrefab, _leaderboardValuesContainer);
                    _leaderboardRowInstances.Add(leaderboardRow);
                }

                // Populate the UI Element.
                _leaderboardRowInstances[i].SetPlace(i);
                _leaderboardRowInstances[i].SetInformation(
                    playerName:     _networkPostGame.PostGameData[i].Name,
                    score:          _networkPostGame.PostGameData[i].Score,
                    killsCount:     _networkPostGame.PostGameData[i].Kills,
                    deathsCount:    _networkPostGame.PostGameData[i].Deaths);
            }
        }
    }
}