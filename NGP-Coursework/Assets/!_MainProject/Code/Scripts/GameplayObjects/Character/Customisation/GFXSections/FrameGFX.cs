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

        [Header("Container References")]
        [SerializeField] private LegGFXSection[] _legDatas;
        [SerializeField] private WeaponAttachmentSlot[] _weaponsAttachPoints;
        [SerializeField] private AbilityGFXSection[] _abilityDatas;


        #if UNITY_EDITOR

        [ContextMenu(itemName: "Setup/Auto Setup Container References")]
        private void Editor_AutoSetupContainerReferences()
        {
            _legDatas = GetComponentsInChildren<LegGFXSection>();
            _weaponsAttachPoints = GetComponentsInChildren<WeaponAttachmentSlot>();
            _abilityDatas = GetComponentsInChildren<AbilityGFXSection>();
        }

        #endif

    
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
        public FrameGFX OnSelectedWeaponChanged(int weaponSlot, WeaponData activeData)
        {
            if (weaponSlot >= _weaponsAttachPoints.Length)
            {
                //Debug.LogError("Weapon Slot Index is out of range");
                return this;
            }

            _weaponsAttachPoints[weaponSlot].Toggle(activeData);

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


            // Weapons.
            int attachPointsCount = _weaponsAttachPoints.Length;
            if (attachPointsCount > 0)
            {
                _weaponsAttachPoints[0].Finalise(activePrimaryWeapon);
                if (attachPointsCount > 1)
                {
                    _weaponsAttachPoints[1].Finalise(activeSecondaryWeapon);
                    if (attachPointsCount > 2)
                    {
                        _weaponsAttachPoints[2].Finalise(activeTertiaryWeapon);
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