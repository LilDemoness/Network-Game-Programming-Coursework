using TMPro;
using UnityEngine;
using VContainer;
using Gameplay;
using Gameplay.GameState;

namespace UI.PostGame
{
    public class VoteForMatchButtonUI : MonoBehaviour
    {
        [SerializeField] private GameMode _gameType;

        [SerializeField] private TextMeshProUGUI _gameTypeText;
        [SerializeField] private TextMeshProUGUI _currentVotesText;


        // Injected via DI.
        private NetworkPostGame_FFA _networkPostGameState;

        [Inject]
        private void InjectDependenciesAndSubscribe(NetworkPostGame_FFA networkPostGameState)
        {
            this._networkPostGameState = networkPostGameState;
            _networkPostGameState.PlayerVotes.OnListChanged += PlayerVotes_OnListChanged;

            if (_networkPostGameState.PlayerVotes.Count > (int)_gameType)
                _currentVotesText.text = _networkPostGameState.PlayerVotes[(int)_gameType].ToString();  // Initialise the text in case we received our dependency late.
        }


        private void Awake()
        {
            InitialiseUI();
        }
        private void OnEnable()
        {
            if (_networkPostGameState != null && _networkPostGameState.PlayerVotes != null && _networkPostGameState.PlayerVotes.Count > (int)_gameType)
                _currentVotesText.text = _networkPostGameState.PlayerVotes[(int)_gameType].ToString();  // Initialise the text in case we received our dependency late.
        }
        private void OnDestroy()
        {
            if (_networkPostGameState != null)
                _networkPostGameState.PlayerVotes.OnListChanged -= PlayerVotes_OnListChanged;
        }


        private void InitialiseUI()
        {
            _gameTypeText.text = ConvertGameTypeToTextString(_gameType);
            _currentVotesText.text = "0";
        }
        private static string ConvertGameTypeToTextString(GameMode gameType)
        {
            return gameType switch
            {
                GameMode.FreeForAll => "Free For All",
                GameMode.TeamDeathmatch => "Team Deathmatch",
                GameMode.KingOfTheHill => "King of the Hill",

                _ => throw new System.NotImplementedException(),
            };
        }

        private void PlayerVotes_OnListChanged(Unity.Netcode.NetworkListEvent<int> changeEvent)
        {
            if (changeEvent.Index != (int)_gameType)
                return; // Not for this button.

            // For this button's GameType. Set our votes counter.
            _currentVotesText.text = changeEvent.Value.ToString();
        }


        public void OnButtonPressed() => _networkPostGameState.SetPlayerVoteServerRpc(_gameType);


#if UNITY_EDITOR

        private void OnValidate()
        {
            if (_gameTypeText != null)
                _gameTypeText.text = ConvertGameTypeToTextString(_gameType);
        }

#endif
    }
}