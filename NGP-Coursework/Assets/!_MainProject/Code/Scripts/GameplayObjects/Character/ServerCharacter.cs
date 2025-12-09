using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.StatusEffects;
using Gameplay.GameplayObjects.Character.Customisation;
using Utils;
using NUnit.Framework.Constraints;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Contains all NetworkVariables, RPCs, and Server-Side Logic of a Character.
    ///     Separated from the Client Logic so that it is always known whether a section of code is running on the server or the client.
    /// </summary>
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField] private ClientCharacter m_clientCharacter;
        public ClientCharacter ClientCharacter => m_clientCharacter;


        [SerializeField] private NetworkNameState m_networkNameState;
        [SerializeField] private string _characterName;
        public string CharacterName => m_networkNameState ? m_networkNameState.Name.Value : _characterName;
        public FixedPlayerName FixedCharacterName => m_networkNameState ? m_networkNameState.Name.Value : new FixedPlayerName(_characterName);


        [SerializeField, ReadOnly] private BuildData _buildDataReference;
        /// <summary>
        ///     Property for this ServerCharacter's BuildData.<br/>
        ///     Must be populated through both Clients and Servers.
        /// </summary>
        public BuildData BuildDataReference
        {
            get => _buildDataReference;
            set
            {
                _buildDataReference = value;

                if (IsServer)
                    ServerCharacter_OnBuildDataChanged(_buildDataReference);
            }
        }


        /// <summary>
        ///     Indicates how the character's movement should be depicted.
        /// </summary>
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        /// <summary>
        ///     Indicates whether this character is in "stealth" (Invisible to AI agents and other players).
        /// </summary>
        public NetworkVariable<bool> IsInStealth { get; } = new NetworkVariable<bool>();


        [SerializeField] private NetworkHealthComponent _networkHealthComponent;
        public NetworkHealthComponent NetworkHealthComponent => _networkHealthComponent;


        // Heat.

        public NetworkVariable<float> CurrentHeat { get; private set; } = new NetworkVariable<float>();
        public float MaxHeat => _buildDataReference.GetFrameData().HeatCapacity;
        private float _lastHeatIncreaseTime = 0.0f;

        [SerializeField] private Gameplay.StatusEffects.Definitions.Overheating _overheatingEffectDefinition;


        // Movement.
        public float BaseMoveSpeed => _buildDataReference.GetFrameData().MovementSpeed;


        // References.

        /// <summary>
        ///     This Character's ActionPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerActionPlayer ActionPlayer => m_serverActionPlayer;
        private ServerActionPlayer m_serverActionPlayer;
        public bool CanPerformActionInstantly = true;

        public bool CanPerformActions => !_networkHealthComponent.IsDead;


        /// <summary>
        ///     The character's StatusEffectPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerStatusEffectPlayer StatusEffectPlayer => m_statusEffectPlayer;
        private ServerStatusEffectPlayer m_statusEffectPlayer;


        [SerializeField] private ServerCharacterMovement _movement; 
        public ServerCharacterMovement Movement => _movement;

        public NetworkVariable<int> TeamID { get; set; } = new NetworkVariable<int>(-1);

        [SerializeField] private GameObject _gfxRoot;
        [SerializeField] private VisualEffects.SpecialFXGraphic _deathExplosionEffectPrefab;


        private void Awake()
        {
            m_serverActionPlayer = new ServerActionPlayer(this);
            m_statusEffectPlayer = new ServerStatusEffectPlayer(this);
        }
        public override void OnNetworkSpawn()
        {
            // We're subscribing to this event on clients too in order to relay our BuildData through the class reference as opposed to duplicating the struct.
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }

            CurrentHeat.OnValueChanged += CheckIfExceededHeatCap;
            NetworkHealthComponent.OnDied += OnCharacterDied;
            ActionPlayer.OnActionQueueFilled += ServerActionPlayer_OnActionQueueFilled;
            ActionPlayer.OnActionQueueEmptied += ServerActionPlayer_OnActionQueueEmptied;
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe from NetworkVariable Events.
            CurrentHeat.OnValueChanged -= CheckIfExceededHeatCap;
            NetworkHealthComponent.OnDied -= OnCharacterDied;
            ActionPlayer.OnActionQueueFilled -= ServerActionPlayer_OnActionQueueFilled;
            ActionPlayer.OnActionQueueEmptied -= ServerActionPlayer_OnActionQueueEmptied;
        }



        /// <summary>
        ///     ServerRPC to send movement input for this character.
        /// </summary>
        /// <param name="movementInput"> The character's movement input</param>
        [ServerRpc]
        public void SendCharacterMovementInputServerRpc(Vector2 movementInput)
        {
            // Check that we're not dead or currently experiencing forced movement (E.g. Knockback/Charge).
            if (!CanPerformActions || _movement.IsPerformingForcedMovement())
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
            if (!CanPerformActions)
                return;

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
        private void PlayAction(ref ActionRequestData action)
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

        private void ServerActionPlayer_OnActionQueueFilled() => UpdateInstantActionAvailabilityOwnerRpc(false);
        private void ServerActionPlayer_OnActionQueueEmptied() => UpdateInstantActionAvailabilityOwnerRpc(true);
        [Rpc(SendTo.Owner)]
        private void UpdateInstantActionAvailabilityOwnerRpc(bool canPerformInstantAction) => CanPerformActionInstantly = canPerformInstantAction;



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
        ///     Set the value of CurrentHeat, clamping if below 0 or above <see cref="MaxHeat"/>.</br>
        ///     Heat Cap Exceeding is handled separately via the OnValueChanged event on the server.
        /// </summary>
        private void SetCurrentHeat(float newValue)
        {
            if (CurrentHeat.Value < newValue)
            {
                // Our heat is increasing. Cache this value so we know when we can next start cooling down.
                _lastHeatIncreaseTime = NetworkManager.ServerTime.TimeAsFloat;
            }

            CurrentHeat.Value = Mathf.Clamp(newValue, 0, MaxHeat);
        }
        private void CheckIfExceededHeatCap(float previousHeat, float newHeat)
        {
            if (newHeat >= MaxHeat && previousHeat < MaxHeat)
            {
                // Just Exceeded Heat Cap.
                StatusEffectPlayer.AddStatusEffect(_overheatingEffectDefinition);
            }
            else if (newHeat < MaxHeat && previousHeat >= MaxHeat)
            {
                // Just went under our Heat Cap.
                StatusEffectPlayer.ClearAllEffectsOfType(_overheatingEffectDefinition);
            }
        }

        #endregion


        #region Build

        //private void NetworkedBuildDataChanged(BuildDataState oldValue, BuildDataState newValue)
        //{
        //    OnBuildDataChanged?.Invoke(new BuildDataReference(newValue));
        //}
        [Rpc(SendTo.ClientsAndHost)]
        public void UpdateBuildStateClientRpc(int activeFrame, int[] activeSlottables)
        {
            this.BuildDataReference = new BuildData(activeFrame, activeSlottables);
        }

        private void ServerCharacter_OnBuildDataChanged(BuildData buildData)
        {
            _networkHealthComponent.InitialiseDamageReceiver_Server(buildData.GetFrameData().MaxHealth);
            InitialiseHeat();
        }

        #endregion



        #region Death & Respawning

        public void OnCharacterDied(NetworkHealthComponent.BaseDamageReceiverEventArgs _) => OnCharacterDied();
        public void OnCharacterDied()
        {
            // Spawn Death Model.

            // Play Death Effects.
            PlayDeathEffectsClientRpc();
        }

        // Server-only.
        public void RespawnCharacter(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // Remove the Death Model (Or have it handle that itself).

            // Show GFX.
            ShowCharacterGFXClientRpc();

            // Perpetuate Required Revive Changes.
            NetworkHealthComponent.Revive_Server(null); // Revive the character from the server.
            Movement.SetPositionAndRotation(spawnPosition, spawnRotation);
        }


        [Rpc(SendTo.ClientsAndHost)]
        private void ShowCharacterGFXClientRpc() => _gfxRoot.SetActive(true);
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayDeathEffectsClientRpc()
        {
            // Death Explosion.
            VisualEffects.SpecialFXGraphic deathExplosion = VisualEffects.SpecialFXPoolManager.GetFromPrefab(_deathExplosionEffectPrefab);
            deathExplosion.OnShutdownComplete += EffectShutdownComplete;
            deathExplosion.transform.SetPositionAndRotation(transform.position, transform.rotation);
            deathExplosion.Play();

            // Hide GFX.
            _gfxRoot.SetActive(false);

            void EffectShutdownComplete(VisualEffects.SpecialFXGraphic instance)
            {
                instance.OnShutdownComplete -= EffectShutdownComplete;
                VisualEffects.SpecialFXPoolManager.ReturnFromPrefab(_deathExplosionEffectPrefab, instance);
            }
        }

        #endregion



        #region Editor Testing Functions
#if UNITY_EDITOR

        [ContextMenu("Kill Character")]
        private void Editor_KillCharacter()
        {
            _networkHealthComponent.SetLifeState_Server(this, LifeState.Dead);
        }

#endif
        #endregion
    }


    public enum LifeState
    {
        Alive = 0,
        Dead = 1,
    }
    public class CharacterDeadEventArgs : System.EventArgs
    {
        public ServerCharacter Character;
        public ServerCharacter Inflicter;

        public CharacterDeadEventArgs(ServerCharacter character, ServerCharacter inflicter)
        {
            this.Character = character;
            this.Inflicter = inflicter;
        }
    }
}