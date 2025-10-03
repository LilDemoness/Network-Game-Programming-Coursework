using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions
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
        [field: System.NonSerialized] public ActionID ActionID { get; private set; }
        public void SetActionID(int newID) => ActionID = new ActionID() { ID = newID };

        
        [Tooltip("Does this count as a hostile Action? (Should it: Break Stealth, Dropp Shields, etc?)")]
        public bool IsHostileAction;


        public abstract float ExecutionDelay { get; }
        public abstract float RetriggerDelay { get; }


        public abstract bool OnStart(ServerCharacter owner, Vector3 origin, Vector3 direction);
        public abstract bool OnUpdate(ServerCharacter owner, Vector3 origin, Vector3 direction);
        public abstract void OnEnd(ServerCharacter owner, Vector3 origin, Vector3 direction);
        public abstract void OnCancel(ServerCharacter owner, Vector3 origin, Vector3 direction);

        public abstract bool OnStartClient(ClientCharacter clientCharacter, Vector3 origin, Vector3 direction);
        public abstract bool OnUpdateClient(ClientCharacter clientCharacter, Vector3 origin, Vector3 direction);
        public abstract void OnEndClient(ClientCharacter clientCharacter, Vector3 origin, Vector3 direction);
        public abstract void OnCancelClient(ClientCharacter clientCharacter, Vector3 origin, Vector3 direction);



        public abstract bool ShouldNotifyClient();

        public abstract bool CancelsOtherActions { get; }
        public abstract bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData);

        public abstract bool ShouldBecomeNonBlocking(float timeRunning);
        public abstract bool HasExpired(float timeStarted);

        public abstract bool HasCooldown();
        public abstract bool HasCooldownCompleted(float lastActivationTime);
    }
}