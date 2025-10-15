using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Health
{
    public class DamageLogger : NetworkBehaviour, IDamageable
    {
        public void ReceiveHealthChange(ServerCharacter inflicter, int change)
        {
            if (!IsDamageable())
            {
                Debug.Log($"{this.name} is Invulnerable");
                return;
            }

            Debug.Log($"{this.name} received {(change > 0.0 ? (change + " healing") : (Mathf.Abs(change) + " damage"))} from {inflicter.name}");
        }
        public int GetMissingHealth() => 0;

        public bool IsDamageable() => true;
    }
}