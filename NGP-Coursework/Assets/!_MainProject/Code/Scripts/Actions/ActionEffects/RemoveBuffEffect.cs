using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.StatusEffects.Definitions;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Removes buffs from the hit characters.
    /// </summary>
    [System.Serializable]
    public class RemoveBuffEffect : ActionEffect
    {
        [Header("Buff Removal Settings")]
        [SerializeField] private bool _removeAllBuffs = false;
        [SerializeField] private bool _removeAllDebuffs = false;

        [Space(10)]
        [SerializeField] private bool _removeByDefinition = false;
        [System.Serializable] private enum RemovalType { Oldest, Newest, All }
        [SerializeField] private RemovalType _removalType = RemovalType.Oldest;
        [SerializeField] private StatusEffectDefinition[] _statusEffectDefinitionsToRemove;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            if (hitInfo.Target.TryGetComponent<ServerCharacter>(out ServerCharacter serverCharacter))
            {
                // Perform removal by type (All Buffs, All Debuffs).
                if (_removeAllBuffs)
                    serverCharacter.StatusEffectPlayer.ClearAllBuffs();
                if (_removeAllDebuffs)
                    serverCharacter.StatusEffectPlayer.ClearAllDebuffs();

                // Perform removal by definition.
                if (_removeByDefinition)
                {
                    // Determine the desired removal effect by our '_removalType' value.
                    //  Note: We have the loop within the case statement so that we only perform the check once, at the cost of duplicating the loop definition in code.
                    switch (_removalType)
                    {
                        case RemovalType.Oldest:
                            for (int i = 0; i < _statusEffectDefinitionsToRemove.Length; i++)
                                serverCharacter.StatusEffectPlayer.ClearOldestEffectOfType(_statusEffectDefinitionsToRemove[i]);
                            break;
                        case RemovalType.Newest:
                            for (int i = 0; i < _statusEffectDefinitionsToRemove.Length; i++)
                                serverCharacter.StatusEffectPlayer.ClearNewestEffectOfType(_statusEffectDefinitionsToRemove[i]);
                            break;
                        case RemovalType.All:
                            for (int i = 0; i < _statusEffectDefinitionsToRemove.Length; i++)
                                serverCharacter.StatusEffectPlayer.ClearAllEffectsOfType(_statusEffectDefinitionsToRemove[i]);
                            break;
                    }
                }
            }
        }
    }
}