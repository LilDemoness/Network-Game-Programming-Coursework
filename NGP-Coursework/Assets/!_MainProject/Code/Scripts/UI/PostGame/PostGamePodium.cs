using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameState;
using UnityEngine;
using VContainer;

namespace UI.PostGame
{
    /// <summary>
    ///     Class for the podium which displays the top players in a match.
    /// </summary>
    public class PostGamePodium : MonoBehaviour
    {
        private FFAPostGameData[] _postGameData;


        [Header("Podium Models")]
        [SerializeField] private PlayerCustomisationDisplay[] _podiumDummies;


        [Header("UI")]
        [SerializeField] private Canvas _uiContainerCanvas;

        [SerializeField] private LeaderboardRow[] _podiumLeaderboardElements;
        private const int PODIUM_POSITIONS = 3;

        [SerializeField] private GameObject _thisPlayerUIIdentifier;
        [SerializeField] private Vector3 _thisPlayerIdentifierOffset = new Vector3(0.0f, -275.0f, 0.0f);



        // Injected via DI.
        private NetworkPostGame_FFA _networkPostFFAState;
        [Inject]
        private void InjectDependenciesAndSubscribe(NetworkPostGame_FFA networkPostFFAState)
        {
            this._networkPostFFAState = networkPostFFAState;
            _networkPostFFAState.OnScoresSet += OnGameComplete;
        }

        private void OnDestroy()
        {
            if (_networkPostFFAState != null)
                _networkPostFFAState.OnScoresSet -= OnGameComplete;
        }


        public void OnGameComplete()
        {
            int displayCount = Mathf.Min(_networkPostFFAState.PostGameData.Length, PODIUM_POSITIONS);   // Display the desired number of podium positions, or the number of players if that is smaller.
            _postGameData = new FFAPostGameData[displayCount];
            for(int i = 0; i < displayCount; ++i)
                _postGameData[i] = _networkPostFFAState.PostGameData[i];

            // Show Podium Leaderboard (Top X Players).
            UpdatePodiumUI();

            // Spawn Player Models.
            SpawnPlayerModels();
        }


        private void UpdatePodiumUI()
        {
            _thisPlayerUIIdentifier.SetActive(false);

            int dataCount = _postGameData.Length;
            for (int i = 0; i < PODIUM_POSITIONS; ++i)
            {
                if (i >= dataCount)
                {
                    // We don't have enough data to populate this position (There likely aren't enough players in the match).
                    // Hide this leaderboard element.
                    _podiumLeaderboardElements[i].gameObject.SetActive(false);
                    continue;
                }
                else if (_podiumLeaderboardElements[i].gameObject.activeSelf)
                    _podiumLeaderboardElements[i].gameObject.SetActive(true);  // This element was previously disabled, but we're wanting to use it. Enable it.

                // Populate the UI Element.
                _podiumLeaderboardElements[i].SetPlaceWithSuffix(i + 1);
                _podiumLeaderboardElements[i].SetInformation(
                    playerName: _postGameData[i].Name,
                    score: _postGameData[i].Score,
                    killsCount: _postGameData[i].Kills,
                    deathsCount: _postGameData[i].Deaths);


                // If this entry relates to this client, mark it as such.
                if (i == _networkPostFFAState.ThisClientDataIndex)
                {
                    // Enable the indicator.
                    _thisPlayerUIIdentifier.SetActive(true);

                    // Position the indicator.
                    Vector3 indicatorPosition = _podiumLeaderboardElements[i].transform.localPosition;
                    indicatorPosition.z = _thisPlayerUIIdentifier.transform.localPosition.z;
                    indicatorPosition += _thisPlayerIdentifierOffset;
                    _thisPlayerUIIdentifier.transform.localPosition = indicatorPosition;
                }
            }
        }

        private void SpawnPlayerModels()
        {
            int dataCount = _postGameData.Length;
            for (int i = 0; i < PODIUM_POSITIONS; ++i)
            {
                if (i >= dataCount)
                {
                    // We don't have enough data to populate this position (There likely aren't enough players in the match).
                    // Hide this dummy.
                    _podiumDummies[i].gameObject.SetActive(false);
                    continue;
                }
                else if (!_podiumDummies[i].gameObject.activeSelf)
                    _podiumDummies[i].gameObject.SetActive(true);   // This dummy was previously disabled, but we're wanting to use it. Enable it.

                BuildData playerBuildData = new BuildData(_postGameData[i].FrameIndex, _postGameData[i].SlottableIndicies);
                _podiumDummies[i].UpdateDummy(playerBuildData);
            }
        }



#if UNITY_EDITOR

        private void OnValidate()
        {
            if (_podiumLeaderboardElements.Length != PODIUM_POSITIONS)
                Debug.LogError($"Podium Leaderboard's Podium Leaderboard Elements UI count ({_podiumLeaderboardElements.Length}) doesn't match the number of podium positions {PODIUM_POSITIONS}");

            if (_podiumDummies.Length != PODIUM_POSITIONS)
                Debug.LogError($"Podium Leaderboard's Player Dummies count ({_podiumDummies.Length}) doesn't match the number of podium positions {PODIUM_POSITIONS}");
        }

#endif
    }
}
