using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles
{
    [System.Serializable]
    public struct ProjectileInfo
    {
        [Tooltip("Prefab used for the projectile.")]
        public Projectile ProjectilePrefab;

        [Tooltip("Projectile's speed (In meters/second).")]
        public float Speed;
        
        [Tooltip("The layers the projectile can hit.")]
        public LayerMask TargetableLayers;
        

        [Header("Auto Destruction")]
        [Tooltip("Maximum range of the Projectile (In Seconds). '0.0' for Infinite Range.")]
        [Min(0.0f)] public float MaxRange;

        [Tooltip("Maximum lifetime of the Projectile (In Seconds). '0.0' for Infinite Lifetime.")]
        [Min(0.0f)] public float MaxLifetime;

        [Tooltip("Maximum number of additional targets the Projectile can hit (Either piercing or bouncing). 0 for only a single target.")]
        [Min(0)] public int MaxHits;


        [Header("Seeking")]
        //[Tooltip("Should this projectile seek after a set target (Won't seek if there is no target)?")]
        //public bool PerformSeeking;

        [Tooltip("Projectile's rotation speed to face its target (In Degrees/Second).")]
        public float SeekingSpeed;

        [Tooltip("Delay between the spawning of a projectile and its seeking activating (In Seconds).")]
        public float SeekingInitialDelay;
    }
}