using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utils.Editor
{
    public class NetworkOverlay : MonoBehaviour
    {
        public static NetworkOverlay Instance { get; private set; }

        [SerializeField] private GameObject _debugCanvasPrefab;
        private Transform _verticalLayoutTransform;


        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        /// <summary>
        ///     Create and add a TextMeshProUGUI instance to the NetworkOverlay canvas.
        /// </summary>
        public void AddTextToUI(string gameObjectName, string defaultText, out TextMeshProUGUI textComponent)
        {
            // Create the Text Component.
            GameObject rootObject = new GameObject(gameObjectName);
            textComponent = rootObject.AddComponent<TextMeshProUGUI>();

            // Setup the Text Component.
            textComponent.fontSize = 28;
            textComponent.text = defaultText;
            textComponent.horizontalAlignment = HorizontalAlignmentOptions.Left;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            textComponent.raycastTarget = false;
            textComponent.autoSizeTextContainer = true;

            // Add the text component to the UI.
            RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
            AddToUI(rectTransform);
        }

        /// <summary>
        ///     Add a RectTransform instance to the UI.
        /// </summary>
        public void AddToUI(RectTransform displayTransform)
        {
            if (_verticalLayoutTransform == null)
                CreateDebugCanvas();

            displayTransform.sizeDelta = new Vector2(100.0f, 24.0f);
            displayTransform.SetParent(_verticalLayoutTransform);
            displayTransform.SetAsFirstSibling();
            displayTransform.localScale = Vector3.one;
        }

        private void CreateDebugCanvas()
        {
            GameObject canvas = Instantiate(_debugCanvasPrefab, transform);
            _verticalLayoutTransform = canvas.GetComponentInChildren<VerticalLayoutGroup>().transform;
        }
    }
}