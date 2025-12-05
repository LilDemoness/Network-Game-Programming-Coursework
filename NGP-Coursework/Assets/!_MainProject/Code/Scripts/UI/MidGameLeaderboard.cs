using System.Collections.Generic;
using Gameplay.GameState;
using TMPro;
using UnityEngine;
using UserInput;
using VContainer;

namespace UI
{
    public class MidGameLeaderboard : MonoBehaviour
    {
        [SerializeField] private Transform _leaderboardValuesContainer;
        [SerializeField] private LeaderboardRow _leaderboardRowPrefab;
        private List<LeaderboardRow> _leaderboardRowInstances = new List<LeaderboardRow>();

        // Injected via DI.
        private NetworkFFAGameplayState _networkPostGame;

        [Inject]
        private void InjectDependenciesAndSubscribe(NetworkFFAGameplayState networkPostGame)
        {
            this._networkPostGame = networkPostGame;
            _networkPostGame.PlayerData.OnListChanged += OnListChanged;
            InitialiseUI();
        }

        private void Awake()
        {
            HideLeaderboard();
            ClientInput.OnToggleLeaderboardPerformed += ToggleLeaderboard;
        }
        private void OnDestroy()
        {
            ClientInput.OnToggleLeaderboardPerformed -= ToggleLeaderboard;

            if (_networkPostGame != null)
                _networkPostGame.PlayerData.OnListChanged -= OnListChanged;
        }


        private void ToggleLeaderboard()
        {
            if (this.gameObject.activeSelf)
                HideLeaderboard();
            else
                ShowLeaderboard();
        }
        [ContextMenu("Show")]
        private void ShowLeaderboard() => this.gameObject.SetActive(true);
        [ContextMenu("Hide")]
        private void HideLeaderboard() => this.gameObject.SetActive(false);


        private void OnListChanged(Unity.Netcode.NetworkListEvent<NetworkFFAGameplayState.PlayerGameData> changeEvent) => InitialiseUI();
        private void InitialiseUI()
        {
            int currentInstancesCount = _leaderboardRowInstances.Count;
            for (int i = 0; i < _networkPostGame.PlayerData.Count; ++i)
            {
                if (i >= currentInstancesCount)
                {
                    // Create a new row.
                    LeaderboardRow leaderboardRow = Instantiate<LeaderboardRow>(_leaderboardRowPrefab, _leaderboardValuesContainer);
                    _leaderboardRowInstances.Add(leaderboardRow);
                }

                // Populate the UI Element.
                _leaderboardRowInstances[i].SetPlace(i);
                _leaderboardRowInstances[i].SetInformation(
                    playerName:     _networkPostGame.PlayerData[i].Name,
                    score:          _networkPostGame.PlayerData[i].Score,
                    killsCount:     _networkPostGame.PlayerData[i].Kills,
                    deathsCount:    _networkPostGame.PlayerData[i].Deaths);
            }
        }
    }
}