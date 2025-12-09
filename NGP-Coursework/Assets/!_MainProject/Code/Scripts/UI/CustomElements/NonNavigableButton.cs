using TMPro;
using UI.Icons;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UI
{
    /// <summary>
    ///     A button that can be pressed by a mouse and triggered via a PlayerInputAction, but that cannot be selected via navigation
    /// </summary>
    public class NonNavigableButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [System.Serializable]
        private enum InputActionType
        {
            /// <summary> Trigger the action without a check.</summary>
            Press,

            /// <summary> Trigger the action if there is any horizontal input.</summary>
            Horizonal,
            /// <summary> Trigger the action if there is positive horizontal input.</summary>
            HorizonalPositive,
            /// <summary> Trigger the action if there is negative horizontal input.</summary>
            HorizonalNegative,

            /// <summary> Trigger the action if there is any vertical input.</summary>
            Vertical,
            /// <summary> Trigger the action if there is positive vertical input.</summary>
            VerticalPositive,
            /// <summary> Trigger the action if there is negative vertical input.</summary>
            VerticalNegative,
        }


        [SerializeField] private bool _isInteractable = true;
        public bool IsInteractable
        {
            get => _isInteractable;
            set => _isInteractable = true;
        }


        [Space(10)]
        [Tooltip("(Optional) The input action that this button will be triggered by.")]
        [SerializeField] private InputActionReference _inputAction;
        [Tooltip("The type of check that will be run when the input action is performed.")]
        [SerializeField] private InputActionType _inputActionType;
        [SerializeField] private bool _allowInputWhenNotInFocus = false;

        [Space(10)]
        [SerializeField] private UnityEvent _onButtonTriggered;


        [Header("Text Updating")]
        [SerializeField] private TMP_Text _iconDisplayText;
        [SerializeField][TextArea] private string _textFormattingString = "{0}";


        private void Awake()
        {
            if (_iconDisplayText != null && _inputAction != null)
            {
                InputIconManager.OnShouldUpdateSpriteIdentifiers += UpdateIconDisplayText;
                InputIconManager.OnSpriteAssetChanged += UpdateIconDisplayText;

                UpdateIconDisplayText();
            }
        }
        private void OnEnable()
        {
            if (_inputAction != null)
            {
                _inputAction.action.Enable();
                _inputAction.action.performed += Action_performed;
            }
        }
        private void OnDisable()
        {
            if (_inputAction != null)
                _inputAction.action.performed -= Action_performed;
        }
        private void OnDestroy()
        {
            InputIconManager.OnShouldUpdateSpriteIdentifiers -= UpdateIconDisplayText;
            InputIconManager.OnSpriteAssetChanged -= UpdateIconDisplayText;
        }


        private void Action_performed(InputAction.CallbackContext ctx)
        {
            Debug.Log("Action Performed");

            if (!_isInteractable)
                return; // The action is not interractable.

            if (!_allowInputWhenNotInFocus && !OverlayMenu.IsWithinActiveMenu(this.transform))
                return; // We are not in focus and aren't allowing out-of-focus input.

            if (!IsValidActionInput(ref ctx))
                return; // Invalid input for the action trigger type.

            // Valid input. Trigger our callback.
            _onButtonTriggered?.Invoke();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable)
                return; // The action is not interractable.

            _onButtonTriggered?.Invoke();
        }
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


        private void UpdateIconDisplayText()
        {
            _iconDisplayText.text = InputIconManager.FormatTextForIconFromInputAction(_textFormattingString, _inputAction);
            _iconDisplayText.spriteAsset = InputIconManager.GetSpriteAsset();
        }


#if UNITY_EDITOR

        private void OnValidate()
        {
            if (_iconDisplayText != null && _inputAction != null)
            {
                if (Editor_IsFormattingStringValid())
                {
                    _iconDisplayText.text = _textFormattingString;
                }
                else
                {
                    _iconDisplayText.text = "!INVALID!";
                }
            }
        }

        private bool Editor_IsFormattingStringValid()
        {
            return _textFormattingString.Contains("{0}");
        }

#endif
    }
}