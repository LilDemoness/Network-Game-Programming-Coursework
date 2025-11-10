using Gameplay.GameplayObjects.Character.Customisation.Sections;
using UnityEngine;

namespace UI.Crosshairs
{
    public class TargetingCrosshair : MonoBehaviour
    {
        // Types of crosshairs:
        //  - Static Aim
        //  - Seeking Target Lock


        [SerializeField] private SlotGFXSection _slotGFX;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _obstructionLayers;


        private void Update()
        {
            if (_slotGFX == null)
                return;

            // Get targeted position.
            Vector3 crosshairOriginPosition = _slotGFX.GetAbilityWorldOrigin();
            Vector3 targetWorldPosition = _slotGFX.SlottableData.AssociatedAction.GetTargetPosition(crosshairOriginPosition, _slotGFX.GetAbilityWorldDirection());
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