using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gameplay.UI.Tooltips
{
    /// <summary>
    ///     Controls the tooltip popup.
    /// </summary>
    public class UITooltipPopup : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [Tooltip("This transform is shown/hideen to show/hide the popup box")]
        [SerializeField] private GameObject _windowRoot;
        [SerializeField] private TextMeshProUGUI _textField;
        [SerializeField] private Vector3 _cursorOffset;


        private void Awake() => Assert.IsNotNull(_canvas);


        /// <summary>
        ///     Shows a tooltip at the given mouse coordiantes.
        /// </summary>
        public void ShowTooltip(string text, Vector3 screenCoordinates)
        {
            screenCoordinates += _cursorOffset;
            _windowRoot.transform.position = GetCanvasCoords(screenCoordinates);
            _textField.text = text;
            _windowRoot.SetActive(true);
        }

        /// <summary>
        ///     Hides the current tooltip.
        /// </summary>
        public void HideTooltip()
        {
            _windowRoot.SetActive(false);
        }

        /// <summary>
        ///     Maps screen coordinates (Such as Mouse Position) to coordinates on our Canvas.
        /// </summary>
        private Vector3 GetCanvasCoords(Vector3 screenCoords)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenCoords,
                _canvas.worldCamera,
                out Vector2 canvasCoords);
            return canvasCoords;
        }


        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (gameObject.scene.rootCount > 1) // A hacky way to check if this is a Scene Object/Prefab Instance and not a Prefab Definition.
            {
                // Not a Prefab Definition.
                if (_canvas == null)
                {
                    // Typically there's only one canvas in the scene, so pick that.
                    _canvas = FindAnyObjectByType<Canvas>();
                }
            }
        }

        #endif
    }
}