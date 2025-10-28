using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Customisation
{
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
        private void ResetTransitionParameters()
        {
            // Set Transition Type.
            this.transition = Transition.ColorTint;

            // Set Transition Colours.
            ColorBlock cb = new ColorBlock();
            cb.normalColor = DEFAULT_UNSELECTED_COLOR;
            cb.highlightedColor = DEFAULT_HIGHLIGHTED_COLOR;
            cb.selectedColor = DEFAULT_SELECTED_COLOR;
            cb.pressedColor = DEFAULT_SELECTED_COLOR;
            this.colors = cb;
        }


        public void SetupButton(SlottableData slottableData)
        {
            //this._text.text = slottableData.Name;
            this._image.sprite = slottableData.Sprite;
            this._slottableDataIndex = CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForSlottableData(slottableData);
        }
        public void Show() => this.gameObject.SetActive(true);
        public void Hide() => this.gameObject.SetActive(false);


        private bool _performingManualSelection = false;
        public void MarkAsSelected()
        {
            _performingManualSelection = true;
            EventSystem.current.SetSelectedGameObject(this.gameObject);
        }
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);

            if (_performingManualSelection)
            {
                _performingManualSelection = false;
                return;
            }

            OnPressed?.Invoke(_slottableDataIndex);
        }
    }
}