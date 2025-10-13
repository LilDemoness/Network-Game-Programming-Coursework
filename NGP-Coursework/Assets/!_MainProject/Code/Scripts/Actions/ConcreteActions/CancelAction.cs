using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions
{
    /// <summary>
    ///     An Action that cancels other Actions when started.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/New Cancel Action")]
    public class CancelAction : Action
    {
        public override bool ShouldNotifyClient => false;   // Maybe?
        [SerializeField] private List<Action> _actionsThisCancels = new List<Action>();
        [SerializeField] private bool _requireSharedSlotIdentifier = false;


        protected override bool HandleStart(ServerCharacter owner)
        {
            //foreach (ActionDefinition actionDefinition in OtherActionsThisCancels)
            //    owner.ClientCharacter.CancelAllActionsByActionIDClientRpc(actionDefinition.ActionID);

            return ActionConclusion.Stop;
        }

        protected override bool HandleUpdate(ServerCharacter owner)
        {
            throw new System.Exception("A CancelAction has made it to a point where it's 'OnUpdate' method has been called.");
        }


        public override bool CancelsOtherActions => true;

        public override bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData)
        {
            return CanCancelAction(otherData.ActionID) && (!_requireSharedSlotIdentifier || thisData.SlotIdentifier == otherData.SlotIdentifier);
        }
        private bool CanCancelAction(ActionID otherActionID)
        {
            foreach(Action action in _actionsThisCancels)
            {
                if (action.ActionID == otherActionID)
                    return true;
            }

            return false;
        }

        public override bool HasCooldown => false;
        public override bool HasCooldownCompleted(float lastActivatedTime) => true;
        public override bool HasExpired => true;

        public override bool ShouldBecomeNonBlocking() => true;
    }
}