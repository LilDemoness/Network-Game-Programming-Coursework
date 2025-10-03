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

        public virtual void Apply(ServerCharacter owner, ref ActionHitInfo[] hitInfoArray)
        {
            bool hasTargetedSelf = false;
            for (int i = 0; i < hitInfoArray.Length; ++i)
            {
                if (!hitInfoArray[i].HitTransform.TryGetComponent<NetworkObject>(out NetworkObject targetObject))
                    continue;

                if (!hasTargetedSelf && AutoTargetSelf && hitInfoArray[i].HitTransform != owner.transform)
                    hasTargetedSelf = true;

                if (AffectedTypes.IsValidTarget(owner, targetObject))
                {
                    ApplyToTarget(owner, ref hitInfoArray[i]);
                }
            }

            if (!hasTargetedSelf && AutoTargetSelf && AffectedTypes.HasFlag(TargetableTypes.Self))
            {
                ActionHitInfo ownerHitInfo = new ActionHitInfo(owner.transform);
                ApplyToTarget(owner, ref ownerHitInfo);
            }
        }
        protected abstract void ApplyToTarget(ServerCharacter owner, ref ActionHitInfo hitInfo);
    }
}