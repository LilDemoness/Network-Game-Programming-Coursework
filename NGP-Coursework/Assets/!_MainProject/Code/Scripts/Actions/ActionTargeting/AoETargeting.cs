using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Targeting
{
    [System.Serializable]
    public class AoETargeting : ActionTargeting
    {
        [SerializeField] private float _radius;
        [SerializeField] private bool _throughObstructions;

        public override ulong[] GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction)
        {
            List<ulong> targetsList = new List<ulong>();
            foreach(Collider potentialTarget in Physics.OverlapSphere(origin, _radius))
            {
                // Obstruction Check (If required).
                if (_throughObstructions && Physics.Raycast(origin, potentialTarget.transform.position))
                    continue;

                if (potentialTarget.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject) && IsValidType(targetNetworkObject))
                {
                    targetsList.Add(targetNetworkObject.NetworkObjectId);
                }
            }

            return targetsList.ToArray();
        }
    }
}