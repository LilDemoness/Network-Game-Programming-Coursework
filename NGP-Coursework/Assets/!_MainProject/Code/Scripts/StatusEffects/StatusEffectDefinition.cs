using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.StatusEffects
{
    public abstract class StatusEffectDefinition : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        [field: SerializeField] public float Lifetime { get; private set; }
        [field: SerializeField] public StackingType StackingType { get; private set; } = StackingType.ResetDuration;
        [field: SerializeField] public StatusEffectType Type { get; private set; }


        [field: SerializeField, Min(0.0f)] public float RetriggerDelay = 0.0f;
        [field: SerializeField] public float HeatGeneratedPerSecond = 0.0f;


        public virtual void OnStart(ServerCharacter serverCharacter) { }
        public virtual void OnTick(ServerCharacter serverCharacter) { }
        public virtual void OnEnd(ServerCharacter serverCharacter) { }
        public virtual void OnCancel(ServerCharacter serverCharacter) { }


        public virtual void OnStartClient(ClientCharacter clientCharacter) { }
        public virtual void OnTickClient(ClientCharacter clientCharacter) { }
        public virtual void OnEndClient(ClientCharacter clientCharacter) { }
        public virtual void OnCancelClient(ClientCharacter clientCharacter) { }
    }

    public enum StatusEffectType { Buff, Debuff }
}