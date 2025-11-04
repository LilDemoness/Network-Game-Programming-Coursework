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
        #region Active Menu Stuff

        /// <summary> Are any Overlay Menus currently active? </summary>
        public static bool IsOverlayMenuOpen { get; private set;} = false;
        private static OverlayMenu s_activeOverlayMenu => IsOverlayMenuOpen ? s_overlayMenus[0] : null;
        private static List<OverlayMenu> s_overlayMenus = new List<OverlayMenu>();

        
        /// <summary> Is this Overlay Menu the active/highlighted Overlay Menu?</summary>
        protected bool IsActiveMenu => IsOverlayMenuOpen ? s_overlayMenus[0] == this : false;


        /// <summary>
        ///     Returns true if the passed transform is a child of the active overlay menu.
        /// </summary>
        /// <param name="transformToCheck"> The transform to check.</param>
        /// <param name="countNoneAsTrue"> If there is no active overlay menu, should this function return true or false?</param>
        public static bool IsWithinActiveMenu(Transform transformToCheck, bool countNoneAsTrue = true)
        {
            if (!IsOverlayMenuOpen)
                return countNoneAsTrue;    // No open menu, so the element is in focus.

            while(transformToCheck != null)
            {
                if (transformToCheck == s_activeOverlayMenu.transform)
                    return true;    // Found the parent.

                transformToCheck = transformToCheck.parent;
            }

            // None of this object's parents were the active menu.
            return false;
        }

        #endregion


        /// <summary> The GameObject that should be selected when this Overlay Menu is opened.</summary>
        protected abstract GameObject FirstSelectedItem { get; }

        /// <summary> The Root Gameobject of this Overlay Menu that will be toggled on and off for Opening & Closing.</summary>
        protected virtual GameObject RootObject { get => this.gameObject; }
        /// <summary> Is this Overlay Menu open (Even if not in focus)?</summary>
        protected bool IsOpen => RootObject.activeSelf;


        private GameObject _previousSelectable;


        /// <summary>
        ///     Open the Overlay Menu.
        /// </summary>
        public void Open() => Open(EventSystem.current.currentSelectedGameObject);
        /// <inheritdoc cref="OverlayMenu.Open"/>
        /// <param name="previousSelectedObject"> The previously selected GameObject.</param>
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
            if (!IsOpen)
                return; // We were not open to begin with.

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