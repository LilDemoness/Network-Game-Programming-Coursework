using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace Labs
{
    public class PlayerInput : NetworkBehaviour
    {
        #region Continuous Input

        public Vector2 MovementInput { get; private set; }
        public Vector2 NormalizedMovementInput { get; private set; }

        #endregion

        #region Input Events

        public System.Action OnJumpPerformed;

        #endregion


        private PlayerInputActions _inputActions;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (NetworkManager.Singleton.LocalClientId != this.OwnerClientId)
            {
                Destroy(this);
                return;
            }

            CreateInputActions();
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            DisposeInputActions();
        }


        private void Update()
        {
            MovementInput = _inputActions.General.Movement.ReadValue<Vector2>();
            NormalizedMovementInput = MovementInput.normalized;
        }


        private void CreateInputActions()
        {
            _inputActions = new PlayerInputActions();


            // Subscribe to Input Events.
            _inputActions.General.Jump.performed += Jump_performed;


            // Enable the Input Actions Map.
            _inputActions.Enable();
        }
        private void DisposeInputActions()
        {
            // Unsubscribe from Input Events.
            _inputActions.General.Jump.performed -= Jump_performed;


            // Dispose of the Input Acitons Map.
            _inputActions.Dispose();
        }


        #region Input Event Functions

        private void Jump_performed(InputAction.CallbackContext obj) => OnJumpPerformed?.Invoke();

        #endregion
    }
}