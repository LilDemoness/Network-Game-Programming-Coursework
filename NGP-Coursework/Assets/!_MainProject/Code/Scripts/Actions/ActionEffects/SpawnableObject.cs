using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    public class SpawnableObject : NetworkBehaviour
    {
        private ServerCharacter _owner;

        // Attaching to Objects.
        private Transform _attachedTransform;
        private bool _hasAttachedTransform;
        private Vector3 _localPosition;
        private Vector3 _localUp;
        private Vector3 _localForward;


        // Lifetime.
        private Coroutine _handleLifetimeCoroutine;


        public event System.Action<ServerCharacter, SpawnableObject> OnShouldReturnToPool;
        public event System.Action<SpawnableObject> OnReturnedToPool;


        public void Setup(ServerCharacter owner, float lifetime = 0.0f)
        {
            this._owner = owner;


            if (_handleLifetimeCoroutine != null)
                StopCoroutine(_handleLifetimeCoroutine);
            if (lifetime > 0.0f)
            {
                _handleLifetimeCoroutine = StartCoroutine(HandleLifetime(lifetime));
            }
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
        }


        /// <summary>
        ///     Attach this spawnable object to a transform.
        ///     Similar to parenting them, but done this way to prevent NetworkObject parenting issues.
        /// </summary>
        public void AttachToTransform(Transform parent)
        {
            this._attachedTransform = parent;
            this._hasAttachedTransform = true;
            if (parent.TryGetComponentThroughParents<SpawnableObject>(out SpawnableObject parentSpawnableObject))
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
            

            this._localPosition = parent.InverseTransformPoint(transform.position);
            this._localUp = parent.InverseTransformDirection(transform.up);
            this._localForward = parent.InverseTransformDirection(transform.forward);
        }

        private void ParentSpawnableObject_OnReturnedToPool(SpawnableObject parentInstance)
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