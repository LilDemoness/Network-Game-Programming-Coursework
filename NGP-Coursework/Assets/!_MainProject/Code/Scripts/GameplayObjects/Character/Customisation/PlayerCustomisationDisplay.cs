using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Character.Customisation.Sections;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    /// <summary>
    ///     A client-side script to relay the changes in customisation to the avatar's FrameGFX instances when a player is customising their character.
    /// </summary>
    public class PlayerCustomisationDisplay : MonoBehaviour
    {
        [SerializeField] private CustomisationOptionsDatabase _optionsDatabase;
        private ulong ownerClientID;
        public ulong ClientID => this.ownerClientID;



        [SerializeField] private FrameGFX[] _gfxElements;
    

        public void Setup(ulong ownerClientID) => this.ownerClientID = ownerClientID;
        public void Setup(ulong ownerClientID, PlayerCustomisationState initialState)
        {
            this.ownerClientID = ownerClientID;
            UpdatePlayer(initialState);
        }
        public void UpdatePlayer(PlayerCustomisationState customisationState)
        {
            for(int i = 0; i < _gfxElements.Length; ++i)
            {
                _gfxElements[i]
                    .OnSelectedFrameChanged(_optionsDatabase.FrameDatas[customisationState.FrameIndex])
                    .OnSelectedLegChanged(_optionsDatabase.LegDatas[customisationState.LegIndex])
                    .OnSelectedWeaponChanged(0, _optionsDatabase.WeaponDatas[customisationState.PrimaryWeaponIndex])
                    .OnSelectedWeaponChanged(1, _optionsDatabase.WeaponDatas[customisationState.SecondaryWeaponIndex])
                    .OnSelectedWeaponChanged(2, _optionsDatabase.WeaponDatas[customisationState.TertiaryWeaponIndex])
                    .OnSelectedAbilityChanged(_optionsDatabase.AbilityDatas[customisationState.AbilityIndex]);
            }
        }
        private void Awake()
        {
            PlayerCustomisationManager.OnPlayerCustomisationStateChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
            PlayerCustomisationManager.OnPlayerCustomisationFinalised += PlayerCustomisationManager_OnPlayerCustomisationFinalised;

            _gfxElements = GetComponentsInChildren<FrameGFX>();
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
            LegData activeLeg = _optionsDatabase.LegDatas[customisationState.LegIndex];
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
            //OnPlayerCustomisationFinalised?.Invoke(activeFrame, activeLeg, activeWeapons, activeAbility);
        }
    }
}