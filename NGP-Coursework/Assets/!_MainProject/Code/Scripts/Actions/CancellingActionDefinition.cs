using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Actions/New Cancelling Action")]
    public class CancellingActionDefinition : ActionDefinition
    {
        public override bool CancelsOtherActions => true;

        [Tooltip("The actions that this Action automatically interrupts (Used for 'ActionLogic.Cancelling' type actions).")]
        public List<ActionDefinition> OtherActionsThisCancels;

        [Tooltip("Can this Action only interrupt other Actions if they share the same Slot Identifier?")]
        public bool RequireSharedSlotIdentifier;    // E.g. Used for Weapon Cancelling so that if the entity has multiple of the same weapons they can cancel firing one instance but not the others.
        
        public override bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData)
        {
            return CanCancelAction(otherData.ActionID) && (!RequireSharedSlotIdentifier || thisData.SlotIdentifier == otherData.SlotIdentifier);
        }
        public bool CanCancelAction(ActionID otherActionID)
        {
            foreach (var action in OtherActionsThisCancels)
            {
                if (action.ActionID == otherActionID)
                    return true;
            }

            return false;
        }



        public override float ExecutionDelay => 0.0f;
        public override float RetriggerDelay => 1.0f; // Note: Not 0.0f just in case we accidentally get this into an update loop (If it was 0.0 it may run eternally).


        public override bool OnStart(ServerCharacter owner) => false;
        public override bool OnUpdate(ServerCharacter owner) => false;
        public override void OnEnd(ServerCharacter owner) { }
        public override void OnCancel(ServerCharacter owner) { }


        public override bool ShouldBecomeNonBlocking(float timeRunning) => true;
        public override bool HasExpired(float startTime) => true;

        public override bool HasCooldown() => false;
        public override bool HasCooldownCompleted(float lastActivationTime) => true;
    }
}