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
            // Set our position to the screen-space position of our target world position.
            transform.position = _camera.WorldToScreenPoint(CalculateTargetWorldPosition());
        }

        /// <summary>
        ///     Calculates the world position of the crosshair, taking into account obstructions and camera offset alignment.
        /// </summary>
        private Vector3 CalculateTargetWorldPosition()
        {
            Vector3 crosshairOriginPosition = _slotGFXSection.GetAbilityWorldOrigin();
            Vector3 naiveCrosshairWorldPosition = crosshairOriginPosition + _slotGFXSection.GetAbilityWorldDirection() * Constants.TARGET_ESTIMATION_RANGE;    // Crosshair position assuming no obstacles or camera offset.
            if (Physics.Linecast(crosshairOriginPosition, naiveCrosshairWorldPosition, out RaycastHit hitInfo, _obstructionLayers))
            {
                // There is an obstruction between our origin and furthest position.
                // Our hit position will be the world position of our crosshair.
                return hitInfo.point;
            }
            else
            {
                // There are no obstructions between our origin and furthest position.
                // Our hit position will be the furthest position, but adjusted to account for the horizontal offset of the player's camera.
                Ray ray = new Ray(crosshairOriginPosition, (crosshairOriginPosition - naiveCrosshairWorldPosition).normalized);
                CameraControllerTest.MaxTargetDistancePlane.Raycast(ray, out float enter);
                return ray.GetPoint(enter);
            }
        }
    }
}