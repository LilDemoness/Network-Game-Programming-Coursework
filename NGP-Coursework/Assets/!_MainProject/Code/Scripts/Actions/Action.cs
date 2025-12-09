using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Definitions;
using Unity.Mathematics;

namespace Gameplay.Actions
{
    /// <summary>
    ///     A container class for action instances.<br/>
    ///     Facilitates instance variables and correctly processing when the action should perform Start, Update, etc functions. 
    /// </summary>
    public class Action
    {
        /// <summary>
        ///     The default string for hit reaction animation triggers.
        /// </summary>
        public const string DEFAULT_HIT_REACT_ANIMATION_STRING = "";


        /// <inheritdoc cref="ActionDefinition.ActionID"/>
        public ActionID ActionID { get => _definition.ActionID; }

        protected ActionRequestData m_data;
        public bool IsGhost { get; set; }

        public Transform OriginTransform;


        /// <summary>
        ///     The time when this Action was started (From Time.time) in seconds.
        /// </summary>
        public float TimeStarted { get; set; }

        /// <summary>
        ///     How long the Action has been running (Since it's Start was called). Measured in seconds via Time.time.
        /// </summary>
        public float TimeRunning => NetworkManager.Singleton.ServerTime.TimeAsFloat - TimeStarted;

        /// <summary>
        ///     RequestData we were instantiated with. Value should be reated as readonly.
        /// </summary>
        public ref ActionRequestData Data => ref m_data;


        /// <summary>
        ///     This action instance's corresponding definition.
        /// </summary>
        [SerializeField] private readonly ActionDefinition _definition;
        /// <inheritdoc cref="Action._definition"/>
        public ActionDefinition Definition => _definition;


        private float _nextUpdateTime;
        private bool _hasPerformedLastTrigger;
        private int _burstsRemaining;

        private bool _isCharging, _isFirstCharge;
        private float _chargeStartTime;


        #region Pass-through Functions to ActionDefinition

        /// <inheritdoc cref="ActionDefinition.ShouldNotifyClient"/>
        public bool ShouldNotifyClient => _definition.ShouldNotifyClient;


        /// <inheritdoc cref="ActionDefinition.HasCooldown"/>
        public bool HasCooldown => _definition.HasCooldown;

        /// <inheritdoc cref="ActionDefinition.HasCooldownCompleted"/>
        public bool HasCooldownCompleted(float lastActivatedTime) => _definition.HasCooldownCompleted(lastActivatedTime);

        /// <inheritdoc cref="ActionDefinition.GetHasExpired(float)"/>
        public bool HasExpired => _definition.GetHasExpired(this.TimeStarted);


        /// <inheritdoc cref="ActionDefinition.CancelsOtherActions"/>
        public bool CancelsOtherActions => _definition.CancelsOtherActions;


        /// <inheritdoc cref="ActionDefinition.CanBeInterruptedBy"/>
        public bool CanBeInterruptedBy(in ActionID otherActionID) => _definition.CanBeInterruptedBy(otherActionID);

        /// <inheritdoc cref="ActionDefinition.ShouldCancelAction"/>
        public bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData) => _definition.ShouldCancelAction(ref thisData, ref otherData);

        /// <summary>
        ///     Returns true if this action is able to be cancelled, or false if the action should still persist past cancellation.
        /// </summary>
        public bool CanBeCancelled() => _burstsRemaining == 0 || _burstsRemaining == _definition.Bursts;
        


        /// <inheritdoc cref="ActionDefinition.ShouldBecomeNonBlocking"/>
        public bool ShouldBecomeNonBlocking() => _definition.ShouldBecomeNonBlocking(TimeRunning);

        #endregion


        #region Charging Events

        public static event System.EventHandler<StartedChargingEventArgs> OnClientStartedCharging;
        public class StartedChargingEventArgs : System.EventArgs
        {
            public ClientCharacter Client { get; set; }
            public AttachmentSlotIndex AttachmentSlotIndex { get; set; }
            public float ChargeStartedTime { get; set; }
            public float MaxChargeDuration { get; set; }

