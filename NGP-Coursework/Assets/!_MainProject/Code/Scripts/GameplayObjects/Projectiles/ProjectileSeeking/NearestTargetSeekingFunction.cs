using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles
{
    [System.Serializable]
    public class NearestTargetSeekingFunction : SeekingFunction
    {
        // Target Aquisition.
        public float TargetAquisitionRadius;
        public LayerMask TargetableLayers;
        private Transform _targetCheckOriginTransform;
        private Transform _owner;


        // Target Caching.
        public float MinUpdateTargetDelay;  // How often we check for a target when we already have one. We check every 'GetTargetPosition' call if we have no target no matter this value.
        private float _nextUpdateTargetTime;
        private Transform _currentTarget;


        public NearestTargetSeekingFunction(NearestTargetSeekingFunction other)
        {
            this.TargetAquisitionRadius = other.TargetAquisitionRadius;
            this.TargetableLayers = other.TargetableLayers;
            this.MinUpdateTargetDelay = other.MinUpdateTargetDelay;
        }
        public NearestTargetSeekingFunction Setup(Transform owner, Projectile projectile)
        {
            this._targetCheckOriginTransform = projectile.transform;
            this._owner = owner;
            this._nextUpdateTargetTime = 0.0f;

            return this;
        }

        public override bool TryGetTargetPosition(out Vector3 targetPosition)
        {
            if (_nextUpdateTargetTime <= Time.time || _currentTarget != null)
            {
                // Update our current target.
                _nextUpdateTargetTime = Time.time + MinUpdateTargetDelay;
                _currentTarget = FindClosestTarget();
            }

            if (_currentTarget == null)
            {
                targetPosition = Vector3.zero;
                return false;
            }
            else
            {
                targetPosition = _currentTarget.position;
                return true;
            }
        }
        private Transform FindClosestTarget()
        {
            Collider[] potentialTargets = Physics.OverlapSphere(_targetCheckOriginTransform.position, TargetAquisitionRadius, TargetableLayers, QueryTriggerInteraction.Ignore);
            
            // No targets within range.
            if (potentialTargets.Length == 0)
                return null;

            // Find the closest valid target.
            int closestTargetIndex = -1;
            float closestTargetSqrDistance = TargetAquisitionRadius * TargetAquisitionRadius;
            for(int i = 0; i < potentialTargets.Length; ++i)
            {
                // Check the sqr distance to the target.
                float sqrDistance = (_targetCheckOriginTransform.position - potentialTargets[i].transform.position).sqrMagnitude;
                if (sqrDistance > closestTargetSqrDistance)
                    continue;   // Not the closest target.


                // Check if the target is valid.
                if (potentialTargets[i].HasParent(_targetCheckOriginTransform))
                    continue;   // This target is the origin target.
                if (potentialTargets[i].HasParent(_owner))
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