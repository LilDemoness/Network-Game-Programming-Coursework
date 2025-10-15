using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles.Seeking
{
    [System.Serializable]
    public class RaycastSeekingFunction : SeekingFunction
    {
        private Transform m_originTransform;
        private Vector3 m_position;    // World space if 'OriginTransform' is null. Otherwise, local space.
        private Vector3 m_direction;   // World space if 'OriginTransform' is null. Otherwise, local space.
        private float m_maxDistance;
        private const float DEFAULT_MAX_DISTANCE = 50.0f;
        [SerializeField] private LayerMask _targetableLayers;


        public RaycastSeekingFunction(RaycastSeekingFunction other)
        {
            this._targetableLayers = other._targetableLayers;
        }
        public RaycastSeekingFunction Setup(in ProjectileInfo projectile, Transform originTransform, Vector3 position, Vector3 direction)
        {
            this.m_originTransform = originTransform;
            this.m_position = position;
            this.m_direction = direction;

            // Determine distance based on MaxRange, MaxLifetime & Speed, or use the Default Max Distance.
            this.m_maxDistance = projectile.MaxRange > 0
                ? projectile.MaxRange
                : projectile.MaxLifetime > 0
                    ? projectile.MaxLifetime * projectile.Speed
                    : DEFAULT_MAX_DISTANCE;

            return this;
        }
        public override bool TryGetTargetDirection(Vector3 currentPosition, out Vector3 seekingDirection)
        {
            Vector3 rayOrigin = m_originTransform != null ? m_originTransform.TransformPoint(m_position) : m_position;
            Vector3 rayDirection = (m_originTransform != null ? m_originTransform.TransformDirection(m_direction) : m_direction).normalized;

            Debug.DrawRay(rayOrigin, rayDirection, Color.red, 0.1f);

            // Determine our target position.
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, m_maxDistance, _targetableLayers, QueryTriggerInteraction.Ignore))
                seekingDirection = (hitInfo.point - currentPosition).normalized;
            else
            {
                Vector3 seekingPos = (rayOrigin + rayDirection * m_maxDistance);
                Debug.DrawRay(seekingPos, Vector3.up, Color.red, 0.1f);
                seekingDirection = (seekingPos - currentPosition).normalized;
            }

            // We will always have a target position, even if we don't get a hit on our raycast.
            return true;
        }
    }
}