            private StartedChargingEventArgs() { }
            public StartedChargingEventArgs(ClientCharacter client, AttachmentSlotIndex attachmentSlotIndex, float chargeStartedTime, float maxChargeTime)
            {
                this.Client = client;
                this.AttachmentSlotIndex = attachmentSlotIndex;
                this.ChargeStartedTime = chargeStartedTime;
                this.MaxChargeDuration = maxChargeTime;
            }
        }
        public static event System.EventHandler<StoppedChargingEventArgs> OnClientStoppedCharging;
        public class StoppedChargingEventArgs : System.EventArgs
        {
            public ClientCharacter Client { get; set; }
            public AttachmentSlotIndex AttachmentSlotIndex { get; set; }
            public float ChargeFullyDepletedTime { get; set; }
            public float MaxChargeDepletionTime { get; set; }

            private StoppedChargingEventArgs() { }
            public StoppedChargingEventArgs(ClientCharacter client, AttachmentSlotIndex attachmentSlotIndex, float chargeFullyDepletedTime, float maxChargeDepletionTime)
            {
                this.Client = client;
                this.AttachmentSlotIndex = attachmentSlotIndex;
                this.ChargeFullyDepletedTime = chargeFullyDepletedTime;
                this.MaxChargeDepletionTime = maxChargeDepletionTime;
            }
        }
        public static event System.EventHandler<ResetChargingEventArgs> OnClientResetCharging;
        public class ResetChargingEventArgs : System.EventArgs
        {
            public ClientCharacter Client { get; set; }
            public AttachmentSlotIndex AttachmentSlotIndex { get; set; }
            public float CurrentChargePercentage { get; set; }
            public float TimeToReset { get; set; }

            private ResetChargingEventArgs() { }
            public ResetChargingEventArgs(ClientCharacter client, AttachmentSlotIndex attachmentSlotIndex, float currentChargePercentage, float timeToReset)
            {
                this.Client = client;
                this.AttachmentSlotIndex = attachmentSlotIndex;
                this.CurrentChargePercentage = currentChargePercentage;
                this.TimeToReset = timeToReset;
            }
        }

        #endregion

        public event System.Action<Action> OnUpdateTriggered;


        public bool IsChaseAction => ActionID == GameDataSource.Instance.GeneralChaseActionDefinition.ActionID;
        public bool IsStunAction => ActionID == GameDataSource.Instance.StunnedActionDefinition.ActionID;
        public bool IsGeneralTargetAction => ActionID == GameDataSource.Instance.GeneralTargetActionDefinition.ActionID;


        public Action(ActionDefinition definition)
        {
            this._definition = definition;
        }
        /// <summary>
        ///     Used as a Constructor.
        ///     The "data" parameter should not be retained after passing into this method, as we're taking ownership of its internal memory.
        ///     Needs to be called by the ActionFactory.
        /// </summary>
        public void Initialise(ref ActionRequestData data)
        {
            if (data.ActionID != this.ActionID)
                throw new System.ArgumentException($"The ActionID of the passed data doesn't match this Action's ID (Action: {ActionID} | Data: {data.ActionID})");

            this.m_data = data;
        }

        /// <summary>
        ///     Reset the action before returning it to the pool.
        /// </summary>
        public virtual void ReturnToPool()
        {
            this.m_data = default;
            this.IsGhost = false;
            
            this.TimeStarted = 0;
            this._nextUpdateTime = 0.0f;
            this._burstsRemaining = 0;
            this._hasPerformedLastTrigger = false;

            this._isCharging = false;
            this._isFirstCharge = false;
            this._chargeStartTime = 0.0f;
        }


