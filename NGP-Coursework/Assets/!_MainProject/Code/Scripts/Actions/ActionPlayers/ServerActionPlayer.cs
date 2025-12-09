using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using System;

namespace Gameplay.Actions
{
    /// <summary>
    ///     A class responsible for playing back action inputs from a user.
    /// </summary>
    public class ServerActionPlayer
    {
        private ServerCharacter _serverCharacter;
        private ServerCharacterMovement _movement;

        private List<Action> _actionQueue;
        private List<Action> _nonBlockingActions;

        private Dictionary<AttachmentSlotIndex, float> _slotIndexToChargeDepletedTimeDict = new Dictionary<AttachmentSlotIndex, float>();

        private struct ActionIDSlotIndexWrapper : System.IEquatable<ActionIDSlotIndexWrapper>
        {
            public ActionID ActionID;
            public AttachmentSlotIndex AttachmentSlotIndex;

            public void SetValues(ActionID id, AttachmentSlotIndex slotIndex)
            {
                this.ActionID = id;
                this.AttachmentSlotIndex = slotIndex;
            }
            public bool Equals(ActionIDSlotIndexWrapper other) => ActionID == other.ActionID && AttachmentSlotIndex == other.AttachmentSlotIndex;
        }
        private Dictionary<ActionIDSlotIndexWrapper, float> _actionCooldownCompleteTime;  // Stores when the Action with the associated ActionID & Slot Identifier will finish its cooldown.
        private ActionIDSlotIndexWrapper _timestampComparison;



        private ActionRequestData _pendingSynthesisedAction = ActionRequestData.Default;  // A synthesised action is an action that was created in order to allow another action to occur (E.g. Charging towards a target in order to perform a melee attack).
        private bool _hasPendingSynthesisedAction;


        public event System.Action OnActionQueueFilled;
        public event System.Action OnActionQueueEmptied;


        public ServerActionPlayer(ServerCharacter serverCharacter)
        {
            this._serverCharacter = serverCharacter;
            this._movement = serverCharacter.Movement;

            _actionQueue = new List<Action>();
            _nonBlockingActions = new List<Action>();
            _actionCooldownCompleteTime = new Dictionary<ActionIDSlotIndexWrapper, float>();
            _hasPendingSynthesisedAction = false;
        }


        /// <summary>
        ///     Perform a sequence of actions.
        /// </summary>
        /// <returns> True if a new action was queued.</returns>
        public bool PlayAction(ref ActionRequestData action)
        {
            // Check if we should interrupt the active action.
            if (!action.ShouldQueue && _actionQueue.Count > 0 && (/*_actionQueue[0].ActionInterruptible || */_actionQueue[0].CanBeInterruptedBy(action.ActionID)))
            {
                ClearActions(false);
            }


            // Create our action.
            var newAction = ActionFactory.CreateActionFromData(ref action);
            newAction.OnUpdateTriggered += Action_OnUpdateTriggered;
            
            if (CancelExistingActionsForToggle(newAction))
            {
                // We cancelled an action rather than starting a new one.
                ActionFactory.ReturnAction(newAction);
                return false;
            }

            // Cancel any actions that this action should cancel when being queued.
            CancelInterruptedActions(newAction);

            // Add our action to the queue and start it if we don't have other actions.
            _actionQueue.Add(newAction);
            OnActionQueueFilled?.Invoke();
            if(_actionQueue.Count == 1){ StartAction(); }
            return true;
        }
        /// <summary>
        ///     If the passed action is a Toggle action, cancel all active instances of that action and return true.
        /// </summary>
        private bool CancelExistingActionsForToggle(Action action)
        {
            if (action.Definition.ActivationStyle != ActionActivationStyle.Toggle)
                return false; // The action is not a toggleable action, so we shouldn't cancel existing ones.
            
            // Cancel all actions that match our action's ID and Slot ID.
            bool ShouldCancelFunc(Action action) => action.ActionID == action.ActionID && (action.Data.AttachmentSlotIndex == 0 || action.Data.AttachmentSlotIndex == action.Data.AttachmentSlotIndex);
            return CancelActions(ShouldCancelFunc, true, true, true);
        }

