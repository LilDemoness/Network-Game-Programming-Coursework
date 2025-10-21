using Gameplay.GameplayObjects;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI.Customisation
{
    public class SlottableSelectionUITab : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _unselectedColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        [SerializeField] private Color _selectedColor = new Color(0.247f, 0.3137f, 0.3921f, 1.0f);

        private SlotIndex _slotIndex;

        public event System.Action<SlotIndex> OnPressed;


        public void SetSlotIndex(SlotIndex slotIndex) => _slotIndex = slotIndex;
        public void SetSelectedState(bool isSelected) => _backgroundImage.color = isSelected ? _selectedColor : _unselectedColor;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Debug.Log("Clicked");
                OnPressed?.Invoke(_slotIndex);
            }
        }
    }
}