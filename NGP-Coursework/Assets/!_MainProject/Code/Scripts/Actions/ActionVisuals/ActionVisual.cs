using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Visuals
{
    /// <summary>
    ///     Base class for all purely visual elements created by an action.
    /// </summary>
    /// <remarks> Should only be triggered Client-side.</remarks>
    [System.Serializable]
    public abstract class ActionVisual
    {
        [System.Flags]
        private enum TriggerTimes
        {
            OnStart = 1 << 0,           // Triggered when the Action Starts or (Special Case) is Anticipated.
            OnStartCharging = 1 << 1,
            OnUpdate = 1 << 2,          // Triggered when the Action Updates or (Special Case) has an Anticipated Update.
            OnEnd = 1 << 3,
            OnCancel = 1 << 4,

            All = ~0
        }

        [Header("Trigger Times")]
        [SerializeField] private TriggerTimes _triggerTimes;


        public void OnClientStart(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)     { if (_triggerTimes.HasFlag(TriggerTimes.OnStart))  { Trigger(clientCharacter, origin, direction); } }
        public void OnClientStartCharging(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction) { if (_triggerTimes.HasFlag(TriggerTimes.OnStartCharging))  { Trigger(clientCharacter, origin, direction); } }
        public void OnClientUpdate(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)    { if (_triggerTimes.HasFlag(TriggerTimes.OnUpdate)) { Trigger(clientCharacter, origin, direction); } }
        public void OnClientEnd(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)       { if (_triggerTimes.HasFlag(TriggerTimes.OnEnd))    { Trigger(clientCharacter, origin, direction); } }
        public void OnClientCancel(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)    { if (_triggerTimes.HasFlag(TriggerTimes.OnCancel)) { Trigger(clientCharacter, origin, direction); } }

        /// <summary>
        ///     Trigger the ActionVisual with the passed origin parameters.
        /// </summary>
        /// <param name="clientCharacter"> The triggering character.</param>
        /// <param name="origin"> The origin of the visual.</param>
        /// <param name="direction"> The forwards direction of the visual.</param>
        protected abstract void Trigger(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction);
    }
}