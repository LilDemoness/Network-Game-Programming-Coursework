using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    // Server Logic of a SpawnableObject.
    [RequireComponent(typeof(SpawnableObject_Client))]
    public class SpawnableObject_Server : NetworkBehaviour
    {
        private ServerCharacter _owner;
        private SpawnableObject_Client _clientScript;


        // Attachment.
        private Transform _attachedTransform;
        private bool _hasAttachedTransform;

        private Vector3 _localPosition;
        private Vector3 _localUp;
        private Vector3 _localForward;


        // Lifetime.
        private Coroutine _handleLifetimeCoroutine;


        // Special FX.
        private int _specialFXIndex;


        public event System.Action<ServerCharacter, SpawnableObject_Server> OnShouldReturnToPool;
        public event System.Action<SpawnableObject_Server> OnReturnedToPool;


        private void Awake()
        {
            _clientScript = this.GetComponent<SpawnableObject_Client>();
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }
        }
        public void Setup(ServerCharacter owner, int specialFXIndex, float lifetime = 0.0f)
        {
            this._owner = owner;
            this._specialFXIndex = specialFXIndex;

            if (_handleLifetimeCoroutine != null)
                StopCoroutine(_handleLifetimeCoroutine);
            if (lifetime > 0.0f)
            {
                _handleLifetimeCoroutine = StartCoroutine(HandleLifetime(lifetime));
            }

            this._clientScript.SpawnRpc(transform.position, transform.forward, transform.up, specialFXIndex);
        }
        public void Setup(ServerCharacter owner, NetworkObject parentObject, int specialFXIndex, float lifetime = 0.0f)
        {
            this._owner = owner;
            this._specialFXIndex = specialFXIndex;

            if (_handleLifetimeCoroutine != null)
            {
                StopCoroutine(_handleLifetimeCoroutine);
            }
            if (lifetime > 0.0f)
            {
                _handleLifetimeCoroutine = StartCoroutine(HandleLifetime(lifetime));
            }

            AttachToTransform(parentObject, specialFXIndex);
        }

        public void ReturnedToPool()
        {
            // Notify attached objects that we've been returned to the pool.
            OnReturnedToPool?.Invoke(this);

            // Reset variables & ensure that coroutines have stopped (Just in case).
            _attachedTransform = null;
            _hasAttachedTransform = false;

            if (_handleLifetimeCoroutine != null)
                StopCoroutine(_handleLifetimeCoroutine);


            // Notify client-only visuals script.
            _clientScript.ReturnedToPoolRpc();
        }


        /// <summary>
        ///     Attach this spawnable object to a transform and spawn it.
        ///     Similar to parenting them, but done this way to prevent NetworkObject parenting issues.
        /// </summary>
        private void AttachToTransform(NetworkObject parentObject, int specialFXIndex)
        {
            this._attachedTransform = parentObject.transform;
            this._hasAttachedTransform = true;
            if (_attachedTransform.TryGetComponentThroughParents<SpawnableObject_Server>(out SpawnableObject_Server parentSpawnableObject))
            {
                if (!parentSpawnableObject.gameObject.activeSelf)
                {
                    // We're trying to parent to a SpawnableObject that is disabled (Meaning that the spawning of this object caused it to despawn).
                    // Despawn.
                    this.OnShouldReturnToPool?.Invoke(_owner, this);
                    return;
                }

                // Despawn when our parent is returned to the pool (We would lose our parent reference, but due to it being pooled this is how we are checking).
                parentSpawnableObject.OnReturnedToPool += ParentSpawnableObject_OnReturnedToPool;
            }
            

            this._localPosition = _attachedTransform.InverseTransformPoint(transform.position);
            this._localForward = _attachedTransform.InverseTransformDirection(transform.forward);
            this._localUp = _attachedTransform.InverseTransformDirection(transform.up);


            // Notify client-only visuals script.
            this._clientScript.SpawnRpc(parentObject.NetworkObjectId, _localPosition, _localForward, _localUp, specialFXIndex);
        }


        private void ParentSpawnableObject_OnReturnedToPool(SpawnableObject_Server parentInstance)
        {
            parentInstance.OnReturnedToPool -= ParentSpawnableObject_OnReturnedToPool;
            this.OnShouldReturnToPool?.Invoke(_owner, this);
        }

        private void LateUpdate()
        {
            // Check that our attached transform is still valid.
            if (_attachedTransform == null)
            {
                if (_hasAttachedTransform)
                {
                    // We've just lost our attached transform. Notify for returning to the pool only once.
                    OnShouldReturnToPool?.Invoke(_owner, this);
                    _hasAttachedTransform = false;
                }
                return;
            }

            transform.position = _attachedTransform.TransformPoint(_localPosition);
            transform.rotation = Quaternion.LookRotation(_attachedTransform.TransformDirection(_localForward), _attachedTransform.TransformDirection(_localUp));
        }
        /// <summary>
        ///     Return ourselves to the pool once the specified lifetime has elapsed.
        /// </summary>
        private IEnumerator HandleLifetime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            OnShouldReturnToPool?.Invoke(_owner, this);
        }
    }
}