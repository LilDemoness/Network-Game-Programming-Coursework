using System.Collections;
using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.StatusEffects;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ApplyBuffEffect : ActionEffect
    {
        [SerializeField] private StatusEffectDefinition _statusEffectDefinition;
        [SerializeField] private bool _cancelEffectOnEnd = false;

        [Space(5)]
        [SerializeField] private bool _scaleValueWithCharge = false;
        [SerializeField] private bool _scaleDurationWithCharge = true;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            /*float valueChange = _newValue - Action.GetUnbuffedValue(_buffableValueType);
            float buffValue = _scaleDurationWithCharge ? (Action.GetUnbuffedValue(_buffableValueType) + valueChange * chargePercentage) : _newValue;
            float buffLifetime = _scaleDurationWithCharge ? _buffLifetime * chargePercentage : _buffLifetime;*/

            owner.StatusEffectPlayer.AddStatusEffect(_statusEffectDefinition);
        }

        // We don't need to perform any cleanup as the buff is automatically removed by the character when its duration elapses.
        public override void Cleanup(ServerCharacter owner)
        {
            if (!_cancelEffectOnEnd)
                return; // Our StatusEffectPlayes wil automatically handle the clearing of this StatusEffect on the character.
            
            // Cancel the Effect.
            owner.StatusEffectPlayer.ClearAllStatusEffectsOfType(_statusEffectDefinition);
        }
    }
}