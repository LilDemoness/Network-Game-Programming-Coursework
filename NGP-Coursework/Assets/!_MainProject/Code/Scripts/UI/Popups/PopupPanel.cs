using TMPro;
using UnityEngine;

namespace Gameplay.UI.Popups
{
    /// <summary>
    ///     A simple Popup Panel to display information to players.
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [Space(5)]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _mainText;
        [SerializeField] private GameObject _confirmButton;
        [SerializeField] private GameObject _loadingSpinner;


        private bool _isDisplaying;
        public bool IsDisplaying => _isDisplaying;

        private bool _closableByUser;


        private void Awake()
        {
            Hide();
        }

        public void OnConfirmClick()
        {
            if (_closableByUser)
                Hide();
        }


        /// <summary>
        ///     Setup and show the Popup Panel.
        /// </summary>
        public void SetupPopupPanel(string titleText, string mainText, bool closeableByUser = true)
        {
            _titleText.text = titleText;
            _mainText.text = mainText;
            _closableByUser = closeableByUser;

            _confirmButton.SetActive(_closableByUser);
            _loadingSpinner.SetActive(!_closableByUser);

            Show();
        }


        private void Show()
        {
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;
            _isDisplaying = true;
        }
        public void Hide()
        {
            _canvasGroup.alpha = 0.0f;
            _canvasGroup.blocksRaycasts = false;
            _isDisplaying = false;
        }
    }
}