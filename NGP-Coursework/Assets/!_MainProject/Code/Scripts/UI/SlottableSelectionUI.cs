using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using UnityEngine;

namespace UI.Customisation
{
    public class SlottableSelectionUI : MonoBehaviour
    {
        [SerializeField] private SlotIndex _slotIndex;
        [SerializeField] private PlayerCustomisationUI _customisationUI;


        [Header("Buttons")]
        [SerializeField] private SlottableSelectionUIButton _buttonPrefab;
        [SerializeField] private Transform _buttonContainer;
        private SlottableSelectionUIButton[] _buttons;



        private void GenerateButtons()
        {
            int maxOptionsCount = CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas.Length;
            for(int i = 0; i < maxOptionsCount; ++i)
            {
                SlottableSelectionUIButton button = Instantiate(_buttonPrefab, _buttonContainer);
                button.OnPressed += Button_OnPressed;
            }
        }
        public void ToggleOptionsForFrame(FrameData frameData)
        {
            if (_slotIndex.GetSlotInteger() >= frameData.AttachmentPoints.Length)
            {
                // Disable Self
                this.gameObject.SetActive(false);
                return;
            }

            AttachmentPoint attachmentPoint = frameData.AttachmentPoints[_slotIndex.GetSlotInteger()];
            for(int i = 0; i < _buttons.Length; ++i)
            {
                _buttons[i].SetupButton(attachmentPoint.ValidSlottableDatas[i]);
            }
        }


        private void Button_OnPressed(int slottableDataIndex) => _customisationUI.SelectSlottableData(_slotIndex, slottableDataIndex);
    }
}