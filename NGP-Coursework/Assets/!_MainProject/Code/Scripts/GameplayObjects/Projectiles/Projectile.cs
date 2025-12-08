using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions.Effects;
using Gameplay.GameplayObjects.Projectiles.Seeking;

namespace Gameplay.GameplayObjects.Projectiles
{
    public class Projectile : NetworkBehaviour
    {
        // Information.
        private ulong _ownerNetworkID;
        private bool _hasStarted = false;
        private bool _isDead = false;

        // Auto Destruction Settings.
        private float _remainingSqrDistance;
        private float _remainingLifetime;
        private int _remainingHits;

        // Data & Callbacks.
        [SerializeField] private ProjectileInfo _projectileInfo;
        private System.Action<ActionHitInformation> _onHitCallback;


        [Header("Projectile Movement")]
        [SerializeField] private float _gravityStrength = 9.81f;
        [SerializeField] private bool _continuousForce;
        [SerializeField] private float _acceleration;

        private Vector3 _targetMovementDirection;
        private Vector3 _currentVelocity;


        [Header("Projectile Seeking")]
        [SerializeReference] private SeekingFunction _seekingFunction;
        [SerializeField] private bool _continueSeekingWhenNotUpdated = true;    // Should we continue steering towards the seeking direction even if our attempt to update it failed.
        private float _seekingStartTime;


        [SerializeField] private SphereCollider _collider;
        public float GetAdditionalSpawnDistance() => _collider.radius;


        public void Initialise(ulong ownerNetworkID, in ProjectileInfo projectileInfo, SeekingFunction seekingFunction, System.Action<ActionHitInformation> onHitCallback)
        {
            this._ownerNetworkID = ownerNetworkID;
            this._projectileInfo = projectileInfo;
            this._seekingFunction = seekingFunction;
            this._onHitCallback = onHitCallback;

            _targetMovementDirection = transform.forward;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _hasStarted = true;

                _isDead = false;

                _remainingSqrDistance = _projectileInfo.MaxRange * _projectileInfo.MaxRange;
                _remainingLifetime = _projectileInfo.MaxLifetime;
                _remainingHits = _projectileInfo.MaxHits;

                _seekingStartTime = Time.time + _projectileInfo.SeekingInitialDelay;
                _currentVelocity = _targetMovementDirection * _projectileInfo.Speed + Vector3.up * 0.25f;
            }

            if (IsClient)
            {

            }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _hasStarted = false;
            }

            if (IsClient)
            {

            }
        }


        private void FixedUpdate()
        {
            if (!IsServer)
                return;
            if (!_hasStarted)
                return;

            
            // Perform Seeking.
            if (_seekingFunction != null && _seekingStartTime <= Time.time)
            {
                if (_seekingFunction.TryGetTargetDirection(transform.position, out Vector3 targetDirection))
                {
                    // Update target direction.
                    _targetMovementDirection = targetDirection;

                    // Rotate towards the target direction.
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(_targetMovementDirection), _projectileInfo.SeekingSpeed * Time.fixedDeltaTime);
                }
                else if (_continueSeekingWhenNotUpdated)
                {
                    // Continue Rotation towards '_targetMovementDirection'.
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(_targetMovementDirection), _projectileInfo.SeekingSpeed * Time.fixedDeltaTime);
                }
            }
            
            // Perform Movement
            Vector3 displacement = transform.forward * _projectileInfo.Speed * Time.fixedDeltaTime;
            Vector3 previousPosition = transform.position;
            transform.position += displacement;


            // Check for Collision.
            if (Physics.Linecast(previousPosition, transform.position, out RaycastHit hitInfo, _projectileInfo.TargetableLayers))
            {
                if (hitInfo.transform.TryGetComponentThroughParents<NetworkObject>(out NetworkObject networkObject))
                    if (networkObject.NetworkObjectId == _ownerNetworkID)
                        return; // Don't hit the entity that spawned us.

                HandleTargetHit(hitInfo);

                _remainingHits -= 1;
                if (_remainingHits < 0)
                {
                    EndProjectile();
                    return;
                }
            }


            if (_remainingSqrDistance > 0.0f)
            {
                // Remaining Distance.
                _remainingSqrDistance -= displacement.sqrMagnitude; // Not quite distance, but close enough for now.
                if (_remainingSqrDistance <= 0.0f)
                    EndProjectile();
            }

            if (_remainingLifetime > 0.0f)
            {
                // Remaining Lifetime.
                _remainingLifetime -= Time.fixedDeltaTime;
                if (_remainingLifetime <= 0.0f)
                    EndProjectile();
            }
        }
        private void Update()
        {
            // Handle lerping graphics.
        }


        protected virtual void EndProjectile() => DisposeSelf();
        protected void DisposeSelf()
        {
            if (_isDead)
                return;
            _isDead = true;
            
            this.GetComponent<NetworkObject>().Despawn(true);
        }


        protected virtual void HandleTargetHit(RaycastHit rayHit)
        {
            EffectTarget(rayHit.transform, rayHit.point, rayHit.normal);
        }
        protected virtual void HandleTargetHit(Collision target)
        {
            Vector3 closestPoint = target.GetContact(0).point;
            Vector3 hitNormal = target.GetContact(0).normal;

            EffectTarget(target.transform, closestPoint, hitNormal);
        }

        protected void EffectTarget(Transform target, Vector3 hitPosition, Vector3 hitNormal)
        {
            ActionHitInformation hitInfo = new ActionHitInformation(target.transform, hitPosition, hitNormal, Vector3.zero);
            _onHitCallback?.Invoke(hitInfo);
        }
    }
}