        /// <summary>
        ///     Called when the Action starts actually playing (Which may be after it is created, due to queueing).
        /// </summary>
        /// <returns> False if the Action decided it doesn't want to run. True otherwise.</returns>
        public virtual bool OnStart(ServerCharacter owner, float chargeDepletedTime)
        {
            if (Definition.ShouldNotifyClient)
                owner.ClientCharacter.PlayActionClientRpc(Data, TimeStarted);

            _nextUpdateTime = TimeStarted + _definition.ExecutionDelay;

            // Initialise Charging Time.
            float startingChargePercentage = _definition.MaxChargeDepletionTime > 0.0f ? Mathf.Max((chargeDepletedTime - TimeStarted) / _definition.MaxChargeDepletionTime, 0.0f) : 0.0f;
            _chargeStartTime = _nextUpdateTime - (_definition.MaxChargeTime * startingChargePercentage);
            _isFirstCharge = true;

            // Initialise other parameters.
            this._burstsRemaining = _definition.Bursts;

            // Apply Heat.
            owner.ReceiveHeatChange(owner, _definition.ImmediateHeat);

            // Call our OnStart function.
            return _definition.OnStart(owner, ref Data);
        }




        private bool CalculateNextUpdateTime()
        {
            switch (_definition.TriggerType)
            {
                case ActionTriggerType.Burst:
                    --_burstsRemaining;

                    if (_burstsRemaining <= 0)
                        return ActionConclusion.Stop;   // The burst has finished. The action has concluded.
                    else
                        _nextUpdateTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.BurstDelay; // We still have shots remaining in this burst.

                    break;
                case ActionTriggerType.RepeatedBurst:
                    --_burstsRemaining;

                    if (_burstsRemaining > 0)
                    {
                        // There are still shots remaining in this burst.
                        _nextUpdateTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.BurstDelay;
                    }
                    else
                    {
                        // Finished this burst, wait until the next burst.
                        _burstsRemaining = _definition.Bursts;
                        _nextUpdateTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.RetriggerDelay;
                    }
                    break;
                case ActionTriggerType.Repeated:
                    _nextUpdateTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + _definition.RetriggerDelay;
                    break;
                default:
                    return ActionConclusion.Stop;
            }

            return ActionConclusion.Continue;
        }
        private bool IsStillCharging(out bool justStartedCharging)
        {
            justStartedCharging = false;
            if (!_definition.CanCharge)
                return false;
            if (_burstsRemaining != _definition.Bursts)
                return false;   // We're performing a burst, so can't possibly be charging.

            if (!_isCharging)
            {
                // We haven't yet started charging this action since we last fired.
                justStartedCharging = true;
                _isCharging = true;
                if (!_isFirstCharge)
                    _chargeStartTime = NetworkManager.Singleton.ServerTime.TimeAsFloat; // This isn't our first charge, so note our charge start time.
                _isFirstCharge = false;
                return true;
            }
                
            if ((NetworkManager.Singleton.ServerTime.TimeAsFloat - _chargeStartTime) < _definition.MaxChargeTime)
            {
                // This action is a charging action, and we haven't yet reached full charge.
                return true;
            }

            // We've fully charged and should perform this action now.

            if (!_definition.RetainChargeAfterFull)
            {
                // We're not wanting to charge this weapon only once.
                // Mark ourselves as no longer charging to prevent issues with retaining charge after cancelling while in the middle of a burst.
                _isCharging = false;
            }
            return false;
        }
        /// <summary>
        ///     Called each frame the Action is running.
        /// </summary>
        /// <returns> True to keep running, false to stop. The action will stop by default when its duration expires, if it has one set.</returns>
        public virtual bool OnUpdate(ServerCharacter owner)
        {
            // Apply Heat.
            owner.ReceiveHeatChange(owner, _definition.ContinuousHeat * Time.deltaTime);

            // Check if we should update.
            if (_hasPerformedLastTrigger)
                return !_definition.CancelOnLastTrigger;

            if (NetworkManager.Singleton.ServerTime.TimeAsFloat < _nextUpdateTime)
                return ActionConclusion.Continue;   // We shouldn't yet update.

            if (IsStillCharging(out _))
                return ActionConclusion.Continue;

            // We should update.

            // Apply Heat.
            owner.ReceiveHeatChange(owner, _definition.RetriggerHeat);
            // Update the action.
            OnUpdateTriggered?.Invoke(this);
            if (_definition.OnUpdate(owner, ref Data) == false)
                return ActionConclusion.Stop;   // The action has concluded. Stop.

            // We have performed our update and haven't yet stopped.
            // Check if and when we should perform our next update.
            _hasPerformedLastTrigger = !CalculateNextUpdateTime();

            if (_definition.CancelOnLastTrigger && _hasPerformedLastTrigger)
                return ActionConclusion.Stop;
            else
                return ActionConclusion.Continue;
        }
        
        

        

