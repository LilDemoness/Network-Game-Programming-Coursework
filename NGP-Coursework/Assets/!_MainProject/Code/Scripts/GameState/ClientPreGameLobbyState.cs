using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Players;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UI;
using UI.Lobby;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Client specialisation of the Pre-Game Lobby game state.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientPreGameLobbyState : GameStateBehaviour
    {
        /// <summary>
        ///     Conceptual modes/states/stages that the session can be in.<br/>
        ///     Not tracked. Instead used as an abstraction for readability and ease of UI configuration.
        /// </summary>
        enum SessionMode
        {
            Unready = 0,    // The player isn't yet readied.
            Ready,          // The player is ready, but the game isn't starting yet.
            GameStarting,   // The game is starting, but players can still unready.
            LobbyLocked,    // The game is starting, and players cannot change their ready state.
            FatalError      // An error has occured.
        }
        private const int SESSION_MODE_COUNT = 5;


        public static ClientPreGameLobbyState Instance { get; private set; }
        public override GameState ActiveState => GameState.GameLobby;

        [SerializeField] private NetcodeHooks _netcodeHooks;
        [SerializeField] private NetworkLobbyState _networkLobbyState;


        [SerializeField] private CustomisationDummyManager _customisationDummyManager;


        [Header("UI Element References")]
        [SerializeField] private TextMeshProUGUI _playerCountText;

        [Space(5)]
        [SerializeField] private NonNavigableButton _readyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonText;

        [Space(5)]
        [SerializeField] private Transform _playerReadyIndicatorRoot;
        [SerializeField] private ReadyCheckMark _playerReadyIndicatorPrefab;
        private List<ReadyCheckMark> _playerReadyIndicatorInstances = new List<ReadyCheckMark>();


        [Header("UI Elements for Different Session Modes")]
        [SerializeField] private List<GameObject> _uiElementsForUnready;
        [SerializeField] private List<GameObject> _uiElementsForReady;
        [SerializeField] private List<GameObject> _uiElementsForGameStarting;
        [SerializeField] private List<GameObject> _uiElementsForLobbyLocked;
        [SerializeField] private List<GameObject> _uiElementsForFatalError;

        private Dictionary<SessionMode, List<GameObject>> _sessionUIElementsByMode;


        private int _localPlayerNumber = -1;
        private bool _isLocalPlayerReady;

        public static float LobbyClosedEstimatedTime { get; private set; }



        protected override void Awake()
        {
            base.Awake();
            Instance = this;

            _netcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            _netcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;

            _sessionUIElementsByMode = new Dictionary<SessionMode, List<GameObject>>()
            {
                { SessionMode.Unready, _uiElementsForUnready },
                { SessionMode.Ready, _uiElementsForReady },
                { SessionMode.GameStarting, _uiElementsForGameStarting },
                { SessionMode.LobbyLocked, _uiElementsForLobbyLocked },
                { SessionMode.FatalError, _uiElementsForFatalError },
            };

            Cursor.lockState = CursorLockMode.None;
        }
        protected override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            base.OnDestroy();

            if (_netcodeHooks != null)
            {
                _netcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                _netcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }
        }

        protected override void Start()
        {
            base.Start();

            ConfigureUIForSessionMode(SessionMode.Unready);
            //SetReadyState(isReady: false);
        }


        private void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                // Only run on clients.
                this.enabled = false;
                return;
            }

            _networkLobbyState.IsStartingGame.OnValueChanged += OnStartingGameValueChanged;
            _networkLobbyState.IsLobbyLocked.OnValueChanged += OnLobbyLockedValueChanged;
            _networkLobbyState.SessionPlayers.OnListChanged += OnSessionPlayerStateChanged;

            PersistentPlayer.OnPlayerBuildChanged += OnPlayerBuildChanged;
        }
        private void OnNetworkDespawn()
        {
            if (_networkLobbyState != null)
            {
                _networkLobbyState.IsStartingGame.OnValueChanged -= OnStartingGameValueChanged;
                _networkLobbyState.IsLobbyLocked.OnValueChanged -= OnLobbyLockedValueChanged;
                _networkLobbyState.SessionPlayers.OnListChanged -= OnSessionPlayerStateChanged;
            }

            PersistentPlayer.OnPlayerBuildChanged -= OnPlayerBuildChanged;
        }


        private void OnStartingGameValueChanged(bool wasStartingGame, bool isStartingGame)
        {
            if (isStartingGame)
            {
                LobbyClosedEstimatedTime = Time.time + ServerPreGameLobbyState.LOBBY_READY_TIME;
                ConfigureUIForSessionMode(SessionMode.GameStarting);
            }
            else
            {
                // The game could have been starting without the player being ready for several reasons, such as:
                //  - The player has just joined.
                ConfigureUIForSessionMode(_isLocalPlayerReady ? SessionMode.Ready : SessionMode.Unready);
                LobbyClosedEstimatedTime = -1.0f;
            }
        }
        private void OnLobbyLockedValueChanged(bool wasLobbyLocked, bool isLobbyLocked)
        {
            if (isLobbyLocked)
            {
                ConfigureUIForSessionMode(SessionMode.LobbyLocked);
            }
            else
            {
                // The game could have been starting without the player being ready for several reasons, such as:
                //  - The player has just joined.
                //  - The game was starting automatically (Though we shouldn't be unlocking at this point).
                ConfigureUIForSessionMode(_isLocalPlayerReady ? SessionMode.Ready : SessionMode.Unready);
            }
        }
        private void OnSessionPlayerStateChanged(NetworkListEvent<NetworkLobbyState.SessionPlayerState> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<NetworkLobbyState.SessionPlayerState>.EventType.Add)
                Debug.Log("Player Joined");
            else if (changeEvent.Type == NetworkListEvent<NetworkLobbyState.SessionPlayerState>.EventType.Remove || changeEvent.Type == NetworkListEvent<NetworkLobbyState.SessionPlayerState>.EventType.RemoveAt)
                Debug.Log("Player Disconnected");


            UpdatePlayerReadyIndicators();
            UpdatePlayerCountUI();

            if (changeEvent.Value.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                _localPlayerNumber = changeEvent.Value.PlayerNumber;
                UpdateLocalReadyState(changeEvent.Value.IsReady);
            }
        }


        /// <summary>
        ///     Enables/Disables the UI elements for a specified SessionMode.</br>
        ///     Also enables/disables the "Ready" button if it is inappropriate for the given mode.
        /// </summary>
        /// <param name="session"></param>
        private void ConfigureUIForSessionMode(SessionMode mode)
        {
            // Disable all inappropriate UI elements.
            for (int i = 0; i < SESSION_MODE_COUNT; ++i)
            {
                if ((SessionMode)i != mode)
                {
                    foreach (GameObject uiElement in _sessionUIElementsByMode[(SessionMode)i])
                    {
                        uiElement.SetActive(false);
                    }
                }
            }
            // Enable all appropriate UI elements.
            foreach (GameObject uiElement in _sessionUIElementsByMode[mode])
            {
                uiElement.SetActive(true);
            }


            // Update assorted UI elements (Ready Button Interactability & Text, etc).
            switch (mode)
            {
                case SessionMode.Unready:
                    _readyButton.IsInteractable = true;
                    _readyButtonText.text = "READY";
                    break;
                case SessionMode.Ready:
                    _readyButton.IsInteractable = true;
                    _readyButtonText.text = "UNREADY";
                    break;
                case SessionMode.GameStarting:
                    _readyButton.IsInteractable = true;
                    _readyButtonText.text = "UNREADY";
                    break;
                case SessionMode.LobbyLocked:
                    _readyButton.IsInteractable = false;
                    break;
            }
        }

        private void UpdateLocalReadyState(bool isReady)
        {
            if (isReady && !_isLocalPlayerReady)
            {
                // The player has just readied up.
                _isLocalPlayerReady = true;
                ConfigureUIForSessionMode(SessionMode.Ready);
                // Play an animation?
            }
            else if (!isReady && _isLocalPlayerReady)
            {
                // We have just stopped being ready.
                _isLocalPlayerReady = false;
                ConfigureUIForSessionMode(SessionMode.Unready);
                // Play an animation?
            }
        }

        private void UpdatePlayerModel(ulong clientId, BuildData buildData) => _customisationDummyManager.UpdateCustomisationDummy(clientId, buildData);
        

        private void UpdatePlayerReadyIndicators()
        {
            EnsurePlayerReadyIndicatorCount();

            for (int i = 0; i < _networkLobbyState.SessionPlayers.Count; ++i)
            {
                // Update the indicator.
                _playerReadyIndicatorInstances[i].SetToggleText(_networkLobbyState.SessionPlayers[i].PlayerName);
                _playerReadyIndicatorInstances[i].SetToggleVisibility(_networkLobbyState.SessionPlayers[i].IsReady);
            }
        }
        private void EnsurePlayerReadyIndicatorCount()
        {
            int playerCount = _networkLobbyState.SessionPlayers.Count;
            int delta = playerCount - _playerReadyIndicatorInstances.Count;
            if (delta > 0)
            {
                // We need more instances.
                _playerReadyIndicatorInstances.Capacity = playerCount;  // Set capacity so that we don't dynamically resize the list.
                for (int i = 0; i < delta; ++i)
                {
                    ReadyCheckMark readyCheckMark = Instantiate<ReadyCheckMark>(_playerReadyIndicatorPrefab, _playerReadyIndicatorRoot);
                    _playerReadyIndicatorInstances.Add(readyCheckMark);
                }
            }

            // Ensure required instances are enabled, and the rest are disabled.
            for (int i = 0; i < _playerReadyIndicatorInstances.Count; ++i)
            {
                _playerReadyIndicatorInstances[i].gameObject.SetActive(i < playerCount);
            }
        }

        private void UpdatePlayerCountUI()
        {
            int playerCount = _networkLobbyState.SessionPlayers.Count;
            string playerString = (playerCount > 1) ? "players" : "player";
            _playerCountText.text = $"<b>{playerCount}</b> {playerString} connected";
        }




        public void OnReadyButtonPressed() => SetReadyState(!_isLocalPlayerReady);
        public void OnPlayerBuildChanged(ulong clientId, BuildData buildData)
        {
            UpdatePlayerModel(clientId, buildData);
        }

        private void SetReadyState(bool isReady)
        {
            if (!_networkLobbyState.IsSpawned)
                return;

            _networkLobbyState.ChangeReadyStateServerRpc(NetworkManager.Singleton.LocalClientId, _localPlayerNumber, isReady);
        }
    }
}