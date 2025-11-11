using System.Collections.Generic;
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


        public SlotGFXSection[] Toggle(SlottableData activeData)
        {
            List<SlotGFXSection> activeSlots = new List<SlotGFXSection>(_slotGFXs.Length);
            for (int i = 0; i < _slotGFXs.Length; ++i)
            {
                if (_slotGFXs[i].Toggle(activeData))
                    activeSlots.Add(_slotGFXs[i]);
            }

            return activeSlots.ToArray();
        }
    }
}