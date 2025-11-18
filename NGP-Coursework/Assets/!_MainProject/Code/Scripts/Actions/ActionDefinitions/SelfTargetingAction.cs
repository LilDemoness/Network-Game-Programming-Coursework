using Gameplay.Actions.Effects;
using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Definitions
{
    /// <summary>
    ///     An action that always targets itself.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/Self Targeting Action", order = 3)]
    public class SelfTargetingAction : ActionDefinition
    {
        [SerializeField] private bool _overrideOriginToOwnerPosition = true;
        [SerializeField] private bool _overrideDirectionToOwnerUp = true;


        public override bool OnStart(ServerCharacter owner, ref ActionRequestData data) => ActionConclusion.Continue;
        public override bool OnUpdate(ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            Vector3 origin = _overrideOriginToOwnerPosition ? owner.transform.position : GetActionOrigin(ref data);
            Vector3 direction = _overrideDirectionToOwnerUp ? owner.transform.up : GetActionDirection(ref data);
            Vector3 forward = Vector3.Cross(direction, owner.transform.right); // To-do: Fix & Test

            ActionHitInformation hitInfo = new ActionHitInformation(owner.transform, origin, direction, forward);
            for (int i = 0; i < ActionEffects.Length; ++i)
            {
                ActionEffects[i].ApplyEffect(owner, hitInfo, chargePercentage);
            }

            return ActionConclusion.Continue;
        }
    }
}