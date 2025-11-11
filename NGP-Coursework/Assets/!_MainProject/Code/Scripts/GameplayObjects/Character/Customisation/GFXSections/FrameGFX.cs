using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    /// <summary>
    ///     A client-side script to display the currently selected customisation options for a given frame.
    /// </summary>
    // Note: We are having the frame prefabs contain all options so that we can have custom positions for each section (Weapons, Abilities, etc) per frame.
    public class FrameGFX : MonoBehaviour
    {
        [SerializeField] private FrameData _associatedFrameData;

        [SerializeField] private SlottableDataSlot[] m_slottableDataSlotArray;
        private Dictionary<SlotIndex, SlottableDataSlot> _slottableDataSlots = new Dictionary<SlotIndex, SlottableDataSlot>();
        

        #if UNITY_EDITOR

        [ContextMenu(itemName: "Setup/Auto Setup Container References")]
        private void Editor_AutoSetupContainerReferences()
        {
            // Ensure Changes are Recorded.
            UnityEditor.Undo.RecordObject(this, "Setup FrameGFX Container References");

            m_slottableDataSlotArray = GetComponentsInChildren<SlottableDataSlot>();

            // Ensure Changes are Recorded.
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

#endif

        private void Awake()
        {
            _slottableDataSlots = new Dictionary<SlotIndex, SlottableDataSlot>(SlotIndexExtensions.GetMaxPossibleSlots());
            foreach(SlottableDataSlot attachmentSlot in m_slottableDataSlotArray)
            {
                if (!_slottableDataSlots.TryAdd(attachmentSlot.SlotIndex, attachmentSlot))
                {
                    // We should only have 1 attachment slot for each SlotIndex, however reaching here means that we don't. Throw an exception so we know about this.
                    throw new System.Exception($"We have multiple Attachment Slots with the same Slot Index ({attachmentSlot.SlotIndex}).\n" +
                        $"Duplicates: '{_slottableDataSlots[attachmentSlot.SlotIndex].name}' & '{attachmentSlot.name}'");
                }
            }
        }

        public bool Toggle(FrameData frameData)
        {
            bool newActive = _associatedFrameData.Equals(frameData);
            this.gameObject.SetActive(newActive);
            return newActive;
        }


        public FrameGFX OnSelectedFrameChanged(FrameData activeData)
        {
            this.gameObject.SetActive(activeData == _associatedFrameData);

            return this;
        }
        public FrameGFX OnSelectedSlottableDataChanged(SlotIndex slotIndex, SlottableData activeData)
        {
            if (_slottableDataSlots.TryGetValue(slotIndex, out SlottableDataSlot slottableDataSlot))
            {
                slottableDataSlot.Toggle(activeData);
            }
            return this;
        }


        public FrameData GetAssociatedData() => _associatedFrameData;
        public SlottableDataSlot[] GetSlottableDataSlotArray() => m_slottableDataSlotArray;
    }
}