        public void ClearActions(bool cancelNonBlocking)
        {
            // Clear the active Action in the Action Queue.
            if (_actionQueue.Count > 0)
            {
                CancelAction(_actionQueue[0]);
                //_actionQueue[0].Cancel(_serverCharacter);
            }

            Action actionToBeCancelled; // So we don't repeatedly allocate space.

            // Clear the non-active Actions in the Action Queue.
            for (int i = _actionQueue.Count - 1; i >= 0; --i)
            {
                actionToBeCancelled = _actionQueue[i];
                _actionQueue.RemoveAt(i);
                TryReturnAction(actionToBeCancelled);
            }
            _actionQueue.Clear();

            OnActionQueueEmptied?.Invoke();


            if (cancelNonBlocking)
            {
                for (int i = _nonBlockingActions.Count - 1; i >= 0; --i)
                {
                    actionToBeCancelled = _nonBlockingActions[i];
                    CancelAction(actionToBeCancelled);
                    //actionToBeCancelled.Cancel(_serverCharacter);
                    _nonBlockingActions.RemoveAt(i);
                    TryReturnAction(actionToBeCancelled);
                }
                _nonBlockingActions.Clear();
            }
        }


        /// <summary>
        ///     Cancels all actions that this action interrupts.
        /// </summary>
        private void CancelInterruptedActions(Action action)
        {
            if (!action.CancelsOtherActions)
                return;


            // Check our active Actions to see if any should be cancelled.
            if (_actionQueue.Count > 0)
            {
                // Only check the active action (Index 0) as the others haven't been started and therefore cannot be interrupted.
                if (action.ShouldCancelAction(ref action.Data, ref _actionQueue[0].Data))
                {
                    // Cancel this action.
                    CancelAction(_actionQueue[0]);
                    AdvanceQueue(false);    // Advance the queue to the next action and remove the current head.
                }
            }

            for(int i = _nonBlockingActions.Count - 1; i >= 0; --i)
            {
                Action nonBlockingAction = _nonBlockingActions[i];
                if (action.ShouldCancelAction(ref action.Data, ref nonBlockingAction.Data))
                {
                    // Cancel this action
                    CancelAction(nonBlockingAction);
                    _nonBlockingActions.RemoveAt(i);
                    TryReturnAction(nonBlockingAction);
                }
            }
        }

        /// <summary>
        ///     If an action is active, fills out the 'data' param and returns true.
        ///     If no action is active, returns false.
        /// </summary>
        /// <remarks>
        ///     This only refers to the blocking action (Index 0).
        ///     Multiple non-blocking actions can be running in the background, and this would still return false.
        /// </remarks>
        public bool GetActiveActionInfo(out ActionRequestData data)
        {
            if (_actionQueue.Count > 0)
            {
                data = _actionQueue[0].Data;
                return true;
            }
            else
            {
                data = new ActionRequestData();
                return false;
            }
        }


        /// <summary>
        ///     Figures out if an action can be played now, or if it would automatically fail because it was used too recently.
        /// </summary>
        /// <returns> True if the action is still on cooldown, false if it can be triggered.</returns>
        public bool IsActionOnCooldown(ActionID actionID, AttachmentSlotIndex slotIndex)
        {
            _timestampComparison.SetValues(actionID, slotIndex);

            return _actionCooldownCompleteTime.TryGetValue(_timestampComparison, out float cooldownCompleteTime)    // True if we have a cooldown time.
                && NetworkManager.Singleton.ServerTime.TimeAsFloat <= cooldownCompleteTime;                                                               // True if our cooldown time hasn't yet passed.
        }
        /// <inheritdoc cref=" IsActionOnCooldown(ActionID, AttachmentSlotIndex)"/>
        private bool IsActionOnCooldown(Action action) => IsActionOnCooldown(action.ActionID, action.Data.AttachmentSlotIndex);


