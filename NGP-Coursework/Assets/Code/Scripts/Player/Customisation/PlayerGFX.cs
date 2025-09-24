using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGFX : MonoBehaviour
{
    // Note: Currently is very dependant on the order of things in the hierarchy. Change to be less dependant on this (E.g. Using a dictionary with enums)
    // We are having the prefabs contain all options so that we can have custom positions per frame.


    [SerializeField] private PlayerCustomisation _associatedCustomisation;
    [SerializeField] private FrameData _associatedFrameData;

    [Header("Container References")]
    [SerializeField] private LegsGFXSection[] _legDatas;
    [SerializeField] private WeaponAttachmentSlot[] _weaponsAttachPoints;
    [SerializeField] private AbilityGFXSection[] _abilityDatas;


    private void Awake()
    {
        _associatedCustomisation.OnSelectedFrameChanged += PlayerCustomisation_OnSelectedFrameChanged;
        _associatedCustomisation.OnSelectedLegChanged += PlayerCustomisation_OnSelectedLegChanged;
        _associatedCustomisation.OnSelectedWeaponChanged += PlayerCustomisation_OnSelectedWeaponChanged;
        _associatedCustomisation.OnSelectedAbilityChanged += PlayerCustomisation_OnSelectedAbilityChanged;

        _associatedCustomisation.OnFinalisedCustomisation += PlayerCustomisation_OnCustomisationFinalised;
    }
    private void OnDestroy()
    {
        _associatedCustomisation.OnSelectedFrameChanged -= PlayerCustomisation_OnSelectedFrameChanged;
        _associatedCustomisation.OnSelectedLegChanged -= PlayerCustomisation_OnSelectedLegChanged;
        _associatedCustomisation.OnSelectedWeaponChanged -= PlayerCustomisation_OnSelectedWeaponChanged;
        _associatedCustomisation.OnSelectedAbilityChanged -= PlayerCustomisation_OnSelectedAbilityChanged;

        _associatedCustomisation.OnFinalisedCustomisation -= PlayerCustomisation_OnCustomisationFinalised;
    }


    
    private void PlayerCustomisation_OnSelectedFrameChanged(FrameData activeData) => this.gameObject.SetActive(activeData == _associatedFrameData);
    private void PlayerCustomisation_OnSelectedLegChanged(LegsData activeData)
    {
        for (int i = 0; i < _legDatas.Length; ++i)
        {
            _legDatas[i].Toggle(activeData);
        }
    }
    private void PlayerCustomisation_OnSelectedWeaponChanged(int weaponSlot, WeaponData activeData)
    {
        if (weaponSlot >= _weaponsAttachPoints.Length)
        {
            //Debug.LogError("Weapon Slot Index is out of range");
            return;
        }

        _weaponsAttachPoints[weaponSlot].Toggle(activeData);
    }
    private void PlayerCustomisation_OnSelectedAbilityChanged(AbilityData activeData)
    {
        for (int i = 0; i < _abilityDatas.Length; ++i)
        {
            _abilityDatas[i].Toggle(activeData);
        }
    }

    private void PlayerCustomisation_OnCustomisationFinalised(FrameData activeFrame, LegsData activeLeg, WeaponData[] activeWeapons, AbilityData activeAbility)
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
        if (activeWeapons.Length != _weaponsAttachPoints.Length)
        {
            Debug.LogError("Associated Weapons Length doesn't match Weapon Slots Length");
            return;
        }

        for (int i = 0; i < activeWeapons.Length; ++i)
        {
            _weaponsAttachPoints[i].Finalise(activeWeapons[i]);
        }


        // Ability.
        for (int i = 0; i < _abilityDatas.Length; ++i)
        {
            _abilityDatas[i].Finalise(activeAbility);
        }
    }
}