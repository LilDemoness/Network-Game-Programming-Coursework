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

        public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ulong[]> onCompleteCallback)
        {
            List<ulong> targetsList = new List<ulong>();
            foreach(Collider potentialTarget in Physics.OverlapSphere(origin, _radius))
            {
                // Obstruction Check (If required).
                if (_throughObstructions && Physics.Raycast(origin, potentialTarget.transform.position))
                    continue;

                if (potentialTarget.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject) && TargetableEntityTypes.IsValidTarget(owner, targetNetworkObject))
                {
                    targetsList.Add(targetNetworkObject.NetworkObjectId);
                }
            }

            onCompleteCallback?.Invoke(owner, targetsList.ToArray());
        }
    }
}