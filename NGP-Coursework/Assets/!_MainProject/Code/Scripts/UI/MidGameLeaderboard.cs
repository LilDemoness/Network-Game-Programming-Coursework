using System.Collections;
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
        }

        private void Awake()
        {
            HideLeaderboard();
            ClientInput.OnToggleLeaderboardPerformed += ToggleLeaderboard;
        }
        private void OnEnable() => UpdateUI();  // Always ensure that our UI is updated when we open the Leaderboard.
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


        private void OnListChanged(Unity.Netcode.NetworkListEvent<NetworkFFAGameplayState.PlayerGameData> changeEvent)
        {
            if (this.isActiveAndEnabled)
                StartCoroutine(InitialiseAfterFrame()); // Update the UI after a frame to allow for the data to be sorted before accessing.
        }
        private IEnumerator InitialiseAfterFrame() { yield return null; UpdateUI(); }
        private void UpdateUI()
        {
            int currentInstancesCount = _leaderboardRowInstances.Count;
            int desiredCount = _networkPostGame.GetActualDataCount();

            // Create/Enable and Setup our Leaderboard Rows.
            for (int i = 0; i < desiredCount; ++i)
            {
                if (i >= currentInstancesCount)
                {
                    // Create a new row.
                    LeaderboardRow leaderboardRow = Instantiate<LeaderboardRow>(_leaderboardRowPrefab, _leaderboardValuesContainer);
                    _leaderboardRowInstances.Add(leaderboardRow);
                }
                else if (!_leaderboardRowInstances[i].gameObject.activeSelf)
                    _leaderboardRowInstances[i].gameObject.SetActive(true);

                // Populate the UI Element.
                NetworkFFAGameplayState.PlayerGameData data = _networkPostGame.GetSortedData(i);    // Access the Sorted Data rather than the Unsorted Data, ensuring that our Leaderboard is always in order.
                _leaderboardRowInstances[i].SetPlace(i + 1);
                _leaderboardRowInstances[i].SetInformation(
                    playerName:     data.Name,
                    score:          data.Score,
                    killsCount:     data.Kills,
                    deathsCount:    data.Deaths);
            }

            // Disable unneeded rows (Can occur if a player left, etc).
            for(int i = desiredCount; i < currentInstancesCount; ++i)
                if (_leaderboardRowInstances[i].gameObject.activeSelf)
                    _leaderboardRowInstances[i].gameObject.SetActive(false);
        }
    }
}