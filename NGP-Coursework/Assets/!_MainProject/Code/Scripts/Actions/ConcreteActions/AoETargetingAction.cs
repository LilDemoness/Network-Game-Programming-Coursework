using System.Collections.Generic;
using UnityEngine;
using Gameplay.Actions.Effects;
using Gameplay.GameplayObjects.Character;
using System;

namespace Gameplay.Actions
{
    /// <summary>
    ///     An action that always targets itself.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/New AoE Targeting Action")]
    public class AoETargetingAction : DefaultAction
    {
        [Header("Targeting")]
        [SerializeReference, SubclassSelector] private AoETargeting _targetingMethod;


        [Header("Effects")]
        [SerializeReference][SubclassSelector] private ActionEffect[] _actionEffects;


        protected override bool HandleStart(ServerCharacter owner) => ActionConclusion.Continue;
        protected override bool HandleUpdate(ServerCharacter owner)
        {
            _targetingMethod.GetTargets(owner, base.GetActionOrigin(), base.GetActionDirection(), callback: ProcessTarget);

            return ActionConclusion.Continue;
        }
        private void ProcessTarget(ServerCharacter owner, ActionHitInformation hitInfo)
        {
            Debug.Log($"{hitInfo.Target.name} was hit!");

            for (int i = 0; i < _actionEffects.Length; ++i)
            {
                _actionEffects[i].ApplyEffect(owner, hitInfo);
            }
        }



        // Sphere, Line, Cone.
        private abstract class AoETargeting
        {
            [SerializeField] protected bool CanTargetOwner;

            [Space(5)]
            [SerializeField] protected bool RequireLineOfSight;
            [SerializeField] protected LayerMask ObstructionsMask;

            [Space(5)]
            [SerializeField] protected LayerMask ValidLayers;

            public abstract void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ActionHitInformation> callback);
        }
        [System.Serializable]
        private class SphereAoETargeting : AoETargeting
        {
            [SerializeField] private float _sphereRadius;

            public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ActionHitInformation> callback)
            {
                foreach(Collider potentialTarget in Physics.OverlapSphere(origin, _sphereRadius, ValidLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!CanTargetOwner && potentialTarget.HasParent(owner.transform))
                        continue;   // We're not wanting to target the owner, and this target is the owner.

                    if (RequireLineOfSight && Physics.Linecast(origin, potentialTarget.transform.position, ObstructionsMask, QueryTriggerInteraction.Ignore))
                        continue;

                    //yield return potentialTarget.transform;
                    callback?.Invoke(owner, new ActionHitInformation(potentialTarget.transform, potentialTarget.transform.position, potentialTarget.transform.up, potentialTarget.transform.forward));
                }
            }
        }
        [System.Serializable]
        private class LineAoETargeting : AoETargeting
        {
            [SerializeField] private float _lineLength;
            [SerializeField] private float _lineRadius;

            public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, Action<ServerCharacter, ActionHitInformation> callback)
            {
                // Thick Raycast (Spherecast).
                throw new NotImplementedException();
            }
        }
        [System.Serializable]
        private class ConeAoETargeting : AoETargeting
        {
            [SerializeField] private float _coneLength;
            [SerializeField] private float _coneAngle;

            public override void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, Action<ServerCharacter, ActionHitInformation> callback)
            {
                // OverlapSphere with Angle Check.
                throw new NotImplementedException();
            }
        }
    }
}