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
            Debug.Log($"{this.name} {(data.SlotIndex != 0 ? $"in slot {data.SlotIndex}" : "")} says: Started Firing \"{_weaponName}\"");
            return ActionConclusion.Continue;
        }

        public override bool OnUpdate(ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            Debug.Log($"{this.name} {(data.SlotIndex != 0 ? $"in slot {data.SlotIndex}" : "")} says: Updated \"{_weaponName}\"");
            return ActionConclusion.Continue;
        }
        public override void OnEnd(ServerCharacter owner, ref ActionRequestData data)
        {
            if (_displayEndLog)
                Debug.Log($"Ending Action: {this.name} (Slot: {data.SlotIndex})");
            base.OnEnd(owner, ref data);
        }
        public override void OnCancel(ServerCharacter owner, ref ActionRequestData data)
        {
            if (_displayCancelLog)
                Debug.Log($"Cancelling Action: {this.name} (Slot: {data.SlotIndex})");
            base.OnCancel(owner, ref data);
        }
    }
}