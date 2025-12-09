using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Effects;
using UnityEngine.UIElements;
using static UnityEngine.Analytics.IAnalytic;

namespace Gameplay.Actions.Definitions
{
    /// <summary>
    ///     An action that uses a raycast to trigger effects on targets from a range.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/Raycast Action", order = 1)]
    public class RangedRaycastAction : ActionDefinition
    {
        [Header("Targeting")]
        [field: SerializeField] public float MaxRange { get; private set; }
        [field: SerializeField] public LayerMask ValidLayers { get; private set; }

        [field: Space(5)]
        [field: SerializeField, Min(0)] public int Pierces { get; private set; } = 0;
        public bool CanPierce => Pierces > 0;



        public override bool OnStart(ServerCharacter owner, ref ActionRequestData data) => ActionConclusion.Continue;
        public override bool OnUpdate(ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            // Handle Logic
            PerformRaycast(owner, ref data, chargePercentage, PrepareAndProcessTarget);

            return ActionConclusion.Continue;
        }


        /// <summary>
        ///     Perform a Raycast and trigger the callback for all hit entities.
        /// </summary>
        private void PerformRaycast(ServerCharacter owner, ref ActionRequestData data, float chargePercentage, System.Action<ServerCharacter, RaycastHit, Vector3, float> onHitCallback)
        {
            // Cache origin information.
            Vector3 rayOrigin = GetActionOrigin(ref data);
            Vector3 rayDirection = GetActionDirection(ref data);

            if (CanPierce)
            {
                // Get all valid targets.
                RaycastHit[] colliders = Physics.RaycastAll(rayOrigin, rayDirection, MaxRange, ValidLayers, QueryTriggerInteraction.Ignore);

                // Order our targets by distance (Closest Target First), and then only store the number we wish to pierce.
                IEnumerable<RaycastHit> orderedValidTargets = colliders.OrderBy(t => (t.point - rayOrigin).sqrMagnitude).Take(Pierces + 1);

                // Loop through and process all valid targets.
                IEnumerator<RaycastHit> enumerator = orderedValidTargets.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    onHitCallback?.Invoke(owner, enumerator.Current, rayDirection, chargePercentage);
                }
            }
            else
            {
                // The action cannot pierce, so find and process only the first hit object.
                // We don't need to retrieve multiple targets, so just use a Raycast for efficiency.
                if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, MaxRange, ValidLayers, QueryTriggerInteraction.Ignore))
                {
                    onHitCallback?.Invoke(owner, hitInfo, rayDirection, chargePercentage);
                }
            }
        }

        /// <summary>
        ///     Prepare the passed data into a form that lets us process a hit target, then process it.
        /// </summary>
        private void PrepareAndProcessTarget(ServerCharacter owner, RaycastHit hitInfo, Vector3 rayDirection, float chargePercentage)
        {
            ActionHitInformation actionHitInfo = new ActionHitInformation(hitInfo.transform, hitInfo.point, hitInfo.normal, GetHitForward(hitInfo.normal));
            ProcessTarget(owner, actionHitInfo, chargePercentage);

            // Helper Function to calculate the HitForward value from a given normal.
            Vector3 GetHitForward(Vector3 hitNormal)
            {
                return Mathf.Approximately(Mathf.Abs(Vector3.Dot(hitNormal, rayDirection)), 1.0f)
                    ? Vector3.Cross(hitNormal, -owner.transform.right)  // The normal & ray direction are approximately perpendicular. Calculate forward using our owner's left vector to get an estimation and prevent a miscalculation.
                    : Vector3.Cross(hitNormal, rayDirection);           // The normal & ray direction aren't perpendicular, so using their Cross product should get us the correct forward.
            }
        }
        /// <summary>
        ///     Process a target hit by this Action.
        /// </summary>
        private void ProcessTarget(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            // Play the contents of HitEffects on all Non-Triggering Clients (Triggering client is handled in anticipation).
            //  This does mean that these clients will have a slight delay in their displaying of hit information, however they will always display accurate information no matter their local values.
            HitEffectManager.PlayHitEffectsOnNonTriggeringClients(owner.OwnerClientId, hitInfo, chargePercentage, ActionID);

            // Perform this action's effects (Damage, Applying Statuses, etc) on the server (Changes are perpetuated to clients).
            for (int i = 0; i < ActionEffects.Length; ++i)
            {
                ActionEffects[i].ApplyEffect(owner, hitInfo, chargePercentage);
            }
        }


        public override bool OnUpdateClient(ClientCharacter clientCharacter, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            // Because OnUpdateClient is called on all clients, regardless of whether they were the triggering one or not,
            //  we need to check whether we are playing on the the triggering client before playing anticipation-based hit effects.
            if (clientCharacter.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                PerformRaycast(null, ref data, chargePercentage, PrepareHitEffectAndNotify_AnticipatedUpdate);

            return base.OnUpdateClient(clientCharacter, ref data, chargePercentage);
        }
        /// <summary>
        ///     Passes the required data to the <see cref="HitEffectManager"/> for displaying hit effects on the triggering client.<br/>
        ///     Only to be called through OnUpdateClient() from the triggering client.
        /// </summary>
        private void PrepareHitEffectAndNotify_AnticipatedUpdate(ServerCharacter _, RaycastHit hitInfo, Vector3 rayDirection, float chargePercentage)
        {
            HitEffectManager.PlayHitEffectsOnSelf(true, hitInfo.point, hitInfo.normal, chargePercentage, ActionID);
        }

        /// <summary>
        ///     Anticipate the initial effects of this action on the triggering client to give immediate feedback.
        /// </summary>
        public override void AnticipateClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            PerformRaycast(null, ref data, 0.0f, PrepareHitEffectAndNotify_AnticipationStart);
        }
        private void PrepareHitEffectAndNotify_AnticipationStart(ServerCharacter _, RaycastHit hitInfo, Vector3 rayDirection, float chargePercentage)
        {
            HitEffectManager.PlayHitEffectsOnSelfAnticipate(hitInfo.point, hitInfo.normal, chargePercentage, ActionID);
        }
    }
}