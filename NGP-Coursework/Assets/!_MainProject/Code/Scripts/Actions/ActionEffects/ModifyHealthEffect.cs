using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects;
using Gameplay.Actions.Targeting;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ModifyHealthEffect : ActionEffect
    {
        [SerializeField] private int _healthChange;


        protected override void ApplyToTarget(ServerCharacter owner, ref ActionHitInfo hitInfo)
        {
            if (hitInfo.HitTransform.TryGetComponentThroughParents<IDamageable>(out IDamageable damageable) && damageable.IsDamageable())
            {
                damageable.ReceiveHitPoints(owner, _healthChange);
            }
        }
    }
}