using System.Collections.Generic;
using UnityEngine;
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
        private Dictionary<ActionID, float> _actionLastUsedTimestamps;  // Stores when the Action with the associated ActionID was last used.



        private ActionRequestData _pendingSynthesisedAction = ActionRequestData.Default;  // A synthesised action is an action that was created in order to allow another action to occur (E.g. Charging towards a target in order to perform a melee attack).
        private bool _hasPendingSynthesisedAction;


        public ServerActionPlayer(ServerCharacter serverCharacter)
        {
            this._serverCharacter = serverCharacter;
            this._movement = serverCharacter.Movement;

            _actionQueue = new List<Action>();
            _nonBlockingActions = new List<Action>();
            _actionLastUsedTimestamps = new Dictionary<ActionID, float>();
            _hasPendingSynthesisedAction = false;
        }


        /// <summary>
        ///     Perform a sequence of actions.
        /// </summary>
        public void PlayAction(ref ActionRequestData action)
        {
            // Check if we should interrupt the active action.
            if (!action.ShouldQueue && _actionQueue.Count > 0 && (_actionQueue[0].Config.ActionInterruptible || _actionQueue[0].Config.CanBeInterruptedBy(action.ActionID)))
            {
                ClearActions(false);
            }


            // Create our action.
            var newAction = ActionFactory.CreateActionFromData(ref action);

            // Cancel any actions that this action should cancel when being queued.
            CancelInterruptedActions(newAction);

            // Add our action to the queue and start it if we don't have other actions.
            _actionQueue.Add(newAction);
            if(_actionQueue.Count == 1) { StartAction(); }
        }

        public void ClearActions(bool cancelNonBlocking) => throw new System.NotImplementedException();


        /// <summary>
        ///     Cancels all actions that this action interrupts.
        /// </summary>
        private void CancelInterruptedActions(Action action)
        {
            if (!action.Config.CancelsOtherActions || action.Config.OtherActionsThisCancels.Count <= 0)
                return;

            // Check our active Actions to see if any should be cancelled.
            if (_actionQueue.Count > 0)
            {
                if (action.Config.CanCancelAction(_actionQueue[0].ActionID) && (!action.Config.RequireSharedSlotIdentifier || action.Data.SlotIdentifier == _actionQueue[0].Data.SlotIdentifier))
                {
                    // Cancel this action.
                    _actionQueue[0].Cancel(_serverCharacter);
                    AdvanceQueue(false);
                }
            }

            for(int i = _nonBlockingActions.Count - 1; i >= 0; --i)
            {
                Action nonBlockingAction = _nonBlockingActions[i];
                if (action.Config.CanCancelAction(nonBlockingAction.ActionID) && (!action.Config.RequireSharedSlotIdentifier || action.Data.SlotIdentifier == nonBlockingAction.Data.SlotIdentifier))
                {
                    // Cancel this action
                    nonBlockingAction.Cancel(_serverCharacter);
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
        ///     This only refers to the blocking action.
        ///     Multiple non-blocking actions can be running in the background, and this would still return false.
        /// </remarks>
        public bool GetActiveActionInfo(out ActionRequestData data) => throw new System.NotImplementedException();


        /// <summary>
        ///     Figures out if an action can be played now, or if it would automatically fail because it was used too recently.
        /// </summary>
        /// <param name="actionID"> The action we want to run.</param>
        /// <returns> True if the action can be run now, false if more time must elapse before this action can be run.</returns>
        public bool IsReuseTimeElapsed(ActionID actionID) => throw new System.NotImplementedException();


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
                return;

            float reuseTime = _actionQueue[0].Config.ReuseTimeSeconds;
            if (reuseTime > 0.0f && _actionLastUsedTimestamps.TryGetValue(_actionQueue[0].ActionID, out float lastTimeUsed) && Time.time - lastTimeUsed < reuseTime)
            {
                // We've used this action too recently.
                AdvanceQueue(false);    // Note: This calls 'StartAction()' recursively if there is more stuff in the queue.
                return;
            }


            //int index = SynthesiseTargetIfNeccessary(0);
            //SynthesiseChaseIfNeccessary(index);

            // Cancel any actions that this action should cancel when being played.
            CancelInterruptedActions(_actionQueue[0]);

            _actionQueue[0].TimeStarted = Time.time;
            bool play = _actionQueue[0].OnStart(_serverCharacter);
            if (!play)
            {
                // Actions that exit in their "Start" method don't have their "End" method called by design.
                AdvanceQueue(false);    // Note: This calls 'StartAction()' recursively if there is more stuff in the queue.
                return;
            }

            _actionLastUsedTimestamps[_actionQueue[0].ActionID] = Time.time;

            if (_actionQueue[0].Config.ExecuteTimeSeconds == 0 && _actionQueue[0].Config.BlockingMode == BlockingModeType.OnlyDuringExecutionTime)
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

            ActionFactory.ReturnAction(action);
        }


        public void OnUpdate()
        {
            if (_hasPendingSynthesisedAction)
            {
                _hasPendingSynthesisedAction = false;
                PlayAction(ref _pendingSynthesisedAction);
            }

            if (_actionQueue.Count > 0 && _actionQueue[0].ShouldBecomeNonBlocking())
            {
                // The active action is no longer blocking, meaning that it should be moved out of the blocking queue and into the non-blocking one.
                // (We use this for things like projectile attacks so that the projectile can keep flying but the player can start other actions in the meantime).
                _nonBlockingActions.Add(_actionQueue[0]);
                AdvanceQueue(false);
            }

            // If there's a blocking action, update it.
            if (_actionQueue.Count > 0)
            {
                if (!UpdateAction(_actionQueue[0]))
                {
                    AdvanceQueue(true);
                }
            }

            // If there are non-blocking actions, update them (Done in reverse order to easily remove expired actions).
            for(int i = _nonBlockingActions.Count - 1; i >= 0; --i)
            {
                Action runningAction = _nonBlockingActions[i];
                if (!UpdateAction(runningAction))
                {
                    // The action has died.
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

            bool expirable = action.Config.DurationSeconds > 0.0f;  // Non-positive values indicate that the duration is infinite.
            float timeElapsed = Time.time - action.TimeStarted;
            bool hasTimeExpired = expirable && timeElapsed >= action.Config.DurationSeconds;

            return shouldKeepGoing && !hasTimeExpired;
        }

        /// <summary>
        ///     How much time will it take for all remaining blocking Actions in the queue to play out?
        /// </summary>
        /// <remarks> This is an ESTIMATE. An action may block indefinetely if it wishes.</remarks>
        /// <returns> The total "time depth" of the queue, or how long it would take to play in seconds, if no more actions were added.</returns>
        private float GetQueueTimeDepth() => throw new System.NotImplementedException();


        public void CollisionEntered(Collision collision)
        {
            if (_actionQueue.Count > 0)
            {
                _actionQueue[0].CollisionEntered(_serverCharacter, collision);
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


        //public bool CancelRunningActionsByLogic(ActionLogic logic, bool cancelAll, Action exceptThis = null) => throw new System.NotImplementedException();
    }
}