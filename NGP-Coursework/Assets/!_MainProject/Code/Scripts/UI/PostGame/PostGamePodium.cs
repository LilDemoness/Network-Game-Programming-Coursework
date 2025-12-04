using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameState;
using UnityEngine;

namespace UI.PostGame
{
    /// <summary>
    ///     Class for the podium which displays the top players in a match.
    /// </summary>
    public class PostGamePodium : MonoBehaviour
    {
        [Header("Podium Models")]
        [SerializeField] private PlayerCustomisationDisplay[] _podiumDummies;


        [Header("UI")]
        [SerializeField] private CanvasGroup _uiContainerCanvasGroup;

        [SerializeField] private PostGameLeaderboardRow[] _podiumLeaderboardElements;
        private const int PODIUM_POSITIONS = 3;


        [SerializeField] private NetworkPostFFAGame _networkPostFFAState;


        private void Awake()
        {
            _networkPostFFAState.OnScoresSet += OnGameComplete;
        }
        private void OnDestroy()
        {
            _networkPostFFAState.OnScoresSet -= OnGameComplete;
        }


        public void OnGameComplete()
        {
            // Spawn Player Models.
            // Show Podium Leaderboard (Top X Players).
            SpawnPlayerModels();
        }


        public void SpawnPlayerModels()
        {
            int maxDisplay = Mathf.Min(_networkPostFFAState.PostGameData.Length, PODIUM_POSITIONS);
            for(int i = 0; i < maxDisplay; ++i)
            {
                BuildData playerBuildData = new BuildData(_networkPostFFAState.PostGameData[i].FrameIndex, _networkPostFFAState.PostGameData[i].SlottableIndicies);
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