        /// <summary>
        ///     Returns how many actions are actively running, including all non-blocking actions and the one blocking action at the head of the queue (If it exists).
        /// </summary>
        public int RunningActionCount => _nonBlockingActions.Count + (_actionQueue.Count > 0 ? 1 : 0);


        /// <summary>
        ///     Starts the action at the head of the queue, if any.
        /// </summary>
        private void StartAction()
        {
            if (_actionQueue.Count <= 0)
            {
                // The Action Queue has been emptied
                OnActionQueueEmptied?.Invoke();
                return;
            }

            if (IsActionOnCooldown(_actionQueue[0]))
            {
                // We've used this action too recently.
                Debug.LogWarning("Action on Cooldown - Implement Feedback to Player");
                AdvanceQueue(false);    // Note: This calls 'StartAction()' recursively if there is more stuff in the queue.
                return;
            }


            // (Functions for AI Agents).
            //int index = SynthesiseTargetIfNeccessary(0);
            //SynthesiseChaseIfNeccessary(index);

            // Cancel any actions that this action should cancel when being played.
            CancelInterruptedActions(_actionQueue[0]);

            _slotIndexToChargeDepletedTimeDict.TryGetValue(_actionQueue[0].Data.AttachmentSlotIndex, out float chargeDepletedTime);

            _actionQueue[0].TimeStarted = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            bool play = _actionQueue[0].OnStart(_serverCharacter, chargeDepletedTime);
            if (!play)
            {
                // Actions that exit in their "Start" method don't have their "End" method called by design.
                AdvanceQueue(false);    // Note: This calls 'StartAction()' recursively if there is more stuff in the queue.
                return;
            }

            if (_actionQueue[0].ShouldBecomeNonBlocking())
            {
                // This is a non-blocking action with no execute time. It should never have be at the front of the queue because a new action coming in could cause it to be cleared.
                _nonBlockingActions.Add(_actionQueue[0]);
                AdvanceQueue(false);    // Note: This calls 'StartAction()' recursively if there is more stuff in the queue.
                return;
            }
        }

        /// <summary>
        ///     Synthesises a Chase Action for the action at the head of the queue, if neccessary
        ///     (The Base Action must have a taret, and must have the 'ShouldClose' flag set).
        /// </summary>
        /// <remarks>
        ///     This method must not be called if the queue is empty.
        /// </remarks>
        /// <returns> The new index of the Action being operated on.</returns>
        private int SynthesiseChaseIfNeccessary(int baseIndex) => throw new System.NotImplementedException();

        /// <summary>
        ///     Targeted Skills should implicitly set the active target of the character, if not already set.
        /// </summary>
        /// <returns> The new index of the base action.</returns>
        private int SynthesiseTargetIfNeccessary(int baseIndex) => throw new System.NotImplementedException();


        /// <summary>
        ///     Advance to the next action in the queue, optionally ending the currently playing action.
        /// </summary>
        /// <param name="callEndOnRemoved"> If true, we call 'End()' on the removed element.</param>
        private void AdvanceQueue(bool callEndOnRemoved)
        {
            if (_actionQueue.Count > 0)
            {
                // Remove the currently active action.
                if (callEndOnRemoved)
                {
                    _actionQueue[0].End(_serverCharacter);
                    if (_actionQueue[0].ChainIntoNewAction(ref _pendingSynthesisedAction))
                    {
                        _hasPendingSynthesisedAction = true;
                    }
                }

                var action = _actionQueue[0];
                _actionQueue.RemoveAt(0);
                TryReturnAction(action);
            }

            // Try to start the new action (Unless we now have a pending synthesised action that should supercede it).
            if (!_hasPendingSynthesisedAction || _pendingSynthesisedAction.ShouldQueue)
            {
                StartAction();
            }
        }
        
