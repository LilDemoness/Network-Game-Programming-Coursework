using Gameplay.GameplayObjects.Players;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Character.Customisation.Sections;
using System.Collections.Generic;
using UI.Actions;
using Unity.Netcode;
using UnityEngine;

namespace UI.Crosshairs
{
    public class TargetingCrosshair : MonoBehaviour
    {
        // Crosshair Components:
        //  - Static Aim (Where it would hit at max range)
        //  - Actual Aim (Where we are expecting to hit given collisions)
        //  - Seeking Target Lock Radius
        //  - Charge Percentage Bar


        [SerializeField] private AttachmentSlotIndex _attachmentSlotIndex;

        private SlotGFXSection _slotGFXSection;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _obstructionLayers;

        private Crosshair _crosshair;


        private void Awake()
        {
            if (_attachmentSlotIndex == AttachmentSlotIndex.Unset)
            {
                Debug.LogError($"Error: {this.name} has an unset {nameof(AttachmentSlotIndex)}", this);
                return;
            }

            // Build Change Event (Enable/Disable State of this UI element).
            Player.OnLocalPlayerBuildUpdated += PlayerManager_OnLocalPlayerBuildUpdated;
        }
        private void OnDestroy()
        {
            Player.OnLocalPlayerBuildUpdated -= PlayerManager_OnLocalPlayerBuildUpdated;
        }


        private void PlayerManager_OnLocalPlayerBuildUpdated(BuildData buildData)
        {
            if (buildData.GetFrameData().AttachmentPoints.Length < (int)_attachmentSlotIndex)
                DisableCrosshair();
            else
                EnableCrosshair();
        }

        private void DisableCrosshair() => this.gameObject.SetActive(false);
        private void EnableCrosshair()
        {
            // Enable the crosshair root (Currently always this GO).
            this.gameObject.SetActive(true);

            // Cache a reference to our slot section.
            _slotGFXSection = Player.LocalClientInstance.GetSlotGFXForIndex(_attachmentSlotIndex);

            // Update our Crosshair Settings (Reticule Type, Seeking Radius, Charging Bar, etc).
            SetCrosshairFromPrefab(_slotGFXSection.SlottableData.AssociatedAction.ActionCrosshairPrefab);
        }


        private void Update()
        {
            if (_slotGFXSection == null)
                return;

            _crosshair.UpdateCrosshairPosition(_camera);
        }

        public void SetCrosshairFromPrefab(Crosshair crosshairPrefab)
        {
            if (_crosshair != null)
            {
                // Remove the current crosshair.
                Destroy(_crosshair.gameObject);
            }

            if (crosshairPrefab == null)
            {
                DisableCrosshair();
                return;
            }    

            // Create and cache the new crosshair.
            _crosshair = Instantiate<Crosshair>(crosshairPrefab, this.transform);
            _crosshair.SetupCrosshair(_camera, _slotGFXSection, _attachmentSlotIndex, _obstructionLayers);
        }


#if UNITY_EDITOR
        [ContextMenu(itemName: "Debug/Setup Crosshair Again")]
        private void Editor_ReSetupCrosshair()
        {
            if (_crosshair == null)
                return;

            _crosshair.SetupCrosshair(_camera, _slotGFXSection, _attachmentSlotIndex, _obstructionLayers);
        }
#endif
    }
}