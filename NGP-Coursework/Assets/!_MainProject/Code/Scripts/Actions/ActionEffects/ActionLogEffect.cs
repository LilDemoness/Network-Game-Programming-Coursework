using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Targeting;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ActionLogEffect : ActionEffect
    {
        protected override void ApplyToTarget(ServerCharacter owner, ref ActionHitInfo actionHitInfo) => Debug.Log($"Effect Triggered by: {owner.name}; Targeting: {actionHitInfo.HitTransform.name}");
    }
}