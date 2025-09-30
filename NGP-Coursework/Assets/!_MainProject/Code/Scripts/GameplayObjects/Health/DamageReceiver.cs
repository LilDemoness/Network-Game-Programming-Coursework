using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects.Health
{
    public class DamageReceiver : NetworkBehaviour, IDamageable
    {
        public event System.Action<ServerCharacter, int> OnDamageReceived;
        public event System.Func<int> GetMissingHealthFunc;

        [SerializeField] private NetworkLifeState _networkLifeState;


        public void ReceiveHitPoints(ServerCharacter inflicter, int change)
        {
            if (!IsDamageable())
                return;

            OnDamageReceived?.Invoke(inflicter, change);
        }
        public int GetMissingHealth()
        {
            if (!IsDamageable())
            {
                return 0;
            }

            return GetMissingHealthFunc?.Invoke() ?? 0;
        }

        public bool IsDamageable() => !_networkLifeState.IsDead.Value;
    }
}