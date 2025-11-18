using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Definitions
{
    [CreateAssetMenu(menuName = "Actions/Testing/Log Action")]
    public class LogAction : ActionDefinition
    {
        [SerializeField] private string _debugMessage;

        public override bool OnStart(ServerCharacter owner, ref ActionRequestData data)
        {
            Debug.Log($"{this.name} {(data.AttachmentSlotIndex != 0 ? $"in slot {data.AttachmentSlotIndex}" : "")} says: {_debugMessage}");
            return ActionConclusion.Stop;
        }

        public override bool OnUpdate(ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            throw new System.Exception("A LogAction has made it to a point where it's 'OnUpdate' method has been called.");
        }



        public override bool HasCooldown => false;
        public override bool HasCooldownCompleted(float lastActivatedTime) => true;
        public override bool GetHasExpired(float timeStarted) => true;
        public override bool ShouldBecomeNonBlocking(float timeRunning) => true;
    }
}