using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Targeting;
using Gameplay.Actions.Effects;

namespace Gameplay.Actions
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Actions/New Default Action", order = 0)]
    public class DefaultActionDefinition : ActionDefinition
    {
        // Targeting (Change so that Action Effects determine their own targeting?).
        [SerializeReference, SubclassSelector] public ActionTargeting PrimaryTargetingType = new SelfTargeting();


        [Header("Assorted Action Stuff")]
        public float Cost;              // How much energy/ammo/etc this Action costs.


        [Header("Timings")]
        public float m_executionDelay;
        public override float ExecutionDelay => m_executionDelay;
        public float ActionCooldown;


        [Header("Charging Settings")]
        public bool CanChargeAction;
        public float MaxChargeTime;
        public bool ExecuteIfNotFullAtCharge;


        [Header("Active Timings")]
        public float MaxActiveDuration; // How long this effect can be held as active for (E.g. Max Input Held Duration)
        public float m_retriggerDelay;    // The delay between valid OnUpdate calls
        public override float RetriggerDelay => m_retriggerDelay;



        [Header("Animation Triggers")] // (For Owner. Target Animations are in an ActionEffect).



        [Header("Action Effects")]
        [SerializeReference, SubclassSelector] public List<ActionEffect> ImmediateEffects;
        private bool _hasInitialEffects;
        
        [Space(5)]
        [SerializeReference, SubclassSelector] public List<ActionEffect> ExecutionEffects;
        private bool _hasExecutionEffects;

        [Space(5)]
        [SerializeReference, SubclassSelector] public List<ActionEffect> EndEffects;
        private bool _hasEndEffects;

        [Space(5)]
        [SerializeReference, SubclassSelector] public List<ActionEffect> CancelledEffects;
        private bool _hasCancelledEffects;


        [Header("Interruption")]
        [Tooltip("Is this Action interruptible by other action-plays or by movement? (Implicitly stops movement when action starts.) Generally, actions with short exec times should not be interruptible in this way.")]
        public bool ActionInterruptible;

        [Tooltip("This action is interrupted if any of the following actions is requested")]
        public List<ActionDefinition> IsInterruptableBy;


        [Header("Cancellation")]
        [Tooltip("Does this action cancel other actions when starting?")]
        private bool m_cancelsOtherActions;
        public override bool CancelsOtherActions => m_cancelsOtherActions;

        [Tooltip("The actions that this Action automatically interrupts (Used for 'ActionLogic.Cancelling' type actions).")]
        public List<ActionDefinition> OtherActionsThisCancels;

        [Tooltip("Can this Action only interrupt other Actions if they share the same Slot Identifier?")]
        public bool RequireSharedSlotIdentifier;    // E.g. Used for Weapon Cancelling so that if the entity has multiple of the same weapons they can cancel firing one instance but not the others.


        [Space(5)]
        public Action FollowingAction;  // An action that occurs when this action ends, at the endpoint of its targeting (E.g. Projectile Hit Position).


#if UNITY_EDITOR

        private void OnValidate()
        {
            _hasInitialEffects      = ImmediateEffects.Count > 0;
            _hasExecutionEffects    = ExecutionEffects.Count > 0;
            _hasEndEffects          = EndEffects.Count > 0;
            _hasCancelledEffects    = CancelledEffects.Count > 0;
        }

#endif


        public bool CanBeInterruptedBy(ActionID actionActionID)
        {
            foreach (var action in IsInterruptableBy)
            {
                if (action.ActionID == actionActionID)
                {
                    return true;
                }
            }

            return false;
        }
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



        #if UNITY_EDITOR

        [ContextMenu("Clear Missing References")]
        private void ClearMissingReferences() => UnityEditor.SerializationUtility.ClearAllManagedReferencesWithMissingTypes(this);

        #endif



        public override bool OnStart(ServerCharacter owner, Vector3 origin, Vector3 direction)
        {
            if (_hasInitialEffects)
                PrimaryTargetingType.GetTargets(owner, origin, direction, HandleImmediateEffects);
            
            return true;
        }
        private void HandleImmediateEffects(ServerCharacter owner, ulong[] targetIDs)
        {
            foreach (ActionEffect immediateEffect in ImmediateEffects)
                immediateEffect.Apply(owner, targetIDs);
        }

        public override bool OnUpdate(ServerCharacter owner, Vector3 origin, Vector3 direction)
        {
            if (_hasExecutionEffects)
            PrimaryTargetingType.GetTargets(owner, origin, direction, HandleExecutionEffects);
            return true;
        }
        private void HandleExecutionEffects(ServerCharacter owner, ulong[] targetIDs)
        {
            foreach (ActionEffect actionEffect in ExecutionEffects)
                actionEffect.Apply(owner, targetIDs);
        }

        public override void OnEnd(ServerCharacter owner, Vector3 origin, Vector3 direction)
        {
            if (_hasEndEffects)
                PrimaryTargetingType.GetTargets(owner, origin, direction, HandleEndEffects);
        }
        private void HandleEndEffects(ServerCharacter owner, ulong[] targetIDs)
        {
            foreach (ActionEffect effect in EndEffects)
                effect.Apply(owner, targetIDs);
        }

        public override void OnCancel(ServerCharacter owner, Vector3 origin, Vector3 direction)
        {
            if (_hasCancelledEffects)
                PrimaryTargetingType.GetTargets(owner, origin, direction, HandleCancellationEffects);
        }
        private void HandleCancellationEffects(ServerCharacter owner, ulong[] targetIDs)
        {
            foreach (ActionEffect effect in CancelledEffects)
                effect.Apply(owner, targetIDs);
        }


        public override bool ShouldBecomeNonBlocking(float timeRunning) => timeRunning >= ExecutionDelay /*BlockingMode == BlockingModeType.OnlyDuringExecutionTime ? timeRunning >= ExecutionDelay : false*/;


        public override bool HasExpired(float timeStarted)
        {
            bool isExpirable = MaxActiveDuration > 0.0f;  // Non-positive values indicate that the duration is infinite.
            float timeElapsed = Time.time - timeStarted;
            return isExpirable && timeElapsed >= MaxActiveDuration;
        }

        public override bool HasCooldown() => ActionCooldown > 0;
        public override bool HasCooldownCompleted(float lastActivationTime) => (Time.time - lastActivationTime) >= ActionCooldown;
    }
}