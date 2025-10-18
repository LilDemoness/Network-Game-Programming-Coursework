using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Netcode;

namespace Gameplay.GameplayObjects
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private SlottableData m_weaponData;
        public SlottableData WeaponData => m_weaponData;


        [SerializeField] private Transform _firingOrigin;
        private NetworkObject _parentNetworkObject;


        private void Awake()
        {
            _parentNetworkObject = GetComponentInParent<NetworkObject>();
            if (_parentNetworkObject == null)
            {
                //Debug.LogError("Weapon failed to find parent NetworkObject");
                this.enabled = false;
                return;
            }
        }


        public ulong GetAttackOriginTransformID() => _parentNetworkObject.NetworkObjectId;
        public Vector3 GetAttackLocalOffset() => _parentNetworkObject.transform.InverseTransformPoint(_firingOrigin.position);
        public Vector3 GetAttackLocalDirection() => _parentNetworkObject.transform.InverseTransformDirection(_firingOrigin.forward);


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

            if (_parentNetworkObject == null)
            {
                Gizmos.DrawSphere(transform.TransformPoint(_firingOrigin.localPosition), 0.05f);
                Gizmos.DrawRay(transform.TransformPoint(_firingOrigin.localPosition), transform.TransformDirection(_firingOrigin.forward));
            }
            else
            {
                Gizmos.DrawSphere(GetAttackLocalOffset(), 0.05f);
                Gizmos.DrawRay(GetAttackLocalOffset(), GetAttackLocalDirection());
            }
        }
        #endif
    }
}