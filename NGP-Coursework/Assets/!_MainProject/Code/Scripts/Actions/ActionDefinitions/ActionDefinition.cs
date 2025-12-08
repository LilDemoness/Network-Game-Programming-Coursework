using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Effects;
using Gameplay.Actions.Visuals;
using UI.Crosshairs;

namespace Gameplay.Actions.Definitions
{
    /// <summary>
    ///     The shared definition for an Action.<br/>
    ///     Contains the shared data and the functions/logic for the triggering of an action.
    /// </summary>
    public abstract class ActionDefinition : ScriptableObject
    {
        /// <summary>
        ///     An index into the GameDataSource array of action prototypes.
        ///     Set at runtime by GameDataSource class.
        ///     If the action is not itself a prototype, it will contain the ActionID of the prototype reference.
        ///     <br/>This field is used to identify actions in a way that can be sent over the network.
        /// </summary>
        /// <remarks> Non-serialized, so it doesn't get saved between editor sessions.</remarks>
        [field: System.NonSerialized] public ActionID ActionID { get; set; }


        #region Action Settings

        [field: Header("Action Settings")]
        [Tooltip("Does this count as a hostile Action? (Should it: Break Stealth, Dropp Shields, etc?)")]
        public bool IsHostileAction;

        // Change to be based on the Action Type?
        [field: SerializeField] public bool ShouldNotifyClient { get; private set; } = true;

        [field: SerializeField] public ActionActivationStyle ActivationStyle { get; private set; } = ActionActivationStyle.Held;


        [field: Header("Heat")]
        [Tooltip("How much heat this Action generates when activated.")]
        [field: SerializeField] public float ImmediateHeat { get; private set; } = 0.0f;

        [Tooltip("How much heat this Action generates each second while in use.")]
        [field: SerializeField] public float ContinuousHeat { get; private set; } = 0.0f;

        [Tooltip("How much heat this Action generates each time that it successfully updates.")]
        [field: SerializeField] public float RetriggerHeat { get; private set; } = 0.0f;




        [field: Header("Timing Settings")]
        [Tooltip("The time in seconds between starting this action and its effects triggering")]
        [field:SerializeField] public float ExecutionDelay { get; private set; }
        [field: SerializeField] public float ActionCooldown { get; private set; }
        [field: SerializeField] public BlockingModeType BlockingMode { get; private set; } = BlockingModeType.OnlyDuringExecutionTime;
        [field: SerializeField] public float MaxActiveDuration { get; private set; }


        [field: Header("Charging Settings")]
        [field: SerializeField] public bool CanCharge { get; private set; }
        [field: SerializeField] public float MaxChargeTime { get; private set; }
        [field: SerializeField] [field: Range(0.0f, 1.0f)] public float MinChargeActivationPercentage { get; private set; } = 1.0f;

        [field: Space(10)]
        [Tooltip("Should this action treat itself as having no charging once it reaches full charge.")]
        [field: SerializeField] public bool RetainChargeAfterFull { get; private set; } = false;

        [field: Space(10)]
        [Tooltip("The time (In seconds) that it takes for this action to go from 100% charge to 0% charge after being ended/cancelled.")]
        [field: SerializeField] public float MaxChargeDepletionTime { get; private set; }

        [field: Space(10)]
        [Tooltip("The time (In seconds) that it takes for the UI of this action to reset to 0 after successfully firing (0.0f for instant).")]
        [field: SerializeField] public float UIChargeDepletionTime { get; private set; }


        [field: Header("Retrigger Settings")]
        [field: SerializeField] public ActionTriggerType TriggerType { get; private set; }
        [field: SerializeField] public bool CancelOnLastTrigger { get; private set; } = true;


        [field: Space(5)]
        [field: SerializeField] public float RetriggerDelay { get; private set; }

        [field: Space(5)]
        [field: SerializeField] public int Bursts { get; private set; }
        [field: SerializeField] public float BurstDelay { get; private set; }


        [field: Header("Action Interruption Settings")]
        [field: SerializeField] public bool IsInterruptable { get; private set; }
        // What can interrupt this?
        // What this interrupts?



        // Server Only Settings.
        [field: Header("Server-Only Settings")]
        [SerializeReference][SubclassSelector] public ActionEffect[] ActionEffects;



        // Client Only Settings.
        [field: Header("Client Settings")]
        [SerializeReference, SubclassSelector] public ActionVisual[] TriggeringVisuals;

        [Space(5)]
        [SerializeReference, SubclassSelector] public ActionVisual[] HitVisuals;
        // Animation Triggers.

        [field: Space(5)]
        [field: SerializeField] public Crosshair ActionCrosshairPrefab { get; private set; }

        #endregion


        public virtual bool HasCooldown => ActionCooldown > 0.0f;
        public virtual bool HasCooldownCompleted(float lastActivatedTime) => (NetworkManager.Singleton.ServerTime.TimeAsFloat - lastActivatedTime) >= ActionCooldown;
        /// <summary>
        ///     Determines if this action has expired given the passed starting time.
        /// </summary>
        /// <returns> True if the action has exprired, otherwise false.</returns>
        public virtual bool GetHasExpired(float timeStarted)
        {
            bool isExpirable = MaxActiveDuration > 0.0f;  // Non-positive values indicate that the duration is infinite.
            float timeElapsed = NetworkManager.Singleton.ServerTime.TimeAsFloat - timeStarted;
            return isExpirable && timeElapsed >= MaxActiveDuration;
        }

        public virtual bool CancelsOtherActions => false;


