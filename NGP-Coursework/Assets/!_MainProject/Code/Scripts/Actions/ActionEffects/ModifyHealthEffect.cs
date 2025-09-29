using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class ModifyHealthEffect : ActionEffect
    {
        [SerializeField] private int _healthChange;


        protected override void ApplyToTarget(ServerCharacter owner, NetworkObject targetObject)
        {
            if (targetObject.TryGetComponent<IDamageable>(out IDamageable damageable) && damageable.IsDamageable())
            {
                damageable.ReceiveHitPoints(owner, _healthChange);
            }
        }
    }
}