using UnityEngine;
using Unity.Netcode;

namespace Gameplay.Actions.Effects
{
    // Visuals of a SpawnableObject.
    public class SpawnableObject_Client : NetworkBehaviour
    {
        private Transform _attachedTransform;
        private Vector3 _localPosition;
        private Vector3 _localUp;
        private Vector3 _localForward;


        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                this.enabled = false;
                return;
            }
        }


        [Rpc(SendTo.NotServer)]
        public void SpawnRpc()
        {
            this.gameObject.SetActive(true);
        }

        [Rpc(SendTo.NotServer)]
        public void AttachToTransformRpc(ulong attachmentObjectID, Vector3 localPos, Vector3 localForward, Vector3 localUp)
        {
            this._attachedTransform = NetworkManager.SpawnManager.SpawnedObjects[attachmentObjectID].transform;

            this._localPosition = localPos;
            this._localForward = localForward;
            this._localUp = localUp;
        }
        [Rpc(SendTo.NotServer)]
        public void ReturnedToPoolRpc()
        {
            // Reset attachment variables.
            _attachedTransform = null;
            this.gameObject.SetActive(false);
        }


        private void LateUpdate()
        {
            if (_attachedTransform == null)
            {
                return;
            }

            transform.position = _attachedTransform.TransformPoint(_localPosition);
            transform.rotation = Quaternion.LookRotation(_attachedTransform.TransformDirection(_localForward), _attachedTransform.TransformDirection(_localUp));
        }
    }
}