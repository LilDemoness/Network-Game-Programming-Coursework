using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UI.Buttons
{
    /// <summary>
    ///     A button that can be pressed by a mouse and triggered via a PlayerInputAction, but that cannot be selected via navigation
    /// </summary>
    public class NonNavigableButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [System.Serializable]
        private enum InputActionType
        {
            Press,

            Horizonal,
            HorizonalPositive,
            HorizonalNegative,

            Vertical,
            VerticalPositive,
            VerticalNegative,
        }


        [SerializeField] private InputActionReference _inputAction;
        [SerializeField] private InputActionType _inputActionType;

        [Space(10)]
        [SerializeField] private UnityEvent _onButtonTriggered;


        private void OnEnable()
        {
            if (_inputAction != null)
                _inputAction.action.performed += Action_performed;
        }
        private void OnDisable()
        {
            if (_inputAction != null)
                _inputAction.action.performed -= Action_performed;
        }


        private void Action_performed(InputAction.CallbackContext ctx)
        {
            if (!IsValidActionInput(ref ctx))
                return; // Invalid input for the action trigger type.

            // Valid input. Trigger our callback.
            _onButtonTriggered?.Invoke();
        }
        public void OnPointerClick(PointerEventData eventData) => _onButtonTriggered?.Invoke();
        public void OnPointerEnter(PointerEventData eventData) { }  // Required for IPointerClickHandler().
        public void OnPointerExit(PointerEventData eventData) { }   // Required for IPointerClickHandler().


        private bool IsValidActionInput(ref InputAction.CallbackContext ctx) => _inputActionType switch
        {
            InputActionType.Press => true,  // No required checks.

            // Horizontal.
            InputActionType.Horizonal => !Mathf.Approximately(ctx.ReadValue<Vector2>().x, 0.0f),// True if there is any horizontal input.
            InputActionType.HorizonalPositive => ctx.ReadValue<Vector2>().x > 0.0f,             // True for positive horizontal input.
            InputActionType.HorizonalNegative => ctx.ReadValue<Vector2>().x < 0.0f,             // True for negative horizontal input.
            // Vertical
            InputActionType.Vertical => !Mathf.Approximately(ctx.ReadValue<Vector2>().y, 0.0f), // True if there is any vertical input.
            InputActionType.VerticalPositive => ctx.ReadValue<Vector2>().y > 0.0f,              // True for positive vertical input.
            InputActionType.VerticalNegative => ctx.ReadValue<Vector2>().y < 0.0f,              // True for negative vertical input.

            _ => throw new System.NotImplementedException()
        };
    }
}