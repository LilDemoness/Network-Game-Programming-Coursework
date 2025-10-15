using Gameplay.Actions.Effects;
using Gameplay.Actions.Visuals;
using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Actions.Definitions
{
    public abstract class ActionDefinition : ScriptableObject
    {
        /// <summary>
        ///     An index into the GameDataSource array of action prototypes.
        ///     Set at runtime by GameDataSource class.
        ///     If the action is not itself a prototype, it will contain the ActionID of the prototype reference.
        ///     <br></br>This field is used to identify actions in a way that can be sent over the network.
        /// </summary>
        /// <remarks> Non-serialized, so it doesn't get saved between editor sessions.</remarks>
        [field: System.NonSerialized] public ActionID ActionID { get; set; }


        #region Action Settings

        [Header("Action Settings")]
        [Tooltip("Does this count as a hostile Action? (Should it: Break Stealth, Dropp Shields, etc?)")]
        public bool IsHostileAction;

        [Tooltip("How much energy/ammo/etc this Action costs.")]
        public float Cost;

        // Change to be based on the Action Type?
        [field: SerializeField] public bool ShouldNotifyClient { get; private set; } = true;


        [Header("Timing Settings")]
        [Tooltip("The time in seconds between starting this action and its effects triggering")]
        [field:SerializeField] public float ExecutionDelay { get; private set; }
        [field: SerializeField] public float ActionCooldown { get; private set; }
        [field: SerializeField] public BlockingModeType BlockingMode { get; private set; } = BlockingModeType.OnlyDuringExecutionTime;
        [field: SerializeField] public float MaxActiveDuration { get; private set; }


        [Header("Charging Settings")]
        [field: SerializeField] public bool CanCharge { get; private set; }
        [field: SerializeField] public float MaxChargeTime { get; private set; }
        [field: SerializeField] public bool ExecuteIfNotAtFullCharge { get; private set; }


        [Header("Retrigger Settings")]
        [field: SerializeField] public ActionTriggerType TriggerType { get; private set; }
        [field: SerializeField] public bool CancelOnLastTrigger { get; private set; } = true;


        [field: Space(5)]
        [field: SerializeField] public float RetriggerDelay { get; private set; }

        [field: Space(5)]
        [field: SerializeField] public int Bursts { get; private set; }
        [field: SerializeField] public float BurstDelay { get; private set; }


        [Header("Action Interruption Settings")]
        [field: SerializeField] public bool IsInterruptable { get; private set; }
        // What can interrupt this?
        // What this interrupts?



        // Server Only Settings.
        [Header("Server-Only Settings")]
        [SerializeReference][SubclassSelector] public ActionEffect[] ActionEffects;



        // Client Only Settings.
        [Header("Client Settings")]
        [SerializeReference, SubclassSelector] public ActionVisual[] TriggeringVisuals;

        [Space(5)]
        [SerializeReference, SubclassSelector] public ActionVisual[] HitVisuals;
        // Animation Triggers.

        #endregion


        public virtual bool HasCooldown => ActionCooldown > 0.0f;
        public virtual bool HasCooldownCompleted(float lastActivatedTime) => (NetworkManager.Singleton.ServerTime.TimeAsFloat - lastActivatedTime) >= ActionCooldown;
        public virtual bool GetHasExpired(float timeStarted)
        {
            bool isExpirable = MaxActiveDuration > 0.0f;  // Non-positive values indicate that the duration is infinite.
            float timeElapsed = NetworkManager.Singleton.ServerTime.TimeAsFloat - timeStarted;
            return isExpirable && timeElapsed >= MaxActiveDuration;
        }

        public virtual bool CancelsOtherActions => false;


        public virtual bool CanBeInterruptedBy(in ActionID otherActionID)
        {
            Debug.LogWarning("Not Implemented");
            return false;
        }
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


        protected Vector3 GetActionOrigin(ref ActionRequestData data) => data.OriginTransformID != 0 ? NetworkManager.Singleton.SpawnManager.SpawnedObjects[data.OriginTransformID].transform.TransformPoint(data.Position) : data.Position;
        protected Vector3 GetActionDirection(ref ActionRequestData data) => (data.OriginTransformID != 0 ? NetworkManager.Singleton.SpawnManager.SpawnedObjects[data.OriginTransformID].transform.TransformDirection(data.Direction) : data.Direction).normalized;


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
        public abstract bool OnUpdate(ServerCharacter owner, ref ActionRequestData data);

        /// <summary>
        ///     Called when the Action ends naturally.
        /// </summary>
        public virtual void OnEnd(ServerCharacter owner, ref ActionRequestData data) => Cleanup(owner);

        /// <summary>
        ///     Called when the Action gets cancelled.
        /// </summary>
        public virtual void OnCancel(ServerCharacter owner, ref ActionRequestData data) => Cleanup(owner);


        /// <summary>
        ///     Cleans up any ongoing effects.
        /// </summary>
        public virtual void Cleanup(ServerCharacter owner) { }

        
        public virtual void OnCollisionEntered(ServerCharacter owner, Collision collision) { }


        public virtual bool OnStartClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientStart(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            return ActionConclusion.Continue;
        }
        public virtual bool OnUpdateClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientUpdate(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            return ActionConclusion.Continue;
        }
        public virtual void OnEndClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientEnd(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            CleanupClient(clientCharacter);
        }
        public virtual void OnCancelClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientCancel(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            CleanupClient(clientCharacter);
        }

        public virtual void CleanupClient(ClientCharacter clientCharacter) { }

        #endregion
    }
}