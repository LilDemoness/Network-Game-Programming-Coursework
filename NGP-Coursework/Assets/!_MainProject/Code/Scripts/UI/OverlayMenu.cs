using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    ///     A menu that appears above other menus.
    /// </summary>
    /// <remarks> Allows for the correct selection of the selected element when opening and closing the menu.</remarks>
    public abstract class OverlayMenu : MonoBehaviour
    {
        public static bool IsOverlayMenuOpen { get; private set;} = false;
        public static OverlayMenu ActiveOverlayMenu => IsOverlayMenuOpen ? s_overlayMenus[0] : null;
        private static List<OverlayMenu> s_overlayMenus = new List<OverlayMenu>();

        protected abstract GameObject FirstSelectedItem { get; }
        protected virtual GameObject RootObject { get => this.gameObject; }
        private GameObject _previousSelectable;


        /// <summary>
        ///     Open the Overlay Menu.
        /// </summary>
        public void Open() => Open(EventSystem.current.currentSelectedGameObject);
        /// <inheritdoc cref="OverlayMenu.Open">
        public virtual void Open(GameObject previousSelectedObject)
        {
            _previousSelectable = previousSelectedObject;
            EventSystem.current.SetSelectedGameObject(FirstSelectedItem);

            AddToActiveList();
            RootObject.SetActive(true);
        }

        /// <summary>
        ///     Close the Overlay Menu.
        /// </summary>
        public virtual void Close(bool selectPreviousSelectable = true)
        {
            if (selectPreviousSelectable)
                EventSystem.current.SetSelectedGameObject(_previousSelectable); // Add a check for if this menu isn't the only overlay menu, and instead select from the menu beneath?

            RemoveFromActiveList();
            RootObject.SetActive(false);
        }

        /// <summary>
        ///     Add this Overlay Menu to the start of the active menus list.
        /// </summary>
        private void AddToActiveList()
        {
            s_overlayMenus.Insert(0, this);
            IsOverlayMenuOpen = true;
        }
        /// <summary>
        ///     Remove this Overlay Menu from the active menus list and cache whether there are any menus still open.
        /// </summary>
        private void RemoveFromActiveList()
        {
            s_overlayMenus.Remove(this);
            IsOverlayMenuOpen = s_overlayMenus.Count > 0;
        }
    }
}