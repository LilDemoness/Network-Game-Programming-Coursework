using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions
{
    [CreateAssetMenu(menuName = "Actions/New Log Action")]
    public class LogAction : Action
    {
        public override bool ShouldNotifyClient => false;
        [SerializeField] private string _debugMessage;

        protected override bool HandleStart(ServerCharacter owner)
        {
            Debug.Log($"{this.name} {(Data.SlotIdentifier != 0 ? $"in slot {Data.SlotIdentifier}" : "")} says: {_debugMessage}");
            return ActionConclusion.Stop;
        }

        protected override bool HandleUpdate(ServerCharacter owner)
        {
            throw new System.Exception("A LogAction has made it to a point where it's 'OnUpdate' method has been called.");
        }



        public override bool HasCooldown => false;
        public override bool HasCooldownCompleted(float lastActivatedTime) => true;
        public override bool HasExpired => true;
        public override bool ShouldBecomeNonBlocking() => true;
    }
}