        private void TryReturnAction(Action action)
        {
            if (_actionQueue.Contains(action))
                return;

            if (_nonBlockingActions.Contains(action))
                return;

            action.OnUpdateTriggered -= Action_OnUpdateTriggered;
            ActionFactory.ReturnAction(action);
        }


        public void OnUpdate()
        {
            if (_hasPendingSynthesisedAction)
            {
                _hasPendingSynthesisedAction = false;
                PlayAction(ref _pendingSynthesisedAction);
            }

            UpdateRunningActions();
        }
        private void UpdateRunningActions()
        {
            if (_actionQueue.Count > 0 && _actionQueue[0].ShouldBecomeNonBlocking())
            {
                // The active action is no longer blocking, meaning that it should be moved out of the blocking queue and into the non-blocking one.
                // (We use this for things like projectile attacks so that the projectile can keep flying but the player can start other actions in the meantime).
                _nonBlockingActions.Add(_actionQueue[0]);
                AdvanceQueue(callEndOnRemoved: false);
            }

            // If there's a blocking action, update it.
            if (_actionQueue.Count > 0)
            {
                if (!UpdateAction(_actionQueue[0]))
                {
                    AdvanceQueue(callEndOnRemoved: true);
                }
            }

            // If there are non-blocking actions, update them (Done in reverse order to easily remove expired actions).
            for(int i = _nonBlockingActions.Count - 1; i >= 0; --i)
            {
                Action runningAction = _nonBlockingActions[i];
                if (!UpdateAction(runningAction))
                {
                    // The action has concluded. Remove it.
                    runningAction.End(_serverCharacter);
                    _nonBlockingActions.RemoveAt(i);
                    TryReturnAction(runningAction);
                }
            }
        }


        /// <summary>
        ///     Calls a given action's Update(), and decides if the action is still alive.
        /// </summary>
        /// <returns> True if the action is still alive, false if it's dead.</returns>
        private bool UpdateAction(Action action)
        {
            bool shouldKeepGoing = action.OnUpdate(_serverCharacter);
            if (action.IsGhost)
                return !action.CanBeCancelled();

            return (shouldKeepGoing && !action.HasExpired);
        }


        private void Action_OnUpdateTriggered(Action action)
        {
            // If our action's retrigger delay exceeds its cooldown duration, then start a small cooldown
            //      to prevent the player from being able to cancel and restart the action to activate it faster.
            if (action.Definition.RetriggerDelay > action.Definition.ActionCooldown)
            {
                _timestampComparison.SetValues(action.ActionID, action.Data.AttachmentSlotIndex);
                _actionCooldownCompleteTime[_timestampComparison] = NetworkManager.Singleton.ServerTime.TimeAsFloat + action.Definition.RetriggerDelay;
            }
        }


        /// <summary>
        ///     How much time will it take for all remaining blocking Actions in the queue to play out?
        /// </summary>
        /// <remarks> This is an ESTIMATE. An action may block indefinetely if it wishes.</remarks>
        /// <returns> The total "time depth" of the queue, or how long it would take to play in seconds, if no more actions were added.</returns>
        private float GetQueueTimeDepth() => throw new System.NotImplementedException();


