using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.StatusEffects.Definitions;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Applies a <see cref="StatusEffectDefinition"/> to hit <see cref="ServerCharacter"/> instances.
    /// </summary>
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
#if UNITY_EDITOR
            if (chargePercentage > 0.0f)
                Debug.LogWarning("No Charge Scaling Logic");
#endif

            if (hitInfo.Target.TryGetComponent<ServerCharacter>(out ServerCharacter serverCharacter))
            {
                serverCharacter.StatusEffectPlayer.AddStatusEffect(_statusEffectDefinition);
            }
        }

        // Unless we wish to cancel our status effect on end,
        //  we don't need to perform any cleanup as the buff is automatically removed by the character when its duration elapses.
        public override void Cleanup(ServerCharacter owner)
        {
            if (!_cancelEffectOnEnd)
                return; // Our StatusEffectPlayes will automatically handle the clearing of this StatusEffect on the character.
            
            // Cancel the Effect.
            owner.StatusEffectPlayer.ClearAllEffectsOfType(_statusEffectDefinition);
        }
    }
}