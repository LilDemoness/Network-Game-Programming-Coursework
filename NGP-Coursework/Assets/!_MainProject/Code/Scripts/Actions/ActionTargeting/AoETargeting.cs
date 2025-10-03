using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Targeting
{
    [System.Serializable]
    public class AoETargeting : ActionTargeting
    {
        [SerializeField] protected TargetableTypes TargetableEntityTypes = TargetableTypes.AllOthers;
        [Space(5)]

        [SerializeField] private float _radius;
        [SerializeField] private bool _throughObstructions;

        public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ActionHitInfo[]> onCompleteCallback)
        {
            List<ActionHitInfo> hitInfoList = new List<ActionHitInfo>();
            foreach(Collider potentialTarget in Physics.OverlapSphere(origin, _radius))
            {
                // Obstruction Check (If required).
                if (_throughObstructions && Physics.Raycast(origin, potentialTarget.transform.position))
                    continue;

                if (potentialTarget.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject) && TargetableEntityTypes.IsValidTarget(owner, targetNetworkObject))
                {
                    // Valid target.
                    hitInfoList.Add(new ActionHitInfo(targetNetworkObject.transform));
                }
            }

            onCompleteCallback?.Invoke(owner, hitInfoList.ToArray());
        }


        public override bool CanTriggerOnClient() => false;
    }
}