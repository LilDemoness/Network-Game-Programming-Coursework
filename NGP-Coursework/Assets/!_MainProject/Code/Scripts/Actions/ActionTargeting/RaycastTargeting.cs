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


        public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ulong[]> onCompleteCallback)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hitInfo, _maxRange, _validLayers))
                return; // Nothing was hit.

            if (!hitInfo.transform.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject))
                return; // Not a NetworkObject.

            if (!TargetableEntityTypes.IsValidTarget(owner, targetNetworkObject))
                return; // Not a valid target.
            
            // We hit a valid target.
            onCompleteCallback?.Invoke(owner, new ulong[1] { targetNetworkObject.NetworkObjectId });
        }
    }
}