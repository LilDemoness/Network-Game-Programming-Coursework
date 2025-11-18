using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    /// <summary>
    ///     An attachment slot on a frame.
    /// </summary>
    public class AttachmentSlot : MonoBehaviour
    {
        [SerializeField] private AttachmentSlotIndex _slotIndex = AttachmentSlotIndex.Primary;
        public AttachmentSlotIndex AttachmentSlotIndex => _slotIndex;


        [Header("GFX")]
        [SerializeField] private SlotGFXSection[] _slotGFXs;
        private int _activeGFXSlotIndex = -1;


        /// <summary>
        ///     Toggles all SlotGFXSections under this AttachmentSlot and returns the value of the active element (If one exists).
        /// </summary>
        /// <returns> False if no elements were enabled.</returns>
        public bool Toggle(SlottableData activeData)
        {
            _activeGFXSlotIndex = -1;
            for (int i = 0; i < _slotGFXs.Length; ++i)
            {
                if (_slotGFXs[i].Toggle(activeData))
                {
                    _activeGFXSlotIndex = i;
                }
            }

            return _activeGFXSlotIndex != -1;
        }

        public bool HasActiveGFXSlot() => _activeGFXSlotIndex >= 0 && _activeGFXSlotIndex < _slotGFXs.Length;
        /// <summary> Returns the active SlotGFXSection (Or null if none are active).</summary>
        public SlotGFXSection GetActiveGFXSlot() => _activeGFXSlotIndex >= 0 ? _slotGFXs[_activeGFXSlotIndex] : null;
    }
}