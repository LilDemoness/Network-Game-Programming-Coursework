using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles
{
    public abstract class SeekingFunction
    {
        public abstract bool TryGetTargetPosition(out Vector3 targetPosition);
    }
}