using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Gameplay.UI.MainMenu.Session
{
    public class SessionCreationUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private TMP_InputField _sessionNameInputField;
        [SerializeField] private GameObject _loadingIndicatorObject;
        [SerializeField] private Toggle _isPrivate;

        [Inject]
        private SessionUIMediator _sessionUIMediator;


        private void Awake()
        {
            EnableUnityRelayUI();
        }

        private void EnableUnityRelayUI()
        {
            _loadingIndicatorObject.SetActive(false);
        }

        public void OnCreateButtonPressed()
        {
            _sessionUIMediator.CreateSessionRequest(_sessionNameInputField.text, _isPrivate.isOn);
        }


        public void Show()
        {
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;
        }
        public void Hide()
        {
            _canvasGroup.alpha = 0.0f;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}