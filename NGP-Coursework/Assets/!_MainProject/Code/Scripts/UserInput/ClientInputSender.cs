using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects;
using Gameplay.Actions;

namespace UserInput
{
    /// <summary>
    ///     Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    // Based on 'https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/UserInput/ClientInputSender.cs'.
    [RequireComponent(typeof(ServerCharacter))]
    public class ClientInputSender : NetworkBehaviour
    {
        private enum ActionType
        {
            StartShooting,
            StopShooting
        }
        private struct ActionRequest
        {
            public ActionID RequestedActionID;
            public ActionType ActionType;
            public int SlotIdentifier;
        }

        /// <summary>
        ///     A List of ActionRequests that have been received since the last FixedUpdate run.
        ///     This is a static array to avoid allocs, and because we don't want the list to grow indefinitely.
        /// </summary>
        private readonly ActionRequest[] _actionRequests = new ActionRequest[5];

        /// <summary>
        ///     The number of ActionRequests that have been queued since the last FixedUpdate.
        /// </summary>
        int _actionRequestCount;


        // Cap our movement input rate to preserve network bandwith, but also keep it responsive.
        private const float MAX_MOVEMENT_RATE_SECONDS = 0.04f; // 25fps
        private float _lastSendMoveTime;

        private bool _hasMoveRequest;
        private Vector2 _movementInput;


        /// <summary>
        ///     This event fires at the time when an action event is sent to the server.
        /// </summary>
        public event System.Action<ActionRequestData> ActionInputEvent;


        [SerializeField] private ServerCharacter _serverCharacter;


        private PlayerInputActions _inputActions;


        [Header("(Temp) Weapon Actions")]
        [SerializeField] private ActionDefinition _primaryWeaponAction;
        private ActionState _primaryWeaponActionState;

        [SerializeField] private ActionDefinition _secondaryWeaponAction;
        private ActionState _scondaryWeaponActionState;

        [SerializeField] private ActionDefinition _tertiaryWeaponAction;
        private ActionState _tertiaryWeaponActionState;

        [Space(5)]
        [SerializeField] private ActionDefinition _testCancelAction;
        private ActionState _testCancelActionState;


        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                this.enabled = false;
                return;
            }

            _primaryWeaponActionState = new ActionState() { ActionID = _primaryWeaponAction.ActionID };
            _scondaryWeaponActionState = new ActionState() { ActionID = _secondaryWeaponAction.ActionID };
            _tertiaryWeaponActionState = new ActionState() { ActionID = _tertiaryWeaponAction.ActionID };
            _testCancelActionState = new ActionState() { ActionID = _testCancelAction.ActionID };


