using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions
{
    [CreateAssetMenu(menuName = "Actions/New Weapons Testing Action")]
    public class WeaponsTestingLogAction : DefaultAction
    {
        [SerializeField] private string _weaponName;

        [Space(5)]
        [SerializeField] private bool _displayEndLog = true;
        [SerializeField] private bool _displayCancelLog = true;


        protected override bool HandleStart(ServerCharacter owner)
        {
            Debug.Log($"{this.name} {(Data.SlotIdentifier != 0 ? $"in slot {Data.SlotIdentifier}" : "")} says: Started Firing \"{_weaponName}\"");
            return ActionConclusion.Continue;
        }

        protected override bool HandleUpdate(ServerCharacter owner)
        {
            Debug.Log($"{this.name} {(Data.SlotIdentifier != 0 ? $"in slot {Data.SlotIdentifier}" : "")} says: Updated \"{_weaponName}\"");
            return ActionConclusion.Continue;
        }
        public override void End(ServerCharacter owner)
        {
            if (_displayEndLog)
                Debug.Log($"Ending Action: {this.name} (Slot: {this.Data.SlotIdentifier})");
            base.End(owner);
        }
        public override void Cancel(ServerCharacter owner)
        {
            if (_displayCancelLog)
                Debug.Log($"Cancelling Action: {this.name} (Slot: {this.Data.SlotIdentifier})");
            base.Cancel(owner);
        }
    }
}