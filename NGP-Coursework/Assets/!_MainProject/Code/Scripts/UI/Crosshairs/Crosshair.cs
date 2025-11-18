using Gameplay.GameplayObjects.Character.Customisation.Sections;
using UI.Actions;
using UnityEngine;

namespace UI.Crosshairs
{
    // Crosshair Types:
    //  - Static Aim (Where it would hit at max range)
    //  - Actual Aim (Where we are expecting to hit given collisions)
    //  - Seeking Target Lock Radius
    //  - Charge Percentage Bar
    public class Crosshair : MonoBehaviour
    {
        protected SlotGFXSection SlotGFX;
        protected LayerMask ObstructionMask;


        [Header("Subcomponents")]
        [SerializeField] protected PlayerActionChargeDisplayUI[] ChargeDisplayUI;


        public virtual void SetupCrosshair(Camera camera, SlotGFXSection slotGFX, AttachmentSlotIndex attachmentSlot, LayerMask obstructionMask)
        {
            // Set our cached values.
            this.SlotGFX = slotGFX;
            this.ObstructionMask = obstructionMask;

            // Update our required subcomponents.
            for (int i = 0; i < ChargeDisplayUI.Length; ++i)
            {
                ChargeDisplayUI[i].SetAttachmentSlotIndex(attachmentSlot);
            }
        }

        /// <summary>
        ///     Update the screen position of the crosshair.
        /// </summary>
        public virtual void UpdateCrosshairPosition(Camera camera)
        {
            transform.position = camera.WorldToScreenPoint(GetTargetWorldPosition());
        }

        /// <summary>
        ///     Calculates the world position of the crosshair, taking into account obstructions and camera offset alignment.
        /// </summary>
        protected virtual Vector3 GetTargetWorldPosition()
        {
            Vector3 crosshairOriginPosition = SlotGFX.GetAbilityWorldOrigin();
            Vector3 naiveCrosshairWorldPosition = crosshairOriginPosition + SlotGFX.GetAbilityWorldDirection() * Constants.TARGET_ESTIMATION_RANGE;    // Crosshair position assuming no obstacles or camera offset.
            if (Physics.Linecast(crosshairOriginPosition, naiveCrosshairWorldPosition, out RaycastHit hitInfo, ObstructionMask))
            {
                // There is an obstruction between our origin and furthest position.
                // Our hit position will be the world position of our crosshair.
                return hitInfo.point;
            }
            else
            {
                // There are no obstructions between our origin and furthest position.
                // Our hit position will be our naive position, but adjusted to account for the horizontal offset of the player's camera.
                Ray ray = new Ray(crosshairOriginPosition, (naiveCrosshairWorldPosition - crosshairOriginPosition).normalized);
                CameraControllerTest.CrosshairAdjustmentPlane.Raycast(ray, out float enter);
                return ray.GetPoint(enter);
            }
        }
    }


    // Handles Multiple Crosshairs to show the piercing position.
    public class PiercingCrosshair : Crosshair
    { }
    // Displays.
    public class SeekingCrosshair : Crosshair
    { }
}