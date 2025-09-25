using UnityEngine;
using Unity.Netcode;

public class PlayerCustomisation : NetworkBehaviour
{
    [SerializeField] private PlayerCustomisationOptionsDatabase _optionsDatabase;

    // Events.
    public event System.Action<FrameData> OnSelectedFrameChanged;
    public event System.Action<LegsData> OnSelectedLegChanged;
    public event System.Action<int, WeaponData> OnSelectedWeaponChanged;
    public event System.Action<AbilityData> OnSelectedAbilityChanged;


    public void UpdatePlayer(PlayerCustomisationState customisationState)
    {
        OnSelectedFrameChanged?.Invoke(_optionsDatabase.FrameDatas[customisationState.FrameIndex]);
        OnSelectedLegChanged?.Invoke(_optionsDatabase.LegDatas[customisationState.LegIndex]);
        OnSelectedWeaponChanged?.Invoke(0, _optionsDatabase.WeaponDatas[customisationState.PrimaryWeaponIndex]);
        OnSelectedWeaponChanged?.Invoke(1, _optionsDatabase.WeaponDatas[customisationState.SecondaryWeaponIndex]);
        OnSelectedWeaponChanged?.Invoke(2, _optionsDatabase.WeaponDatas[customisationState.TertiaryWeaponIndex]);
        OnSelectedAbilityChanged?.Invoke(_optionsDatabase.AbilityDatas[customisationState.AbilityIndex]);
    }
    public override void OnNetworkSpawn()
    {
        PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
    }
    public override void OnNetworkDespawn()
    {
        PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
    }

    private void PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ulong clientID, PlayerCustomisationState customisationState)
    {
        if (clientID == this.OwnerClientId)
            UpdatePlayer(customisationState);
    }
}