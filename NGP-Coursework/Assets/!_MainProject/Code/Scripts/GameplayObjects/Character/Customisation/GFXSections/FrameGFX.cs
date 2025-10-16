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
        [SerializeField] private AbilityGFXSection[] _abilityDatas;

        [SerializeField] private WeaponAttachmentSlot[] m_weaponAttachmentSlotArray;
        private Dictionary<WeaponSlotIndex, WeaponAttachmentSlot> _weaponsAttachPoints = new Dictionary<WeaponSlotIndex, WeaponAttachmentSlot>();
        
        public bool TryGetAttachmentSlot(WeaponSlotIndex slotIndex, out WeaponAttachmentSlot weaponAttachmentSlot) => _weaponsAttachPoints.TryGetValue(slotIndex, out weaponAttachmentSlot);


        #if UNITY_EDITOR

        [ContextMenu(itemName: "Setup/Auto Setup Container References")]
        private void Editor_AutoSetupContainerReferences()
        {
            // Ensure Changes are Recorded.
            UnityEditor.Undo.RecordObject(this, "Setup FrameGFX Container References");

            _legDatas = GetComponentsInChildren<LegGFXSection>();
            _abilityDatas = GetComponentsInChildren<AbilityGFXSection>();
            m_weaponAttachmentSlotArray = GetComponentsInChildren<WeaponAttachmentSlot>();

            // Ensure Changes are Recorded.
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

#endif

        private void Awake()
        {
            _weaponsAttachPoints = new Dictionary<WeaponSlotIndex, WeaponAttachmentSlot>(m_weaponAttachmentSlotArray.Length);
            foreach(WeaponAttachmentSlot weaponAttachmentSlot in m_weaponAttachmentSlotArray)
                _weaponsAttachPoints.Add(weaponAttachmentSlot.SlotIndex, weaponAttachmentSlot);
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
        public FrameGFX OnSelectedWeaponChanged(WeaponSlotIndex weaponSlot, WeaponData activeData)
        {
            if (_weaponsAttachPoints.TryGetValue(weaponSlot, out WeaponAttachmentSlot weaponAttachmentSlot))
            {
                weaponAttachmentSlot.Toggle(activeData);
            }
            return this;
        }
        public FrameGFX OnSelectedAbilityChanged(AbilityData activeData)
        {
            for (int i = 0; i < _abilityDatas.Length; ++i)
            {
                _abilityDatas[i].Toggle(activeData);
            }

            return this;
        }

        public void OnCustomisationFinalised(FrameData activeFrame, LegData activeLeg, WeaponData activePrimaryWeapon, WeaponData activeSecondaryWeapon, WeaponData activeTertiaryWeapon, AbilityData activeAbility)
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


            // Weapons. (Can we make this into a for loop or similar?)
            WeaponAttachmentSlot weaponAttachmentSlot = null;
            if (_weaponsAttachPoints.TryGetValue(WeaponSlotIndex.Primary, out weaponAttachmentSlot))
            {
                // Has Primary Slot.
                weaponAttachmentSlot.Finalise(activePrimaryWeapon);
                
                if (_weaponsAttachPoints.TryGetValue(WeaponSlotIndex.Secondary, out weaponAttachmentSlot))
                {
                    // Has Secondary Slot.
                    weaponAttachmentSlot.Finalise(activeSecondaryWeapon);
                    
                    if (_weaponsAttachPoints.TryGetValue(WeaponSlotIndex.Tertiary, out weaponAttachmentSlot))
                    {
                        // Has Tertiary Slot.
                        weaponAttachmentSlot.Finalise(activeTertiaryWeapon);
                    }
                }
            }


            // Ability.
            for (int i = 0; i < _abilityDatas.Length; ++i)
            {
                _abilityDatas[i].Finalise(activeAbility);
            }
        }
    }
}