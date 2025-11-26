using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.UI.Popups
{
    /// <summary>
    ///     Handles the display of Popup Messages.<br/>
    ///     Uses Object Pooling to allow displaying multiple messages in succession.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        private static PopupManager s_instance;

        
        [SerializeField] private GameObject _popupPanelPrefab;
        [SerializeField] private GameObject _canvas;

        private List<PopupPanel> _popupPanels = new List<PopupPanel>();

        private const float OFFSET = 30.0f;
        private const float MAX_OFFSET = 200.0f;


        private void Awake()
        {
            if (s_instance != null)
                throw new System.Exception("Invalid State. Another PopupManager instance already exists");

            s_instance = this;
            DontDestroyOnLoad(_canvas);
        }

        private void OnDestroy()
        {
            s_instance = null;
        }


        /// <summary>
        ///     Displays a popup message with the specified title and main text.
        /// </summary>
        /// <param name="titleText"> The title text at the top of the panel.</param>
        /// <param name="mainText"> The main body text, displayed just under the title.</param>
        /// <param name="closeableByUser"> Can the user close this popup themselves or can it only close automatically.</param>
        public static PopupPanel ShowPopupPanel(string titleText, string mainText, bool closeableByUser = true)
        {
            if (s_instance == null)
            {
                Debug.LogError($"No PopupManager instance found. Cannot display message: {titleText}: {mainText}");
                return null;
            }

            return s_instance.DisplayPopupPanel(titleText, mainText, closeableByUser);
        }

        private PopupPanel DisplayPopupPanel(string titleText, string mainText, bool closeableByUser)
        {
            PopupPanel popup = GetNextAvailablePopupPanel();
            if (popup != null)
            {
                popup.SetupPopupPanel(titleText, mainText, closeableByUser);
            }

            return popup;
        }

        /// <summary>
        ///     Retrieve the next available PopupPanel instance, creating one if there are none available.
        /// </summary>
        private PopupPanel GetNextAvailablePopupPanel()
        {
            // Find the index of the first PopupPanel that is not displaying and has no popups after it that are currently displaying.
            int nextAvailablePopupIndex = 0;
            for(int i = 0; i < _popupPanels.Count; ++i)
            {
                if (_popupPanels[i].IsDisplaying)
                {
                    nextAvailablePopupIndex = i + 1;
                }
            }

            if (nextAvailablePopupIndex < _popupPanels.Count)
            {
                return _popupPanels[nextAvailablePopupIndex];
            }

            // None of our current PopupPanels are available, so instantiate a new one.
            // Create and position the panel.
            GameObject popupGameObject = Instantiate(_popupPanelPrefab, this.transform);
            popupGameObject.transform.position += new Vector3(1.0f, -1.0f) * (OFFSET * _popupPanels.Count % MAX_OFFSET);

            // Track the panel instance for future retrieval.
            PopupPanel popupPanel = popupGameObject.GetComponent<PopupPanel>();
            if (popupPanel != null)
            {
                _popupPanels.Add(popupPanel);
            }
            else
            {
                Debug.LogError("PopupPanel prefab does not have a PopupPanel component!");
            }

            return popupPanel;
        }
    }
}