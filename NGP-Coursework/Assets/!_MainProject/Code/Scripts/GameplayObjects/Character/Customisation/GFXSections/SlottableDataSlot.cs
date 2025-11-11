using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    public class SlottableDataSlot : MonoBehaviour
    {
        [SerializeField] private AttachmentSlotIndex _slotIndex = AttachmentSlotIndex.Primary;
        public AttachmentSlotIndex AttachmentSlotIndex => _slotIndex;


        [Header("GFX")]
        [SerializeField] private SlotGFXSection[] _slotGFXs;


        public SlotGFXSection Toggle(SlottableData activeData)
        {
            SlotGFXSection activeSlot = null;
            for (int i = 0; i < _slotGFXs.Length; ++i)
            {
                if (_slotGFXs[i].Toggle(activeData))
                {
                    activeSlot = _slotGFXs[i];
                }
            }

            return activeSlot;
        }
    }
}