using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.StatusEffects.Definitions
{
    public abstract class StatusEffectDefinition : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }


        [Tooltip("How long this Status Effect lasts after being created.")]
        [field: SerializeField] public float Lifetime { get; private set; }
        
        [Tooltip("How instances of this Status Effect stack with themselves.")]
        [field: SerializeField] public StackingType StackingType { get; private set; } = StackingType.ResetDuration;
        
        [Tooltip("The category/type of this status effect (Buff, Debuff, etc).")]
        [field: SerializeField] public StatusEffectType Type { get; private set; }


        [Tooltip("The time in seconds between activations of this Status Effect.")]
        [field: SerializeField, Min(0.0f)] public float RetriggerDelay = 0.0f;


        /// <summary>
        ///     Called when the StatusEffect is first activated.
        /// </summary>
        /// <remarks> Is also triggered when the status Effect is reapplied if <see cref="StackingType"/> is <see cref="StackingType.Retrigger"/>.</remarks>
        public virtual void OnStart(ServerCharacter serverCharacter) { }
        /// <summary>
        ///     Called when the StatusEffect updates.
        /// </summary>
        /// <remarks> Triggers once if <see cref="RetriggerDelay"/> is <=0, otherwise every <see cref="RetriggerDelay"/> seconds.</remarks>
        public virtual void OnTick(ServerCharacter serverCharacter) { }
        /// <summary>
        ///     Called when the StatusEffect ends naturally.
        /// </summary>
        public virtual void OnEnd(ServerCharacter serverCharacter) { }
        /// <summary>
        ///     Called when the StatusEffect ends prematurely.
        /// </summary>
        public virtual void OnCancel(ServerCharacter serverCharacter) { }


        /// <summary>
        ///     Called on the client when the StatusEffect is first activated.
        /// </summary>
        /// <remarks> Is also triggered when the status Effect is reapplied if <see cref="StackingType"/> is <see cref="StackingType.Retrigger"/>.</remarks>
        public virtual void OnStartClient(ClientCharacter clientCharacter) { }
        /// <summary>
        ///     Called on the client when the StatusEffect updates.
        /// </summary>
        /// <remarks> Triggers once if <see cref="RetriggerDelay"/> is <=0, otherwise every <see cref="RetriggerDelay"/> seconds.</remarks>
        public virtual void OnTickClient(ClientCharacter clientCharacter) { }
        /// <summary>
        ///     Called on the client when the StatusEffect ends naturally.
        /// </summary>
        public virtual void OnEndClient(ClientCharacter clientCharacter) { }
        /// <summary>
        ///     Called on the client when the StatusEffect ends prematurely.
        /// </summary>
        public virtual void OnCancelClient(ClientCharacter clientCharacter) { }
    }

    public enum StatusEffectType { Buff, Debuff, Effect }
}