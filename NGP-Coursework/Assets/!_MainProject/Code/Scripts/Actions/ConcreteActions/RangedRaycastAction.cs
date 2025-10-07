using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

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
        [SerializeField] private int _temp;



        protected override bool HandleStart(ServerCharacter owner) => ActionConclusion.Continue;
        protected override bool HandleUpdate(ServerCharacter owner)
        {
            // Handle Logic
            PerformRaycast(owner);

            return ActionConclusion.Continue;
        }


        private Vector3 GetRaycastOrigin() => Data.OriginTransformID != 0 ? NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.OriginTransformID].transform.TransformPoint(Data.Position) : Data.Position;
        private Vector3 GetRaycastDirection() => (Data.OriginTransformID != 0 ? NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.OriginTransformID].transform.TransformDirection(Data.Direction) : Data.Direction).normalized;
        private void PerformRaycast(ServerCharacter owner)
        {
            Debug.DrawRay(GetRaycastOrigin(), GetRaycastDirection() * _maxRange, Color.red, 0.5f);

            if (_canPierce)
            {
                // Get all valid targets.
                Vector3 originPos = GetRaycastOrigin();
                RaycastHit[] colliders = Physics.RaycastAll(originPos, GetRaycastDirection(), _maxRange, _validLayers, QueryTriggerInteraction.Ignore);

                // Order our targets in ascending order, taking only the number we wish to pierce.
                IEnumerable<RaycastHit> orderedValidTargets = colliders.OrderBy(t => (t.point - originPos).sqrMagnitude).Take(_pierces + 1);

                // Loop through and process all valid targets.
                IEnumerator<RaycastHit> enumerator = orderedValidTargets.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    ProcessTarget(enumerator.Current.transform, enumerator.Current.point, enumerator.Current.normal);
                }
            }
            else
            {
                if (Physics.Raycast(GetRaycastOrigin(), GetRaycastDirection(), out RaycastHit hitInfo, _maxRange, _validLayers, QueryTriggerInteraction.Ignore))
                {
                    ProcessTarget(hitInfo.transform, hitInfo.point, hitInfo.normal);
                }
            }
        }

        private void ProcessTarget(Transform transform, Vector3 hitPoint, Vector3 hitNormal)
        {
            Debug.Log($"{transform.name} was hit!");
            Debug.DrawRay(hitPoint, hitNormal, Color.yellow, 0.5f);
        }
    }
}