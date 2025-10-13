using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions;
using Gameplay.GameplayObjects.Health;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Contains all NetworkVariables, RPCs, and Server-Side Logic of a Character.
    ///     Separated from the Client Logic so that it is always known whether a section of code is running on the server or the client.
    /// </summary>
    [RequireComponent(typeof(NetworkLifeState), typeof(NetworkHealthState))]
    public class ServerCharacter : NetworkBehaviour
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

        [SerializeField] private DamageReceiver _damageReceiver;

        public int CurrentHealth
        {
            get => NetHealthState.CurrentHealth.Value;
            private set => NetHealthState.CurrentHealth.Value = value;
        }
        public int MaxHealth => BuildData.GetFrameData()?.MaxHealth ?? 0;
        public bool IsDead
        {
            get => NetLifeState.IsDead.Value;
            private set => NetLifeState.IsDead.Value = value;
        }


        /// <summary>
        ///     This Character's ActionPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerActionPlayer ActionPlayer => m_serverActionPlayer;
        private ServerActionPlayer m_serverActionPlayer;

        public bool CanPerformActions => !IsDead;


        [SerializeField] private ServerCharacterMovement _movement; 
        public ServerCharacterMovement Movement => _movement;

        public int TeamID;


        private void Awake()
        {
            m_serverActionPlayer = new ServerActionPlayer(this);
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
            _damageReceiver.OnDamageReceived += ReceiveHealthChange;
            _damageReceiver.GetMissingHealthFunc += GetMissingHealth;

            InitialiseHealth();
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe from Health/Life Events.
            NetLifeState.IsDead.OnValueChanged -= OnLifeStateChanged;
            if (_damageReceiver != null)
            {
                _damageReceiver.OnDamageReceived -= ReceiveHealthChange;
                _damageReceiver.GetMissingHealthFunc -= GetMissingHealth;
            }
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
            if (GameDataSource.Instance.GetActionPrototypeByID(data1.ActionID).IsHostileAction)
            {
                // Notify our running actions that we're using a new hostile action.
                // Called so that things like Stealth can end themselves.
                ActionPlayer.OnGameplayActivity(Action.GameplayActivity.UsingHostileAction);
            }

            if (GameDataSource.Instance.GetActionPrototypeByID(data1.ActionID).ShouldNotifyClient)
                m_clientCharacter.PlayActionClientRpc(data, NetworkManager.Singleton.ServerTime.TimeAsFloat);

            PlayAction(ref data1);
        }

        [Rpc(SendTo.Server)]
        public void CancelActionByIDServerRpc(ActionID actionID, int slotIdentifier = 0) => CancelAction(actionID, slotIdentifier);

        [Rpc(SendTo.Server)]
        public void CancelActionBySlotServerRpc(int slotIdentifier) => CancelAction(slotIdentifier);


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
        private void CancelAction(ActionID actionID, int slotIndentifier = 0)
        {
            if (GameDataSource.Instance.GetActionPrototypeByID(actionID).ShouldNotifyClient)
                m_clientCharacter.CancelRunningActionsByIDClientRpc(actionID, slotIndentifier);

            ActionPlayer.CancelRunningActionsByID(actionID, slotIndentifier, true);
        }
        private void CancelAction(int slotIndentifier)
        {
            m_clientCharacter.CancelRunningActionsBySlotIDClientRpc(slotIndentifier);
            ActionPlayer.CancelRunningActionsBySlotID(slotIndentifier, true);
        }
        


        private void Update()
        {
            ActionPlayer.OnUpdate();
        }



        #region Health & Life

        private void InitialiseHealth()
        {
            CurrentHealth = MaxHealth;
        }

        private void ReceiveHealthChange(ServerCharacter inflicter, int healthChange)
        {
            if (healthChange > 0)
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
        private int GetMissingHealth() => Mathf.Max(0, MaxHealth - CurrentHealth);

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
    }
}