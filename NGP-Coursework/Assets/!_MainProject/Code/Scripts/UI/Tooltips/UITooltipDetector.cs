using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Gameplay.UI.Tooltips
{
    /// <summary>
    ///     Attach to any UI element that should have a tooltip popup.<br/>
    ///     If the mouse hovers over this element long enough, the tooltip will appear and show the specified text.
    /// </summary>
    /// <remarks>
    ///     Uses Physics Raycasting, so ensure that the Camera has PhysicsRaycaster
    ///     and that the attached UI element has "Raycast Target" enabled.
    /// </remarks>
    // To-do: Implement Support for Non-Mouse Selection (Keyboard, Gamepad, etc).
    public class UITooltipDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private InputActionReference _pointAction;

        [Tooltip("The actual Tooltip that will be triggered")]
        [SerializeField] private UITooltipPopup _tooltipPopup;

        [Tooltip("The text of the popup (This is the default text; it can also be changed in code)")]
        [SerializeField] [Multiline] private string _tooltipText;

        [Tooltip("Should the tooltip appear instantly if the player clicks this UI element?")]
        [SerializeField] private bool _activateOnClick = true;

        [Tooltip("The length of time the mouse needs to hover over this element before the tooltip appears (In seconds)")]
        [SerializeField] private float _tooltipDelay = 0.5f;


        private float _pointerEnterTime = 0.0f;
        private bool _isShowingTooltip;


        public void SetText(string text)
        {
            bool wasChanged = text != _tooltipText;
            _tooltipText = text;

            if (wasChanged && _isShowingTooltip)
            {
                // We changed the text while our tooltip was being shown. Re-show the tooltip.
                HideTooltip();
                ShowTooltip();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerEnterTime = Time.time;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerEnterTime = 0.0f;
            HideTooltip();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_activateOnClick)
                ShowTooltip();
        }


        private void Update()
        {
            if (_pointerEnterTime != 0 && (Time.time - _pointerEnterTime) > _tooltipDelay)
            {
                ShowTooltip();
            }
        }


        private void ShowTooltip()
        {
            if (_isShowingTooltip)
                return; // Already showing the tooltip.

            _tooltipPopup.ShowTooltip(_tooltipText, _pointAction.action.ReadValue<Vector2>());
            _isShowingTooltip = true;
        }
        private void HideTooltip()
        {
            if (!_isShowingTooltip)
                return; // Already not showing the tooltip.

            _tooltipPopup.HideTooltip();
            _isShowingTooltip = false;
        }


#if UNITY_EDITOR

        private void OnValidate()
        {
            if (this.gameObject.scene.rootCount > 1)    // A hacky way to check if this is a Scene Object or Prefab Instance and not a Prefab Definition.
            {
                // Not a prefab definition.
                if (_tooltipPopup == null)
                {
                    // Typically there's only one Tooltip Popup instance in a scene, so find that.
                    _tooltipPopup = FindAnyObjectByType<UITooltipPopup>();
                }
            }
        }

#endif
    }
}