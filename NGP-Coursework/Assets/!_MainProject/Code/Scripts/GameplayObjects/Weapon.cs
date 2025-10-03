using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Netcode;

namespace Gameplay.GameplayObjects
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData m_weaponData;
        public WeaponData WeaponData => m_weaponData;


        [SerializeField] private Vector3 _firingPosition = Vector3.zero;
        [SerializeField] private Vector3 _firingDirection = Vector3.forward;
        private NetworkObject _parentNetworkObject;
        private Matrix4x4 conversionMatrix;


        private void Awake()
        {
            _parentNetworkObject = GetComponentInParent<NetworkObject>();
            if (_parentNetworkObject == null)
            {
                //Debug.LogError("Weapon failed to find parent NetworkObject");
                this.enabled = false;
                return;
            }

            conversionMatrix = _parentNetworkObject.transform.worldToLocalMatrix * transform.localToWorldMatrix;
        }


        public ulong GetAttackOriginTransformID() => _parentNetworkObject.NetworkObjectId;
        public Vector3 GetAttackLocalOffset() => conversionMatrix.MultiplyPoint(_firingPosition);
        public Vector3 GetAttackLocalDirection() => conversionMatrix.MultiplyPoint(_firingDirection).normalized;


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(_firingPosition), 0.05f);
            Gizmos.DrawRay(transform.TransformPoint(_firingPosition), transform.TransformDirection(_firingDirection));
        }
    }
}