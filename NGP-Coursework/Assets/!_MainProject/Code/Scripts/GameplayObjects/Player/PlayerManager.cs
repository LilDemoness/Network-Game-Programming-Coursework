using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Sections;
using Gameplay.GameplayObjects.Character.Customisation.Data;

public class PlayerManager : NetworkBehaviour
{
    public void SetBuild(int frameIndex, int legIndex, int[] weaponIndicies, int abilityIndex)
    {
        foreach (FrameGFX childGFX in GetComponentsInChildren<FrameGFX>())
        {
            WeaponData[] weaponDatas = new WeaponData[weaponIndicies.Length];
            for(int i = 0; i < weaponDatas.Length; ++i)
                weaponDatas[i] = BuildData.ActiveOptionsDatabase.GetWeapon(weaponIndicies[i]);

            childGFX.OnCustomisationFinalised(
                activeFrame:            BuildData.ActiveOptionsDatabase.GetFrame(frameIndex),
                activeLeg:              BuildData.ActiveOptionsDatabase.GetLeg(legIndex),
                activeWeapons:          weaponDatas,
                activeAbility:          BuildData.ActiveOptionsDatabase.GetAbility(abilityIndex)
            );
        }
    }
}