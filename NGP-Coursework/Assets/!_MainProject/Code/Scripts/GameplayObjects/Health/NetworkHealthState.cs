using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Health
{
    /// <summary>
    ///     NetworkBehaviour containing a NetworkVariable that represents this object's health.
    /// </summary>
    public class NetworkHealthState : NetworkBehaviour
    {
        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();


        /// <summary> Invoked when the object's health reached 0. </summary>
        public event System.Action OnHitPointsDepleted;
        /// <summary> Invoked when the object's health was healed from 0. </summary>
        public event System.Action OnHitPointsReplenished;


        private void OnEnable() => CurrentHealth.OnValueChanged += OnHitPointsChanged;
        private void OnDisable() => CurrentHealth.OnValueChanged -= OnHitPointsChanged;

        private void OnHitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > 0 && newValue <= 0)
            {
                // Just reached 0 HP
                OnHitPointsDepleted?.Invoke();
            }
            else if (previousValue <= 0 && newValue > 0)
            {
                // HP Restored when we were previously dead.
                OnHitPointsReplenished?.Invoke();
            }
        }
    }
}