        /// <summary>
        ///     Notify our active actions that we've collided with something.
        /// </summary>
        public void CollisionEntered(Collision collision)
        {
            // Blocking Action.
            if (_actionQueue.Count > 0)
            {
                _actionQueue[0].CollisionEntered(_serverCharacter, collision);
            }

            // Non-blocking Actions.
            for(int i = 0; i < _nonBlockingActions.Count; ++i)
            {
                _nonBlockingActions[i].CollisionEntered(_serverCharacter, collision);
            }
        }

        
        /// <summary>
        ///     Gives all active Actions a change to alter a gameplay varaible.
        /// </summary>
        /// <remarks> Note that this handles both positive alterations ("Buffs") AND negative alterates ("Debuffs"). </remarks>
        /// <param name="buffType"> Which gameplay variable is being calcuated.</param>
        /// <returns> The final ("Buffed") value of the variable.</returns>
        public float GetBuffedValue(Action.BuffableValue buffType)
        {
            float buffedValue = Action.GetUnbuffedValue(buffType);

            if (_actionQueue.Count > 0)
            {
                _actionQueue[0].BuffValue(buffType, ref buffedValue);
            }

            foreach(Action action in _nonBlockingActions)
            {
                action.BuffValue(buffType, ref buffedValue);
            }

            return buffedValue;
        }
        
        
        /// <summary>
        ///     Tells all active Actions that a particular gameplay event happened (Such as being hit, getting healed, dying, etc).
        ///     Actions can then change their behaviour as a result.
        /// </summary>
        /// <param name="activityThatOccured"> The type of event that has occured.</param>
        public virtual void OnGameplayActivity(Action.GameplayActivity activityThatOccured)
        {
            if (_actionQueue.Count > 0)
            {
                _actionQueue[0].OnGameplayActivity(_serverCharacter, activityThatOccured);
            }

            foreach (Action action in _nonBlockingActions)
            {
                action.OnGameplayActivity(_serverCharacter, activityThatOccured);
            }
        }


        /// <summary>
        ///     Cancel all playing actions that match the given parameters.
        /// </summary>
        /// <param name="actionID"> The <see cref="ActionID"/> of the action to cancel.</param>
        /// <param name="slotIndex"> The <see cref="AttachmentSlotIndex"/> of the action to cancel.</param>
        /// <param name="cancelNonBlocking"> Should we also cancel non-blocking actions?</param>
        /// <param name="exceptThis"> The action you don't wish to cancel.</param>
        public void CancelRunningActionsByID(ActionID actionID, AttachmentSlotIndex slotIndex = AttachmentSlotIndex.Unset, bool cancelNonBlocking = true, Action exceptThis = null, bool forceCancel = false)
        {
            bool ShouldCancelFunc(Action action) => action.ActionID == actionID && action != exceptThis && (slotIndex == AttachmentSlotIndex.Unset || action.Data.AttachmentSlotIndex == slotIndex);
            CancelActions(ShouldCancelFunc, false, cancelNonBlocking, forceCancel);
        }
        /// <summary>
        ///     Cancel all actions for the given <see cref="AttachmentSlotIndex"/>.
        /// </summary>
        /// <param name="cancelNonBlocking"> Should we also cancel non-blocking actions?</param>
        public void CancelRunningActionsBySlotID(AttachmentSlotIndex slotIndex, bool cancelNonBlocking, bool forceCancel = false)
        {
            bool ShouldCancelFunc(Action action) => action.Data.AttachmentSlotIndex == slotIndex;
            CancelActions(ShouldCancelFunc, false, cancelNonBlocking, forceCancel);
        }

