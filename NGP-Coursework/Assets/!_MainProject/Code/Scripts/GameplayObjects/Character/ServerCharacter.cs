using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.StatusEffects;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Contains all NetworkVariables, RPCs, and Server-Side Logic of a Character.
    ///     Separated from the Client Logic so that it is always known whether a section of code is running on the server or the client.
    /// </summary>
    public class ServerCharacter : NetworkBehaviour, IDamageable
    {
        [SerializeField] private ClientCharacter m_clientCharacter;
        public ClientCharacter ClientCharacter => m_clientCharacter;


        public NetworkVariable<BuildData> BuildData { get; set; } = new NetworkVariable<BuildData>();


        /// <summary>
        ///     Indicates how the character's movement should be depicted.
        /// </summary>
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        /// <summary>
        ///     Indicates whether this character is in "stealth" (Invisible to AI agents and other players).
        /// </summary>
        public NetworkVariable<bool> IsInStealth { get; } = new NetworkVariable<bool>();


        // Networked State Variables.
        public NetworkVariable<float> CurrentHealth { get; private set; } = new NetworkVariable<float>();
        public float MaxHealth => BuildData.Value?.GetFrameData().MaxHealth ?? 0.0f;

        public NetworkVariable<bool> IsDead { get; private set; } = new NetworkVariable<bool>();


        // Heat.

        public NetworkVariable<float> CurrentHeat { get; private set; } = new NetworkVariable<float>();
        public float MaxHeat => BuildData.Value.GetFrameData()?.HeatCapacity ?? 0.0f;
        private float _lastHeatIncreaseTime = 0.0f;


        // References.

        /// <summary>
        ///     This Character's ActionPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerActionPlayer ActionPlayer => m_serverActionPlayer;
        private ServerActionPlayer m_serverActionPlayer;

        public bool CanPerformActions => !IsDead.Value;


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
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }

            // Subscribe to NetworkVariable Events.
            BuildData.OnValueChanged += OnBuildChanged;
            IsDead.OnValueChanged += OnLifeStateChanged;

            // Initialise all our stats (May not trigger OnValidChanged events if the initialisation values are equal).
            InitialiseHealth();
            InitialiseHeat();
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe from NetworkVariable Events.
            BuildData.OnValueChanged -= OnBuildChanged;
            IsDead.OnValueChanged -= OnLifeStateChanged;
        }



        /// <summary>
        ///     ServerRPC to send movement input for this character.
        /// </summary>
        /// <param name="movementInput"> The character's movement input</param>
        [ServerRpc]
        public void SendCharacterMovementInputServerRpc(Vector2 movementInput)
        {
            // Check that we're not dead or currently experiencing forced movement (E.g. Knockback/Charge).
            if (IsDead.Value || _movement.IsPerformingForcedMovement())
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

            //if (GameDataSource.Instance.GetActionDefinitionByID(data1.ActionID).ShouldNotifyClient)
            //    m_clientCharacter.PlayActionClientRpc(data, NetworkManager.Singleton.ServerTime.TimeAsFloat);

            PlayAction(ref data1);
        }

        /// <summary>
        ///     ServerRPC to cancel the actions with the given ActionID.
        /// </summary>
        /// <param name="actionID"> The ActionID of the actions we wish to cancel.</param>
        /// <param name="slotIndex"> The AttachmentSlot that the cancelled actions should be in.</param>
        [Rpc(SendTo.Server)]
        public void CancelActionByIDServerRpc(ActionID actionID, AttachmentSlotIndex slotIndex = AttachmentSlotIndex.Unset) => CancelAction(actionID, slotIndex);

        /// <summary>
        ///     ServerRPC to cancel all actions in a given Attachment Slot.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void CancelActionBySlotServerRpc(AttachmentSlotIndex slotIndex) => CancelAction(slotIndex);


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
        private void CancelAction(ActionID actionID, AttachmentSlotIndex slotIndex = AttachmentSlotIndex.Unset)
        {
            if (GameDataSource.Instance.GetActionDefinitionByID(actionID).ShouldNotifyClient)
                m_clientCharacter.CancelRunningActionsByIDClientRpc(actionID, slotIndex);

            ActionPlayer.CancelRunningActionsByID(actionID, slotIndex, true);
        }
        /// <summary>
        ///     Cancel all actions in a given Attachment Slot.
        /// </summary>
        /// <remarks> Called on the Server.</remarks>
        private void CancelAction(AttachmentSlotIndex slotIndex)
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
                ReceiveHeatChange(this, -heatDecreaseRate * Time.deltaTime);
            }
        }



        #region Health & Life

        /// <summary>
        ///     Initialise our Health value.
        /// </summary>
        private void InitialiseHealth()
        {
            CurrentHealth.Value = MaxHealth;
        }

        /// <summary>
        ///     Apply a change in health to this ServerCharacter. Handles adjustments for Healing and Damage, and the notification that we've recieved each.
        /// </summary>
        /// <param name="inflicter"> The ServerCharacter that afflicted the damage.</param>
        /// <param name="healthChange"> The change in health to apply (Positive is Healing, Negative is Damage).</param>
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

            // Change the value of our health.
            SetCurrentHealth(CurrentHealth.Value + healthChange);
        }
        /// <summary>
        ///     Set the value of the character's health, clamped between 0 & MaxHealth.
        /// </summary>
        /// <param name="newValue"> The new value of CurrentHealth before clamping.</param>
        /// <param name="excessBecomesOverhealth"> Should health above our maximum health become Overhealth?</param>
        private void SetCurrentHealth(float newValue, bool excessBecomesOverhealth = false)
        {
            if (excessBecomesOverhealth)
                throw new System.NotImplementedException();

            CurrentHealth.Value = Mathf.Clamp(newValue, 0, MaxHealth);
            Debug.Log($"New Health: {CurrentHealth.Value}");

            if (CurrentHealth.Value <= 0)
            {
                // We've died.
                IsDead.Value = true;
                //m_serverActionPlayer.ClearActions(false);
            }
        }

        public float GetMissingHealth()
        {
            if (!IsDamageable())
            {
                return 0.0f;
            }

            return Mathf.Max(0.0f, MaxHealth - CurrentHealth.Value);
        }
        public bool IsDamageable() => !IsDead.Value;


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

        /// <summary>
        ///     Initialise our Heat.
        /// </summary>
        private void InitialiseHeat()
        {
            CurrentHeat.Value = 0.0f;
        }


        /// <summary>
        ///     Apply a heat change to the ServerCharacter.
        /// </summary>
        /// <param name="heatChange"> The heat to be applied (Negative values reduce heat).</param>
        public void ReceiveHeatChange(ServerCharacter inflicter, float heatChange)
        {
            SetCurrentHeat(CurrentHeat.Value + heatChange);
        }
        
        /// <summary>
        ///     Set the value of CurrentHeat, clamping if below 0 and notifying if we exceed our heat cap.
        /// </summary>
        private void SetCurrentHeat(float newValue)
        {
            if (CurrentHeat.Value < newValue)
            {
                // Our heat is increasing. Cache this value so we know when we can next start cooling down.
                _lastHeatIncreaseTime = NetworkManager.ServerTime.TimeAsFloat;
            }

            if (newValue > MaxHeat)
            {
                // Exceeded Heat Cap.
                Debug.Log("Exceeded Heat Cap");
                CurrentHeat.Value = 0.0f;
            }

            CurrentHeat.Value = Mathf.Max(newValue, 0);
        }

        #endregion


        #region Build

        private void OnBuildChanged(BuildData oldValue, BuildData newValue)
        {
            InitialiseHealth();
            InitialiseHeat();
        }

        #endregion
    }
}