using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles.Seeking
{
    public abstract class SeekingFunction
    {
        /// <summary>
        ///     Calculate the seeking direction for a projectile to steer towards, if possible.
        /// </summary>
        /// <param name="currentPosition"> The current position of the projectile.</param>
        /// <param name="seekingDirection"> The normalized direction that the projectile should steer towards.</param>
        /// <returns> True if successfully calculated a seekingDirection, false otherwise.</returns>
        public abstract bool TryGetTargetDirection(Vector3 currentPosition, out Vector3 seekingDirection);
    }
}