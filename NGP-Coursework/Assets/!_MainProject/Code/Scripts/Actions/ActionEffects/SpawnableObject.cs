using UnityEngine;
using Unity.Netcode;

namespace Gameplay.Actions.Effects
{
    public class SpawnableObject : NetworkBehaviour
    {
        private Transform _attachedTransform;
        private Vector3 _localPosition;
        private Vector3 _localUp;
        private Vector3 _localForward;

        public void AttachToTransform(Transform parent)
        {
            this._attachedTransform = parent;

            this._localPosition = parent.InverseTransformPoint(transform.position);
            this._localUp = parent.InverseTransformDirection(transform.up);
            this._localForward = parent.InverseTransformDirection(transform.forward);
        }
        private void LateUpdate()
        {
            if (_attachedTransform == null)
                return;

            transform.position = _attachedTransform.TransformPoint(_localPosition);
            transform.rotation = Quaternion.LookRotation(_attachedTransform.TransformDirection(_localForward), _attachedTransform.TransformDirection(_localUp));
        }
    }
}