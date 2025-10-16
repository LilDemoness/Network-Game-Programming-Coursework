using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class RemoveBuffEffect : ActionEffect
    {
        [SerializeField] private Action.BuffableValue[] _removedBuffTypes;

        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            Debug.Log($"Removing Buffs from {hitInfo.Target.name}: {_removedBuffTypes.ToString()}");
        }

        // We don't need to perform any cleanup as this effect is instantaneous.
        public override void Cleanup() { }
    }
}