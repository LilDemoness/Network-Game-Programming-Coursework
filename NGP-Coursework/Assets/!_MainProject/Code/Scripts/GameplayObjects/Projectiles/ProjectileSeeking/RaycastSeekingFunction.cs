using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles
{
    [System.Serializable]
    public class RaycastSeekingFunction : SeekingFunction
    {
        private Transform _originTransform;
        private Vector3 _position;    // World space if 'OriginTransform' is null. Otherwise, local space.
        private Vector3 _direction;   // World space if 'OriginTransform' is null. Otherwise, local space.
        public float MaxDistance;
        public LayerMask TargetableLayers;


        public RaycastSeekingFunction(RaycastSeekingFunction other)
        {
            this.MaxDistance = other.MaxDistance;
            this.TargetableLayers = other.TargetableLayers;
        }
        public RaycastSeekingFunction Setup(Transform originTransform, Vector3 position, Vector3 direction)
        {
            this._originTransform = originTransform;
            this._position = position;
            this._direction = direction;

            return this;
        }
        public override bool TryGetTargetPosition(out Vector3 targetPosition)
        {
            Vector3 rayOrigin = _originTransform != null ? _originTransform.TransformPoint(_position) : _position;
            Vector3 rayDirection = _originTransform != null ? _originTransform.TransformDirection(_direction) : _direction;

            Debug.DrawRay(rayOrigin, rayDirection, Color.red, 0.1f);

            // Determine our target position.
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, MaxDistance, TargetableLayers, QueryTriggerInteraction.Ignore))
                targetPosition = hitInfo.point;
            else
                targetPosition = rayOrigin + rayDirection * MaxDistance;
            
            // We will always have a target position, even if we don't get a hit on our raycast.
            return true;
        }
    }
}