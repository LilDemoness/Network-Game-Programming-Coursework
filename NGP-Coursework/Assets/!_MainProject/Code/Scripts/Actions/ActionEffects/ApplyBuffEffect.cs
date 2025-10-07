using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ApplyBuffEffect : ActionEffect
    {
        [SerializeField] private Action.BuffableValue _buffableValueType;
        [SerializeField] private float _newValue;
        [SerializeField] private float _buffLifetime;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo)
        {
            Debug.Log($"Applying Buff {_buffableValueType} (Value: {_newValue}) to '{hitInfo.Target.name}' for {_buffLifetime} seconds");
        }

        // We don't need to perform any cleanup as the buff is automatically removed by the character when its duration elapses.
        public override void Cleanup() { }
    }
}