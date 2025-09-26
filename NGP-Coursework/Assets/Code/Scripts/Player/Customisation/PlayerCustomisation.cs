using UnityEngine;
using Unity.Netcode;

public class PlayerCustomisation : MonoBehaviour
{
    [SerializeField] private PlayerCustomisationOptionsDatabase _optionsDatabase;
    private ulong ownerClientID;
    public ulong ClientID => this.ownerClientID;

    // Events.
    public event System.Action<FrameData> OnSelectedFrameChanged;
    public event System.Action<LegsData> OnSelectedLegChanged;
    public event System.Action<int, WeaponData> OnSelectedWeaponChanged;
    public event System.Action<AbilityData> OnSelectedAbilityChanged;

    public event System.Action<FrameData, LegsData, WeaponData[], AbilityData> OnPlayerCustomisationFinalised;


    public void Setup(ulong ownerClientID) => this.ownerClientID = ownerClientID;
    public void Setup(ulong ownerClientID, PlayerCustomisationState initialState)
    {
        this.ownerClientID = ownerClientID;
        UpdatePlayer(initialState);
    }
    public void UpdatePlayer(PlayerCustomisationState customisationState)
    {
        OnSelectedFrameChanged?.Invoke(_optionsDatabase.FrameDatas[customisationState.FrameIndex]);
        OnSelectedLegChanged?.Invoke(_optionsDatabase.LegDatas[customisationState.LegIndex]);
        OnSelectedWeaponChanged?.Invoke(0, _optionsDatabase.WeaponDatas[customisationState.PrimaryWeaponIndex]);
        OnSelectedWeaponChanged?.Invoke(1, _optionsDatabase.WeaponDatas[customisationState.SecondaryWeaponIndex]);
        OnSelectedWeaponChanged?.Invoke(2, _optionsDatabase.WeaponDatas[customisationState.TertiaryWeaponIndex]);
        OnSelectedAbilityChanged?.Invoke(_optionsDatabase.AbilityDatas[customisationState.AbilityIndex]);
    }
    private void Awake()
    {
        PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
        PlayerCustomisationManager.OnPlayerCustomisationFinalised += PlayerCustomisationManager_OnPlayerCustomisationFinalised;
    }
    private void OnDestroy()
    {
        PlayerCustomisationManager.OnPlayerCustomisationStateChanged -= PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
        PlayerCustomisationManager.OnPlayerCustomisationFinalised -= PlayerCustomisationManager_OnPlayerCustomisationFinalised;
    }

    private void PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ulong clientID, PlayerCustomisationState customisationState)
    {
        if (clientID == ownerClientID)
            UpdatePlayer(customisationState);
    }
    private void PlayerCustomisationManager_OnPlayerCustomisationFinalised(ulong clientID, PlayerCustomisationState customisationState)
    {
        if (clientID != ownerClientID)
            return;
        
        // Cache data for event call.
        FrameData activeFrame = _optionsDatabase.FrameDatas[customisationState.FrameIndex];
        LegsData activeLeg = _optionsDatabase.LegDatas[customisationState.LegIndex];
        WeaponData[] activeWeapons = new WeaponData[activeFrame.WeaponSlotCount];
        if (activeFrame.WeaponSlotCount > 0)
        {
            activeWeapons[0] = _optionsDatabase.WeaponDatas[customisationState.PrimaryWeaponIndex];
            if (activeFrame.WeaponSlotCount > 1)
            {
                activeWeapons[1] = _optionsDatabase.WeaponDatas[customisationState.SecondaryWeaponIndex];
                if (activeFrame.WeaponSlotCount > 2)
                {
                    activeWeapons[2] = _optionsDatabase.WeaponDatas[customisationState.TertiaryWeaponIndex];
                }
            }
        }
        AbilityData activeAbility = _optionsDatabase.AbilityDatas[customisationState.AbilityIndex];


        // Call our event.
        OnPlayerCustomisationFinalised?.Invoke(activeFrame, activeLeg, activeWeapons, activeAbility);
    }
}