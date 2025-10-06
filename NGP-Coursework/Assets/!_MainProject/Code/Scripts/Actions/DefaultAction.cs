using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Actions
{
    /// <summary>
    ///     A base class for all typical action definitions.
    ///     Some types of Action don't inherit from this (E.g. CancelAction).
    /// </summary>
    public abstract class DefaultAction : Action
    {
        //protected abstract bool ShouldNotifyClient { get; }


        [Header("Charging Settings")]
        public bool CanCharge;
        public float MaxChargeTime;
        public bool ExecuteIfNotAtFullCharge;


        [Header("Active Timings")]
        public float RetriggerDelay;
        [System.NonSerialized] protected float NextUpdateTime;


        [Header("Animation Triggers")]


        [Header("Interruption")]
        public bool IsInterruptable;


        public override void Reset()
        {
            base.Reset();
            this.NextUpdateTime = 0;
        }


        public override bool OnStart(ServerCharacter owner)
        {
            NextUpdateTime = TimeStarted + ExecutionDelay;

            // Ensure our Data's perameters are set up.
            InitialiseDataParametersIfEmpty(owner);

            /*
            if (ShouldNotifyClient)
                serverCharacter.ClientCharacter.PlayActionClientRpc(this.Data, TimeStarted);
             */

            DebugForAction("Start", owner);

            return HandleStart(owner);
        }
        public override bool OnUpdate(ServerCharacter owner)
        {
            if (RetriggerDelay <= 0)
            {
                if (NextUpdateTime != -1)
                {
                    Debug.Log("Server Update: " + TimeRunning);

                    // Trigger OnUpdate() once.
                    NextUpdateTime = -1;
                    DebugForAction("Update", owner);
                    return HandleUpdate(owner);
                }
            }
            else
            {
                while (NextUpdateTime < NetworkManager.Singleton.ServerTime.TimeAsFloat)
                {
                    Debug.Log("Server Update: " + TimeRunning);

                    // Play Execution Effects.
                    DebugForAction("Update", owner);
                    if (HandleUpdate(owner) == false)
                        return false;

                    NextUpdateTime += RetriggerDelay;
                }
            }

            return true;
        }


        protected abstract bool HandleStart(ServerCharacter owner);
        protected abstract bool HandleUpdate(ServerCharacter owner);
        protected abstract bool HandleEnd(ServerCharacter owner);
        protected abstract bool HandleCancel(ServerCharacter owner);



        /// <summary>
        ///     Initialise our Data's required Parameters (If they aren't already set-up).
        /// </summary>
        private void InitialiseDataParametersIfEmpty(ServerCharacter owner)
        {
            if (Data.OriginTransformID != 0)
            {
                if (Data.Direction == Vector3.zero)
                    Data.Direction = Vector3.forward;
            }
            else
            {
                if (Data.Direction == Vector3.zero)
                    Data.Direction = owner.transform.forward;
                if (Data.Position == Vector3.zero)
                    Data.Position = owner.transform.position;
            }
        }



        public override bool OnStartClient(ClientCharacter clientCharacter, float serverTimeStarted)
        {
            base.OnStartClient(clientCharacter, serverTimeStarted);
            NextUpdateTime = TimeStarted + ExecutionDelay;

            return true;
        }
    }
}