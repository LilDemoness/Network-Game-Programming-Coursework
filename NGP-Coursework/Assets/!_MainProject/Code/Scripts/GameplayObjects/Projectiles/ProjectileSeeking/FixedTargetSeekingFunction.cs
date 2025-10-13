using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles
{
    [System.Serializable]
    public class FixedTargetSeekingFunction : SeekingFunction
    {
        private Transform _target;


        public FixedTargetSeekingFunction(FixedTargetSeekingFunction other) { }
        public FixedTargetSeekingFunction Setup(Transform target)
        {
            this._target = target;

            return this;
        }

        public override bool TryGetTargetPosition(out Vector3 targetPosition)
        {
            if (_target == null)
            {
                targetPosition = Vector3.zero;
                return false;
            }
            else
            {
                targetPosition = _target.position;
                return true;
            }
        }
    }
}