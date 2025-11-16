using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    public class SlotGFXSection : MonoBehaviour
    {
        [field: SerializeField] public SlottableData SlottableData { get; protected set; }


        [SerializeField] private Transform _abilityOrigin;
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


        public bool Toggle(SlottableData activeData)
        {
            bool newActive = activeData != null && activeData.Equals(SlottableData);
            this.gameObject.SetActive(newActive);
            return newActive;
        }


        public ulong GetAbilitySourceObjectId() => _parentNetworkObject.NetworkObjectId;
        public Transform GetAbilityOriginTransform() => _abilityOrigin;
        //public Vector3 GetAbilityLocalOffset() => _parentNetworkObject.transform.InverseTransformPoint(_abilityOrigin.position);
        //public Vector3 GetAbilityLocalDirection() => _parentNetworkObject.transform.InverseTransformDirection(_abilityOrigin.forward);


        public Vector3 GetAbilityWorldOrigin() => _abilityOrigin.position;
        public Vector3 GetAbilityWorldDirection() => _abilityOrigin.forward;


#if UNITY_EDITOR
        private bool Editor_IsThisOrChildSelected()
        {
            Transform selectedTransform = UnityEditor.Selection.activeTransform;
            while (selectedTransform != null)
            {
                if (selectedTransform == this.transform)
                    return true;

                selectedTransform = selectedTransform.parent;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (_abilityOrigin == null)
                return;
            if (!Editor_IsThisOrChildSelected())
                return;

            Gizmos.color = Color.red;

            //if (_parentNetworkObject == null)
            //{
                Gizmos.DrawSphere(transform.TransformPoint(_abilityOrigin.localPosition), 0.05f);
                Gizmos.DrawRay(transform.TransformPoint(_abilityOrigin.localPosition), transform.TransformDirection(_abilityOrigin.forward));
            /*}
            else
            {
                Gizmos.DrawSphere(GetAbilityLocalOffset(), 0.05f);
                Gizmos.DrawRay(GetAbilityLocalOffset(), GetAbilityLocalDirection());
            }*/
        }
#endif
    }
}