        /// <summary>
        ///     Called when the Action ends naturally.
        /// </summary>
        public virtual void End(ServerCharacter owner)
        {
            _definition.OnEnd(owner, ref Data);
            Cleanup(owner);
        }
        

        private float CalculateChargePercentage(out float chargeLostTime)
        {
            float currentTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            float timeSpendCharging = currentTime - _chargeStartTime;
            float chargePercentage = Mathf.Clamp01(timeSpendCharging / _definition.MaxChargeTime);

            chargeLostTime = currentTime + (_definition.MaxChargeDepletionTime * chargePercentage);
            return chargePercentage;
        }

        /// <summary>
        ///     Called when the Action gets cancelled.
        /// </summary>
        public virtual void Cancel(ServerCharacter owner, out float chargeLostTime)
        {
            if (_definition.CanCharge)
            {
                float chargePercentage = CalculateChargePercentage(out chargeLostTime);

                if (chargePercentage > _definition.MinChargeActivationPercentage)
                {
                    const float CHARGE_ROUNDING_TARGET = 0.05f;
                    chargePercentage = Mathf.Round(chargePercentage / CHARGE_ROUNDING_TARGET) * CHARGE_ROUNDING_TARGET;

                    if (_definition.OnUpdate(owner, ref Data, chargePercentage) == false)
                    {
                        End(owner);
                        return;
                    }
                }
            }
            else
            {
                chargeLostTime = 0.0f;
            }

            _definition.OnCancel(owner, ref Data);
            Cleanup(owner);
        }
        


        /// <summary>
        ///     Cleans up any ongoing effects.
        /// </summary>
        public virtual void Cleanup(ServerCharacter owner) { }



        /// <summary>
        ///     Called <b>AFTER</b> End(). At this point, the Action has ended, meaning its Update() etc. functions will never be called again.
        ///     If the Action wants to immediately lead into another Action, it would do so here.
        ///     The new Action will take effect in the next Update().
        /// </summary>
        /// <param name="newAction"> The new Action to immediately transition to.</param>
        /// <returns> True if there's a new Action, false otherwise.</returns>
        // Note: This is not called on prematurely cancelled Action, only on ones that have their End() called.
        public virtual bool ChainIntoNewAction(ref ActionRequestData newAction) { return false; }


        /// <summary>
        ///     Called on the active ("Blocking") Action when this character collides with another.
        /// </summary>
        public virtual void CollisionEntered(ServerCharacter owner, Collision collision) => _definition.OnCollisionEntered(owner, collision);



        #region Buffs

        public enum BuffableValue
        {
            PercentHealingReceived, // Unbuffed Value is 1.0f. Reducing to 0 means "no healing", while 2 is "double healing".
            PercentDamageReceived,  // Unbuffed Value is 1.0f. Reducing to 0 means "no damage", while 2 is "double damage".
            ChanceToStunTramplers,  // Unbuffed Value is 0. If > 0, is the 0-1 percentage chance that someone trampling this character becomes stunned.
        }

        /// <summary>
        ///     A
        /// </summary>
        /// <param name="buffType"> A.</param>
        /// <param name="newBuffedValue"> A.</param>
        public virtual void BuffValue(BuffableValue buffType, ref float newBuffedValue) { }

        public static float GetUnbuffedValue(BuffableValue buffType) => buffType switch
            {
                BuffableValue.PercentHealingReceived => 1.0f,
                BuffableValue.PercentDamageReceived => 1.0f,
                BuffableValue.ChanceToStunTramplers => 0.0f,
                _ => throw new System.Exception($"Unknown buff type {buffType.ToString()}")
            };

        #endregion


        #region Gameplay Activities

