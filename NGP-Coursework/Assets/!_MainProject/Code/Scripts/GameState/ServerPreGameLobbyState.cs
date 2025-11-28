using Gameplay.GameplayObjects.Players;
using Netcode.ConnectionManagement;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.GameState
{
    // This class should handle:
    //  - Player Joining
    //  - Processing/Relaying Build Change Requests?
    //  - Starting the game once all players are ready.

    /// <summary>
    ///     Server specialisation of the Pre-Game Lobby game state.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks), typeof(NetworkLobbyState))]
    public class ServerPreGameLobbyState : GameStateBehaviour
    {
        [SerializeField] private NetcodeHooks _netcodeHooks;

        public override GameState ActiveState => GameState.GameLobby;
        public NetworkLobbyState NetworkLobbyState { get; private set; }

        private Coroutine _waitToEndSessionCoroutine;
        private const float LOBBY_READY_TIME = 3.0f;
        private const float LOBBY_LOCKDOWN_TIME = 1.0f; // Must be less than LOBBY_READY_TIME.

        [Inject]
        private ConnectionManager _connectionManager;


        protected override void Awake()
        {
            base.Awake();
            NetworkLobbyState = GetComponent<NetworkLobbyState>();

            _netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            _netcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_netcodeHooks != null)
            {
                _netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                _netcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }
        }

        private void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                // We only want to exist on servers, and this isn't a server.
                this.enabled = false;
                return;
            }

            // Subscribe to events.
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            NetworkLobbyState.OnClientChangedReadyState += OnClientChangedReadyState;

            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            
        }
        private void OnNetworkDespawn()
        {
            // Unsubscribe from events.
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }

            if (NetworkLobbyState != null)
            {
                NetworkLobbyState.OnClientChangedReadyState += OnClientChangedReadyState;
            }
        }



        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete)
                return; // We are only wanting to process 'LoadComplete' events.

            //SeatNewPlayer(sceneEvent.ClientId);
        }
        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType != ConnectionEvent.ClientDisconnected)
                return; // We're only wanting to handle disconnect events.

            // Client Disconnected. Clear their data.
            for(int i = 0; i < NetworkLobbyState.SessionPlayers.Count; ++i)
            {
                if (NetworkLobbyState.SessionPlayers[i].ClientId == connectionEventData.ClientId)
                {
                    NetworkLobbyState.SessionPlayers.RemoveAt(i);
                    break;
                }
            }

            // If all players are now ready, prepare to start the game.
            if (!NetworkLobbyState.IsStartingGame.Value)    // Ensure we're not already starting the game.
            {
                StartGameIfAllPlayersAreReady();
            }
        }

        // Called when a client changes their ready state.
        private void OnClientChangedReadyState(ulong clientId, bool newReadyState)
        {
            if (NetworkLobbyState.IsLobbyLocked.Value == true)
            {
                // Lobby is currently locked and players cannot change that fact.
                return;
            }

            if (newReadyState == true)
            {
                StartGameIfAllPlayersAreReady();
            }
            else if (NetworkLobbyState.IsStartingGame.Value)
            {
                CancelGameStart();
            }
        }


        /// <summary>
        ///     Save the states of each player to their corresponding PersistentPlayer object.
        /// </summary>
        private void SaveSessionResults()
        {
            for(int i = 0; i < NetworkLobbyState.SessionPlayers.Count; ++i)
            {
                NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(NetworkLobbyState.SessionPlayers[i].ClientId);

                if (playerNetworkObject != null && playerNetworkObject.TryGetComponent<PersistentPlayer>(out PersistentPlayer persistentPlayer))
                {
                    // Pass required information to the PersistentPlayer.
                    //persistentPlayer.NetworkBuildState.BuildData.Value = NetworkLobbyState.SessionPlayers[i].BuildData;
                }
            }
        }


        /// <summary>
        ///     Looks through all our connections and checks to make sure that all players are ready.<br/>
        ///     If they all are, start our countdown to gameplay.
        /// </summary>
        private void StartGameIfAllPlayersAreReady()
        {
            for(int i = 0; i < NetworkLobbyState.SessionPlayers.Count; ++i)
            {
                if (!NetworkLobbyState.SessionPlayers[i].IsReady)
                    return; // At least one player hasn't readied up yet, so don't start.
            }

            _waitToEndSessionCoroutine = StartCoroutine(WaitToStartGame());
        }
        /// <summary>
        ///     Cancels the game start.
        /// </summary>
        private void CancelGameStart()
        {
            if (_waitToEndSessionCoroutine != null)
                StopCoroutine(_waitToEndSessionCoroutine);
            
            NetworkLobbyState.IsStartingGame.Value = false;
        }


        /// <summary>
        ///     Prevent further changes to player builds and save our gameplay state.
        /// </summary>
        private void LockDownLobby()
        {
            NetworkLobbyState.IsLobbyLocked.Value = true;
            SaveSessionResults();
        }
        /// <summary>
        ///     Allow changes to the lobby again.
        /// </summary>
        private void UnlockLobby()
        {
            NetworkLobbyState.IsLobbyLocked.Value = false;
        }

        /// <summary>
        ///     Perform the transition to gameplay.
        /// </summary>
        private void TransitionToGameplay()
        {
            throw new System.NotImplementedException("Scene Loader - Load Gameplay Scene");
        }


        private IEnumerator WaitToStartGame()
        {
            // Mark ourselves as starting the game.
            NetworkLobbyState.IsStartingGame.Value = true;

            yield return new WaitForSeconds(LOBBY_READY_TIME - LOBBY_LOCKDOWN_TIME);

            // Prevent any changes to the lobby and save our current state.
            LockDownLobby();

            yield return new WaitForSeconds(LOBBY_LOCKDOWN_TIME);

            // Transition to Gameplay.
            TransitionToGameplay();
        }
    }
}