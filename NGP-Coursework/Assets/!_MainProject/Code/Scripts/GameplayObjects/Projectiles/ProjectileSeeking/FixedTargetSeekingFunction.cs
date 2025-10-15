using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles.Seeking
{
    [System.Serializable]
    public class FixedTargetSeekingFunction : SeekingFunction
    {
        private Transform m_target;


        public FixedTargetSeekingFunction(FixedTargetSeekingFunction other) { }
        public FixedTargetSeekingFunction Setup(Transform target)
        {
            this.m_target = target;

            return this;
        }

        public override bool TryGetTargetDirection(Vector3 currentPosition, out Vector3 seekingDirection)
        {
            if (m_target == null)
            {
                seekingDirection = Vector3.zero;
                return false;
            }
            else
            {
                seekingDirection = (m_target.position - currentPosition).normalized;
                return true;
            }
        }
    }
}