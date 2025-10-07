using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Actions
{
    /// <summary>
    ///     A base class for all typical action definitions.
    ///     Some types of Action don't inherit from this (E.g. CancelAction).
    /// </summary>
    public abstract class DefaultAction : Action
    {
        //protected abstract bool ShouldNotifyClient { get; }


        [Header("Timings")]
        [Tooltip("The time in seconds between starting this action and its effects triggering")]
        public float ExecutionDelay;
        public float ActionCooldown;
        public BlockingModeType BlockingMode = BlockingModeType.OnlyDuringExecutionTime;


        [Header("Charging Settings")]
        public bool CanCharge;
        public float MaxChargeTime;
        public bool ExecuteIfNotAtFullCharge;


        [Header("Active Timings")]
        public float MaxActiveDuration;
        public float RetriggerDelay;
        [System.NonSerialized] protected float NextUpdateTime;


        [Header("Animation Triggers")]


        [Header("Interruption")]
        public bool IsInterruptable;


        public override void Reset()
        {
            base.Reset();
            this.NextUpdateTime = 0;
        }


        public override bool HasCooldown => ActionCooldown > 0.0f;
        public override bool HasCooldownCompleted(float lastActivatedTime) => (NetworkManager.Singleton.ServerTime.TimeAsFloat - lastActivatedTime) >= ActionCooldown;
        public override bool HasExpired
        {
            get
            {
                bool isExpirable = MaxActiveDuration > 0.0f;  // Non-positive values indicate that the duration is infinite.
                float timeElapsed = NetworkManager.Singleton.ServerTime.TimeAsFloat - TimeStarted;
                return isExpirable && timeElapsed >= MaxActiveDuration;
            }
        }

        public override bool ShouldBecomeNonBlocking() => BlockingMode == BlockingModeType.OnlyDuringExecutionTime ? TimeRunning >= ExecutionDelay : false;

        public override bool CanBeInterruptedBy(ActionID otherActionID)
        {
            return base.CanBeInterruptedBy(otherActionID);
        }


        /// <summary>
        ///     Initialise our Data's required Parameters (If they aren't already set-up).
        /// </summary>
        private void InitialiseDataParametersIfEmpty(ServerCharacter owner)
        {
            if (Data.OriginTransformID != 0)
            {
                if (Data.Direction == Vector3.zero)
                    Data.Direction = Vector3.forward;
            }
            else
            {
                if (Data.Direction == Vector3.zero)
                    Data.Direction = owner.transform.forward;
                if (Data.Position == Vector3.zero)
                    Data.Position = owner.transform.position;
            }
        }



        public override bool OnStartClient(ClientCharacter clientCharacter, float serverTimeStarted)
        {
            base.OnStartClient(clientCharacter, serverTimeStarted);
            NextUpdateTime = TimeStarted + ExecutionDelay;

            return true;
        }
    }
}