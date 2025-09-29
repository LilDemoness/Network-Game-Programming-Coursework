using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ApplyBuffEffect : ActionEffect
    {
        [SerializeField] private Action.BuffableValue _buffType;
        [SerializeField] private float _newBuffValue;

        protected override void ApplyToTarget(ServerCharacter owner, NetworkObject targetObject)
        {
            Debug.Log($"Buffing Target '{targetObject.name}' with {_buffType} {_newBuffValue}");
        }
    }
}