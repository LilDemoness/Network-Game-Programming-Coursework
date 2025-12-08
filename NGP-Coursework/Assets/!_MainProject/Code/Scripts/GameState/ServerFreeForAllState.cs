using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using Gameplay.Messages;
using Infrastructure;
using Netcode.ConnectionManagement;
using SceneLoading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Utils;
using VContainer;
using VContainer.Unity;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Server specialisation of the logic for a Free-For-All Game Match.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks), typeof(NetworkFFAGameplayState), typeof(NetworkTimer))]
    public class ServerFreeForAllState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.InGameplay;


        [SerializeField] private NetcodeHooks _netcodeHooks;
        [SerializeField] private NetworkFFAGameplayState _networkGameplayState;
        [SerializeField] private NetworkTimer _networkTimer;


        [SerializeField] private float _gameTime = 60.0f;
        [SerializeField] private float _matchTimeSyncInterval = 15.0f;
        private float _nextSyncTime;


        [Space(10)]
        [Tooltip("If true, then Score = Kills - Deaths. If false, Score = Kills.")]
        [SerializeField] private bool _deathsCountAsLostPoints = false;
        public bool DeathsCountAsLostPoints => _deathsCountAsLostPoints;


        [Header("Player Spawning")]
        [SerializeField] private NetworkObject _playerPrefab;
        private bool _initialSpawnsComplete = false;


        [Inject]
        private ISubscriber<LifeStateChangedEventMessage> _lifeStateChangedEventMessageSubscriber;

        [Inject]
        private PersistentGameState _persistentGameState;   // Used to transfer score between the Gameplay and Post-Game States.


        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent<NetworkFFAGameplayState>(_networkGameplayState);
            builder.RegisterComponent<NetworkTimer>(_networkTimer);
        }


        protected override void Awake()
        {
            base.Awake();
            _netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            _netcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;

            _networkTimer.OnTimerElapsed += EndGame;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
            _netcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;

            _networkTimer.OnTimerElapsed -= EndGame;
        }

        private void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                this.enabled = false;
                return;
            }

            _persistentGameState.Reset();
            _lifeStateChangedEventMessageSubscriber.Subscribe(OnLifeStateChangedEventMessage);

            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            SceneLoader.OnAllRequestedScenesLoaded += OnMapLoaded;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;

            SessionManager<SessionPlayerData>.Instance.OnSessionStarted();
        }
        private void OnNetworkDespawn()
        {
            if (_lifeStateChangedEventMessageSubscriber != null)
                _lifeStateChangedEventMessageSubscriber.Unsubscribe(OnLifeStateChangedEventMessage);

            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
            SceneLoader.OnAllRequestedScenesLoaded -= OnMapLoaded;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
        }

        private void Update()
        {
            if (!_initialSpawnsComplete)
                return;

            if(_nextSyncTime <= Time.time)
            {
                _networkTimer.SyncGameTime();
                _nextSyncTime += _matchTimeSyncInterval;
            }
        }



        // Server-only.
        private void OnLifeStateChangedEventMessage(LifeStateChangedEventMessage message)
        {
            Debug.Log($"Message! SenderId: {message.OriginCharacterObjectId}, InflicterId: {message.InflicterObjectId}, Name: {message.CharacterName}, LifeState: {message.NewLifeState}");
            if (message.NewLifeState != LifeState.Dead)
                return; // We're only wanting to process death events.


            // If the inflicter was a ServerCharacter, increment their kills.
            if (message.HasInflicter)
            {
                NetworkObject inflicterNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[message.InflicterObjectId];
                if (inflicterNetworkObject.TryGetComponent<ServerCharacter>(out ServerCharacter inflicterServerCharacter))
                {
                    _networkGameplayState.IncrementScore(inflicterServerCharacter);
                }
            }

            NetworkObject originCharacterNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[message.OriginCharacterObjectId];
            if (originCharacterNetworkObject.TryGetComponent<ServerCharacter>(out ServerCharacter originServerCharacter))
            {
                // Add a Death to the character who died.
                _networkGameplayState.OnCharacterDied(originServerCharacter);

                // Start the respawn of the character who died.
                _networkGameplayState.StartRespawn(originServerCharacter);
            }
        }
        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)  // Triggered when a client connects or disconnects from the server.
        {
            if (connectionEventData.EventType != ConnectionEvent.ClientDisconnected)
                return; // We only wish to handle disconnection events.

            if (connectionEventData.ClientId == networkManager.LocalClientId)
                return; // The host has disconnected, but the server will be getting shutdown so we don't need to do anything.

            // A client has disconnected. In a limited-life mode, we would also check for a Game Over here (After a frame to allow for the client's player to be removed/despawned).
            _networkGameplayState.OnPlayerLeft(connectionEventData.ClientId);
        }
        private void OnMapLoaded() // Triggered once all requested scenes have been loaded for all clients.
        {
            // Wait a frame to ensure that all EntitySpawnPoints have registered.
            StartCoroutine(FinishLoadAfterFrame());
        }
        public IEnumerator FinishLoadAfterFrame()
        {
            // Wait for "Awake" events to be called.
            yield return null;

            // Spawn all players.
            _initialSpawnsComplete = true;
            ServerCharacter[] playerCharacters = new ServerCharacter[NetworkManager.Singleton.ConnectedClients.Count];
            List<EntitySpawnPoint> spawnPoints = EntitySpawnPoint.GetInitialSpawnPoints(EntitySpawnPoint.EntityTypes.Player, -1, playerCharacters.Length);
            int index = 0;
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                playerCharacters[index] = SpawnPlayer(kvp.Key, false, spawnPoints[index]);
                ++index;
            }

            // Start the game.
            StartGame(playerCharacters);
        }

        private void OnSynchronizeComplete(ulong clientId)  // Triggered once a newly approved client has finished synchonizing the current game session.
        {
            if (!_initialSpawnsComplete)
                return; // This new client's spawn will be handled in the inital spawn wave.

            //if (__PlayerExists__)
            //    return;   // This client already exists within the game.
            // A client has joined after the initial spawn.

            SessionPlayerData? potentialData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
            bool isRejoin = potentialData.HasValue && potentialData.Value.HasCharacterSpawned;
            Debug.Log("Is Rejoin? " + isRejoin);

            if (isRejoin)
                FinishPlayerJoin(clientId);
            else
                PromptPlayerForInitialCustomisation(clientId);

        }
        private void PromptPlayerForInitialCustomisation(ulong clientId) => _networkGameplayState.PromptInitialCustomisation(clientId, FinishPlayerJoin);
        private void FinishPlayerJoin(ulong clientId)
        {
            ServerCharacter playerCharacter = SpawnPlayer(clientId, true, EntitySpawnPoint.GetRandomSpawnPoint(EntitySpawnPoint.EntityTypes.Player, -1));
            _networkGameplayState.AddPlayer(playerCharacter);
            _networkTimer.SyncGameTime();
        }
        


        
        private void StartGame(ServerCharacter[] playerCharacters)
        {
            _networkGameplayState.Initialise(playerCharacters, null);

            // Time limits.
            _networkTimer.StartTimer(_gameTime);
        }
        private void EndGame()
        {
            // Save data to the PersistentGameState for retrieval in the Post-Game State.
            _networkGameplayState.SavePersistentData(ref _persistentGameState);
            FFAPostGameData.CountDeathsAsNegativePoints = _deathsCountAsLostPoints; // Find a better place to put this.

            SceneLoader.Instance.LoadPostGameScene(GameMode.FreeForAll);
        }


        private ServerCharacter SpawnPlayer(ulong clientId, bool isLateJoin, EntitySpawnPoint spawnPoint)
        {
            // Get the PersistenPlayer Object, throwing an error if none exists for this client.
            if (!NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).TryGetComponent<PersistentPlayer>(out PersistentPlayer persistentPlayer))
                Debug.LogError($"No matching persistent PersistentPlayer for client {clientId} was found");

            Debug.Log($"SpawnPlayer in {spawnPoint.transform.position}");

            // Retrieve our cached data (If this player has already joined & been spawned).
            SessionPlayerData ? sessionPlayerData = isLateJoin ? SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId) : null;
            if (sessionPlayerData is { HasCharacterSpawned: false })
                sessionPlayerData = null;

            // Instantiate the Player.
            NetworkObject newPlayer = Instantiate<NetworkObject>(_playerPrefab, Vector3.zero, Quaternion.identity);
            ServerCharacter newPlayerServerCharacter = newPlayer.GetComponent<ServerCharacter>();

            // Set the player's spawn position.
            if (spawnPoint != null)
            {
                newPlayerServerCharacter.Movement.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
                spawnPoint.SpawnAtPoint();
            }
            if (sessionPlayerData.HasValue && sessionPlayerData.Value.HasCharacterSpawned)
            {
                // Restore the player's previous position.
                Debug.LogWarning("Remve this?");
                newPlayerServerCharacter.Movement.SetPositionAndRotation(sessionPlayerData.Value.PlayerPosition, sessionPlayerData.Value.PlayerRotation);
            }

            // Instantiate NetworkVariables with their values to ensure that they're ready for use on OnNetworkSpawn.
            // Note: Player Builds are handled by the 'Player' and 'PersistentPlayer' scripts.

            // Pass required data from PersistentPlayer to the player instance.
            if (newPlayer.TryGetComponent<NetworkNameState>(out NetworkNameState networkNameState))
                networkNameState.Name = new NetworkVariable<FixedPlayerName>(persistentPlayer.NetworkNameState.Name.Value);
            //newPlayerServerCharacter.TeamID.Value = sessionPlayerData.HasValue ? sessionPlayerData.Value.TeamIndex : persistentPlayer.PlayerNumber;


            // Cache required values.
            persistentPlayer.SavePlayerData();


            // Spawn the Player Character.
            newPlayer.SpawnWithOwnership(clientId, destroyWithScene: true);
            return newPlayerServerCharacter;
        }
    }
}