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


        private void PerformRaycast(ServerCharacter owner, ref ActionRequestData data, float chargePercentage, System.Action<ServerCharacter, RaycastHit, Vector3, float> onHitCallback)
        {
            Vector3 rayOrigin = GetActionOrigin(ref data);
            Vector3 rayDirection = GetActionDirection(ref data);
            Debug.DrawRay(rayOrigin, rayDirection * MaxRange, Color.red, 0.5f);

            if (CanPierce)
            {
                // Get all valid targets.
                RaycastHit[] colliders = Physics.RaycastAll(rayOrigin, rayDirection, MaxRange, ValidLayers, QueryTriggerInteraction.Ignore);

                // Order our targets in ascending order, taking only the number we wish to pierce.
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
                if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, MaxRange, ValidLayers, QueryTriggerInteraction.Ignore))
                {
                    onHitCallback?.Invoke(owner, hitInfo, rayDirection, chargePercentage);
                }
            }
        }

        private void PrepareAndProcessTarget(ServerCharacter owner, RaycastHit hitInfo, Vector3 rayDirection, float chargePercentage)
        {
            ActionHitInformation actionHitInfo = new ActionHitInformation(hitInfo.transform, hitInfo.point, hitInfo.normal, GetHitForward(hitInfo.normal));
            ProcessTarget(owner, actionHitInfo, chargePercentage);

            Vector3 GetHitForward(Vector3 hitNormal)
            {
                return Mathf.Approximately(Mathf.Abs(Vector3.Dot(hitNormal, rayDirection)), 1.0f)
                    ? Vector3.Cross(hitNormal, -owner.transform.right)  // The normal & ray direction are approximately perpendicular. Calculate forward using our right to prevent miscalculation.
                    : Vector3.Cross(hitNormal, rayDirection);
            }
        }
        private void ProcessTarget(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            Debug.Log($"{hitInfo.Target.name} was hit!");
            //Debug.DrawRay(hitInfo.HitPoint, hitInfo.HitNormal, Color.yellow, 0.5f);

            for(int i = 0; i < ActionEffects.Length; ++i)
            {
                ActionEffects[i].ApplyEffect(owner, hitInfo, chargePercentage);
            }
            HitEffectManager.PlayHitEffectsOnNonOwningClients(hitInfo, chargePercentage, ActionID);
        }


        public override bool OnUpdateClient(ClientCharacter clientCharacter, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            PerformRaycast(null, ref data, chargePercentage, (character, hitInfo, rayDirection, chargePercentage) => PrepareHitEffectAndNotify(clientCharacter.OwnerClientId, hitInfo, rayDirection, chargePercentage));

            return base.OnUpdateClient(clientCharacter, ref data, chargePercentage);
        }
        private void PrepareHitEffectAndNotify(ulong ownerClientId, RaycastHit hitInfo, Vector3 rayDirection, float chargePercentage)
        {
            HitEffectManager.PlayHitEffectsOnSelf(ownerClientId, hitInfo.point, hitInfo.normal, chargePercentage, ActionID);
        }

        public override void AnticipateClient(ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            PerformRaycast(null, ref data, 0.0f, PrepareHitEffectAndNotify_Anticipation);
        }
        private void PrepareHitEffectAndNotify_Anticipation(ServerCharacter _, RaycastHit hitInfo, Vector3 rayDirection, float chargePercentage)
        {
            HitEffectManager.PlayHitEffectsOnSelfAnticipate(hitInfo.point, hitInfo.normal, chargePercentage, ActionID);
        }
    }
}