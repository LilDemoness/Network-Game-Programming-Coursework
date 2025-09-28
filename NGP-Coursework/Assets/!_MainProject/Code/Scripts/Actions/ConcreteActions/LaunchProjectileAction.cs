using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions
{
    /// <summary>
    ///     Action responsible for creating a projectile object.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/Launch Projectile Action")]
    public class LaunchProjectileAction : Action
    {
        private bool _hasLaunched = false;


        public override bool OnStart(ServerCharacter serverCharacter)
        {
            return true; // Temp.
        }

        public override void Reset()
        {
            _hasLaunched = false;
            base.Reset();
        }

        public override bool OnUpdate(ServerCharacter serverCharacter)
        {
            if (TimeRunning >= Config.ExecuteTimeSeconds && !_hasLaunched)
            {
                LaunchProjectile(serverCharacter);
            }

            return true;
        }

        /// <summary>
        ///     Looks through the ProjectileInfo list and finds the appropriate one to instantiate.
        ///     For the base class, this is always just the first entry with a valid prefab in it.
        /// </summary>
        /// <exception cref="System.Exception"> Thrown if no projectiles are valid.</exception>
        protected virtual ProjectileInfo GetProjectileInfo()
        {
            foreach (var projectileInfo in Config.Projectiles)
            {
                if (projectileInfo.ProjectilePrefab)
                    return projectileInfo;
            }

            throw new System.Exception($"Action {name} has no usable Projectiles!");
        }


        /// <summary>
        ///     Instantiates and configures the prefab. Repeatedly calling this does nothing.
        /// </summary>
        /// <remarks> This calls GetProjectilePrefab() to find the prefab it should instantiate. </remarks>
        protected void LaunchProjectile(ServerCharacter parent)
        {
            if (_hasLaunched)
                return;

            _hasLaunched = true;

            Debug.Log("Player Started Shooting");
        }


        public override void End(ServerCharacter serverCharacter)
        {
            // Ensure that we've launched the projectile.
            LaunchProjectile(serverCharacter);
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            Debug.Log("Player Stopped Shooting");

            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                //serverCharacter.ServerAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
                throw new System.NotImplementedException();
            }
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return ActionConclusion.Continue;
        }
    }
}