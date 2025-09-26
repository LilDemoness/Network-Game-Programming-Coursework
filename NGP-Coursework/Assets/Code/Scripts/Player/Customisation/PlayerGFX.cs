using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGFX : MonoBehaviour
{
    // Note: Currently is very dependant on the order of things in the hierarchy. Change to be less dependant on this (E.g. Using a dictionary with enums)
    // We are having the prefabs contain all options so that we can have custom positions per frame.


    [SerializeField] private FrameData _associatedFrameData;

    [Header("Container References")]
    [SerializeField] private LegsGFXSection[] _legDatas;
    [SerializeField] private WeaponAttachmentSlot[] _weaponsAttachPoints;
    [SerializeField] private AbilityGFXSection[] _abilityDatas;


    
    public PlayerGFX OnSelectedFrameChanged(FrameData activeData)
    {
        this.gameObject.SetActive(activeData == _associatedFrameData);

        return this;
    }
    public PlayerGFX OnSelectedLegChanged(LegsData activeData)
    {
        for (int i = 0; i < _legDatas.Length; ++i)
        {
            _legDatas[i].Toggle(activeData);
        }

        return this;
    }
    public PlayerGFX OnSelectedWeaponChanged(int weaponSlot, WeaponData activeData)
    {
        if (weaponSlot >= _weaponsAttachPoints.Length)
        {
            //Debug.LogError("Weapon Slot Index is out of range");
            return this;
        }

        _weaponsAttachPoints[weaponSlot].Toggle(activeData);

        return this;
    }
    public PlayerGFX OnSelectedAbilityChanged(AbilityData activeData)
    {
        for (int i = 0; i < _abilityDatas.Length; ++i)
        {
            _abilityDatas[i].Toggle(activeData);
        }

        return this;
    }

    public void OnCustomisationFinalised(FrameData activeFrame, LegsData activeLeg, WeaponData activePrimaryWeapon, WeaponData activeSecondaryWeapon, WeaponData activeTertiaryWeapon, AbilityData activeAbility)
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