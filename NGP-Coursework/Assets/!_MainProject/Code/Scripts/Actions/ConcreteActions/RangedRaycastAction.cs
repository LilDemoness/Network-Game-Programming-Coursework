using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Effects;

namespace Gameplay.Actions
{
    /// <summary>
    ///     An action that uses a raycast to trigger effects on targets from a range.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/New Ranged Raycast Action")]
    public class RangedRaycastAction : DefaultAction
    {
        [Header("Targeting")]
        [SerializeField] private float _maxRange;
        [SerializeField] private LayerMask _validLayers;

        [Space(5)]
        [SerializeField, Min(0)] private int _pierces = 0;
        private bool _canPierce => _pierces > 0;


        [Header("Effects")]
        [SerializeReference] [SubclassSelector] private ActionEffect[] _actionEffects;



        protected override bool HandleStart(ServerCharacter owner) => ActionConclusion.Continue;
        protected override bool HandleUpdate(ServerCharacter owner)
        {
            // Handle Logic
            PerformRaycast(owner);

            return ActionConclusion.Continue;
        }


        private void PerformRaycast(ServerCharacter owner)
        {
            Vector3 rayOrigin = GetActionOrigin();
            Vector3 rayDirection = GetActionDirection();
            Debug.DrawRay(rayOrigin, rayDirection * _maxRange, Color.red, 0.5f);

            if (_canPierce)
            {
                // Get all valid targets.
                RaycastHit[] colliders = Physics.RaycastAll(rayOrigin, rayDirection, _maxRange, _validLayers, QueryTriggerInteraction.Ignore);

                // Order our targets in ascending order, taking only the number we wish to pierce.
                IEnumerable<RaycastHit> orderedValidTargets = colliders.OrderBy(t => (t.point - rayOrigin).sqrMagnitude).Take(_pierces + 1);

                // Loop through and process all valid targets.
                IEnumerator<RaycastHit> enumerator = orderedValidTargets.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    ActionHitInformation actionHitInfo = new ActionHitInformation(enumerator.Current.transform, enumerator.Current.point, enumerator.Current.normal, GetHitForward(enumerator.Current.normal));
                    ProcessTarget(owner, actionHitInfo);
                }
            }
            else
            {
                if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, _maxRange, _validLayers, QueryTriggerInteraction.Ignore))
                {
                    ActionHitInformation actionHitInfo = new ActionHitInformation(hitInfo.transform, hitInfo.point, hitInfo.normal, GetHitForward(hitInfo.normal));
                    ProcessTarget(owner, actionHitInfo);
                }
            }

            Vector3 GetHitForward(Vector3 hitNormal) => Mathf.Approximately(Mathf.Abs(Vector3.Dot(hitNormal, rayDirection)), 1.0f) ? Vector3.Cross(hitNormal, -owner.transform.right) : Vector3.Cross(hitNormal, rayDirection);
        }

        private void ProcessTarget(ServerCharacter owner, in ActionHitInformation hitInfo)
        {
            Debug.Log($"{hitInfo.Target.name} was hit!");
            //Debug.DrawRay(hitInfo.HitPoint, hitInfo.HitNormal, Color.yellow, 0.5f);

            for(int i = 0; i < _actionEffects.Length; ++i)
            {
                _actionEffects[i].ApplyEffect(owner, hitInfo);
            }
        }
    }
}