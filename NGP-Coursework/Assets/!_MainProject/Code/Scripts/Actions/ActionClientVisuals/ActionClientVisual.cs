using Gameplay.Actions.Targeting;
using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Visuals
{
    public abstract class ActionClientVisual : ScriptableObject
    {
        public virtual void Apply(ClientCharacter client, ref ActionHitInfo[] hitInfoArray)
        {
            for(int i = 0; i < hitInfoArray.Length; ++i)
            {
                ApplyToTarget(client, ref hitInfoArray[i]);
            }
        }
        protected abstract void ApplyToTarget(ClientCharacter client, ref ActionHitInfo hitInfo);
    }
}