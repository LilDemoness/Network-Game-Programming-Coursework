using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace UI.Customisation.SlottableSelection
{
    /// <summary>
    ///     A button corresponding to a <see cref="SlottableData"/>.
    /// </summary>
    public class SlottableSelectionUIButton : Selectable
    {
        private static readonly Color DEFAULT_UNSELECTED_COLOR = new Color(0.2901f, 0.3607f, 0.4470f, 1.0f);
        private static readonly Color DEFAULT_HIGHLIGHTED_COLOR = new Color(0.2901f, 0.3607f, 0.4470f, 1.0f);
        private static readonly Color DEFAULT_SELECTED_COLOR = new Color(0.5607f, 0.6313f, 0.6705f, 1.0f);



        private int _slottableDataIndex;
        public event System.Action<int> OnPressed;


        [Header("References")]
        //[SerializeField] private TMP_Text _text;
        [SerializeField] private Image _image;


        #if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
        #else
        private void Reset()
        {
        #endif
            ResetTransitionParameters();
        }

        /// <summary>
        ///     Reset this button's transition parameters to default.
        /// </summary>
        private void ResetTransitionParameters()
        {
            // Reset the Transition Type.
            this.transition = Transition.ColorTint;

            // Reset the Transition Colours.
            ColorBlock cb = new ColorBlock();
            cb.normalColor = DEFAULT_UNSELECTED_COLOR;
            cb.highlightedColor = DEFAULT_HIGHLIGHTED_COLOR;
            cb.selectedColor = DEFAULT_SELECTED_COLOR;
            cb.pressedColor = DEFAULT_SELECTED_COLOR;
            this.colors = cb;
        }


        /// <summary>
        ///  Setup this button for the given <see cref="SlottableData"/>.
        /// </summary>
        public void SetupButton(SlottableData slottableData)
        {
            //this._text.text = slottableData.Name;
            this._image.sprite = slottableData.Sprite;
            this._slottableDataIndex = CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForSlottableData(slottableData);
        }
        /// <summary>
        ///     Show this Button.
        /// </summary>
        public void Show() => this.gameObject.SetActive(true);
        /// <summary>
        ///     Hide this button
        /// </summary>
        public void Hide() => this.gameObject.SetActive(false);
        /// <summary>
        ///     Returns true if the button is Shown or false if it is Hidden.
        /// </summary>
        public bool IsShown() => this.gameObject.activeSelf;


        /// <summary>
        ///     Mark this button as selected manually (E.g. Swapping between tabs).
        /// </summary>
        public void MarkAsSelected()
        {
            EventSystem.current.SetSelectedGameObject(this.gameObject);
        }
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            OnPressed?.Invoke(_slottableDataIndex);
        }
    }
}