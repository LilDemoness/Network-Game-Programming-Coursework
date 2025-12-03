using Gameplay.Actions;
using Netcode.ConnectionManagement;
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
    [RequireComponent(typeof(NetcodeHooks), typeof(NetworkPostGame))]
    public class ServerPostGameState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.PostGameScreen;


        [SerializeField] private NetcodeHooks _netcodeHooks;
        [field:SerializeField] public NetworkPostGame NetworkPostGame { get; private set;}


        [Inject]
        private ConnectionManager _connectionManager;
        [Inject]
        private PersistentGameState _persistentGameState;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(NetworkPostGame);
        }


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
        }



        public void ReturnToLobby()
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MechBuildTestScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            Debug.LogWarning("To Implement - Return to Lobby");
        }
        public void ReturnToMainMenu()
        {
            _connectionManager.RequestShutdown();
        }
    }
}