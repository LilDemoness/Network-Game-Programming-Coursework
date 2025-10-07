using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class HealthChangeEffect : ActionEffect
    {
        [SerializeField] private int _healthChange;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo)
        {
            if (hitInfo.Target.TryGetComponentThroughParents<IDamageable>(out IDamageable damageable))
            {
                damageable.ReceiveHitPoints(owner, _healthChange);
            }
        }

        public override void Cleanup() { }
    }
}