        public enum GameplayActivity
        {
            AttackedByEnemy,
            Healed,
            StoppedChargingUp,
            UsingHostileAction, // Called immediately before using any hostile actions.
        }

        /// <summary>
        ///     Called on active Actions to let them know when a notable gameplay event happens.
        /// </summary>
        /// <remarks> When a GameplayActivity of AttackedByEnemy or Healed happens, OnGameplayAction() is called BEFORE BuffValue() is called.</remarks>
        public virtual void OnGameplayActivity(ServerCharacter owner, GameplayActivity activityType) { }

        #endregion


        #region Client-Side Functions

        /// <returns>
        ///     True if this ActionFX began running immediately, prior to getting a confirmation from the server.
        /// </returns>
        public bool AnticipatedClient { get; protected set; }

        /// <summary>
        ///     Starts the ActionFX.
        ///     Derived classes may return false if they wish to end immediately without their Update being called.
        /// </summary>
        /// <remarks>
        ///     Derived classes should be sure to call base.OnStart() in their implementation, but do not that this resets "AnticipatedClient" to false.
        /// </remarks>
        /// <returns> True to play, false to be immediately cleaned up.</returns>
        public virtual bool OnStartClient(ClientCharacter clientCharacter, float chargeDepletedTime, float serverTimeStarted)
        {
            AnticipatedClient = false;  // Once we start our ActionFX we are no longer an anticipated action.
            TimeStarted = serverTimeStarted;
            this._nextUpdateTime = TimeStarted + _definition.ExecutionDelay;

            this._burstsRemaining = _definition.Bursts;

            // Calculate charge.
            float startingChargePercentage = _definition.MaxChargeDepletionTime > 0.0f ? Mathf.Max((chargeDepletedTime - TimeStarted) / _definition.MaxChargeDepletionTime, 0.0f) : 0.0f;
            this._chargeStartTime = _nextUpdateTime - (_definition.MaxChargeTime * startingChargePercentage);
            this._isFirstCharge = true;

            return _definition.OnStartClient(clientCharacter, ref Data);
        }

        public virtual bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            // Check if we should update.

            if (_hasPerformedLastTrigger)
                return !_definition.CancelOnLastTrigger;

            if (NetworkManager.Singleton.ServerTime.TimeAsFloat < _nextUpdateTime)
                return ActionConclusion.Continue;

            if (IsStillCharging(out bool justStartedCharging))
            {
                if (justStartedCharging)
                {
                    _definition.OnStartChargingClient(clientCharacter, ref Data);
                    OnClientStartedCharging?.Invoke(this, new StartedChargingEventArgs(clientCharacter, Data.AttachmentSlotIndex, _chargeStartTime, _definition.MaxChargeTime));
                }
                return ActionConclusion.Continue;
            }

            // Reset the charge UI smoothly if this is the first shot within a burst/the only shot.
            if (_burstsRemaining == _definition.Bursts)
                OnClientResetCharging?.Invoke(this, new ResetChargingEventArgs(clientCharacter, Data.AttachmentSlotIndex, CalculateChargePercentage(out _), _definition.UIChargeDepletionTime));


            // We should update.

            OnUpdateTriggered?.Invoke(this);
            if (_definition.OnUpdateClient(clientCharacter, ref Data) == false)
                return ActionConclusion.Stop;

            // We've updated and are still wishing to continue updating.
            // Determine if and when we should next update.
            _hasPerformedLastTrigger = !CalculateNextUpdateTime();

