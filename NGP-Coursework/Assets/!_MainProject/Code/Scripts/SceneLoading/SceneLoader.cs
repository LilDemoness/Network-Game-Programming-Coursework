using Gameplay;
using System.Collections.Generic;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VisualEffects;

namespace SceneLoading
{
    /// <summary>
    ///     Handles the Loading & Unloading of screens, along with triggering calls to the <see cref="ClientLoadingScreen"/>.
    /// </summary>
    public class SceneLoader : NetworkSingleton<SceneLoader>
    {
        #region Scene Name Retrival

        public enum NonGameplayScene
        {
            Startup,
            MainMenu,
            Lobby,
        }
        private static string GetNonGameplaySceneName(NonGameplayScene nonGameplayScene) => nonGameplayScene switch
        {
            NonGameplayScene.Startup => "Startup",
            NonGameplayScene.MainMenu => "MainMenu",
            NonGameplayScene.Lobby => "MechBuildTestScene",
            _ => throw new System.NotImplementedException($"No Scene Name conversion implemented for NonGameplayScene '{nonGameplayScene.ToString()}'")
        };
        private static string GetGameModePersistentSceneName(GameMode gameMode) => gameMode switch
        {
            GameMode.FreeForAll => "PersistentGameMode_FFA",
            _ => throw new System.NotImplementedException($"No Persistent-Game Scene Name conversion implemented for GameMode '{gameMode.ToString()}'")
        };
        private static string GetGameModePostGameScene(GameMode gameMode) => gameMode switch
        {
            GameMode.FreeForAll => "PostGameScene_FFA",
            _ => throw new System.NotImplementedException($"No Post-Game Scene Name conversion implemented for GameMode '{gameMode.ToString()}'")
        };

        [SerializeField] private string[] _gameplayMaps;

        private const string MID_GAME_CUSTOMISATION_SCENE_NAME = "MidGameCustomisation";
#endregion



        [SerializeField] ClientLoadingScreen _clientLoadingScreen;
        [SerializeField] LoadingProgressManager _loadingProgressManager;

        private Queue<string> _additiveSceneLoadRequests = new Queue<string>();


        bool IsNetworkSceneManagementEnabled => NetworkManager != null && NetworkManager.SceneManager != null && NetworkManager.NetworkConfig.EnableSceneManagement;
        private bool _hasInitialised = false;
        private bool _isLoadingScene = false;   // Server-side.


        public static event System.Action OnAllRequestedScenesLoaded;


        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            // Subscribe to events.
            SceneManager.sceneLoaded += OnSceneLoaded;
            NetworkManager.OnServerStarted += OnNetworkingSessionStarted;
            NetworkManager.OnClientStarted += OnNetworkingSessionStarted;
            NetworkManager.OnServerStopped += OnNetworkingSessionEnded;
            NetworkManager.OnClientStopped += OnNetworkingSessionEnded;
        }

        public override void OnDestroy()
        {
            // Unsubscribe from events.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (NetworkManager != null)
            {
                NetworkManager.OnServerStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnClientStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnServerStopped -= OnNetworkingSessionEnded;
                NetworkManager.OnClientStopped -= OnNetworkingSessionEnded;
            }

            base.OnDestroy();
        }


