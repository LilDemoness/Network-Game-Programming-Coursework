using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Customisation
{
    public class SlottableSelectionUIButton : MonoBehaviour
    {
        public event System.Action<int> OnPressed;

        private int _slottableDataIndex;


        [Header("References")]
        //[SerializeField] private TMP_Text _text;
        [SerializeField] private Image _image;


        public void SetupButton(SlottableData slottableData)
        {
            //this._text.text = slottableData.Name;
            this._image.sprite = slottableData.Sprite;
            this._slottableDataIndex = CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForSlottableData(slottableData);
        }

        public void ButtonPressed() => OnPressed?.Invoke(_slottableDataIndex);
    }
}