        /// <summary>
        ///     Returns true if this Action can be interrupted by actions with the passed ActionID.
        /// </summary>
        public virtual bool CanBeInterruptedBy(in ActionID otherActionID)
        {
            Debug.LogWarning("Not Implemented");
            return false;
        }
        /// <summary>
        ///     Returns true if this Action should cancel other actions with the passed <see cref="ActionRequestData"/>.
        /// </summary>
        // Using 'ActionRequestData' to let us compare SlotIndexes.
        public virtual bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData)
        {
            Debug.LogWarning("Not Implemented");
            return false;
        }


        /// <summary>
        ///     Called each frame (Before OnUpdate()) for the active ("blocking") Action, asking if it should become a background Action.
        /// </summary>
        /// <returns> True to become a non-blocking Action. False to remain as a blocking Action.</returns>
        public virtual bool ShouldBecomeNonBlocking(float timeRunning)
            => BlockingMode switch
            {
                BlockingModeType.OnlyDuringExecutionTime => timeRunning >= ExecutionDelay,
                BlockingModeType.Never => true,
                _ => false,
            };


        #region Validation

#if UNITY_EDITOR

        public void OnValidate()
        {
            if (TriggerType != ActionTriggerType.Single)
            {
                if (TriggerType == ActionTriggerType.Repeated || TriggerType == ActionTriggerType.RepeatedBurst)
                {
                    // Check for issues with Repeated settings.
                    if (RetriggerDelay <= 0.0f)
                        Debug.LogError($"Error: You have a Repeating RetriggerType, but '{nameof(RetriggerDelay)}' is non-positive.");
                }

                if (TriggerType == ActionTriggerType.Burst || TriggerType == ActionTriggerType.RepeatedBurst)
                {
                    // Check for issues with Burst settings.
                    if (Bursts <= 0)
                        Debug.LogError($"Error: You have a Burst RetriggerType, but '{nameof(Bursts)}' is non-positive");
                    if (BurstDelay <= 0.0f)
                        Debug.LogError($"Error: You have a Burst RetriggerType, but '{nameof(BurstDelay)}' is non-positive");
                }
            }
        }

#endif

        #endregion


        protected Vector3 GetActionOrigin(ref ActionRequestData data) => data.OriginTransform != null ? data.OriginTransform.position : data.Position;
        protected Vector3 GetActionDirection(ref ActionRequestData data) => (data.OriginTransform != null ? data.OriginTransform.forward : data.Direction).normalized;


        #region Overridable Methods

        /// <summary>
        ///     Called when the Action starts actually playing (Which may be after it is created, due to queueing).
        /// </summary>
        /// <returns> False if the Action decided it doesn't want to run. True otherwise.</returns>
        public abstract bool OnStart(ServerCharacter owner, ref ActionRequestData data);

        /// <summary>
        ///     Called when the Action wishes to Update itself.
        /// </summary>
        /// <returns> True to keep running, false to stop. The Action will stop by default when its duration expires, if it has one set.</returns>
        public abstract bool OnUpdate(ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f);

        /// <summary>
        ///     Called when the Action ends naturally.
        /// </summary>
        public virtual void OnEnd(ServerCharacter owner, ref ActionRequestData data) => Cleanup(owner);

        /// <summary>
        ///     Called when the Action gets cancelled.
        /// </summary>
        public virtual void OnCancel(ServerCharacter owner, ref ActionRequestData data) => Cleanup(owner);


        /// <summary>
        ///     Cleans up any ongoing effects on the server.
        /// </summary>
        public virtual void Cleanup(ServerCharacter owner)
        {
            for(int i = 0; i < ActionEffects.Length; ++i)
            {
                ActionEffects[i].Cleanup(owner);
            }
        }

        
        public virtual void OnCollisionEntered(ServerCharacter owner, Collision collision) { }


        /// <summary>
        ///     Called on the client when the Action starts actually playing (Which may be after it is created, due to queueing).
        /// </summary>
        /// <returns> False if the Action decided it doesn't want to run. True otherwise.</returns>
        public virtual bool OnStartClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientStart(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            return ActionConclusion.Continue;
        }
        /// <summary>
        ///     Called on the client when this action should start charging.
        /// </summary>
        public virtual void OnStartChargingClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientStartCharging(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));
        }
        /// <summary>
        ///     Called on the client when the Action wishes to Update itself.
        /// </summary>
        /// <returns> True to keep running, false to stop. The Action will stop by default when its duration expires, if it has one set.</returns>
        public virtual bool OnUpdateClient(ClientCharacter clientCharacter, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientUpdate(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            return ActionConclusion.Continue;
        }
        /// <summary>
        ///     Called on the client when the Action ends naturally.
        /// </summary>
        public virtual void OnEndClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientEnd(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            CleanupClient(clientCharacter);
        }
        /// <summary>
        ///     Called on the client when the Action gets cancelled.
        /// </summary>
        public virtual void OnCancelClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientCancel(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            CleanupClient(clientCharacter);
        }

        /// <summary>
        ///     Cleans up any ongoing effects on the client.
        /// </summary>
        public virtual void CleanupClient(ClientCharacter clientCharacter) { }


        public virtual void AnticipateClient(ClientCharacter clientCharacter, ref ActionRequestData data) => Debug.Log("Anticipate");

        #endregion
    }
}

namespace Gameplay.Actions
{
    public enum ActionActivationStyle
    {
        /// <summary> Activate the action upon press. Cancel it upon release. </summary>
        Held,

        /// <summary> Activate the action upon press. Cancel it upon the next press.</summary>
        Toggle,

        /// <summary> Activate the action upon press. Don't cancel it ourselves.</summary>
        Pressed,
    }
}