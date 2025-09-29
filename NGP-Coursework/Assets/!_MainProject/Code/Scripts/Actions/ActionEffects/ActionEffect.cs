using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Targeting;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public abstract class ActionEffect
    {
        public TargetableTypes AffectedTypes = TargetableTypes.AllOthers;
        public bool AutoTargetSelf = false;

        public virtual void Apply(ServerCharacter owner, ulong[] targetIDs)
        {
            bool hasTargetedSelf = false;
            for (int i = 0; i < targetIDs.Length; ++i)
            {
                NetworkObject targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetIDs[i]];
                if (!hasTargetedSelf && AutoTargetSelf && targetIDs[i] == owner.NetworkObjectId)
                    hasTargetedSelf = true;

                if (AffectedTypes.IsValidTarget(owner, targetObject))
                {
                    ApplyToTarget(owner, targetObject);
                }
            }

            if (!hasTargetedSelf && AutoTargetSelf && AffectedTypes.HasFlag(TargetableTypes.Self))
                ApplyToTarget(owner, owner.NetworkObject);
        }
        protected abstract void ApplyToTarget(ServerCharacter owner, NetworkObject targetObject);
    }
}