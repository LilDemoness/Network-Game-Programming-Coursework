using UnityEngine;
using UserInput;
using Utils;
using VContainer;
using VContainer.Unity;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Client specialisation of the Post-Game state.
    /// </summary>
    [RequireComponent(typeof(NetworkPostGame_FFA))]
    public class ClientPostGameState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.PostGameScreen;
        
        private GameMode _currentVotedGameType = GameMode.Invalid;
        [SerializeField] private NetworkPostGame_FFA _networkPostGame;


        [SerializeField] private GameObject _podiumRoot;
        [SerializeField] private GameObject _leaderboardRoot;


        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_networkPostGame);
            if (this.TryGetComponent<NetworkTimer>(out NetworkTimer networkTimer))
                builder.RegisterComponent(networkTimer);
            base.Configure(builder);
        }

        protected override void Awake()
        {
            base.Awake();
            ShowPodium();

            Cursor.lockState = CursorLockMode.None;
            ClientInput.ResetInputPrevention();
        }


        public void ShowPodium()
        {
            _podiumRoot.SetActive(true);
            _leaderboardRoot.SetActive(false);
        }
        public void ShowLeaderboard()
        {
            _podiumRoot.SetActive(false);
            _leaderboardRoot.SetActive(true);
        }
        public void LeaveMatch()
        {
            Debug.Log("Leave Match");
        }
    }
}