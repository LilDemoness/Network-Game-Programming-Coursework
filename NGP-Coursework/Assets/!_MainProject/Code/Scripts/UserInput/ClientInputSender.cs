using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects;
using Gameplay.Actions;
using Gameplay.Actions.Definitions;

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

            public Vector3 Origin;
            public Vector3 Direction;
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


        [SerializeField] private ServerCharacter _serverCharacter;


        private PlayerInputActions _inputActions;


        [SerializeField] private ServerWeaponController _serverWeaponController;

        
        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                this.enabled = false;
                return;
            }


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
                
                switch (_actionRequests[i].ActionType)
                {
                    case ActionType.StartShooting:
                    case ActionType.StopShooting:
                        // Get our Action Definition.
                        ActionDefinition actionDefinition = GameDataSource.Instance.GetActionDefinitionByID(_actionRequests[i].RequestedActionID);

                        // Create our Data.
                        ActionRequestData data = ActionRequestData.Create(actionDefinition);
                        data.Position = _actionRequests[i].Origin;
                        data.Direction = _actionRequests[i].Direction;
                        data.SlotIdentifier = _actionRequests[i].SlotIdentifier;

                        // Send our Input.
                        SendInput(data);
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

                    _serverCharacter.SendCharacterMovementInputServerRpc(_movementInput);
                }
            }
        }

        private void SendInput(ActionRequestData action) => _serverCharacter.PlayActionServerRpc(action);
        


        /*private void RequestAction(ActionType actionType, ActionDefinition actionState, Vector3 origin = default, Vector3 direction = default, int slotIdentifier = -1)
        {
            if (_actionRequestCount < _actionRequests.Length)
            {
                _actionRequests[_actionRequestCount].RequestedActionID = actionState.ActionID;
                _actionRequests[_actionRequestCount].ActionType = actionType;

                _actionRequests[_actionRequestCount].Origin = origin;
                _actionRequests[_actionRequestCount].Direction = direction;
                _actionRequests[_actionRequestCount].SlotIdentifier = slotIdentifier;
                ++_actionRequestCount;
            }
        }*/


        private void UsePrimaryWeapon_started(InputAction.CallbackContext obj) => _serverWeaponController.StartFiringPrimaryWeaponServerRpc();
        private void UsePrimaryWeapon_cancelled(InputAction.CallbackContext obj) => _serverWeaponController.StopFiringPrimaryWeaponServerRpc();

        private void UseSecondaryWeapon_started(InputAction.CallbackContext obj) => _serverWeaponController.StartFiringSecondaryWeaponServerRpc();
        private void UseSecondaryWeapon_cancelled(InputAction.CallbackContext obj) => _serverWeaponController.StopFiringSecondaryWeaponServerRpc();

        private void UseTertiaryWeapon_started(InputAction.CallbackContext obj) => _serverWeaponController.StartFiringTertiaryWeaponServerRpc();
        private void UseTertiaryWeapon_cancelled(InputAction.CallbackContext obj) => _serverWeaponController.StopFiringTertiaryWeaponServerRpc();
    }
}