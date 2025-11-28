using System.Collections.Generic;
using System.Text.RegularExpressions;
using Infrastructure;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using UnityServices.Sessions;
using VContainer;

namespace Gameplay.UI.MainMenu.Session
{
    /// <summary>
    ///     Handles the list of <see cref="SessionListItemUI"/> elements and ensures it stays synchonised with the Session list from the service.
    /// </summary>
    public class SessionJoiningUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [Space(5)]
        [SerializeField] private SessionListItemUI _sessionListItemPrototype;

        [Space(5)]
        [SerializeField] private TMP_InputField _joinCodeField;
        [SerializeField] private Graphic _emptySessionListLabel;
        [SerializeField] private Button _joinSessionButton;


        private IObjectResolver _container;
        private SessionUIMediator _sessionUIMediator;
        private UpdateRunner _updateRunner;
        private ISubscriber<SessionListFetchedMessage> _localSessionsRefreshedSubscriber;

        private List<SessionListItemUI> _sessionListItems = new List<SessionListItemUI>();


        [Inject]
        private void InjectDependenciesAndInitialize(
            IObjectResolver container,
            SessionUIMediator sessionUIMediator,
            UpdateRunner updateRunner,
            ISubscriber<SessionListFetchedMessage> localSessionsRefreshedSubscriber)
        {
            this._container = container;
            this._sessionUIMediator = sessionUIMediator;
            this._updateRunner = updateRunner;
            this._localSessionsRefreshedSubscriber = localSessionsRefreshedSubscriber;

            _localSessionsRefreshedSubscriber.Subscribe(UpdateUI);
        }


        private void Awake()
        {
            _sessionListItemPrototype.gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            if (_updateRunner != null)
                _updateRunner.Unsubscribe(PeriodicRefresh);
        }


        /// <summary>
        ///     Added to the Join Code InputField component's OnValueChanged callback.
        /// </summary>
        public void OnJoinCodeInputTextChanged()
        {
            _joinCodeField.text = SanitizeJoinCode(_joinCodeField.text);
            _joinSessionButton.interactable = _joinCodeField.text.Length > 0;
        }

        private string SanitizeJoinCode(string dirtyString)
        {
            return Regex.Replace(dirtyString.ToUpper(), "[^A-Z0-9]", "");
        }

        public void OnJoinButtonPressed()
        {
            _sessionUIMediator.JoinSessionWithCodeRequest(SanitizeJoinCode(_joinCodeField.text));
        }


        /// <summary>
        ///     Perform a soft refresh without needing to lock the UI and such.
        /// </summary>
        /// <param name="_"></param>
        private void PeriodicRefresh(float _)
        {
            _sessionUIMediator.QuerySessionRequest(false);
        }

        // Called from UI Button.
        public void OnRefreshButtonPressed()
        {
            _sessionUIMediator.QuerySessionRequest(true);
        }

        /// <summary>
        ///     Updates the list of <see cref="SessionListItemUI"/> elements with the given message contents.
        /// </summary>
        private void UpdateUI(SessionListFetchedMessage message)
        {
            // Update all UI Slots, creating & enabling/disabling as necessary.
            EnsureNumberOfActiveUISlots(message.LocalSessions.Count);
            for(int i = 0; i < message.LocalSessions.Count; ++i)
            {
                ISessionInfo localSession = message.LocalSessions[i];
                _sessionListItems[i].SetData(localSession);
            }

            // Toggle the empty sessions label as required.
            _emptySessionListLabel.enabled = message.LocalSessions.Count == 0;
        }

        /// <summary>
        ///     Ensure that there are the required number of <see cref="SessionListItemUI"/> instances.<br/>
        ///     Creates new instances to reach the required amount and disables those over the count.
        /// </summary>
        private void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - _sessionListItems.Count;

            // Create required new instances.
            for(int i = 0; i < delta; ++i)
            {
                _sessionListItems.Add(CreateSessionListItem());
            }
            
            // Enable/Disable instances.
            for(int i = 0; i < _sessionListItems.Count; ++i)
            {
                _sessionListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }

        private SessionListItemUI CreateSessionListItem()
        {
            SessionListItemUI listItem = Instantiate(_sessionListItemPrototype.gameObject, _sessionListItemPrototype.transform.parent).GetComponent<SessionListItemUI>();
            listItem.gameObject.SetActive(true);

            _container.Inject(listItem);

            return listItem;
        }


        public void OnQuickJoinPressed()
        {
            _sessionUIMediator.QuickJoinRequest();
        }


        public void Show()
        {
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = false;
            _joinCodeField.text = "";
            _updateRunner.Subscribe(PeriodicRefresh, 10.0f);
        }
        public void Hide()
        {
            _canvasGroup.alpha = 0.0f;
            _canvasGroup.blocksRaycasts = false;
            _updateRunner.Unsubscribe(PeriodicRefresh);
        }
    }
}