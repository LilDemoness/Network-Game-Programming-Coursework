using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Netcode;

namespace Gameplay.GameplayObjects
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData m_weaponData;
        public WeaponData WeaponData => m_weaponData;


        [SerializeField] private Transform _firingOrigin;
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
        public Vector3 GetAttackLocalOffset() => conversionMatrix.MultiplyPoint(_firingOrigin.localPosition);
        public Vector3 GetAttackLocalDirection() => conversionMatrix.MultiplyPoint(_firingOrigin.localRotation * Vector3.forward).normalized;


        #if UNITY_EDITOR
        private bool Editor_IsThisOrChildSelected()
        {
            Transform selectedTransform = UnityEditor.Selection.activeTransform;
            while(selectedTransform != null)
            {
                if (selectedTransform == this.transform)
                    return true;

                selectedTransform = selectedTransform.parent;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (_firingOrigin == null)
                return;
            if (!Editor_IsThisOrChildSelected())
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(_firingOrigin.localPosition), 0.05f);
            Gizmos.DrawRay(transform.TransformPoint(_firingOrigin.localPosition), transform.TransformDirection(_firingOrigin.localRotation * Vector3.forward));
        }
        #endif
    }
}