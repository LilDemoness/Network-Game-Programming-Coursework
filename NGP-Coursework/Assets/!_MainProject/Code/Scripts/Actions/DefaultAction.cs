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
        [System.NonSerialized]
        protected float NextUpdateTime;


        [Header("Retriggering")]
        [SerializeField] private ActionTriggerType _triggerType;
        [SerializeField] private bool _cancelOnLastTrigger = true;
        [System.NonSerialized]
        private bool _hasPerformedLastTrigger;

        [Space(5)]
        [SerializeField] private float _retriggerDelay;
        
        [Space(5)]
        [SerializeField] private int _bursts;
        [System.NonSerialized]
        private int _burstsRemaining;
        [SerializeField] private float _burstDelay;



        [Header("Animation Triggers")]


        [Header("Interruption")]
        public bool IsInterruptable;


#if UNITY_EDITOR
        
        public void OnValidate()
        {
            if (_triggerType != ActionTriggerType.Single)
            {
                if (_triggerType == ActionTriggerType.Repeated || _triggerType == ActionTriggerType.RepeatedBurst)
                {
                    // Check for issues with Repeated settings.
                    if (_retriggerDelay <= 0.0f)
                        Debug.LogError($"Error: You have a Repeating RetriggerType, but '{nameof(_retriggerDelay)}' is non-positive.");
                }

                if (_triggerType == ActionTriggerType.Burst || _triggerType == ActionTriggerType.RepeatedBurst)
                {
                    // Check for issues with Burst settings.
                    if (_bursts <= 0)
                        Debug.LogError($"Error: You have a Burst RetriggerType, but '{nameof(_bursts)}' is non-positive");
                    if (_burstDelay <= 0.0f)
                        Debug.LogError($"Error: You have a Burst RetriggerType, but '{nameof(_burstDelay)}' is non-positive");
                }
            }
        }

#endif
        public override void Reset()
        {
            base.Reset();
            this.NextUpdateTime = 0;
            this._burstsRemaining = 0;
            this._hasPerformedLastTrigger = false;
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

        public override bool ShouldBecomeNonBlocking()
            => BlockingMode switch
            {
                BlockingModeType.OnlyDuringExecutionTime => TimeRunning >= ExecutionDelay,
                BlockingModeType.Never => true,
                _ => false,
            };
        

        public override bool CanBeInterruptedBy(ActionID otherActionID)
        {
            Debug.LogWarning("Not Implemented");
            return base.CanBeInterruptedBy(otherActionID);
        }


        public override bool OnStart(ServerCharacter owner)
        {
            NextUpdateTime = TimeStarted + ExecutionDelay;
            _burstsRemaining = _bursts;
            InitialiseDataParametersIfEmpty(owner);
            return HandleStart(owner);
        }

        public override bool OnUpdate(ServerCharacter owner)
        {
            if (_hasPerformedLastTrigger)
                return !_cancelOnLastTrigger;

            if (NetworkManager.Singleton.ServerTime.TimeAsFloat < NextUpdateTime)
                return ActionConclusion.Continue;

            if (HandleUpdate(owner) == false)
                return ActionConclusion.Stop;


            _hasPerformedLastTrigger = !CalculateNextUpdateTime();
            
            if (_cancelOnLastTrigger && _hasPerformedLastTrigger)
                return ActionConclusion.Stop;
            else
                return ActionConclusion.Continue;
        }
        private bool CalculateNextUpdateTime()
        {
            switch (_triggerType)
            {
                case ActionTriggerType.Burst:
                    --_burstsRemaining;

                    if (_burstsRemaining <= 0)
                        return ActionConclusion.Stop;
                    else
                        NextUpdateTime += _burstDelay;
                    break;
                case ActionTriggerType.RepeatedBurst:
                    --_burstsRemaining;

                    if (_burstsRemaining > 0)
                    {
                        NextUpdateTime += _burstDelay;
                    }
                    else
                    {
                        _burstsRemaining = _bursts;
                        NextUpdateTime += _retriggerDelay;
                    }
                    break;
                case ActionTriggerType.Repeated:
                    NextUpdateTime += _retriggerDelay;
                    break;
                default: return ActionConclusion.Stop;
            }

            return ActionConclusion.Continue;
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