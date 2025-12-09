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
        [SerializeField] private LeaderboardRow _leaderboardRowPrefab;
        private List<LeaderboardRow> _leaderboardRowInstances = new List<LeaderboardRow>();

        // Injected via DI.
        private NetworkPostGame_FFA _networkPostGame;

        [Inject]
        private void InjectDependenciesAndSubscribe(NetworkPostGame_FFA networkPostGame)
        {
            this._networkPostGame = networkPostGame;
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
                    LeaderboardRow leaderboardRow = Instantiate<LeaderboardRow>(_leaderboardRowPrefab, _leaderboardValuesContainer);
                    _leaderboardRowInstances.Add(leaderboardRow);
                    // Note: We don't need to worry about removing instances as the scene is unloaded & reset between matches,
                    //  and we're not caring about the removal of players who leave during this screen.
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