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
        private ulong ownerClientID;
        public ulong ClientID => this.ownerClientID;



        [SerializeField] private FrameGFX[] _gfxElements;
    

        public void Setup(ulong ownerClientID) => this.ownerClientID = ownerClientID;
        public void Setup(ulong ownerClientID, BuildData initialState)
        {
            this.ownerClientID = ownerClientID;
            PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ownerClientID, initialState);
        }
        private void Awake()
        {
            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged += PlayerCustomisationManager_OnPlayerCustomisationStateChanged;

            _gfxElements = GetComponentsInChildren<FrameGFX>();
        }
        private void OnDestroy()
        {
            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged -= PlayerCustomisationManager_OnPlayerCustomisationStateChanged;
        }

        private void PlayerCustomisationManager_OnPlayerCustomisationStateChanged(ulong clientID, BuildData buildData)
        {
            if (clientID != ownerClientID)
                return; // Not the owning client.


            for (int i = 0; i < _gfxElements.Length; ++i)
            {
                _gfxElements[i].OnSelectedFrameChanged(CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(buildData.ActiveFrameIndex));
                AttachmentSlotIndexExtensions.PerformForAllValidSlots(
                    (slotIndex) => _gfxElements[i].OnSelectedSlottableDataChanged(slotIndex, buildData.GetSlottableData(slotIndex)));
            }
        }
    }
}