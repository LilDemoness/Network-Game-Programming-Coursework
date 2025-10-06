using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions
{
    [CreateAssetMenu(menuName = "Actions/New Weapons Testing Action")]
    public class WeaponsTestingLogAction : Action
    {
        [SerializeField] private string _weaponName;

        [Space(5)]
        [SerializeField] private bool _displayCancelLog = true;


        public override bool OnStart(ServerCharacter owner)
        {
            Debug.Log($"{this.name} {(Data.SlotIdentifier != 0 ? $"in slot {Data.SlotIdentifier}" : "")} says: Started Firing \"{_weaponName}\"");
            return ActionConclusion.Continue;
        }

        public override bool OnUpdate(ServerCharacter owner)
        {
            return ActionConclusion.Continue;
        }
        public override void Cancel(ServerCharacter owner)
        {
            base.Cancel(owner);
            if (_displayCancelLog)
                Debug.Log($"Cancelling Action: {this.name} (Slot: {this.Data.SlotIdentifier})");
        }
    }
}