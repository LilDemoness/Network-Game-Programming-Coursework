using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions
{
    public abstract class ActionDefinition
    {
        public bool IsHostileAction;    // Does this count as a hostile Action? (E.g. Breaking Stealth, Dropping Shields, etc)


        public abstract float ExecutionDelay { get; }
        public abstract float RetriggerDelay { get; }


        public abstract bool OnStart(ServerCharacter owner);
        public abstract bool OnUpdate(ServerCharacter owner);
        public abstract void OnEnd(ServerCharacter owner);
        public abstract void OnCancel(ServerCharacter owner);


        public abstract bool CancelsOtherActions { get; }
        public abstract bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData);

        public abstract bool ShouldBecomeNonBlocking(float timeRunning);
        public abstract bool HasExpired(float timeStarted);

        public abstract bool HasCooldown();
        public abstract bool HasCooldownCompleted(float lastActivationTime);
    }
}