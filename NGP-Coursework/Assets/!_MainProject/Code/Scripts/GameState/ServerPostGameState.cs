using Gameplay.Actions;
using Netcode.ConnectionManagement;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VContainer;
using VContainer.Unity;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Server specialisation of the Post-Game Lobby game state.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks), typeof(NetworkPostGame_FFA))]
    public class ServerPostGameState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.PostGameScreen;


        [SerializeField] private NetcodeHooks _netcodeHooks;
        [field:SerializeField] public NetworkPostGame_FFA NetworkPostGame { get; private set;}


        [Header("Progressing to the Next Scene")]
        [SerializeField] private float _timeTillNextScene = 2.0f;
        private float _remainingTime;
        private bool _hasHandledMatchTime;


        [Inject]
        private ConnectionManager _connectionManager;
        [Inject]
        private PersistentGameState _persistentGameState;


        protected override void Awake()
        {
            base.Awake();

            _netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
        }
        protected override void OnDestroy()
        {
            ActionFactory.PurgePooledActions();
            _persistentGameState.Reset();

            base.OnDestroy();

            if (_netcodeHooks != null)
                _netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
        }


        private void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                this.enabled = false;
                return;
            }

            // Prepare for the next game.
            SessionManager<SessionPlayerData>.Instance.OnSessionEnded();    // Clears data from removed players.

            _remainingTime = _timeTillNextScene;
            _hasHandledMatchTime = false;
        }


        private void Update()
        {
            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0.0f && !_hasHandledMatchTime)
            {
                _hasHandledMatchTime = true;    // Ensure that we don't double-process this.

                _persistentGameState.GameMode = GetVotedGameType();
                ReturnToLobby();
            }
        }

        /// <summary>
        ///     Determine and return the highest voted <see cref="GameMode"/>, randomly selecting between all the most voted options if they are tied.
        /// </summary>
        private GameMode GetVotedGameType()
        {
            // Find the GameMode(s) with the highest number of votes.
            GameMode[] gameTypes = new GameMode[NetworkPostGame.PlayerVotes.Count];
            int highestVotes = -1;
            int votesInDuplicate = -1;
            for(int i = 0; i < gameTypes.Length; ++i)
            {
                if (NetworkPostGame.PlayerVotes[i] == highestVotes)
                {
                    // This GameType has the same number of votes as the current highest.
                    // Add it to the array so that we can randomly select between them.
                    ++votesInDuplicate;
                    gameTypes[votesInDuplicate] = (GameMode)i;
                }
                else if (NetworkPostGame.PlayerVotes[i] > highestVotes)
                {
                    // This GameType has more votes than the previously highest.
                    // Replace the previously highest votes in the array.
                    votesInDuplicate = 0;
                    gameTypes[votesInDuplicate] = (GameMode)i;
                }
            }

            if (votesInDuplicate < 0)
            {
                // No votes. Choose a random GameType.
                gameTypes = GameMode.Invalid.GetAllGameModes();
                return gameTypes[Random.Range(0, gameTypes.Length)];
            }

            return votesInDuplicate > 0 ? gameTypes[Random.Range(0, votesInDuplicate + 1)] : gameTypes[0];    // Randomly select one of the top voted game types (Or the highest voted GameType if it wasn't a tie).
        }


        public void ReturnToLobby()
        {
            Debug.LogWarning("To Implement - Return to Lobby");
            NetworkManager.Singleton.SceneManager.LoadScene("MechBuildTestScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        public void ReturnToMainMenu()
        {
            _connectionManager.RequestShutdown();
        }
    }
}