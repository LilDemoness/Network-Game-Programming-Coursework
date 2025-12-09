using Gameplay.GameplayObjects.Character;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Actions
{
    /// <summary>
    ///     A class responsible for playing action visuals on clients.
    /// </summary>
    public class ClientActionPlayer
    {
        /// <summary>
        ///     The currently active actions that are using client visuals.
        /// </summary>
        private List<Action> _playingActions = new List<Action>();

        /// <summary>
        ///     Don't let anticipated Action FXs persist for longer than this value.
        ///     This acts as a safeguard against scenarios where we never get a confirmed action for one we anticipated.
        /// </summary>
        private const float ANTICIPATION_TIMEOUT_SECONDS = 1.0f;


        /// <summary>
        ///     Maps an AttachmentSlotIndex to the time when the charge percentage will reach 0%.
        /// </summary>
        private Dictionary<AttachmentSlotIndex, float> _slotIndexToChargeDepletedTimeDict = new Dictionary<AttachmentSlotIndex, float>();

        public ClientCharacter ClientCharacter { get; private set;}


        public ClientActionPlayer(ClientCharacter clientCharacter)
        {
            this.ClientCharacter = clientCharacter;
        }

        public void OnUpdate()
        {
            // Loop in reverse to allow for easier removal.
            for(int i = _playingActions.Count - 1; i >= 0; --i)
            {
                Action action = _playingActions[i];

                // Determine if we should end the action.
                if (!UpdateAction(action, out bool hasTimedOut))
                {
                    // End the action.
                    if (hasTimedOut)
                    {
                        // An anticipated action that timed out shouldn't get its End() function called. It should be cancelled instead.
                        CancelAction(action);
                    }
                    else
                        action.EndClient(ClientCharacter);

                    _playingActions.RemoveAt(i);
                    ActionFactory.ReturnAction(action);
                }
            }
        }
        /// <summary>
        ///     Calls a given action's OnUpdateClient(), and decides if the action is still alive.
        /// </summary>
        /// <returns> True if the action is still alive, false if it's dead.</returns>
        private bool UpdateAction(Action action, out bool hasTimedOut)
        {
            bool shouldKeepGoing = action.AnticipatedClient || action.OnUpdateClient(ClientCharacter);    // Only update the action if we are past anticipation.
            hasTimedOut = action.AnticipatedClient && action.TimeRunning >= ANTICIPATION_TIMEOUT_SECONDS;

            if (action.IsGhost)
                return !action.CanBeCancelled();
            
            return shouldKeepGoing && !action.HasExpired && !hasTimedOut;
        }

        /// <summary> A helper wrapper for a FindIndex call on _playingActions.</summary>
        private int FindAction(ActionID actionID, bool anticipatedOnly) => _playingActions.FindIndex(a => a.ActionID == actionID && (!anticipatedOnly || a.AnticipatedClient));
        
        public void OnAnimEvent(string id)
        {
            foreach(Action actionFX in _playingActions)
            {
                actionFX.OnAnimEventClient(ClientCharacter, id);
            }
        }


        /// <summary>
        ///     Called on the client that owns the Character when the player triggers an action.
        ///     This allows for actions to immediately start playing feedback.
        /// </summary>
        public void AnticipateAction(ref ActionRequestData data)
        {
            if (!ClientCharacter.IsAnimating() && Action.ShouldClientAnticipate(ClientCharacter, ref data))
            {
                Action actionFX = ActionFactory.CreateActionFromData(ref data);
                actionFX.AnticipateActionClient(ClientCharacter, ref data);
                _playingActions.Add(actionFX);
            }
        }


        /// <summary>
        ///     Start playing an action based on the given ActionRequestData.
        /// </summary>
        /// <param name="serverTimeStarted"> The time on the server that this action was started.<br/>Used for synchronisation.</param>
        public void PlayAction(ref ActionRequestData data, float serverTimeStarted)
        {
            // If the action was anticipated and is therefore already playing, use the existing action instance rather than creating a new one.
            int anticipatedActionIndex = FindAction(data.ActionID, true);

            Action action = anticipatedActionIndex >= 0 ? _playingActions[anticipatedActionIndex] : ActionFactory.CreateActionFromData(ref data);
            _slotIndexToChargeDepletedTimeDict.TryGetValue(action.Data.AttachmentSlotIndex, out float chargeDepletedTime);
            if (action.OnStartClient(ClientCharacter, chargeDepletedTime, serverTimeStarted))
            {
                if (anticipatedActionIndex < 0)
                {
                    _playingActions.Add(action);
                }
            }
            else
            {
                // Start returned false. The actionFX shouldn't persist.
                if (anticipatedActionIndex >= 0)
                    _playingActions.RemoveAt(anticipatedActionIndex);
                ActionFactory.ReturnAction(action);
            }
        }
        /// <summary>
        ///     Cancel all playing actions.
        /// </summary>
        public void CancelAllActions()
        {
            foreach(Action action in _playingActions)
            {
                CancelAction(action);
                ActionFactory.ReturnAction(action);
            }
            _playingActions.Clear();
        }

        /// <summary>
        ///     Cancel all playing actions that match the given parameters.
        /// </summary>
        /// <param name="actionID"> The <see cref="ActionID"/> of the action to cancel.</param>
        /// <param name="slotIndex"> The <see cref="SlotIndex"/> of the action to cancel.</param>
        /// <param name="exceptThis"> The action you don't wish to cancel.</param>
        public void CancelRunningActionsByID(ActionID actionID, AttachmentSlotIndex slotIndex = AttachmentSlotIndex.Unset, Action exceptThis = null, bool forceCancel = false)
        {
            bool ShouldCancelFunc(Action action) => action.ActionID == actionID && action != exceptThis && (slotIndex == AttachmentSlotIndex.Unset || action.Data.AttachmentSlotIndex == slotIndex);
            CancelActiveActions(ShouldCancelFunc, forceCancel);
        }
        /// <summary>
        ///     Cancel all actions for the given <see cref="AttachmentSlotIndex"/>.
        /// </summary>
        public void CancelRunningActionsBySlotID(AttachmentSlotIndex slotIndex, bool forceCancel = false)
        {
            bool ShouldCancelFunc(Action action) => action.Data.AttachmentSlotIndex == slotIndex;
            CancelActiveActions(ShouldCancelFunc, forceCancel);
        }

        /// <summary>
        ///     Cancel all actions based on the passed condition function.
        /// </summary>
        /// <param name="cancelCondition"> The cancel condition we are checking against.</param>
        private void CancelActiveActions(System.Func<Action, bool> cancelCondition, bool forceCancel)
        {
            for (int i = _playingActions.Count - 1; i >= 0; --i)
            {
                Action action = _playingActions[i];
                if (!forceCancel && !action.CanBeCancelled())
                {
                    action.IsGhost = true;  // Cancel the action the next tick after it can be cancelled.
                    continue;
                }
                if (!cancelCondition(action))
                    continue;

                // Cancel the action instantly.
                CancelAction(action);
                _playingActions.RemoveAt(i);
                ActionFactory.ReturnAction(action);
            }
        }

        /// <summary>
        ///     A helper function to properly cancel the passed action.
        /// </summary>
        /// <remarks> Doesn't remove the Action from its associated list.</remarks>
        private void CancelAction(Action actionToCancel)
        {
            // Cancel the action.
            actionToCancel.CancelClient(ClientCharacter, out float chargeDepletedTime);

            // Charge Reduction Time.
            if (!_slotIndexToChargeDepletedTimeDict.TryAdd(actionToCancel.Data.AttachmentSlotIndex, chargeDepletedTime))
            {
                _slotIndexToChargeDepletedTimeDict[actionToCancel.Data.AttachmentSlotIndex] = chargeDepletedTime;
            }
        }
    }
}