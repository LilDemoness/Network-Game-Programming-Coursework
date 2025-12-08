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


        
        private void SpawnProjectile(ServerCharacter owner, ref ActionRequestData data, float chargePercentage)
        {
            Vector3 spawnPosition = GetActionOrigin(ref data);
            Vector3 spawnDirection = GetActionDirection(ref data);
            spawnPosition += spawnDirection * _projectileInfo.ProjectilePrefab.GetAdditionalSpawnDistance();

            Projectile projectileInstance = GameObject.Instantiate<Projectile>(_projectileInfo.ProjectilePrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
            SeekingFunction seekingFunction = _seekingFunction != null ? SetupSeekingFunction(owner, projectileInstance) : null;
            projectileInstance.Initialise(owner.NetworkObjectId, _projectileInfo, seekingFunction, (ActionHitInformation info) => OnProjectileHit(owner, info, chargePercentage));
            projectileInstance.GetComponent<NetworkObject>().Spawn(true);
        }
        private SeekingFunction SetupSeekingFunction(ServerCharacter owner, Projectile projectileInstance)
        {
            switch (_seekingFunction)
            {
                case RaycastSeekingFunction:
                    RaycastSeekingFunction raycastSeekingFunction = new RaycastSeekingFunction(_seekingFunction as RaycastSeekingFunction);

                    /*Transform originTransform = Data.OriginTransformID != 0 ? NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.OriginTransformID].transform : null;
                    Vector3 origin = m_data.Position;
                    Vector3 direction = m_data.Direction;*/
                    Transform originTransform = owner.transform;
                    Vector3 origin = Vector3.zero, direction = Vector3.forward;

                    return raycastSeekingFunction.Setup(_projectileInfo, originTransform, origin, direction);
                case FixedTargetSeekingFunction:
                    FixedTargetSeekingFunction fixedTargetSeekingFunction = new FixedTargetSeekingFunction(_seekingFunction as FixedTargetSeekingFunction);
                    throw new System.NotImplementedException("Not Implemented Target Aquisiton");
                case NearestTargetSeekingFunction:
                    NearestTargetSeekingFunction nearestTargetSeekingFunction = new NearestTargetSeekingFunction(_seekingFunction as NearestTargetSeekingFunction);
                    return nearestTargetSeekingFunction.Setup(owner.transform, projectileInstance);
                default: throw new System.NotImplementedException();
            }
        }

        private void OnProjectileHit(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            Debug.Log($"{hitInfo.Target.name} was hit!");
            HitEffectManager.PlayHitEffectsOnTriggeringClient(owner.OwnerClientId, hitInfo.HitPoint, hitInfo.HitNormal, chargePercentage, ActionID);
            HitEffectManager.PlayHitEffectsOnNonTriggeringClients(owner.OwnerClientId, hitInfo, chargePercentage, ActionID);

            for (int i = 0; i < ActionEffects.Length; ++i)
            {
                ActionEffects[i].ApplyEffect(owner, hitInfo, chargePercentage);
            }
        }
    }
}