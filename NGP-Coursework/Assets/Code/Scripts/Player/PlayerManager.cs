using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour
{
    public void SetBuild(int frameIndex, int legIndex, int primaryWeaponIndex, int secondaryWeaponIndex, int tertiaryWeaponIndex, int abilityIndex)
    {
        foreach (PlayerGFX childGFX in GetComponentsInChildren<PlayerGFX>())
        {
            childGFX.OnCustomisationFinalised(
                activeFrame:            BuildData.ActiveOptionsDatabase.GetFrame(frameIndex),
                activeLeg:              BuildData.ActiveOptionsDatabase.GetLeg(legIndex),
                activePrimaryWeapon:    BuildData.ActiveOptionsDatabase.GetWeapon(primaryWeaponIndex),
                activeSecondaryWeapon:  BuildData.ActiveOptionsDatabase.GetWeapon(secondaryWeaponIndex),
                activeTertiaryWeapon:   BuildData.ActiveOptionsDatabase.GetWeapon(tertiaryWeaponIndex),
                activeAbility:          BuildData.ActiveOptionsDatabase.GetAbility(abilityIndex)
            );
        }
    }
}