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

        [SerializeField] private AttachmentSlot[] m_slottableDataSlotArray;
        private Dictionary<AttachmentSlotIndex, AttachmentSlot> _slottableDataSlots = new Dictionary<AttachmentSlotIndex, AttachmentSlot>();
        

        [Header("Rotation")]
        [SerializeField] private Transform _verticalRotationPivot;
        [Tooltip("Does this frame use the X-axis for vertical rotation (True), or the Z-axis (False).")]
            [SerializeField] private bool _usesXRotationForVertical;

        public Transform VerticalRotationPivot => _verticalRotationPivot;
        /// <summary>
        ///     An offset from the Vertical Rotation Pivot applied when calculating the desired rotation.<br/>Used to align the average position of all weapons to the estimated target position.
        /// </summary>
        [field: SerializeField] public Vector3 VerticalRotationPivotOffset { get; private set; }
        /// <summary>
        ///     True if this frame uses the X-Axis for Vertical Rotation, or False if it uses the Z-Axis.
        /// </summary>
        public bool UsesXRotationForVertical => _usesXRotationForVertical;


        #if UNITY_EDITOR

        [ContextMenu(itemName: "Setup/Auto Setup Container References")]
        private void Editor_AutoSetupContainerReferences()
        {
            // Ensure Changes are Recorded.
            UnityEditor.Undo.RecordObject(this, "Setup FrameGFX Container References");

            m_slottableDataSlotArray = GetComponentsInChildren<AttachmentSlot>();

            // Ensure Changes are Recorded.
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

#endif

        private void Awake()
        {
            _slottableDataSlots = new Dictionary<AttachmentSlotIndex, AttachmentSlot>(AttachmentSlotIndexExtensions.GetMaxPossibleSlots());
            foreach(AttachmentSlot attachmentSlot in m_slottableDataSlotArray)
            {
                if (!_slottableDataSlots.TryAdd(attachmentSlot.AttachmentSlotIndex, attachmentSlot))
                {
                    // We should only have 1 attachment slot for each SlotIndex, however reaching here means that we don't. Throw an exception so we know about this.
                    throw new System.Exception($"We have multiple Attachment Slots with the same Slot Index ({attachmentSlot.AttachmentSlotIndex}).\n" +
                        $"Duplicates: '{_slottableDataSlots[attachmentSlot.AttachmentSlotIndex].name}' & '{attachmentSlot.name}'");
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
        public FrameGFX OnSelectedSlottableDataChanged(AttachmentSlotIndex slotIndex, SlottableData activeData)
        {
            if (_slottableDataSlots.TryGetValue(slotIndex, out AttachmentSlot slottableDataSlot))
            {
                slottableDataSlot.Toggle(activeData);
            }
            return this;
        }


        public FrameData GetAssociatedData() => _associatedFrameData;
        public AttachmentSlot[] GetSlottableDataSlotArray() => m_slottableDataSlotArray;
    }
}