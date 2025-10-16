using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Health
{
    public class DamageLogger : NetworkBehaviour, IDamageable
    {
        public void ReceiveHealthChange(ServerCharacter inflicter, float change)
        {
            if (!IsDamageable())
            {
                Debug.Log($"{this.name} is Invulnerable");
                return;
            }

            Debug.Log($"{this.name} received {(change > 0.0f ? (change + " healing") : (Mathf.Abs(change) + " damage"))} from {inflicter.name}");
        }
        public float GetMissingHealth() => 0;

        public bool IsDamageable() => true;
    }
}