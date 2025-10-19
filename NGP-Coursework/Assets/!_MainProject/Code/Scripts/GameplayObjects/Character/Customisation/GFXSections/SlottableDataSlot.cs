using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    public class SlottableDataSlot : MonoBehaviour
    {
        [SerializeField] private SlotIndex _slotIndex = SlotIndex.PrimaryWeapon;
        public SlotIndex SlotIndex => _slotIndex;


        [Header("GFX")]
        [SerializeField] private SlotGFXSection[] _slotGFXs;


        public void Toggle(SlottableData activeData)
        {
            for (int i = 0; i < _slotGFXs.Length; ++i)
                _slotGFXs[i].Toggle(activeData);
        }
        public void Finalise(SlottableData activeData)
        {
            for (int i = 0; i < _slotGFXs.Length; ++i)
                _slotGFXs[i].Finalise(activeData);
        }
    }
}