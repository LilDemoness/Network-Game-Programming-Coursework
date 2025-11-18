using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Definitions
{
    /// <summary>
    ///     An Action that cancels other Actions when started.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/Cancel Action", order = 0)]
    public class CancelAction : ActionDefinition
    {
        [SerializeField] private List<Action> _actionsThisCancels = new List<Action>();
        [SerializeField] private bool _requireSharedSlotIdentifier = false;


        public override bool OnStart(ServerCharacter owner, ref ActionRequestData data)
        {
            //foreach (ActionDefinition actionDefinition in OtherActionsThisCancels)
            //    owner.ClientCharacter.CancelAllActionsByActionIDClientRpc(actionDefinition.ActionID);

            return ActionConclusion.Stop;
        }

        public override bool OnUpdate(ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            throw new System.Exception("A CancelAction has made it to a point where it's 'OnUpdate' method has been called.");
        }


        public override bool CancelsOtherActions => true;

        public override bool ShouldCancelAction(ref ActionRequestData thisData, ref ActionRequestData otherData)
        {
            return CanCancelAction(otherData.ActionID) && (!_requireSharedSlotIdentifier || thisData.AttachmentSlotIndex == otherData.AttachmentSlotIndex);
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
        public override bool GetHasExpired(float timeStarted) => true;
        public override bool ShouldBecomeNonBlocking(float timeRunning) => true;
    }
}