            // Setup our InputActionMap.
            CreateInputActions();
        }
        public override void OnNetworkDespawn()
        {
            if (_inputActions != null)
            {
                // Dispose of our InputActionMap.
                DestroyInputActions();
            }
        }
        // Note: We can possible remove this?
        public override void OnDestroy()
        {
            base.OnDestroy();

            if (_inputActions != null)
            {
                // Dispose of our InputActionMap.
                DestroyInputActions();
            }
        }


        private void CreateInputActions()
        {
            // Create the InputActionMap.
            _inputActions = new PlayerInputActions();


            // Subscribe to Input Events.
            #region Movement

            //_inputActions.General.Movement.performed += Movement_performed;

            #endregion

            #region Combat

            _inputActions.Combat.UsePrimaryWeapon.started += UsePrimaryWeapon_started;
            _inputActions.Combat.UsePrimaryWeapon.canceled += UsePrimaryWeapon_cancelled;

            _inputActions.Combat.UseSecondaryWeapon.started += UseSecondaryWeapon_started;
            _inputActions.Combat.UseSecondaryWeapon.canceled += UseSecondaryWeapon_cancelled;

            _inputActions.Combat.UseTertiaryWeapon.started += UseTertiaryWeapon_started;
            _inputActions.Combat.UseTertiaryWeapon.canceled += UseTertiaryWeapon_cancelled;

            #endregion


            // Enable the Action Maps.
            _inputActions.Enable();
        }
        private void DestroyInputActions()
        {
            // Unsubscribe from Input Events.
            //_inputActions.General.Movement.started -= Movement_performed;

            _inputActions.Combat.UsePrimaryWeapon.started -= UsePrimaryWeapon_started;
            _inputActions.Combat.UsePrimaryWeapon.canceled -= UsePrimaryWeapon_cancelled;

            _inputActions.Combat.UseSecondaryWeapon.started -= UseSecondaryWeapon_started;
            _inputActions.Combat.UseSecondaryWeapon.canceled -= UseSecondaryWeapon_cancelled;

            _inputActions.Combat.UseTertiaryWeapon.started -= UseTertiaryWeapon_started;
            _inputActions.Combat.UseTertiaryWeapon.canceled -= UseTertiaryWeapon_cancelled;


            // Dispose of the Input Actions.
            _inputActions.Dispose();

            // Remove our Reference.
            _inputActions = null;
        }


        private void Update()
        {
            // Get Framewise Input.
            Vector2 newMovementInput = _inputActions.General.Movement.ReadValue<Vector2>();
            if (newMovementInput != _movementInput)
            {
                _movementInput = newMovementInput;
                _hasMoveRequest = true;
            }
        }
        private void FixedUpdate()
        {
            // Send Non-client Only Input Requests to the Server.
            // Play All ActionRequests (In FIFO order).
            for(int i = 0; i < _actionRequestCount; ++i)
            {
                ActionDefinition actionDefinition;
                switch (_actionRequests[i].ActionType)
                {
                    case ActionType.StartShooting:
                        actionDefinition = GameDataSource.Instance.GetActionDefinitionByID(_actionRequests[i].RequestedActionID);
                        /*if (actionPrototype.Config.ActionInput != null)
                        {
                            var skillPlayer = Instantiate(actionPrototype.Config.ActionInput);
                            skillPlayer.Initialise(_serverCharacter, transform.position, actionPrototype.ActionID, SendInput, null);
                        }
                        else
                        {*/
                            var data = ActionRequestData.Create(actionDefinition);
                            data.SlotIdentifier = _actionRequests[i].SlotIdentifier;

                            SendInput(data);
                        //}
                        break;
                    case ActionType.StopShooting:
                        actionDefinition = GameDataSource.Instance.GetActionDefinitionByID(_actionRequests[i].RequestedActionID);
                        /*if (actionPrototype.Config.ActionInput != null)
                        {
                            var skillPlayer = Instantiate(actionPrototype.Config.ActionInput);
                            skillPlayer.Initialise(_serverCharacter, transform.position, actionPrototype.ActionID, SendInput, null);
                        }
                        else
                        {*/
                            data = ActionRequestData.Create(actionDefinition);
                            data.SlotIdentifier = _actionRequests[i].SlotIdentifier;

                            SendInput(data);
                        //}
                        break;
                }
            }

            _actionRequestCount = 0;


            if (_hasMoveRequest)
            {
                if ((Time.time - _lastSendMoveTime) > MAX_MOVEMENT_RATE_SECONDS)
                {
                    _hasMoveRequest = false;
                    _lastSendMoveTime = Time.time;

                    Debug.Log($"Processing Client Move: {_movementInput}");
                    _serverCharacter.SendCharacterMovementInputServerRpc(_movementInput);
                }
            }
        }

        private void SendInput(ActionRequestData action)
        {
            ActionInputEvent?.Invoke(action);
            _serverCharacter.PlayActionServerRpc(action);
        }


        private void RequestAction(ActionType actionType, ActionState actionState, int slotIdentifier = -1)
        {
            if (_actionRequestCount < _actionRequests.Length)
            {
                _actionRequests[_actionRequestCount].RequestedActionID = actionState.ActionID;
                _actionRequests[_actionRequestCount].ActionType = actionType;
                _actionRequests[_actionRequestCount].SlotIdentifier = slotIdentifier;
                ++_actionRequestCount;
            }
        }


        // For Value & Pass-Through Actions (Such as our movement), 'performed' is triggered whenever they are changed (Which is what we desire).
        private void Movement_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            _movementInput = obj.ReadValue<Vector2>();
            _hasMoveRequest = true;
            //Debug.Log("Input Received: " + obj.ReadValue<Vector2>());
        }
        

        private void UsePrimaryWeapon_started(InputAction.CallbackContext obj) => RequestAction(ActionType.StartShooting, _primaryWeaponActionState, 1);
        private void UsePrimaryWeapon_cancelled(InputAction.CallbackContext obj) => RequestAction(ActionType.StopShooting, _testCancelActionState, 1);

        private void UseSecondaryWeapon_started(InputAction.CallbackContext obj) => RequestAction(ActionType.StartShooting, _scondaryWeaponActionState, 2);
        private void UseSecondaryWeapon_cancelled(InputAction.CallbackContext obj) => RequestAction(ActionType.StopShooting, _testCancelActionState, 2);

        private void UseTertiaryWeapon_started(InputAction.CallbackContext obj) => RequestAction(ActionType.StartShooting, _tertiaryWeaponActionState, 3);
        private void UseTertiaryWeapon_cancelled(InputAction.CallbackContext obj) => RequestAction(ActionType.StopShooting, _testCancelActionState, 3);
    }

    public class ActionState
    {
        public ActionID ActionID { get; internal set; }

        internal void SetActionState(ActionID newActionID)
        {
            this.ActionID = newActionID;
        }
    }
}