        private void OnNetworkingSessionStarted()
        {
            if (!_hasInitialised)   // Prevents getting called twice on the host (Once as the server, once as a client)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                }

                _hasInitialised = true;
            }
        }
        private void OnNetworkingSessionEnded(bool unused)
        {
            if (_hasInitialised)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
                }

                _hasInitialised = false;
            }
        }


        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!IsSpawned || NetworkManager.ShutdownInProgress)
            {
                _clientLoadingScreen.StopLoadingScreen();
            }
        }
        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:   // The server told the client to load a scene.
                    // Client or Host only.
                    if (NetworkManager.IsClient)
                    {
                        // Start a new loading screen if the scene is being loading in the 'Single' mode (Replacing existing),
                        //      otherwise update the existing loading screen.
                        if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                        {
                            _clientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName);
                            _loadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                        }
                        else
                        {
                            _clientLoadingScreen.UpdateLoadingScreen(sceneEvent.SceneName);
                            _loadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                        }
                    }

                    // Server-only.
                    if (NetworkManager.IsServer && !_isLoadingScene)
                        throw new System.Exception($"A scene is being loaded, but we never set '{nameof(_isLoadingScene)} which will lead to unexpected behaviour if we try to load another scene at the same time.");
                    break;
                case SceneEventType.LoadEventCompleted: // The Server and all Clients finished loading a screen.
                    // Server-only.
                    if (NetworkManager.IsServer)
                    {
                        _isLoadingScene = false;
                        if (_additiveSceneLoadRequests.Count == 0)
                        {
                            // All scene requests have been processed.
                            OnAllRequestedScenesLoaded?.Invoke();

                            // Notify the clients to hide their Loading Screens.
                            StopLoadingScreenClientRpc(RpcTarget.ClientsAndHost);
                        }
                        else
                        {
                            // We have another Scene Load Request.
                            LoadNetworkScene(_additiveSceneLoadRequests.Dequeue(), LoadSceneMode.Additive);
                        }
                    }
                    break;
                case SceneEventType.Synchronize:    // The server told the client to start synchronising scenes.
                    // Non-host Clients only.
                    if (NetworkManager.IsClient && !NetworkManager.IsHost)
                    {
                        if (NetworkManager.SceneManager.ClientSynchronizationMode == LoadSceneMode.Single)
                        {
                            // If using the Single ClientSynchronisationMode, unload all currently loaded additive scenes.
                            //  In this case, we want the client to only keep the same scenes loaded as the server.
                            //  If the server's main scene is the same as the client's, NGO doesn't automatically unload additive scenes,
                            //  which is why we're doing it manually here.
                            //  See '' for more information.
                            UnloadAdditiveScenes();
                        }
                    }
                    break;
                case SceneEventType.SynchronizeComplete:    // A Client told the Server they finished synchronizing.
                    // Server-only.
                    if (NetworkManager.IsServer)
                    {
                        // Send a client RPC to make sure the Client stop the loading screen after the server handles what it needs to do to finish the client sync (E.g. Spawning Characters).
                        StopLoadingScreenClientRpc(RpcTarget.Group(new[] { sceneEvent.ClientId }, RpcTargetUse.Temp));
                    }
                    break;
            }
        }


        private void UnloadAdditiveScenes()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            for(int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != activeScene)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }


        [Rpc(SendTo.Everyone)]
        private void NotifyEveryoneOfAllLoadRequestsProcessedRpc()
        {
            OnAllRequestedScenesLoaded?.Invoke();
            _clientLoadingScreen.StopLoadingScreen();
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void StopLoadingScreenClientRpc(RpcParams rpcParams = default)
        {
            _clientLoadingScreen.StopLoadingScreen();
        }


        #region Scene Loading Functions

        public void LoadNonGameplayScene(NonGameplayScene nonGameplayScene, bool useNetworkSceneManager)
        {
            string sceneName = GetNonGameplaySceneName(nonGameplayScene);

            if (useNetworkSceneManager)
            {
                LoadNetworkScene(sceneName, LoadSceneMode.Single);
            }
            else
            {
                // Load using the SceneManager.
                var loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                _clientLoadingScreen.StartLoadingScreen(sceneName);
                _loadingProgressManager.LocalLoadOperation = loadOperation;
            }
        }
        private void LoadNetworkScene(string sceneName, LoadSceneMode loadSceneMode)
        {
            if (IsSpawned && IsNetworkSceneManagementEnabled && !NetworkManager.ShutdownInProgress)
            {
                if (!NetworkManager.IsServer)
                    return; // Only run this code on Servers.
                

                if (loadSceneMode == LoadSceneMode.Single)
                {
                    _isLoadingScene = true;
                    NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                }
                else
                {
                    if (_isLoadingScene)
                    {
                        _additiveSceneLoadRequests.Enqueue(sceneName);
                    }
                    else
                    {
                        _isLoadingScene = true;
                        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                    }
                }
            }
        }


        public void LoadGameModeAndMap(GameMode gameMode, string mapName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            LoadGameMode(gameMode, loadSceneMode);
            LoadNetworkScene(MID_GAME_CUSTOMISATION_SCENE_NAME, LoadSceneMode.Additive);
            LoadMap(mapName);
        }
        public void LoadGameMode(GameMode gameMode, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            LoadNetworkScene(GetGameModePersistentSceneName(gameMode), loadSceneMode);
        }
        public void LoadMap(string mapName, bool unloadPreviousMaps = true)
        {
            SpecialFXPoolManager.Clear();
            
            if (unloadPreviousMaps)
            {
                Debug.LogWarning("To Implement - Previous Map Reloading");
                //throw new System.NotImplementedException("To Implement - Previous Map Reloading");
            }

            LoadNetworkScene(mapName, LoadSceneMode.Additive);
        }

        public void LoadPostGameScene(GameMode gameMode)
        {
            LoadNetworkScene(GetGameModePostGameScene(gameMode), LoadSceneMode.Single);
        }

#endregion
    }
}