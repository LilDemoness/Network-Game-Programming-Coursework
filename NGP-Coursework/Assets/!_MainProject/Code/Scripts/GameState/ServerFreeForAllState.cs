using System.Collections;
using System.Collections.Generic;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using Gameplay.Messages;
using Infrastructure;
using Netcode.ConnectionManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using VContainer;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Server specialisation of the logic for a Free-For-All Game Match.
    /// </summary>
    public class ServerFreeForAllState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.InGameplay;


        [SerializeField] private NetcodeHooks _netcodeHooks;


        [Header("Player Spawning")]
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private Transform[] _playerSpawnPoints;
        private List<Transform> _initialSpawnPointsList;

        private bool _initialSpawnsComplete = false;


        [Header("Player Respawning")]
        private const bool USE_GROUPED_RESPAWNS = true;  // If true, respawns are grouped together at the time which is the nearest multiple of '_respawnDelay'.
        private const float RESPAWN_DELAY = 5.0f;
        private const float MIN_RESPAWN_DELAY = 2.0f;


        [Inject]
        private ISubscriber<LifeStateChangedEventMessage> _lifeStateChangedEventMessageSubscriber;

        [Inject]
        private PersistentGameState _persistentGameState;   // Used to transfer score between the Gameplay and Post-Game States.


        protected override void Awake()
        {
            base.Awake();
            _netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            _netcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
            _netcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
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
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
        }
        private void OnNetworkDespawn()
        {
            if (_lifeStateChangedEventMessageSubscriber != null)
                _lifeStateChangedEventMessageSubscriber.Unsubscribe(OnLifeStateChangedEventMessage);

            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
        }



        private void OnLifeStateChangedEventMessage(LifeStateChangedEventMessage message)
        {
            Debug.Log($"Message! SenderId: {message.OriginCharacterObjectId}, InflicterId: {message.InflicterObjectId}, Name: {message.CharacterName}, LifeState: {message.NewLifeState}");
            if (message.NewLifeState != LifeState.Dead)
                return; // We're only wanting to process death events.


            // If the inflicter was a ServerCharacter, give their team a point.
            if (message.HasInflicter)
            {
                NetworkObject inflicterNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[message.InflicterObjectId];
                if (inflicterNetworkObject.TryGetComponent<ServerCharacter>(out ServerCharacter serverCharacter))
                {
                    _persistentGameState.IncrementScore(serverCharacter.TeamID.Value);
                }
            }

            // If the origin was a Player, mark them for respawn.
            NetworkObject originCharacterNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[message.OriginCharacterObjectId];
            if (originCharacterNetworkObject.TryGetComponent<Player>(out Player player))
            {
                RevivePlayer(player);
            }
        }
        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)  // Triggered when a client connects or disconnects from the server.
        {
            if (connectionEventData.EventType != ConnectionEvent.ClientDisconnected)
                return; // We only wish to handle disconnection events.

            if (connectionEventData.ClientId == networkManager.LocalClientId)
                return; // The host has disconnected, but the server will be getting shutdown so we don't need to do anything.

            // A client has disconnected. In a limited-life mode, we would check for a Game Over here (After a frame to allow for the client's player to be removed/despawned).
        }
        private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) // Triggered once all clients have finished loading a scene.
        {
            if (_initialSpawnsComplete || loadSceneMode != LoadSceneMode.Single)
                return; // We have either performed the initial spawn, or this scene is being added to the game and we don't want to spawn players yet.

            // Spawn all players.
            _initialSpawnsComplete = true;
            foreach(var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                SpawnPlayer(kvp.Key, false);
            }
        }
        private void OnSynchronizeComplete(ulong clientId)  // Triggered once a newly approved client has finished synchonizing the current game session.
        {
            if (!_initialSpawnsComplete)
                return; // This new client's spawn will be handled in the inital spawn wave.

            //if (__PlayerExists__)
            //    return;   // This client already exists within the game.

            // A client has joined after the initial spawn.
            SpawnPlayer(clientId, true);
        }


        private void SpawnPlayer(ulong clientId, bool isLateJoin)
        {
            _initialSpawnPointsList ??= new List<Transform>(_playerSpawnPoints);

            Debug.Assert(_initialSpawnPointsList.Count > 0, "We ran out of spawn points. Ensure that there are enough spawn points within the _playerSpawnPoints array.");

            // Get our spawn point randomly and remove it from our initial spawns list to ensure that duplicate spawns don't occur.
            int selectedIndex = Random.Range(0, _initialSpawnPointsList.Count);
            Transform spawnPoint = _initialSpawnPointsList[selectedIndex];
            _initialSpawnPointsList.RemoveAt(selectedIndex);

            // Get the PersistenPlayer Object, throwing an error if none exists for this client.
            if (!NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).TryGetComponent<PersistentPlayer>(out PersistentPlayer persistentPlayer))
                Debug.LogError($"No matching persistent PersistentPlayer for client {clientId} was found");


            // Instantiate the Player.
            NetworkObject newPlayer = Instantiate<NetworkObject>(_playerPrefab, Vector3.zero, Quaternion.identity);
            ServerCharacter newPlayerServerCharacter = newPlayer.GetComponent<ServerCharacter>();
            Transform playerPhysicsTransform = newPlayerServerCharacter.transform;

            // Set the player's spawn position.
            if (spawnPoint != null)
            {
                playerPhysicsTransform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
            if (isLateJoin)
            {
                // Check if this is a reconnection. If so, set the player's position & rotation to their previous position?
                Debug.LogWarning("Determine if we should keep");
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
                if (sessionPlayerData is { HasCharacterSpawned: true })
                {
                    playerPhysicsTransform.SetPositionAndRotation(sessionPlayerData.Value.PlayerPosition, sessionPlayerData.Value.PlayerRotation);
                }
            }

            // Instantiate NetworkVariables with their values to ensure that they're ready for use on OnNetworkSpawn.

            // Pass required data from PersistentPlayer to the player instance.
            if (newPlayer.TryGetComponent<NetworkNameState>(out NetworkNameState networkNameState))
                networkNameState.Name = new NetworkVariable<FixedPlayerName>(persistentPlayer.NetworkNameState.Name.Value);
            // Note: Player Builds are handled by the 'Player' and 'PersistentPlayer' scripts.
            newPlayerServerCharacter.TeamID = new NetworkVariable<int>(persistentPlayer.PlayerNumber);

            // Spawn the Player Character.
            newPlayer.SpawnWithOwnership(clientId, destroyWithScene: true);
        }


        private void RevivePlayer(Player player)
        {
            float respawnDelay = GetRespawnDelay();
            StartCoroutine(ReviveAfterDelay(player, respawnDelay));
        }
        public static float GetRespawnDelay()
        {
            if (USE_GROUPED_RESPAWNS)
            {
                float serverTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
                float respawnDelay = (Mathf.Ceil(serverTime / RESPAWN_DELAY) * RESPAWN_DELAY) - serverTime;
                return respawnDelay >= MIN_RESPAWN_DELAY ? respawnDelay : respawnDelay + RESPAWN_DELAY; // Prevent being under our minimum respawn time by moving to the next multiple if we are below.
            }
            //else
            //    return RESPAWN_DELAY;
        }
        private IEnumerator ReviveAfterDelay(Player player, float respawnDelay)
        {
            yield return new WaitForSeconds(respawnDelay);

            Transform spawnPoint = _playerSpawnPoints[Random.Range(0, _playerSpawnPoints.Length)];
            player.PerformRespawn(spawnPoint.position, spawnPoint.rotation);
        }
    }
}