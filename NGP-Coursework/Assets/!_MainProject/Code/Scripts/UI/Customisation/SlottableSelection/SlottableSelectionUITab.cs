using Gameplay.GameplayObjects;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI.Customisation.SlottableSelection
{
    /// <summary>
    ///     A tab button for an Attachment Point.
    /// </summary>
    public class SlottableSelectionUITab : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _unselectedColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        [SerializeField] private Color _selectedColor = new Color(0.247f, 0.3137f, 0.3921f, 1.0f);

        private AttachmentSlotIndex _slotIndex;

        public event System.Action<AttachmentSlotIndex> OnPressed;


        /// <summary>
        ///     Show this Button.
        /// </summary>
        public void Show() => this.gameObject.SetActive(true);
        /// <summary>
        ///     Hide this button
        /// </summary>
        public void Hide() => this.gameObject.SetActive(false);


        /// <summary>
        ///     Set our active slot index.
        /// </summary>
        public void SetAttachmentSlotIndex(AttachmentSlotIndex slotIndex) => _slotIndex = slotIndex;
        /// <summary>
        ///     Set our selected state and alter the corresponding visuals.
        /// </summary>
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