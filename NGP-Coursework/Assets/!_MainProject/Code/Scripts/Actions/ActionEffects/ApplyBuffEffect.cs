using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Targeting;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ApplyBuffEffect : ActionEffect
    {
        [SerializeField] private Action.BuffableValue _buffType;
        [SerializeField] private float _newBuffValue;

        protected override void ApplyToTarget(ServerCharacter owner, ref ActionHitInfo hitInfo)
        {
            Debug.Log($"Buffing Target '{hitInfo.HitTransform.name}' with {_buffType} {_newBuffValue}");
        }
    }
}