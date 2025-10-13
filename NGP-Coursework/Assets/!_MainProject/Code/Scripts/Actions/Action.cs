using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using VisualEffects;
using Unity.Netcode;

namespace Gameplay.Actions
{
    public abstract class Action : ScriptableObject
    {
        /// <summary>
        ///     The default string for hit reaction animation triggers.
        /// </summary>
        public const string DEFAULT_HIT_REACT_ANIMATION_STRING = "";

        public abstract bool ShouldNotifyClient { get; }


        /// <summary>
        ///     An index into the GameDataSource array of action prototypes.
        ///     Set at runtime by GameDataSource class.
        ///     If the action is not itself a prototype, it will contain the ActionID of the prototype reference.
        ///     <br></br>This field is used to identify actions in a way that can be sent over the network.
        /// </summary>
        /// <remarks> Non-serialized, so it doesn't get saved between editor sessions.</remarks>
        [field: System.NonSerialized] public ActionID ActionID { get; set; }

        protected ActionRequestData m_data;


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


        [Tooltip("Does this count as a hostile Action? (Should it: Break Stealth, Dropp Shields, etc?)")]
        public bool IsHostileAction;

        [Tooltip("How much energy/ammo/etc this Action costs.")]
        public float Cost;


        public abstract bool HasCooldown { get; }
        public abstract bool HasCooldownCompleted(float lastActivatedTime);
        public abstract bool HasExpired { get; }

        public virtual bool CancelsOtherActions => false;


        public virtual bool CanBeInterruptedBy(ActionID otherActionID) => false;
        public virtual bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData) => false;


        public bool IsChaseAction => ActionID == GameDataSource.Instance.GeneralChaseActionDefinition.ActionID;
        public bool IsStunAction => ActionID == GameDataSource.Instance.StunnedActionDefinition.ActionID;
        public bool IsGeneralTargetAction => ActionID == GameDataSource.Instance.GeneralTargetActionDefinition.ActionID;


        /// <summary>
        ///     Used as a Constructor.
        ///     The "data" parameter should not be retained after passing into this method, as we're taking ownership of its internal memory.
        ///     Needs to be called by the ActionFactory.
        /// </summary>
        public void Initialise(ref ActionRequestData data)
        {
            this.m_data = data;
            this.ActionID = data.ActionID;
        }

        /// <summary>
        ///     Reset the action before returning it to the pool.
        /// </summary>
        public virtual void Reset()
        {
            this.m_data = default;
            this.ActionID = default;
            this.TimeStarted = 0;
        }


        /// <summary>
        ///     Called when the Action starts actually playing (Which may be after it is created, due to queueing).
        /// </summary>
        /// <returns> False if the Action decided it doesn't want to run. True otherwise.</returns>
        public virtual bool OnStart(ServerCharacter owner) => HandleStart(owner);
        /// <summary>
        ///     Called when the Action starts actually playing (Which may be after it is created, due to queueing).
        /// </summary>
        /// <returns> False if the Action decided it doesn't want to run. True otherwise.</returns>
        protected abstract bool HandleStart(ServerCharacter owner);


        /// <summary>
        ///     Called each frame the Action is running.
        /// </summary>
        /// <returns> True to keep running, false to stop. The action will stop by default when its duration expires, if it has one set.</returns>
        public virtual bool OnUpdate(ServerCharacter owner) => HandleUpdate(owner);
        
        /// <summary>
        ///     Called when the Action wishes to Update itself.
        /// </summary>
        /// <returns> True to keep running, false to stop. The Action will stop by default when its duration expires, if it has one set.</returns>
        protected abstract bool HandleUpdate(ServerCharacter owner);

        /// <summary>
        ///     Called each frame (Before OnUpdate()) for the active ("blocking") Action, asking if it should become a background Action.
        /// </summary>
        /// <returns> True to become a non-blocking Action. False to remain as a blocking Action.</returns>
        public abstract bool ShouldBecomeNonBlocking();

        /// <summary>
        ///     Called when the Action ends naturally.
        /// </summary>
        public virtual void End(ServerCharacter owner) => Cleanup(owner);
        

        /// <summary>
        ///     Called when the Action gets cancelled.
        /// </summary>
        public virtual void Cancel(ServerCharacter owner) => Cleanup(owner);
        


        /// <summary>
        ///     Cleans up any ongoing effects.
        /// </summary>
        public virtual void Cleanup(ServerCharacter owner) { }


