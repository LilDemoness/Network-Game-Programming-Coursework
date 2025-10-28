using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class HealthChangeEffect : ActionEffect
    {
        [SerializeField] private float _healthChange;
        [SerializeField] private bool _scaleValueWithCharge = true;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            if (hitInfo.Target.TryGetComponentThroughParents<IDamageable>(out IDamageable damageable))
            {
                damageable.ReceiveHealthChange(owner, _scaleValueWithCharge ? chargePercentage * _healthChange : _healthChange);
            }
        }
    }
}