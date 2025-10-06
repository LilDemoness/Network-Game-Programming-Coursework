using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions
{
    [CreateAssetMenu(menuName = "Actions/New Log Action")]
    public class LogAction : Action
    {
        [SerializeField] private string _debugMessage;

        public override bool OnStart(ServerCharacter owner)
        {
            Debug.Log($"{this.name} {(Data.SlotIdentifier != 0 ? $"in slot {Data.SlotIdentifier}" : "")} says: {_debugMessage}");
            return ActionConclusion.Stop;
        }

        public override bool OnUpdate(ServerCharacter owner)
        {
            throw new System.Exception("A LogAction has made it to a point where it's 'OnUpdate' method has been called.");
        }
    }
}