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
                    .OnSelectedLegChanged(_optionsDatabase.LegDatas[customisationState.LegIndex]);
                for(int j = 1; j <= SlotIndex.Unset.GetMaxPossibleSlots(); ++j)
                {
                    _gfxElements[i].OnSelectedSlottableDataChanged((SlotIndex)j, _optionsDatabase.GetSlottableData(customisationState.GetSlottableDataIndexForSlot((SlotIndex)j)));
                }
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
            /*FrameData activeFrame = _optionsDatabase.FrameDatas[customisationState.FrameIndex];
            LegData activeLeg = _optionsDatabase.LegDatas[customisationState.LegIndex];
            SlottableData[] activeSlots = new SlottableData[SlotIndex.Unset.GetMaxPossibleSlots()];
            for(int i = 0; i < SlotIndex.Unset.GetMaxPossibleSlots(); ++i)
            {
                activeSlots[i] = _optionsDatabase.GetSlottableData(i);
                if (i > SlotIndexExtensions.WEAPON_SLOT_COUNT || i < activeFrame.WeaponSlotCount)
                {
                    activeSlots[i] = _optionsDatabase.WeaponSlotDatas[i];
                }
            }*/


            // Call our event.
            //OnPlayerCustomisationFinalised?.Invoke(activeFrame, activeLeg, activeWeapons, activeAbility);
        }
    }
}