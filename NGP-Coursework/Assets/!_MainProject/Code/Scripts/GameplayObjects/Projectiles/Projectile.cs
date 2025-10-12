using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions.Effects;

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


        // Seeking/Homing.
        [Header("Projectile Movement")]
        [SerializeField] private float _gravityStrength = 9.81f;
        [SerializeField] private bool _continuousForce;
        [SerializeField] private float _acceleration;

        private Vector3 _targetMovementDirection;
        private Vector3 _currentVelocity;


        [SerializeField] private SphereCollider _collider;
        public float GetAdditionalSpawnDistance() => _collider.radius;


        public void Initialise(ulong ownerNetworkID, in ProjectileInfo projectileInfo, System.Action<ActionHitInformation> onHitCallback)
        {
            this._ownerNetworkID = ownerNetworkID;
            this._projectileInfo = projectileInfo;
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


        private Vector3 _targetPreviousPosition;
        private void FixedUpdate()
        {
            if (!IsServer)
                return;
            if (!_hasStarted)
                return;

            
            // Perform Seeking.
            /*if (_getTargetPositionFunc != null)
            {
                Vector3 newTargetPos = _getTargetPositionFunc();
                if(newTargetPos != default)
                    _targetPosition = newTargetPos;
            }

            if (_targetPosition != default)
            {
                Vector3 directionToTarget;
                if (_targetPosition == _targetPreviousPosition)
                {
                    directionToTarget = (_targetPosition - transform.position).normalized;
                }
                else
                {
                    Vector3 targetVelocity = (_targetPreviousPosition - _targetPosition);
                    if (!Interception.CalculateInterceptionDirection(_targetPosition, targetVelocity, transform.position, _projectileInfo.Speed, out directionToTarget))
                        directionToTarget = transform.forward;
                }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(directionToTarget.normalized), _projectileInfo.SeekingSpeed * Time.fixedDeltaTime);

                _targetPreviousPosition = _targetPosition;
            }*/
            
            transform.position += transform.forward * _projectileInfo.Speed * Time.fixedDeltaTime;



            if (_remainingSqrDistance > 0.0f)
            {
                // Remaining Distance.
                //_remainingSqrDistance -= displacement.sqrMagnitude; // Not quite distance, but close enough for now.
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


        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer)
                return; // Only run on the server.
            if (!_hasStarted)
                return; // Only run if we have started.
            if (collision.transform.TryGetComponentThroughParents<NetworkObject>(out NetworkObject networkObject))
                if (networkObject.NetworkObjectId == _ownerNetworkID)
                    return; // Don't hit the entity that spawned us.


            HandleTargetHit(collision);

            _remainingHits -= 1;
            if (_remainingHits < 0)
            {
                DisposeSelf();
            }
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