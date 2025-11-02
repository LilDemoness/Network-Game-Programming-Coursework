using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions;
using Gameplay.GameplayObjects.Health;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.StatusEffects;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Contains all NetworkVariables, RPCs, and Server-Side Logic of a Character.
    ///     Separated from the Client Logic so that it is always known whether a section of code is running on the server or the client.
    /// </summary>
    [RequireComponent(typeof(NetworkLifeState), typeof(NetworkHealthState))]
    public class ServerCharacter : NetworkBehaviour, IDamageable
    {
        [SerializeField] private ClientCharacter m_clientCharacter;
        public ClientCharacter ClientCharacter => m_clientCharacter;


        // Build Data?
        private BuildData m_buildData;
        public BuildData BuildData
        {
            get => m_buildData;
            set => m_buildData = value;
        }


        /// <summary>
        ///     Indicates how the character's movement should be depicted.
        /// </summary>
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        /// <summary>
        ///     Indicates whether this character is in "stealth" (Invisible to AI agents and other players).
        /// </summary>
        public NetworkVariable<bool> IsInStealth { get; } = new NetworkVariable<bool>();


        // Health & Life.
        public NetworkHealthState NetHealthState { get; private set; }
        public NetworkLifeState NetLifeState { get; private set; }


        public float CurrentHealth
        {
            get => NetHealthState.CurrentHealth.Value;
            private set => NetHealthState.CurrentHealth.Value = value;
        }
        public float MaxHealth => BuildData.GetFrameData()?.MaxHealth ?? 0.0f;
        public bool IsDead
        {
            get => NetLifeState.IsDead.Value;
            private set => NetLifeState.IsDead.Value = value;
        }


        // Heat.

        private float m_currentHeat;
        public float CurrentHeat
        {
            get => m_currentHeat;
            set
            {
                if (m_currentHeat < value)
                    _lastHeatIncreaseTime = NetworkManager.ServerTime.TimeAsFloat;

                if (value > MaxHeat)
                {
                    // Exceeded Heat Cap.
                    Debug.Log("Exceeded Heat Cap");
                    m_currentHeat = 0.0f;
                }
                
                m_currentHeat = Mathf.Max(value, 0);
            }
        }
        public float MaxHeat => BuildData.GetFrameData()?.HeatCapacity ?? 0.0f;
        private float _lastHeatIncreaseTime = 0.0f;


        // References.

        /// <summary>
        ///     This Character's ActionPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerActionPlayer ActionPlayer => m_serverActionPlayer;
        private ServerActionPlayer m_serverActionPlayer;

        public bool CanPerformActions => !IsDead;


        /// <summary>
        ///     The character's StatusEffectPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerStatusEffectPlayer StatusEffectPlayer => m_statusEffectPlayer;
        private ServerStatusEffectPlayer m_statusEffectPlayer;


        [SerializeField] private ServerCharacterMovement _movement; 
        public ServerCharacterMovement Movement => _movement;

        public int TeamID;


        private void Awake()
        {
            m_serverActionPlayer = new ServerActionPlayer(this);
            m_statusEffectPlayer = new ServerStatusEffectPlayer(this);
            NetLifeState = GetComponent<NetworkLifeState>();
            NetHealthState = GetComponent<NetworkHealthState>();
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }

            // Subscribe to Health/Life Events.
            NetLifeState.IsDead.OnValueChanged += OnLifeStateChanged;

            InitialiseHealth();
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe from Health/Life Events.
            NetLifeState.IsDead.OnValueChanged -= OnLifeStateChanged;
        }



        /// <summary>
        ///     ServerRPC to send movement input for this character.
        /// </summary>
        /// <param name="movementInput"> The character's movement input</param>
        [ServerRpc]
        public void SendCharacterMovementInputServerRpc(Vector2 movementInput)
        {
            // Check that we're not dead or currently experiencing forced movement (E.g. Knockback/Charge).
            if (IsDead || _movement.IsPerformingForcedMovement())
                return;

            // Check if our current action prevents movement.
            if (ActionPlayer.GetActiveActionInfo(out ActionRequestData data))
            {
                if (data.PreventMovement)
                    return;
            }

            // We can move.

            _movement.SetMovementInput(movementInput);
        }


        /// <summary>
        ///     Client->Server RPC that sends a request to play an action.
        /// </summary>
        /// <param name="data"> The Data about which action to play and its associated details.</param>
        [Rpc(SendTo.Server)]
        public void PlayActionServerRpc(ActionRequestData data)
        {
            ActionRequestData data1 = data;
            if (GameDataSource.Instance.GetActionDefinitionByID(data1.ActionID).IsHostileAction)
            {
                // Notify our running actions that we're using a new hostile action.
                // Called so that things like Stealth can end themselves.
                ActionPlayer.OnGameplayActivity(Action.GameplayActivity.UsingHostileAction);
            }

            if (GameDataSource.Instance.GetActionDefinitionByID(data1.ActionID).ShouldNotifyClient)
                m_clientCharacter.PlayActionClientRpc(data, NetworkManager.Singleton.ServerTime.TimeAsFloat);

            PlayAction(ref data1);
        }

        /// <summary>
        ///     ServerRPC to cancel the actions with the given ActionID.
        /// </summary>
        /// <param name="actionID"> The ActionID of the actions we wish to cancel.</param>
        /// <param name="slotIndex"> The slotIndex that the cancelled actions should be in.</param>
        [Rpc(SendTo.Server)]
        public void CancelActionByIDServerRpc(ActionID actionID, SlotIndex slotIndex = SlotIndex.Unset) => CancelAction(actionID, slotIndex);

        /// <summary>
        ///     ServerRPC to cancel all actions in a given Slot.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void CancelActionBySlotServerRpc(SlotIndex slotIndex) => CancelAction(slotIndex);


        /// <summary>
        ///     Play a sequence of actions.
        /// </summary>
        /// <param name="action"></param>
        public void PlayAction(ref ActionRequestData action)
        {
            if (action.PreventMovement)
            {
                _movement.CancelMove();
            }

            ActionPlayer.PlayAction(ref action);
        }
        /// <summary>
        ///     Cancel the actions with the given ActionID.
        /// </summary>
        /// <remarks> Called on the Server.</remarks>
        private void CancelAction(ActionID actionID, SlotIndex slotIndex = SlotIndex.Unset)
        {
            if (GameDataSource.Instance.GetActionDefinitionByID(actionID).ShouldNotifyClient)
                m_clientCharacter.CancelRunningActionsByIDClientRpc(actionID, slotIndex);

            ActionPlayer.CancelRunningActionsByID(actionID, slotIndex, true);
        }
        /// <summary>
        ///     Cancel all actions in a given Slot.
        /// </summary>
        /// <remarks> Called on the Server.</remarks>
        private void CancelAction(SlotIndex slotIndex)
        {
            m_clientCharacter.CancelRunningActionsBySlotIDClientRpc(slotIndex);
            ActionPlayer.CancelRunningActionsBySlotID(slotIndex, true);
        }
        


        private void Update()
        {
            ActionPlayer.OnUpdate();
            StatusEffectPlayer.OnUpdate();

            float heatDecreaseDelay = 1.0f;
            float heatDecreaseRate = 1.0f;
            if (NetworkManager.ServerTime.TimeAsFloat >= (_lastHeatIncreaseTime + heatDecreaseDelay))
            {
                CurrentHeat -= heatDecreaseRate * Time.deltaTime;
            }
        }



        #region Health & Life

        private void InitialiseHealth()
        {
            CurrentHealth = MaxHealth;
        }

        public void ReceiveHealthChange(ServerCharacter inflicter, float healthChange)
        {
            if (!IsDamageable())
                return;


            if (healthChange > 0.0f)
            {
                // Healing.
                m_serverActionPlayer.OnGameplayActivity(Action.GameplayActivity.Healed);
                float healingModifier = m_serverActionPlayer.GetBuffedValue(Action.BuffableValue.PercentHealingReceived);
                healthChange = Mathf.CeilToInt(healthChange * healingModifier);
            }
            else
            {
                // Damage.
                m_serverActionPlayer.OnGameplayActivity(Action.GameplayActivity.AttackedByEnemy);
                float damageModifier = m_serverActionPlayer.GetBuffedValue(Action.BuffableValue.PercentDamageReceived);
                healthChange = Mathf.CeilToInt(healthChange * damageModifier);

                // Take Damage Animation.
            }

            CurrentHealth = Mathf.Clamp(CurrentHealth + healthChange, 0, MaxHealth);
            Debug.Log($"New Health: {CurrentHealth}");

            if (CurrentHealth <= 0)
            {
                // We've died.
                IsDead = true;
                //m_serverActionPlayer.ClearActions(false);
            }
        }
        public float GetMissingHealth()
        {
            if (!IsDamageable())
            {
                return 0.0f;
            }

            return Mathf.Max(0.0f, MaxHealth - CurrentHealth);
        }
        public bool IsDamageable() => !NetLifeState.IsDead.Value;

        private void OnLifeStateChanged(bool previousValue, bool newValue)
        {
            if (newValue == true)
            {
                // We have died. Cancel active actions.
                m_serverActionPlayer.ClearActions(true);
                _movement.CancelMove();
            }
        }

        #endregion


        #region Heat

        public void ReceiveHeatChange(float heatChange)
        {
            CurrentHeat += heatChange;
        }

        #endregion
    }
}