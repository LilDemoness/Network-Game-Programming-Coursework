using Gameplay.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Gameplay.UI
{
    public class IPJoiningUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private TMP_InputField _ipInputField;
        [SerializeField] private TMP_InputField _portInputField;
        [SerializeField] private Button _joinButton;

        [Inject]
        private IPUIMediator _ipUIMediator;


        private void Awake()
        {
            // Default to default values.
            _ipInputField.text = IPUIMediator.DEFAULT_IP;
            _portInputField.text = IPUIMediator.DEFAULT_PORT.ToString();
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


        // Triggered through UI Button.
        public void OnJoinButtonPressed()
        {
            _ipUIMediator.JoinWithIP(_ipInputField.text, _portInputField.text);
        }


        /// <summary>
        ///     Added to the IP InputField component's OnValueChanged callback.
        /// </summary>
        public void SanitizeIPInputText()
        {
            _ipInputField.text = IPUIMediator.SanitizeIP(_ipInputField.text);
            _joinButton.interactable = IPUIMediator.AreIPAddressAndPortValid(_ipInputField.text, _portInputField.text);
        }
        /// <summary>
        ///     Added to the Port InputField component's OnValueChanged callback.
        /// </summary>
        public void SanitisePortText()
        {
            _portInputField.text = IPUIMediator.SanitizePort(_portInputField.text);
            _joinButton.interactable = IPUIMediator.AreIPAddressAndPortValid(_ipInputField.text, _portInputField.text);
        }
    }
}