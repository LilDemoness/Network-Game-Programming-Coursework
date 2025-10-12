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
        private Vector3 _targetPosition;


        [Header("Effects")]
        [SerializeReference][SubclassSelector] private ActionEffect[] _actionEffects;


        protected override bool HandleStart(ServerCharacter owner) => ActionConclusion.Continue;
        public override bool OnUpdate(ServerCharacter owner)
        {
            Vector3 spawnPosition = GetActionOrigin();
            _targetPosition = Physics.Raycast(spawnPosition, owner.transform.forward, out RaycastHit hitInfo, 15.0f, LayerMask.GetMask("Default", "Player", "Ground"), QueryTriggerInteraction.Ignore) ? hitInfo.point : spawnPosition + owner.transform.forward * 50.0f;

            return base.OnUpdate(owner);
        }
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
            projectileInstance.Initialise(owner.NetworkObjectId, _projectileInfo, (ActionHitInformation info) => OnProjectileHit(owner, info));
            projectileInstance.GetComponent<NetworkObject>().Spawn(true);
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