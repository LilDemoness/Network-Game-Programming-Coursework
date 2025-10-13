using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Visuals
{
    public abstract class ActionVisual
    {
        [System.Flags]
        private enum TriggerTimes
        {
            OnStart = 1 << 0,
            OnUpdate = 1 << 1,
            OnEnd = 1 << 2,
            OnCancel = 1 << 3,

            All = 1
        }

        [Header("Trigger Times")]
        [SerializeField] private TriggerTimes _triggerTimes;


        public void OnClientStart(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)     { if (_triggerTimes.HasFlag(TriggerTimes.OnStart))  { Trigger(clientCharacter, origin, direction); } }
        public void OnClientUpdate(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)    { if (_triggerTimes.HasFlag(TriggerTimes.OnUpdate)) { Trigger(clientCharacter, origin, direction); } }
        public void OnClientEnd(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)       { if (_triggerTimes.HasFlag(TriggerTimes.OnEnd))    { Trigger(clientCharacter, origin, direction); } }
        public void OnClientCancel(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)    { if (_triggerTimes.HasFlag(TriggerTimes.OnCancel)) { Trigger(clientCharacter, origin, direction); } }

        protected abstract void Trigger(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction);
    }
}