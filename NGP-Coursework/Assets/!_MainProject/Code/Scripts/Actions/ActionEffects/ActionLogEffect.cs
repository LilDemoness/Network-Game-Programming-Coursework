using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ActionLogEffect : ActionEffect
    {
        protected override void ApplyToTarget(ServerCharacter owner, NetworkObject targetObject) => Debug.Log($"Effect Triggered by: {owner.name}; Targeting: {targetObject.name}");
    }
}