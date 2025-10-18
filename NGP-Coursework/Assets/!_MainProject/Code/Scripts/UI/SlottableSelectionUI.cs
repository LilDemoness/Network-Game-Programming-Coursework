using Gameplay.GameplayObjects;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Customisation
{
    public class SlottableSelectionUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private SlotIndex _slotIndex;


        [Header("References")]
        [SerializeField] private PlayerCustomisationUI _playerCustomisationUI;

        [Space(5)]
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _previousButton;


        private void Awake()
        {
            _nextButton.onClick.AddListener(OnNextButtonSelected);
            _previousButton.onClick.AddListener(OnPreviousButtonSelected);
        }
        private void OnNextButtonSelected() => _playerCustomisationUI.SelectNextSlottableData(_slotIndex);
        private void OnPreviousButtonSelected() => _playerCustomisationUI.SelectPreviousSlottableData(_slotIndex);
    }
}