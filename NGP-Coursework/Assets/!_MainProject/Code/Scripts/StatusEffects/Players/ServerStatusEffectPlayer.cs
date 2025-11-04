using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.StatusEffects.Definitions;

namespace Gameplay.StatusEffects
{
    public class ServerStatusEffectPlayer
    {
        private ServerCharacter _serverCharacter;

        /// <summary>
        ///     All active StatusEffects on this character.
        /// </summary>
        private List<StatusEffect> _activeStatusEffects;


        public ServerStatusEffectPlayer(ServerCharacter serverCharacter)
        {
            this._serverCharacter = serverCharacter;

            _activeStatusEffects = new List<StatusEffect>();
        }


        /// <summary>
        ///     Apply a Status Effect to the Character.
        /// </summary>
        public void AddStatusEffect(StatusEffectDefinition statusEffectDefinition)
        {
            if (statusEffectDefinition.StackingType != StackingType.InParallel)
            {
                // Check for duplicate Status Effects.
                for (int i = 0; i < _activeStatusEffects.Count; ++i)
                {
                    if (_activeStatusEffects[i].Definition == statusEffectDefinition)
                    {
                        ReapplyStatusEffect(_activeStatusEffects[i]);
                        return;
                    }
                }
            }

            // Create the Status Effect.
            StatusEffect statusEffect = new StatusEffect(statusEffectDefinition);

            // Add our status effect to our list for future access.
            _activeStatusEffects.Add(statusEffect);

            // Trigger our status effect for the first time.
            statusEffect.TimeStarted = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            statusEffect.OnStart(_serverCharacter);
        }
        /// <summary>
        ///     Handles the re-application of a Status Effect that overrides active values when applied (Rather than new running in parallel with old).
        /// </summary>
        private void ReapplyStatusEffect(StatusEffect statusEffect)
        {
            switch(statusEffect.Definition.StackingType)
            {
                case StackingType.ResetDuration:
                    statusEffect.EffectElapsedTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + statusEffect.Definition.Lifetime;
                    break;
                case StackingType.AddDuration:
                    statusEffect.EffectElapsedTime += statusEffect.Definition.Lifetime;
                    break;
                case StackingType.Retrigger:
                    statusEffect.TimeStarted = NetworkManager.Singleton.ServerTime.TimeAsFloat;
                    statusEffect.OnStart(_serverCharacter);
                    break;
                case StackingType.Toggle:
                    ClearStatusEffect(statusEffect);
                    break;
                default: throw new System.NotImplementedException();
            }
        }


        /// <summary>
        ///     Update all StatusEffects on this character.
        /// </summary>
        public void OnUpdate()
        {
            // Update all existing status effects (Loop in reverse order for easier removal).
            for(int i = _activeStatusEffects.Count - 1; i >= 0; --i)
            {
                StatusEffect statusEffect = _activeStatusEffects[i];
                if (!UpdateStatusEffect(statusEffect))
                {
                    statusEffect.OnEnd(_serverCharacter);
                    _activeStatusEffects.RemoveAt(i);
                    // Return to the object pool.
                }
            }
        }
        /// <summary>
        ///     Update the passed StatusEffect.
        /// </summary>
        /// <returns> True if the StatusEffect should be removed, false if it persists.</returns>
        private bool UpdateStatusEffect(StatusEffect statusEffect)
        {
            if (statusEffect.EffectElapsedTime > 0 && statusEffect.EffectElapsedTime < NetworkManager.Singleton.ServerTime.TimeAsFloat)
                return false;   // The effect has elapsed.

            // The effect hasn't yet elapsed.
            statusEffect.OnUpdate(_serverCharacter);
            return true;
        }


        #region Status Effect Clearing

        /// <summary>
        ///     Clear all StatusEffects on this character.
        /// </summary>
        public void ClearAllStatusEffects()
        {
            for (int i = _activeStatusEffects.Count - 1; i >= 0; --i)
            {
                ClearStatusEffect(i);
            }
        }
        /// <summary>
        ///     Clear all StatusEffects of type 'Buff' on this character.
        /// </summary>
        public void ClearAllBuffs()
        {
            for (int i = _activeStatusEffects.Count - 1; i >= 0; --i)
            {
                if (_activeStatusEffects[i].Definition.Type == StatusEffectType.Buff)
                {
                    ClearStatusEffect(i);
                }
            }
        }
        /// <summary>
        ///     Clear all StatusEffects of type 'Debuff' on this character.
        /// </summary>
        public void ClearAllDebuffs()
        {
            for (int i = _activeStatusEffects.Count - 1; i >= 0; --i)
            {
                if (_activeStatusEffects[i].Definition.Type == StatusEffectType.Debuff)
                {
                    ClearStatusEffect(i);
                }
            }
        }


        /// <summary>
        ///     Clear all StatusEffects with the passed definition.
        /// </summary>
        public void ClearAllStatusEffectsOfType(StatusEffectDefinition typeToClear)
        {
            for (int i = _activeStatusEffects.Count - 1; i >= 0; --i)
            {
                if (_activeStatusEffects[i].Definition == typeToClear)
                {
                    ClearStatusEffect(i);
                }
            }
        }
        /// <summary>
        ///     Clear the most recently applied StatusEffect of the given type.
        /// </summary>
        public void ClearMostRecentEffectOfType(StatusEffectType typeToCancel) => throw new System.NotImplementedException();

        /// <summary>
        ///     Clear the passed status effect.
        /// </summary>
        private void ClearStatusEffect(StatusEffect statusEffect)
        {
            for (int i = _activeStatusEffects.Count - 1; i >= 0; --i)
            {
                if (_activeStatusEffects[i] == statusEffect)
                {
                    ClearStatusEffect(i);
                    return;
                }
            }
        }
        /// <summary>
        ///     Clear the status effect at the given index.
        /// </summary>
        private void ClearStatusEffect(int index)
        {
            // Cancel the Effect.
            _activeStatusEffects[index].OnCancel(_serverCharacter);

            // Remove from the List.
            _activeStatusEffects.RemoveAt(index);

            // Return to the pool.

        }

        #endregion
    }
}