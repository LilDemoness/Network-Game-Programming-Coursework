using Gameplay.GameplayObjects.Character;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Actions
{
    public class ClientActionPlayer
    {
        private List<Action> _playingActions = new List<Action>();

        /// <summary>
        ///     Don't let anticipated Action FXs persist for longer than this value.
        ///     This acts as a safeguard against scenarios where we never get a confirmed action for one we anticipated.
        /// </summary>
        private const float ANTICIPATION_TIMEOUT_SECONDS = 1.0f;


        private Dictionary<int, float> _slotIDToChargeDepletedTimeDict = new Dictionary<int, float>();

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

                // Calculate values for Ending the Action FX.
                bool shouldKeepGoing = action.AnticipatedClient || action.OnUpdateClient(ClientCharacter);    // Only update the action if we are past anticipation.
                bool hasExpired = action.HasExpired;
                bool hasTimedOut = action.AnticipatedClient && action.TimeRunning >= ANTICIPATION_TIMEOUT_SECONDS;


                if (!shouldKeepGoing || hasExpired || hasTimedOut)
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
        /// <param name="data"> The action that is being requested.</param>
        public void AnticipateAction(ref ActionRequestData data)
        {
            if (!ClientCharacter.IsAnimating() && Action.ShouldClientAnticipate(ClientCharacter, ref data))
            {
                Action actionFX = ActionFactory.CreateActionFromData(ref data);
                actionFX.AnticipateActionClient(ClientCharacter);
                _playingActions.Add(actionFX);
            }
        }


        public void PlayAction(ref ActionRequestData data, float serverTimeStarted)
        {
            int anticipatedActionIndex = FindAction(data.ActionID, true);

            Action action = anticipatedActionIndex>= 0 ? _playingActions[anticipatedActionIndex] : ActionFactory.CreateActionFromData(ref data);
            _slotIDToChargeDepletedTimeDict.TryGetValue(action.Data.SlotIdentifier, out float chargeDepletedTime);
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
        public void CancelAllActions()
        {
            foreach(Action action in _playingActions)
            {
                CancelAction(action);
                ActionFactory.ReturnAction(action);
            }
            _playingActions.Clear();
        }

        public void CancelRunningActionsByID(ActionID actionID, int slotIdentifier = 0, Action exceptThis = null)
        {
            bool ShouldCancelFunc(Action action) => action.ActionID == actionID && action != exceptThis && (slotIdentifier == 0 || action.Data.SlotIdentifier == slotIdentifier);
            CancelActiveActions(ShouldCancelFunc);
        }
        public void CancelRunningActionsBySlotID(int slotIdentifier)
        {
            if (slotIdentifier <= 0)
                throw new System.ArgumentException($"You are trying to cancel actions with an invalid SlotIdentifier ({slotIdentifier}). Use a value >= 1.");

            bool ShouldCancelFunc(Action action) => action.Data.SlotIdentifier == slotIdentifier;
            CancelActiveActions(ShouldCancelFunc);
        }

        private void CancelActiveActions(System.Func<Action, bool> cancelCondition)
        {
            for (int i = _playingActions.Count - 1; i >= 0; --i)
            {
                Action action = _playingActions[i];
                if (!cancelCondition(action))
                    continue;

                CancelAction(action);
                _playingActions.RemoveAt(i);
                ActionFactory.ReturnAction(action);
            }
        }

        private void CancelAction(Action actionToCancel)
        {
            // Cancel the action.
            actionToCancel.CancelClient(ClientCharacter, out float chargeDepletedTime);

            // Charge Reduction Time.
            if (!_slotIDToChargeDepletedTimeDict.TryAdd(actionToCancel.Data.SlotIdentifier, chargeDepletedTime))
            {
                _slotIDToChargeDepletedTimeDict[actionToCancel.Data.SlotIdentifier] = chargeDepletedTime;
            }
        }
    }
}