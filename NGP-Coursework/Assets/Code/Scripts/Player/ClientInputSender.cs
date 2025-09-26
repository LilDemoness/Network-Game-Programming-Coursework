using Unity.Netcode;
using UnityEngine;

namespace UserInput
{
    /// <summary>
    ///     Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    // Based on 'https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/UserInput/ClientInputSender.cs'.
    public class ClientInputSender : NetworkBehaviour
    {
        // Cap our movement input rate to preserve network bandwith, but also keep it responsive.
        private const float MAX_MOVEMENT_RATE_SECONDS = 0.04f; // 25fps
        private float _lastSendMoveTime;
        private bool _hasMoveRequest;
        private Vector2 _movementInput;


        [SerializeField] private Player.PlayerMovement _playerMovement;


        private PlayerInputActions _inputActions;


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

            /*_inputActions.Combat.UsePrimaryWeapon.started += UsePrimaryWeapon_started;
            _inputActions.Combat.UsePrimaryWeapon.canceled += UsePrimaryWeapon_cancelled;

            _inputActions.Combat.UseSecondaryWeapon.started += UseSecondaryWeapon_started;
            _inputActions.Combat.UseSecondaryWeapon.canceled += UseSecondaryWeapon_cancelled;

            _inputActions.Combat.UseTertiaryWeapon.started += UseTertiaryWeapon_started;
            _inputActions.Combat.UseTertiaryWeapon.canceled += UseTertiaryWeapon_cancelled;*/

            #endregion


            // Enable the Action Maps.
            _inputActions.Enable();
        }
        private void DestroyInputActions()
        {
            // Unsubscribe from Input Events.
            //_inputActions.General.Movement.started -= Movement_performed;

            /*_inputActions.Combat.UsePrimaryWeapon.started -= UsePrimaryWeapon_started;
            _inputActions.Combat.UsePrimaryWeapon.canceled -= UsePrimaryWeapon_cancelled;

            _inputActions.Combat.UseSecondaryWeapon.started -= UseSecondaryWeapon_started;
            _inputActions.Combat.UseSecondaryWeapon.canceled -= UseSecondaryWeapon_cancelled;

            _inputActions.Combat.UseTertiaryWeapon.started -= UseTertiaryWeapon_started;
            _inputActions.Combat.UseTertiaryWeapon.canceled -= UseTertiaryWeapon_cancelled;*/


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
            if (_hasMoveRequest)
            {
                if ((Time.time - _lastSendMoveTime) > MAX_MOVEMENT_RATE_SECONDS)
                {
                    _hasMoveRequest = false;
                    _lastSendMoveTime = Time.time;

                    Debug.Log($"Processing Client Move: {_movementInput}");
                    _playerMovement.SetMovementInputServerRpc(_movementInput);
                }
            }
        }


        // For Value & Pass-Through Actions (Such as our movement), 'performed' is triggered whenever they are changed (Which is what we desire).
        private void Movement_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            _movementInput = obj.ReadValue<Vector2>();
            _hasMoveRequest = true;
            //Debug.Log("Input Received: " + obj.ReadValue<Vector2>());
        }
    }
}