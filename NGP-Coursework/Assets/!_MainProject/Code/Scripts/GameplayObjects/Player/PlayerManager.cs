using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Sections;
using Gameplay.GameplayObjects.Character.Customisation.Data;

public class PlayerManager : NetworkBehaviour
{
    public void SetBuild(int frameIndex, int legIndex, int[] slottableDataIndicies)
    {
        /*int index = 0;
        SlottableData[] slottableDatas = new SlottableData[weaponSlotIndicies.Length + abilitySlotIndicies.Length];
        for(int i = 0; i < weaponSlotIndicies.Length; ++i)
            slottableDatas[index++] = BuildData.ActiveOptionsDatabase.GetWeaponData(weaponSlotIndicies[i]);
        for(int i = 0; i < weaponSlotIndicies.Length; ++i)
            slottableDatas[index++] = BuildData.ActiveOptionsDatabase.GetAbilityData(abilitySlotIndicies[i]);*/
        SlottableData[] slottableDatas = new SlottableData[slottableDataIndicies.Length];
        for(int i = 0; i < slottableDataIndicies.Length; ++i)
        {
            slottableDatas[i] = BuildData.ActiveOptionsDatabase.GetSlottableData(slottableDataIndicies[i]);
            Debug.Log(i + ": " + slottableDataIndicies[i] + "\n" + slottableDatas[i].name);
        }


        foreach (FrameGFX childGFX in GetComponentsInChildren<FrameGFX>())
        {
            childGFX.OnCustomisationFinalised(
                activeFrame:            BuildData.ActiveOptionsDatabase.GetFrame(frameIndex),
                activeLeg:              BuildData.ActiveOptionsDatabase.GetLeg(legIndex),
                activeSlottableDatas:   slottableDatas
            );
        }
    }
}