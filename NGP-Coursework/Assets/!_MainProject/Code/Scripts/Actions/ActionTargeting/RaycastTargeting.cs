using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Targeting
{
    [System.Serializable]
    public class RaycastTargeting : ActionTargeting
    {
        [SerializeField] protected TargetableTypes TargetableEntityTypes = TargetableTypes.AllOthers;
        [Space(5)]

        [SerializeField] private float _maxRange;
        [SerializeField] private LayerMask _validLayers;


        public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ActionHitInfo[]> onCompleteCallback)
        {
            DebugTester.Instance.DrawRay(origin, direction, Color.yellow, 0.25f);
            //Debug.DrawRay(origin, direction, Color.red, 0.25f);
            if (!Physics.Raycast(origin, direction, out RaycastHit hitInfo, _maxRange, _validLayers))
                return; // Nothing was hit.

            if (!hitInfo.transform.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject))
                return; // Not a NetworkObject.

            if (!TargetableEntityTypes.IsValidTarget(owner, targetNetworkObject))
                return; // Not a valid target.
            
            // We hit a valid target.
            onCompleteCallback?.Invoke(owner, new ActionHitInfo[1] { new ActionHitInfo(targetNetworkObject.transform, hitInfo.point, hitInfo.normal) });
        }


        public override bool CanTriggerOnClient() => true;
        public override void TriggerOnClient(ClientCharacter clientCharacter, Vector3 origin, Vector3 direction)
        {
            base.TriggerOnClient(clientCharacter, origin, direction);
            if (!Physics.Raycast(origin, direction, out RaycastHit hitInfo, _maxRange, _validLayers))
                return;

            DebugTester.Instance.DrawRay(hitInfo.point, hitInfo.normal, Color.yellow, 0.25f);
            //Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.yellow, 0.25f);
        }
    }
}