using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Projectiles;
using Gameplay.Actions.Effects;


namespace Gameplay.Actions
{
    /// <summary>
    ///     An action that uses a raycast to trigger effects on targets from a range.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/New Ranged Projectile Action")]
    public class RangedProjectileAction : DefaultAction
    {
        [Header("Projectile Settings")]
        [SerializeField] private ProjectileInfo _projectileInfo;
        [SerializeReference, SubclassSelector] private SeekingFunction _seekingFunction;


        [Header("Effects")]
        [SerializeReference][SubclassSelector] private ActionEffect[] _actionEffects;


        protected override bool HandleStart(ServerCharacter owner) => ActionConclusion.Continue;
        protected override bool HandleUpdate(ServerCharacter owner)
        {
            SpawnProjectile(owner);
            return ActionConclusion.Continue;
        }


        
        private void SpawnProjectile(ServerCharacter owner)
        {
            Vector3 spawnPosition = GetActionOrigin();
            Vector3 spawnDirection = GetActionDirection();
            spawnPosition += spawnDirection * _projectileInfo.ProjectilePrefab.GetAdditionalSpawnDistance();

            Projectile projectileInstance = GameObject.Instantiate<Projectile>(_projectileInfo.ProjectilePrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
            SeekingFunction seekingFunction = _seekingFunction != null ? SetupSeekingFunction(owner, projectileInstance) : null;
            projectileInstance.Initialise(owner.NetworkObjectId, _projectileInfo, seekingFunction, (ActionHitInformation info) => OnProjectileHit(owner, info));
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

        private void OnProjectileHit(ServerCharacter owner, in ActionHitInformation hitInfo)
        {
            Debug.Log($"{hitInfo.Target.name} was hit!");

            for (int i = 0; i < _actionEffects.Length; ++i)
            {
                _actionEffects[i].ApplyEffect(owner, hitInfo);
            }
        }
    }
}