        protected Vector3 GetActionOrigin() => Data.OriginTransformID != 0 ? NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.OriginTransformID].transform.TransformPoint(Data.Position) : Data.Position;
        protected Vector3 GetActionDirection() => (Data.OriginTransformID != 0 ? NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.OriginTransformID].transform.TransformDirection(Data.Direction) : Data.Direction).normalized;



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
        public virtual void CollisionEntered(ServerCharacter owner, Collision collision) { }



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
        public virtual bool OnStartClient(ClientCharacter clientCharacter, float serverTimeStarted)
        {
            AnticipatedClient = false;  // Once we start our ActionFX we are no longer an anticipated action.
            TimeStarted = serverTimeStarted;

            return ActionConclusion.Continue;
        }

        public virtual bool OnUpdateClient(ClientCharacter clientCharacter) => ActionConclusion.Continue;
        
        

        /// <summary>
        ///     End is always called when the ActionFX finishes playing.
        ///     This is a good place for derived classes top put wrap-up logic.
        ///     Derived classes should (But aren't required to) call base.End().
        /// </summary>
        public virtual void EndClient(ClientCharacter clientCharacter) => CleanupClient(clientCharacter);

        /// <summary>
        ///     Cancel is called when an ActionFX is interrupted prematurely.
        ///     It is kept logically distincy from end to allow for the possibility that an Action might want to pay something different if it is interrupted, rather than completing.
        ///     For example, a "ChargeShot" action might want to emit a projectile object in its end method, but instead play a "Stagger" effect in its Cancel method.
        /// </summary>
        public virtual void CancelClient(ClientCharacter clientCharacter) => CleanupClient(clientCharacter);


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
        public virtual void OnAnimEventClient(ClientCharacter clientCharacter, string id) { }

        /// <summary>
        ///     Called when this action has finished "Charging Up".
        ///     Only called for a few types of action.
        /// </summary>
        public virtual void StoppedChargingUpClient(ClientCharacter clientCharacter, float finalChargeUpPercentage) { }


        /// <summary>
        ///     Utility function that instantiates all graphics in the Spawns list. <br></br>
        ///     If parentToOrigin is true, the new graphics will be parented to the origin Transform.
        ///     If false, they are positioned/oriented the same way as the origin but aren't parented.
        /// </summary>
        protected List<SpecialFXGraphic> InstantiateSpecialFXGraphics(Transform origin, bool parentToOrigin)
        {
            throw new System.NotImplementedException();
            /*var returnList = new List<SpecialFXGraphic>();
            foreach(var prefab in Config.Spawns)
            {
                if (!prefab) { continue; } // Skip blank entries in our prefab list.
                returnList.Add(InstantiateSpecialFXGraphic(prefab, origin, parentToOrigin));
            }
            return returnList;*/
        }
        /// <summary>
        ///     Utility function that instantiates one of the graphics from the Spawns list. <br></br>
        ///     If parentToOrigin is true, the new graphics will be parented to the origin Transform.
        ///     If false, they are positioned/oriented the same way as the origin but aren't parented.
        /// </summary>
        protected SpecialFXGraphic InstantiateSpecialFXGraphic(SpecialFXGraphic prefab, Vector3 position, Vector3 forward)
        {
            /*if (prefab.GetComponent<SpecialFXGraphic>() == null)
            {
                throw new System.Exception($"One of the Spawns on the action {this.Config.name} does not have a SpecialFXGraphic component and can't be instantiated");
            }*/

            SpecialFXGraphic graphicsInstance = GameObject.Instantiate<SpecialFXGraphic>(prefab, position, Quaternion.LookRotation(forward));
            return graphicsInstance;
        }

        /// <summary>
        ///     Called when the action is being "anticipated" on the client.
        ///     For example, if you are the owner of a character and you start swinging a slow weapon, you will get this call immediately on the client, before the server round-trip.
        /// </summary>
        /// <remarks>
        ///     Overriders should always call 'base.AnticipateActionClient' in their implementation.
        /// </remarks>
        /// <param name="clientCharacter"></param>
        public virtual void AnticipateActionClient(ClientCharacter clientCharacter)
        {
            AnticipatedClient = true;
            TimeStarted = UnityEngine.Time.time;

            /*if (!string.IsNullOrEmpty(Config.AnimAnticipation))
            {
                clientCharacter.ourAnimator.SetTrigger(Config.AnimAnticipation);
            }*/
        }

        #endregion
    }
}