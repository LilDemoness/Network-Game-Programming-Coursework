using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace Player
{
    public class PlayerInput : NetworkBehaviour
    {
        #region Events

        public event System.Action OnUsePrimaryWeaponStarted;
        public event System.Action OnUsePrimaryWeaponCancelled;

        public event System.Action OnUseSecondaryWeaponStarted;
        public event System.Action OnUseSecondaryWeaponCancelled;

        public event System.Action OnUseTertiaryWeaponStarted;
        public event System.Action OnUseTertiaryWeaponCancelled;

        #endregion


        private PlayerInputActions _inputActions;


        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }

            CreateInputActions();
        }
        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            DestroyInputActions();
        }


        private void CreateInputActions()
        {
            // Create the InputActionMap.
            _inputActions = new PlayerInputActions();


            // Subscribe to Input Events.
            _inputActions.Combat.UsePrimaryWeapon.started += UsePrimaryWeapon_started;
            _inputActions.Combat.UsePrimaryWeapon.canceled += UsePrimaryWeapon_cancelled;

            _inputActions.Combat.UseSecondaryWeapon.started += UseSecondaryWeapon_started;
            _inputActions.Combat.UseSecondaryWeapon.canceled += UseSecondaryWeapon_cancelled;

            _inputActions.Combat.UseTertiaryWeapon.started += UseTertiaryWeapon_started;
            _inputActions.Combat.UseTertiaryWeapon.canceled += UseTertiaryWeapon_cancelled;


            // Enable the Action Maps.
            _inputActions.Enable();
        }
        private void DestroyInputActions()
        {
            // Unsubscribe from Input Events.
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
            if (!IsOwner)
            {
                return;
            }

            // Continuous Input.
            HandleContinuousMovementInput();
        }


        private void HandleContinuousMovementInput()
        {

        }


        #region Input Processing Events

        private void UsePrimaryWeapon_started(InputAction.CallbackContext obj) => OnUsePrimaryWeaponStarted?.Invoke();
        private void UsePrimaryWeapon_cancelled(InputAction.CallbackContext obj) => OnUsePrimaryWeaponCancelled?.Invoke();

        private void UseSecondaryWeapon_started(InputAction.CallbackContext obj) => OnUseSecondaryWeaponStarted?.Invoke();
        private void UseSecondaryWeapon_cancelled(InputAction.CallbackContext obj) => OnUseSecondaryWeaponCancelled?.Invoke();

        private void UseTertiaryWeapon_started(InputAction.CallbackContext obj) => OnUseTertiaryWeaponStarted?.Invoke();
        private void UseTertiaryWeapon_cancelled(InputAction.CallbackContext obj) => OnUseTertiaryWeaponCancelled?.Invoke();


        #endregion
    }
}