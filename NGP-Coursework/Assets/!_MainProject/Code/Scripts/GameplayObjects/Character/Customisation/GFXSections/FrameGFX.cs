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

        [SerializeField] private LegGFXSection[] _legDatas;

        [SerializeField] private SlottableDataSlot[] m_slottableDataSlotArray;
        private Dictionary<SlotIndex, List<SlottableDataSlot>> _slottableDataSlots = new Dictionary<SlotIndex, List<SlottableDataSlot>>();
        
        //public bool TryGetAttachmentSlot(SlotIndex slotIndex, out SlottableDataSlot weaponAttachmentSlot) => _slottableDataSlots.TryGetValue(slotIndex, out weaponAttachmentSlot);


        #if UNITY_EDITOR

        [ContextMenu(itemName: "Setup/Auto Setup Container References")]
        private void Editor_AutoSetupContainerReferences()
        {
            // Ensure Changes are Recorded.
            UnityEditor.Undo.RecordObject(this, "Setup FrameGFX Container References");

            _legDatas = GetComponentsInChildren<LegGFXSection>();
            m_slottableDataSlotArray = GetComponentsInChildren<SlottableDataSlot>();

            // Ensure Changes are Recorded.
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

#endif

        private void Awake()
        {
            _slottableDataSlots = new Dictionary<SlotIndex, List<SlottableDataSlot>>(SlotIndex.Unset.GetMaxPossibleSlots());
            foreach(SlottableDataSlot attachmentSlot in m_slottableDataSlotArray)
            {
                if (!_slottableDataSlots.TryAdd(attachmentSlot.SlotIndex, new List<SlottableDataSlot>() { attachmentSlot }))
                {
                    _slottableDataSlots[attachmentSlot.SlotIndex].Add(attachmentSlot);
                }
            }
        }


        public FrameGFX OnSelectedFrameChanged(FrameData activeData)
        {
            this.gameObject.SetActive(activeData == _associatedFrameData);

            return this;
        }
        public FrameGFX OnSelectedLegChanged(LegData activeData)
        {
            for (int i = 0; i < _legDatas.Length; ++i)
            {
                _legDatas[i].Toggle(activeData);
            }

            return this;
        }
        public FrameGFX OnSelectedSlottableDataChanged(SlotIndex slotIndex, SlottableData activeData)
        {
            if (_slottableDataSlots.TryGetValue(slotIndex, out List<SlottableDataSlot> slottableDataSlots))
            {
                for(int i = 0; i < slottableDataSlots.Count; ++i)
                    slottableDataSlots[i].Toggle(activeData);
            }
            return this;
        }

        public void OnCustomisationFinalised(FrameData activeFrame, LegData activeLeg, SlottableData[] activeSlottableDatas)
        {
            // Frame.
            if (activeFrame != _associatedFrameData)
            {
                Destroy(this.gameObject);
                return;
            }


            // Legs.
            for (int i = 0; i < _legDatas.Length; ++i)
            {
                _legDatas[i].Finalise(activeLeg);
            }


            // Slottables (Weapons & Abilities).
            for(int i = 0; i < _slottableDataSlots.Count; ++i)
            {
                if (_slottableDataSlots.TryGetValue((SlotIndex)(i + 1), out List<SlottableDataSlot> slottableDataSlots))
                {
                    for (int j = 0; j < slottableDataSlots.Count; ++j)
                        slottableDataSlots[j].Finalise(activeSlottableDatas[i]);
                }
            }
        }
    }
}