            if (_definition.CancelOnLastTrigger && _hasPerformedLastTrigger)
                return ActionConclusion.Stop;
            else
                return ActionConclusion.Continue;
        }
        
        

        /// <summary>
        ///     End is called when the Action finishes playing.
        ///     This is a good place for derived classes top put wrap-up logic.
        ///     Derived classes should (But aren't required to) call base.End().
        /// </summary>
        public virtual void EndClient(ClientCharacter clientCharacter)
        {
            _definition.OnEndClient(clientCharacter, ref Data);

            CleanupClient(clientCharacter);
        }

        /// <summary>
        ///     Cancel is called when an Action is interrupted prematurely.
        ///     It is kept logically distincy from end to allow for the possibility that an Action might want to pay something different if it is interrupted, rather than completing.
        ///     For example, a "ChargeShot" action might want to emit a projectile object in its end method, but instead play a "Stagger" effect in its Cancel method.
        /// </summary>
        public virtual void CancelClient(ClientCharacter clientCharacter, out float chargeLostTime)
        {
            if (_definition.CanCharge && _isCharging)
            {
                float chargePercentage = CalculateChargePercentage(out chargeLostTime);
                OnClientStoppedCharging?.Invoke(this, new StoppedChargingEventArgs(clientCharacter, Data.AttachmentSlotIndex, chargeLostTime, _definition.MaxChargeDepletionTime));


                if (chargePercentage > _definition.MinChargeActivationPercentage)
                {
                    if (_definition.OnUpdateClient(clientCharacter, ref Data, chargePercentage) == false)
                    {
                        EndClient(clientCharacter);
                        return;
                    }
                }
            }
            else
            {
                chargeLostTime = 0.0f;
                OnClientStoppedCharging?.Invoke(this, new StoppedChargingEventArgs(clientCharacter, Data.AttachmentSlotIndex, 0.0f, _definition.MaxChargeDepletionTime));
            }

            _definition.OnCancelClient(clientCharacter, ref Data);

            CleanupClient(clientCharacter);
        }


        public virtual void CleanupClient(ClientCharacter clientCharacter) { }


        /// <summary>
        ///     Should this ActionFX be created anticipativelyt on the owning client?
        /// </summary>
        /// <param name="clientCharacter"> The ActionVisualisation that would be playing this ActionFX.</param>
        /// <param name="data"> The request being sent to the server.</param>
        /// <returns> True if the ActionVisualisation should pre-emptively create the ActionFX on the owning client before hearing back from the server.</returns>
        public static bool ShouldClientAnticipate(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            if (!clientCharacter.CanPerformActions)
                return false;

            /*var actionDefinition = GameDataSource.Instance.GetActionDefinitionByID(data.ActionID);

            // For actions with 'ShouldClose' set, we need to check our range loocally.
            // If we are out of range, we shouldn't anticipate, as we will still need to execute a ChaseAction (Will be synthesised on the server) prior to actually playing the action.
            bool isTargetEligible = true;
            if (data.ShouldClose)
            {
                ulong targetID = (data.TargetIDs != null && data.TargetIDs.Length > 0) ? data.TargetIDs[0] : 0;
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetID, out NetworkObject networkObject))
                {
                    float sqrRange = actionDefinition.Range * actionDefinition.Range;
                    isTargetEligible = (networkObject.transform.position - clientCharacter.transform.position).sqrMagnitude < sqrRange;
                }
            }*/

            // Currently, all actions should anticipate.
            return true;
        }

        /// <summary>
        ///     Called when the visualisation receives an animation event.
        /// </summary>
        public virtual void OnAnimEventClient(ClientCharacter clientCharacter, string id) => throw new System.NotImplementedException();

        /// <summary>
        ///     Called when this action has finished "Charging Up".
        ///     Only called for a few types of action.
        /// </summary>
        public virtual void StoppedChargingUpClient(ClientCharacter clientCharacter, float finalChargeUpPercentage) => throw new System.NotImplementedException();


        /// <summary>
        ///     Called when the action is being "anticipated" on the client.
        ///     For example, showing hit-markers for instant firing weapons.
        /// </summary>
        public virtual void AnticipateActionClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            AnticipatedClient = true;
            TimeStarted = UnityEngine.Time.time;    // Replace with 'NetworkManager.Singleton.LocalTime.TimeAsFloat' to better match server-time when receiving the triggering Rpc?

            _definition.AnticipateClient(clientCharacter, ref data);

            /*if (!string.IsNullOrEmpty(Config.AnimAnticipation))
            {
                clientCharacter.ourAnimator.SetTrigger(Config.AnimAnticipation);
            }*/
        }

        #endregion
    }
}