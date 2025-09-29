using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Targeting;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public abstract class ActionEffect
    {
        public TargetableTypes AffectedTypes = TargetableTypes.All;

        public virtual void Apply(ServerCharacter owner, ulong[] targetIDs)
        {
            for (int i = 0; i < targetIDs.Length; ++i)
            {
                NetworkObject targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetIDs[i]];
                if (IsValidType(targetObject))
                {
                    ApplyToTarget(owner, targetObject);
                }
            }
        }
        protected abstract void ApplyToTarget(ServerCharacter owner, NetworkObject targetObject);
        protected bool IsValidType(NetworkObject targetObject)
        {
            Debug.LogWarning("Not Implemented");
            return true;
        }
    }
}