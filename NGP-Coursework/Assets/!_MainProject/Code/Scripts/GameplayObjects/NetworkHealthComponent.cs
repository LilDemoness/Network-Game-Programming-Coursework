using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Health;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects
{
    /// <summary>
    ///     Handles the Health & Life of an object within the game, synced between the Server and Clients via NetworkVariables.<br/>
    ///     Also calculates adjustments to damage/healing based on StatusEffects (Eventually).
    /// </summary>
    /// <remarks>
    ///     All functions marked with '_Server' should only be called on the Server.
    ///     All events are send to the Server and Clients (SendTo.Everyone)
    /// </remarks>
    public class NetworkHealthComponent : NetworkBehaviour, IDamageable
    {
        private NetworkVariable<float> _currentHealth { get; } = new NetworkVariable<float>();
        public float MaxHealth { get; set; } = 10.0f;   // Initial value for testing. Remove once Builds have been re-implemented.

        private NetworkVariable<LifeState> _lifeState { get; } = new NetworkVariable<LifeState>(LifeState.Alive);


        public bool IsDead => _lifeState.Value != Character.LifeState.Alive;


        #region Events

        public event System.Action OnInitialised;

        public event System.Action<HealthChangeEventArgs> OnDamageReceived;
        public event System.Action<HealthChangeEventArgs> OnHealingReceived;

        public event System.Action<BaseDamageReceiverEventArgs> OnDied;
        public event System.Action<BaseDamageReceiverEventArgs> OnRevived;

        #endregion

        #region Event RPCs

        [Rpc(SendTo.Everyone)]
        private void NotifyOfInitialisationRpc() => OnInitialised?.Invoke();

        [Rpc(SendTo.Everyone)]
        private void NotifyOfDamageRpc(ulong inflicterObjectId, float healthChange) => OnDamageReceived?.Invoke(new HealthChangeEventArgs(GetServerCharacterForObjectId(inflicterObjectId), healthChange));
        [Rpc(SendTo.Everyone)]
        private void NotifyOfHealingRpc(ulong inflicterObjectId, float healthChange) => OnHealingReceived?.Invoke(new HealthChangeEventArgs(GetServerCharacterForObjectId(inflicterObjectId), healthChange));

        [Rpc(SendTo.Everyone)]
        private void NotifyOfDeathRpc(ulong inflicterObjectId) => OnDied?.Invoke(new BaseDamageReceiverEventArgs(GetServerCharacterForObjectId(inflicterObjectId)));
        [Rpc(SendTo.Everyone)]
        private void NotifyOfReviveRpc(ulong inflicterObjectId) => OnRevived?.Invoke(new BaseDamageReceiverEventArgs(GetServerCharacterForObjectId(inflicterObjectId)));

        #endregion


        public void InitialiseDamageReceiver_Server(float maxHealth)
        {
            this.MaxHealth = maxHealth;
            this._currentHealth.Value = maxHealth;
            this._lifeState.Value = LifeState.Alive;

            NotifyOfInitialisationRpc();
        }

        public void SetMaxHealth_Server(ServerCharacter inflicter, int newMaxHealth, bool increaseHealth = false, bool excessHealthBecomesOverhealth = false)
        {
            float delta = newMaxHealth - MaxHealth;

            // Set our MaxHealth.
            MaxHealth = newMaxHealth;

            // Handle required changes to current health.
            if (delta > 0.0f)
            {
                // Max Health is being increased.
                if (increaseHealth)
                {
                    SetCurrentHealth_Server(inflicter, _currentHealth.Value + delta);
                    NotifyOfHealingRpc(inflicter.NetworkObjectId, delta);
                }
            }
            else
            {
                // Max Health is being decreased.
                if (MaxHealth < _currentHealth.Value)
                {
                    if (excessHealthBecomesOverhealth)
                        throw new System.NotImplementedException("Overhealth");
                    else
                    {
                        SetCurrentHealth_Server(inflicter, _currentHealth.Value + delta);
                        NotifyOfDamageRpc(inflicter.NetworkObjectId, delta);
                    }
                }
            }
        }


        public void ReceiveHealthChange_Server(ServerCharacter inflicter, float healthChange)
        {
            if (!CanHaveHealthChanged())
                return; // This object cannot be damaged.
            if (healthChange == 0.0f)
                return; // The health change was invalid.

            // Apply modifications to the healing/damage as appropriate.
            bool isHeal = healthChange > 0.0f;
            if (isHeal)
            {
                healthChange = CanReceiveHealing() ? ApplyHealingModifications(healthChange) : 0.0f;
            }
            else
            {
                healthChange = CanTakeDamage() ? ApplyDamageModifications(healthChange) : 0.0f;
            }


            SetCurrentHealth_Server(inflicter, _currentHealth.Value + healthChange);


            // Notify whether we received healing or damage
            if (isHeal)
                NotifyOfHealingRpc(inflicter.NetworkObjectId, healthChange);
            else
                NotifyOfDamageRpc(inflicter.NetworkObjectId, healthChange);
        }
        public void SetCurrentHealth_Server(ServerCharacter inflicter, float newValue, bool excessBecomesOverhealth = false)
        {
            if (excessBecomesOverhealth)
                throw new System.NotImplementedException();

            _currentHealth.Value = Mathf.Clamp(newValue, 0, MaxHealth);
            Debug.Log($"New Health: {_currentHealth.Value}");

            if (_currentHealth.Value <= 0.0f)
            {
                // We've died.
                SetLifeState_Server(inflicter, LifeState.Dead);
            }
        }


        private float ApplyHealingModifications(float unmodifiedValue)
        {
            return unmodifiedValue;
        }
        private float ApplyDamageModifications(float unmodifiedValue)
        {
            return unmodifiedValue;
        }


        public void SetLifeState_Server(ServerCharacter inflicter, LifeState newLifeState)
        {
            LifeState oldLifeState = _lifeState.Value;
            _lifeState.Value = newLifeState;

            if (oldLifeState == LifeState.Alive && newLifeState == LifeState.Dead)
            {
                NotifyOfDeathRpc(inflicter.NetworkObjectId);
            }
            else if (oldLifeState == LifeState.Dead && newLifeState == LifeState.Alive)
            {
                NotifyOfReviveRpc(inflicter.NetworkObjectId);
            }
        }


        public float GetMissingHealth() => MaxHealth - _currentHealth.Value;
        public float GetCurrentHealth() => _currentHealth.Value;


        public bool CanHaveHealthChanged() => !IsDead;
        public bool CanTakeDamage() => !IsDead;
        public bool CanReceiveHealing() => !IsDead;



        private ServerCharacter GetServerCharacterForObjectId(ulong objectId)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
            {
                if (networkObject.TryGetComponent<ServerCharacter>(out ServerCharacter serverCharacter))
                    return serverCharacter;
                else
                    throw new System.Exception($"Invalid Object ID ({objectId}): No corresponding ServerCharacter on NetworkObject");
            }
            else
                throw new System.Exception($"Invalid Object ID ({objectId}): No corresponding NetworkObject");
        }


        #region Event Args Definitions

        public class BaseDamageReceiverEventArgs : System.EventArgs
        {
            public ServerCharacter Inflicter;


            private BaseDamageReceiverEventArgs() { }
            public BaseDamageReceiverEventArgs(ServerCharacter inflicter)
            {
                this.Inflicter = inflicter;
            }
        }
        public class HealthChangeEventArgs : BaseDamageReceiverEventArgs
        {
            public float HealthChange;

            public HealthChangeEventArgs(ServerCharacter inflicter, float healthChange) : base(inflicter)
            {
                this.HealthChange = healthChange;
            }
        }

        #endregion
    }
}