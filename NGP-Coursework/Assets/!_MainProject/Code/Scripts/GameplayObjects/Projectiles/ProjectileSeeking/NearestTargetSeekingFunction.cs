using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles.Seeking
{
    [System.Serializable]
    public class NearestTargetSeekingFunction : SeekingFunction
    {
        // Target Aquisition.
        [SerializeField] private float _targetAquisitionRadius;
        [SerializeField] private LayerMask _targetableLayers;
        private Transform m_targetCheckOriginTransform;
        private Transform m_owner;


        // Target Caching.
        [SerializeField] private float _minUpdateTargetDelay;  // How often we check for a target when we already have one. We check every 'GetTargetPosition' call if we have no target no matter this value.
        private float m_nextUpdateTargetTime;
        private Transform m_currentTarget;


        public NearestTargetSeekingFunction(NearestTargetSeekingFunction other)
        {
            this._targetAquisitionRadius = other._targetAquisitionRadius;
            this._targetableLayers = other._targetableLayers;
            this._minUpdateTargetDelay = other._minUpdateTargetDelay;
        }
        public NearestTargetSeekingFunction Setup(Transform owner, Projectile projectile)
        {
            this.m_targetCheckOriginTransform = projectile.transform;
            this.m_owner = owner;
            this.m_nextUpdateTargetTime = 0.0f;
            this.m_currentTarget = null;

            return this;
        }

        public override bool TryGetTargetDirection(Vector3 currentPosition, out Vector3 seekingDirection)
        {
            if (m_nextUpdateTargetTime <= Time.time || m_currentTarget != null)
            {
                // Update our current target.
                m_nextUpdateTargetTime = Time.time + _minUpdateTargetDelay;
                m_currentTarget = FindClosestTarget();
            }

            if (m_currentTarget == null)
            {
                seekingDirection = Vector3.zero;
                return false;
            }
            else
            {
                seekingDirection = (m_currentTarget.position - currentPosition).normalized;
                return true;
            }
        }
        private Transform FindClosestTarget()
        {
            Collider[] potentialTargets = Physics.OverlapSphere(m_targetCheckOriginTransform.position, _targetAquisitionRadius, _targetableLayers, QueryTriggerInteraction.Ignore);
            
            // No targets within range.
            if (potentialTargets.Length == 0)
                return null;

            // Find the closest valid target.
            int closestTargetIndex = -1;
            float closestTargetSqrDistance = _targetAquisitionRadius * _targetAquisitionRadius;
            for(int i = 0; i < potentialTargets.Length; ++i)
            {
                // Check the sqr distance to the target.
                float sqrDistance = (m_targetCheckOriginTransform.position - potentialTargets[i].transform.position).sqrMagnitude;
                if (sqrDistance > closestTargetSqrDistance)
                    continue;   // Not the closest target.


                // Check if the target is valid.
                if (potentialTargets[i].HasParent(m_targetCheckOriginTransform))
                    continue;   // This target is the origin target.
                if (potentialTargets[i].HasParent(m_owner))
                    continue;   // This target is the owner.


                // The target is valid
                closestTargetIndex = i;
                closestTargetSqrDistance = sqrDistance;
            }

            // Return the closest valid target, or null if there are no valid targets within range.
            return closestTargetIndex != -1 ? potentialTargets[closestTargetIndex].transform : null;
        }
    }
}