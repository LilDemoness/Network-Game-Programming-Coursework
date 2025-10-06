using Gameplay.Actions.Targeting;
using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Visuals
{
    public abstract class ActionClientVisual : ScriptableObject
    {
        public virtual void Apply(ClientCharacter client, Action actionReference, ref ActionHitInfo[] hitInfoArray)
        {
            for(int i = 0; i < hitInfoArray.Length; ++i)
            {
                ApplyToTarget(client, actionReference, ref hitInfoArray[i]);
            }
        }
        protected abstract void ApplyToTarget(ClientCharacter client, Action actionReference, ref ActionHitInfo hitInfo);
    }
}


[System.Flags]
public enum ActionFXCancelCondition
{
    None = 0,
    ActionEnded = 1 << 0,
    ActionCancelled = 1 << 1,
}