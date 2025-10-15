using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Definitions
{
    [CreateAssetMenu(menuName = "Actions/Testing/Weapons Testing Action")]
    public class WeaponsTestingLogAction : ActionDefinition
    {
        [SerializeField] private string _weaponName;

        [Space(5)]
        [SerializeField] private bool _displayEndLog = true;
        [SerializeField] private bool _displayCancelLog = true;


        public override bool OnStart(ServerCharacter owner, ref ActionRequestData data)
        {
            Debug.Log($"{this.name} {(data.SlotIdentifier != 0 ? $"in slot {data.SlotIdentifier}" : "")} says: Started Firing \"{_weaponName}\"");
            return ActionConclusion.Continue;
        }

        public override bool OnUpdate(ServerCharacter owner, ref ActionRequestData data)
        {
            Debug.Log($"{this.name} {(data.SlotIdentifier != 0 ? $"in slot {data.SlotIdentifier}" : "")} says: Updated \"{_weaponName}\"");
            return ActionConclusion.Continue;
        }
        public override void OnEnd(ServerCharacter owner, ref ActionRequestData data)
        {
            if (_displayEndLog)
                Debug.Log($"Ending Action: {this.name} (Slot: {data.SlotIdentifier})");
            base.OnEnd(owner, ref data);
        }
        public override void OnCancel(ServerCharacter owner, ref ActionRequestData data)
        {
            if (_displayCancelLog)
                Debug.Log($"Cancelling Action: {this.name} (Slot: {data.SlotIdentifier})");
            base.OnCancel(owner, ref data);
        }
    }
}