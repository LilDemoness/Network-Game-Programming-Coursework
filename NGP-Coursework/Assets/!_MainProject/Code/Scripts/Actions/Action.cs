using System;
using UnityEngine;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using NUnit.Framework;

namespace Gameplay.Actions
{
    public abstract class Action : ScriptableObject
    {
        /// <summary>
        ///     An index into the GameDataSource array of action prototypes.
        ///     Set at runtime by GameDataSource class.
        ///     If the action is not itself a prototype, it will contain the ActionID of the prototype reference.
        ///     <br></br>This field is used to identify actions in a way that can be sent over the network.
        /// </summary>
        [NonSerialized] public ActionID ActionID;


        public const string DEFAULT_HIT_REACT_ANIMATION_STRING = "";


        protected ActionRequestData m_data;


        /// <summary>
        ///     The time when this Action was started (From Time.time) in seconds.
        /// </summary>
        public float TimeStarted { get; set; }

        /// <summary>
        ///     How long the Action has been running (Since it's Start was called). Measured in seconds via Time.time.
        /// </summary>
        public float TimeRunning => Time.time - TimeStarted;

        /// <summary>
        ///     RequestData we were instantiated with. Value should be reated as readonly.
        /// </summary>
        public ref ActionRequestData Data => ref m_data;
        

        /// <summary>
        ///     Data description for this action.
        /// </summary>
        public ActionConfig Config;


        public bool IsChaseAction => ActionID == GameDataSource.Instance.GeneralChaseActionPrototype.ActionID;
        public bool IsStunAction => ActionID == GameDataSource.Instance.StunnedActionPrototype.ActionID;
        public bool IsGeneralTargetAction => ActionID == GameDataSource.Instance.GeneralTargetActionPrototype.ActionID;


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
        /// <returns> False if the action decided it doesn't want to run. True otherwise.</returns>
        public abstract bool OnStart(ServerCharacter serverCharacter);

        /// <summary>
        ///     Called each frame the action is running.
        /// </summary>
        /// <returns> True to keep running, false to stop. The action will stop by default when its duration expires, if it has one set.</returns>
        public abstract bool OnUpdate(ServerCharacter serverCharacter);

        /// <summary>
        ///     Called each frame (Before OnUpdate()) for the active ("blocking") Action, asking if it sohuld become a background Action.
        /// </summary>
        /// <returns> True to become a non-blocking Action. False to remain as a blocking Action.</returns>
        public virtual bool ShouldBecomeNonBlocking()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///     Called when the Action ends naturally.
        /// </summary>
        /// <remarks>
        ///     The default implementation just calls Cancel.
        /// </remarks>
        public virtual void End(ServerCharacter serverCharacter)
        {
            Cancel(serverCharacter);
        }

        /// <summary>
        ///     Called when the Action gets cancelled.
        ///     Should clean up any ongoing effects at this point.
        /// </summary>
        public virtual void Cancel(ServerCharacter serverCharacter) { }

        /// <summary>
        ///     Called <b>AFTER</b> End(). At this point, the Action has ended, meaning its Update() etc. functions will never be called again.
        ///     If the Action wants to immediately lead into another Action, it would do so here.
        ///     The new Action will take effect in the next Update().
        /// </summary>
        /// <param name="newAction"> The new Action to immediately transition to.</param>
        /// <returns> True if there's a new Action, false otherwise.</returns>
        // Note: This is not called on prematurely cancelled Action, only on ones that have their End() called.
        public virtual bool ChainIntoNewAction(ref ActionRequestData newAction) { return false; }


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
        public virtual bool OnStartClient(ClientCharacter clientCharacter)
        {
            AnticipatedClient = false;  // Once we start our ActionFX we are no longer an anticipated action.
            TimeStarted = UnityEngine.Time.time;
            return true;
        }

        public virtual bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return ActionConclusion.Continue;
        }

        /// <summary>
        ///     End is always called when the ActionFX finishes playing.
        ///     This is a good place for derived classes top put wrap-up logic.
        ///     Derived classes should (But aren't required to) call base.End().
        ///     By default, this method just calls 'ClientCancel' to handle the common case where Cancel and End so the same thing.
        /// </summary>
        public virtual void EndClient(ClientCharacter clientCharacter)
        {
            CancelClient(clientCharacter);
        }

        /// <summary>
        ///     Cancel is called when an ActionFX is interrupted prematurely.
        ///     It is kept logically distincy from end to allow for the possibility that an Action might want to pay something different if it is interrupted, rather than completing.
        ///     For example, a "ChargeShot" action might want to emit a projectile object in its end method, but instead play a "Stagger" effect in its Cancel method.
        /// </summary>
        public virtual void CancelClient(ClientCharacter clientCharacter) { }

        /// <summary>
        ///     Should this ActionFX be created anticipativelyt on the owning client?
        /// </summary>
        /// <param name="clientCharacter"> The ActionVisualisation that would be playing this ActionFX.</param>
        /// <param name="data"> The request being sent to the server.</param>
        /// <returns> True if the ActionVisualisation should pre-emptively create the ActionFX on the owning client before hearing back from the server.</returns>
        public virtual bool ShouldClientAnticipate(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            throw new System.NotImplementedException();
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
            var returnList = new List<SpecialFXGraphics>();
            foreach(var prefab in Config.Spawns)
            {
                if (!prefab) { continue; } // Skip blank entries in our prefab list.
                returnList.Add(InstantiateSpecialFXGraphic(prefab, origin, parentToOrigin));
            }
            return returnList;
        }
        /// <summary>
        ///     Utility function that instantiates one of the graphics from the Spawns list. <br></br>
        ///     If parentToOrigin is true, the new graphics will be parented to the origin Transform.
        ///     If false, they are positioned/oriented the same way as the origin but aren't parented.
        /// </summary>
        protected SpecialFXGraphic InstantiateSpecialFXGraphic(GameObject prefab, Transform origin, bool parentToOrigin)
        {
            if (prefab.GetComponent<SpecialFXGraphic>() == null)
            {
                throw new System.Exception($"One of the Spawns on the action {this.name} does not have a SpecialFXGraphic component and can't be instantiated");
            }
            GameObject graphicsGO = GameObject.Instantiate(prefab, origin.transform.position, origin.transform.rotation, (parentToOrigin ? origin.transform : null));
            return graphicsGO.GetComponent<SpecialFXGraphic>();
        }

        public virtual void AnticipateActionClient(ClientCharacter clientCharacter)
    }
}