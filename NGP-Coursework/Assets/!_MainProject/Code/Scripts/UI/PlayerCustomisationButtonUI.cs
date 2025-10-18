using Gameplay.GameplayObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Customisation
{
    public class PlayerCustomisationButtonUI : MonoBehaviour
    {
        [SerializeField] private PlayerCustomisationUI _playerCustomisationUI;
        [SerializeField] private WeaponSlotIndex _weaponSlotIndex;
        public WeaponSlotIndex WeaponSlotIndex => _weaponSlotIndex;


        [Header("UI References")]
        [SerializeField] private Button _previousButton;
        [SerializeField] private TMP_Text _currentlySelectedOptionText;
        [SerializeField] private Button _nextButton;

        [Space(5)]
        [SerializeField] private CanvasGroup _canvasGroup;



        private void Awake()
        {
            _previousButton.onClick.AddListener(SelectPreviousPressed);
            _nextButton.onClick.AddListener(SelectNextPressed);
        }

        private void SelectPreviousPressed() => _playerCustomisationUI.SelectPreviousWeapon(_weaponSlotIndex);
        private void SelectNextPressed() => _playerCustomisationUI.SelectNextWeapon(_weaponSlotIndex);

        public void SetSelectedOptionText(string newSelectedOptionName) => _currentlySelectedOptionText.text = newSelectedOptionName;
        public void SetEnabledState(bool newState)
        {
            if (newState == true)
            {
                _canvasGroup.alpha = 1.0f;
                _canvasGroup.interactable = true;
            }
            else
            {
                _canvasGroup.alpha = 0.5f;
                _canvasGroup.interactable = false;
            }
        }
    }
}