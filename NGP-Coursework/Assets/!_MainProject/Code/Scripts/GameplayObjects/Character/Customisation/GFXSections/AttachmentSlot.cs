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


        /// <summary>
        ///     Toggles all SlotGFXSections under this AttachmentSlot and returns the value of the active element (If one exists).
        /// </summary>
        /// <returns> The newly active SlotGFXSection (Or null if none are active).</returns>
        public SlotGFXSection Toggle(SlottableData activeData)
        {
            SlotGFXSection activeElement = null;
            for (int i = 0; i < _slotGFXs.Length; ++i)
            {
                if (_slotGFXs[i].Toggle(activeData))
                {
                    activeElement = _slotGFXs[i];
                }
            }

            return activeElement;
        }
    }
}