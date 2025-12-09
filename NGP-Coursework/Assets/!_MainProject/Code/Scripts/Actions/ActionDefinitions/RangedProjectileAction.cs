using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Projectiles;
using Gameplay.GameplayObjects.Projectiles.Seeking;
using Gameplay.Actions.Effects;


namespace Gameplay.Actions.Definitions
{
    /// <summary>
    ///     An action that uses a raycast to trigger effects on targets from a range.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/Projectile Action", order = 2)]
    public class RangedProjectileAction : ActionDefinition
    {
        [Header("Projectile Settings")]
        [SerializeField] private ProjectileInfo _projectileInfo;
        [SerializeReference, SubclassSelector] private SeekingFunction _seekingFunction;

        public float MaxRange => _projectileInfo.MaxRange > 0 ? _projectileInfo.MaxRange : _projectileInfo.MaxLifetime * _projectileInfo.Speed;


        public override bool OnStart(ServerCharacter owner, ref ActionRequestData data) => ActionConclusion.Continue;
        public override bool OnUpdate(ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            SpawnProjectile(owner, ref data, chargePercentage);
            return ActionConclusion.Continue;
        }


        
        /// <summary>
        ///     Spawn & Initialise a Projectile instance.
        /// </summary>
        private void SpawnProjectile(ServerCharacter owner, ref ActionRequestData data, float chargePercentage)
        {
            // Calculate our spawn position & initial facing direction.
            Vector3 spawnPosition = GetActionOrigin(ref data);
            Vector3 spawnDirection = GetActionDirection(ref data);
            spawnPosition += spawnDirection * _projectileInfo.ProjectilePrefab.GetAdditionalSpawnDistance();

            // Create and initialise the projectile instance.
            Projectile projectileInstance = GameObject.Instantiate<Projectile>(_projectileInfo.ProjectilePrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
            SeekingFunction seekingFunction = _seekingFunction != null ? SetupSeekingFunction(owner, projectileInstance, ref data) : null;  // Create & Setup a SeekingFunction instance ONLY IF we are wanting to use one.
            projectileInstance.Initialise(owner.NetworkObjectId, _projectileInfo, seekingFunction, (ActionHitInformation info) => OnProjectileHit(owner, info, chargePercentage));
            projectileInstance.GetComponent<NetworkObject>().Spawn(true);   // Spawn the projectile on clients.
        }
        /// <summary>
        ///     Create, Setup, and Return a SeekingFunction instance based on our set _seekingFunction's type.
        /// </summary>
        private SeekingFunction SetupSeekingFunction(ServerCharacter owner, Projectile projectileInstance, ref ActionRequestData data)
        {
            switch (_seekingFunction)
            {
                case RaycastSeekingFunction:
                    {
                        RaycastSeekingFunction raycastSeekingFunction = new RaycastSeekingFunction(_seekingFunction as RaycastSeekingFunction);

                        //Transform seekingOriginTransform = owner.transform;
                        Transform seekingOriginTransform = data.OriginTransform ?? owner.transform;
                        Vector3 origin = Vector3.zero, direction = Vector3.forward; // Local offsets.

                        return raycastSeekingFunction.Setup(_projectileInfo, seekingOriginTransform, origin, direction);
                    }
                case FixedTargetSeekingFunction:
                    {
                        FixedTargetSeekingFunction fixedTargetSeekingFunction = new FixedTargetSeekingFunction(_seekingFunction as FixedTargetSeekingFunction);
                        throw new System.NotImplementedException("Not Implemented - Target Aquisiton");
                    }
                case NearestTargetSeekingFunction:
                    {
                        NearestTargetSeekingFunction nearestTargetSeekingFunction = new NearestTargetSeekingFunction(_seekingFunction as NearestTargetSeekingFunction);
                        return nearestTargetSeekingFunction.Setup(owner.transform, projectileInstance);
                    }
                default: throw new System.NotImplementedException();
            }
        }

        /// <summary>
        ///     Process the projectile hitting a target.
        /// </summary>
        /// <remarks>
        ///     For projectiles such as <see cref="ExplosiveProjectile"/>,
        ///     this function is also used for their additional hit effects (E.g. Hitting targets via an Explosion).
        /// </remarks>
        private void OnProjectileHit(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            // Play Hit Effects (HitMarkers & Sounds on Triggering Client, contents of HitEffects on All Clients).
            // Note: We don't perform anticipation on projectiles as they are non-instant (Unlike raycasts),
            //      so ALL clients are having the visuals called from the server through RPCs.
            HitEffectManager.PlayHitEffectsOnTriggeringClient(owner.OwnerClientId, hitInfo.HitPoint, hitInfo.HitNormal, chargePercentage, ActionID);
            HitEffectManager.PlayHitEffectsOnNonTriggeringClients(owner.OwnerClientId, hitInfo, chargePercentage, ActionID);

            // Perform this action's effects (Damage, Applying Statuses, etc) on the server (Changes are perpetuated to clients).
            for (int i = 0; i < ActionEffects.Length; ++i)
            {
                ActionEffects[i].ApplyEffect(owner, hitInfo, chargePercentage);
            }
        }
    }
}