        /// <summary>
        ///     Cancel all actions based on the passed condition function.
        /// </summary>
        /// <param name="cancelCondition"> The condition that actions will be tested against to determine if they should be cancelled.</param>
        /// <param name="cancelQueuedActions"> Should we cancel Queued Actions?</param>
        /// <param name="cancelNonBlocking"> Should we cancel Non-Blocking Actions?</param>
        /// <returns> True if at least one action was cancelled, otherwise false.</returns>
        private bool CancelActions(System.Func<Action, bool> cancelCondition, bool cancelQueuedActions, bool cancelNonBlocking, bool forceCancel)
        {
            bool hasRemovedAction = false;

            // Blocking Actions.
            if (_actionQueue.Count > 0)
            {
                Action action;
                if (cancelQueuedActions)
                {
                    // Cancel any matching queued actions.
                    for (int i = _actionQueue.Count - 1; i >= 1; --i)
                    {
                        action = _actionQueue[i];
                        if (!forceCancel && !action.CanBeCancelled())
                        {
                            action.IsGhost = true;  // Cancel the action the next tick after it can be cancelled again.
                            continue;
                        }
                        if (!cancelCondition(action))
                            continue;

                        // Cancel the action instantly
                        hasRemovedAction = true;
                        CancelAction(action);
                        _actionQueue.RemoveAt(i);
                        TryReturnAction(action);
                    }
                }

                // Try to cancel the active blocking action.
                action = _actionQueue[0];
                if (!forceCancel && !action.CanBeCancelled())
                {
                    action.IsGhost = true;  // Cancel the action the next tick after it can be cancelled again.
                }
                else if (cancelCondition(action))
                {
                    // The active blocking action should be removed instantly.
                    hasRemovedAction = true;
                    CancelAction(_actionQueue[0]);

                    // Advance the queue (Removes the now cancelled Action '0').
                    AdvanceQueue(false);
                }
            }


            if (cancelNonBlocking)
            {
                // Cancel all matching Non-Blocking Actions.
                for (int i = _nonBlockingActions.Count - 1; i >= 0; --i)
                {
                    Action action = _nonBlockingActions[i];
                    if (!forceCancel && !action.CanBeCancelled())
                    {
                        action.IsGhost = true;  // Cancel the action the next tick after it can be cancelled again.
                        continue;
                    }
                    if (!cancelCondition(action))
                        continue;

                    // Cancel the action instantly.
                    hasRemovedAction = true;
                    CancelAction(action);
                    _nonBlockingActions.RemoveAt(i);
                    TryReturnAction(action);
                }
            }

            return hasRemovedAction;
        }


        /// <summary>
        ///     A helper function to properly cancel the passed action.
        /// </summary>
        /// <remarks> Doesn't remove the Action from its associated list.</remarks>
        private void CancelAction(Action actionToCancel)
        {
            bool tryStartNextAction = _actionQueue.Count > 0 && actionToCancel == _actionQueue[0];  // If this is the blocking action, cache our desire to start the next action in the queue.

            // Cancel the action.
            actionToCancel.Cancel(_serverCharacter, out float chargeDepletedTime);

            // Calculate & Save Charge Depletion Time.
            if (!_slotIndexToChargeDepletedTimeDict.TryAdd(actionToCancel.Data.AttachmentSlotIndex, chargeDepletedTime))
            {
                _slotIndexToChargeDepletedTimeDict[actionToCancel.Data.AttachmentSlotIndex] = chargeDepletedTime;
            }

            // Action Cooldown.
            if (actionToCancel.HasCooldown)
            {
                _timestampComparison.SetValues(actionToCancel.ActionID, actionToCancel.Data.AttachmentSlotIndex);

                // Calculate the desired cooldown time.
                float cooldownCompleteTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + actionToCancel.Definition.ActionCooldown;
                if (_actionCooldownCompleteTime.TryGetValue(_timestampComparison, out float currentCooldownCompleteTime))
                {
                    // Our current cooldown time may be greater than our desired one (Such as if our retrigger delay is larger than our cooldown time).
                    // Use the larger value.
                    if (currentCooldownCompleteTime > cooldownCompleteTime)
                        Debug.Log("Current Value exceeds desired");
                    cooldownCompleteTime = Mathf.Max(currentCooldownCompleteTime, cooldownCompleteTime);
                }

                // Set our cooldown complete time.
                if (!_actionCooldownCompleteTime.TryAdd(_timestampComparison, cooldownCompleteTime))
                    _actionCooldownCompleteTime[_timestampComparison] = cooldownCompleteTime;
            }

            if (tryStartNextAction)
                StartAction(); // The cancelled action was the blocking action. Start the next action in the queue.
        }
    }
}