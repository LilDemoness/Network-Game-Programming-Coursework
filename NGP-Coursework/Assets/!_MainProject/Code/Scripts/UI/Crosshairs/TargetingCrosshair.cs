using Gameplay.GameplayObjects.Character;
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


        private void Awake()
        {
            if (_attachmentSlotIndex == AttachmentSlotIndex.Unset)
            {
                Debug.LogError($"Error: {this.name} has an unset {nameof(AttachmentSlotIndex)}", this);
                return;
            }

            // Build Change Event (Enable/Disable State of this UI element).
            PlayerManager.OnLocalPlayerBuildUpdated += PlayerManager_OnLocalPlayerBuildUpdated;
        }
        private void OnDestroy()
        {
            PlayerManager.OnLocalPlayerBuildUpdated -= PlayerManager_OnLocalPlayerBuildUpdated;
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
            _slotGFXSection = PlayerManager.LocalClientInstance.GetSlotGFXForIndex(_attachmentSlotIndex);

            // Update our Crosshair Settings (Reticule Type, Seeking Radius, Charging Bar, etc).
        }


        private void Update()
        {
            if (_slotGFXSection == null)
                return;

            UpdateCrosshairPosition();
        }
        private void UpdateCrosshairPosition()
        {
            // Get targeted position.
            Vector3 crosshairOriginPosition = _slotGFXSection.GetAbilityWorldOrigin();
            Vector3 targetWorldPosition = _slotGFXSection.SlottableData.AssociatedAction.GetTargetPosition(crosshairOriginPosition, _slotGFXSection.GetAbilityWorldDirection());
            if (Physics.Linecast(crosshairOriginPosition, targetWorldPosition, out RaycastHit hitInfo, _obstructionLayers))
            {
                targetWorldPosition = hitInfo.point;
            }

            // Translate targeted position from world space to screen space.
            Vector3 targetScreenPosition = _camera.WorldToScreenPoint(targetWorldPosition);

            // Set our position to the screen space targeted position.
            transform.position = targetScreenPosition;
        }
    }
}