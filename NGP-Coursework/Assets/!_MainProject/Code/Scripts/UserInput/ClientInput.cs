using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace UserInput
{
    public class ClientInput : NetworkBehaviour
    {
        public static Vector2 MovementInput { get; private set; }
        public static event System.Action OnMovementInputChanged;
        private static Vector2 s_previousMovementInput;


        #region Combat Events

        public static event System.Action<int> OnActivateSlotStarted;
        public static event System.Action<int> OnActivateSlotCancelled;

        #endregion

        #region UI Events

        // Tabs.
        public static event System.Action OnNextTabPerformed;
        public static event System.Action OnPreviousTabPerformed;

        // Confirmation.
        public static event System.Action OnConfirmPerformed;

        // Customisation UI.
        public static event System.Action OnOpenFrameSelectionPerformed;

        #endregion


        private PlayerInputActions _inputActions;
        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                this.enabled = false;
                return;
            }
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
            #region General Events



            #endregion

            #region Combat Events

            _inputActions.Combat.ActivateSlot0.started  += ActivateSlot0_started;
            _inputActions.Combat.ActivateSlot0.canceled += ActivateSlot0_cancelled;

            _inputActions.Combat.ActivateSlot1.started  += ActivateSlot1_started;
            _inputActions.Combat.ActivateSlot1.canceled += ActivateSlot1_cancelled;

            _inputActions.Combat.ActivateSlot2.started  += ActivateSlot2_started;
            _inputActions.Combat.ActivateSlot2.canceled += ActivateSlot2_cancelled;

            _inputActions.Combat.ActivateSlot3.started  += ActivateSlot3_started;
            _inputActions.Combat.ActivateSlot3.canceled += ActivateSlot3_cancelled;

            #endregion

            #region UI Events

            _inputActions.UI.OpenFrameSelection.performed += OpenFrameSelection_performed;
            _inputActions.UI.Confirm.performed += Confirm_performed;
            _inputActions.UI.NextTab.performed += NextTab_performed;
            _inputActions.UI.PreviousTab.performed += PreviousTab_performed;
            _inputActions.UI.Navigate.performed += Navigate_performed;

            #endregion


            // Enable the Action Maps.
            _inputActions.Enable();
        }
        private void DestroyInputActions()
        {
            // Unsubscribe from Input Events.
            #region General Events

            // A.

            #endregion

            #region Combat Events

            _inputActions.Combat.ActivateSlot0.started  -= ActivateSlot0_started;
            _inputActions.Combat.ActivateSlot0.canceled -= ActivateSlot0_cancelled;

            _inputActions.Combat.ActivateSlot1.started  -= ActivateSlot1_started;
            _inputActions.Combat.ActivateSlot1.canceled -= ActivateSlot1_cancelled;

            _inputActions.Combat.ActivateSlot2.started  -= ActivateSlot2_started;
            _inputActions.Combat.ActivateSlot2.canceled -= ActivateSlot2_cancelled;

            _inputActions.Combat.ActivateSlot3.started  -= ActivateSlot3_started;
            _inputActions.Combat.ActivateSlot3.canceled -= ActivateSlot3_cancelled;

            #endregion

            #region UI Events

            _inputActions.UI.OpenFrameSelection.performed   -= OpenFrameSelection_performed;
            _inputActions.UI.Confirm.performed              -= Confirm_performed;
            _inputActions.UI.NextTab.performed              -= NextTab_performed;
            _inputActions.UI.PreviousTab.performed          -= PreviousTab_performed;
            _inputActions.UI.Navigate.performed             -= Navigate_performed;

            #endregion


            // Dispose of the Input Actions.
            _inputActions.Dispose();

            // Remove our Reference.
            _inputActions = null;
        }


        private void Update()
        {
            if (_inputActions == null)
                return;

            MovementInput = _inputActions.General.Movement.ReadValue<Vector2>();
            if (MovementInput != s_previousMovementInput)
            {
                OnMovementInputChanged?.Invoke();
                s_previousMovementInput = MovementInput;
            }
        }


        #region Combat Event Functions

        private void ActivateSlot0_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(0);
        private void ActivateSlot0_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(0);

        private void ActivateSlot1_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(1);
        private void ActivateSlot1_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(1);

        private void ActivateSlot2_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(2);
        private void ActivateSlot2_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(2);

        private void ActivateSlot3_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(3);
        private void ActivateSlot3_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(3);

        #endregion

        #region UI Event Functions

        private void Navigate_performed(InputAction.CallbackContext obj) { }
        private void NextTab_performed(InputAction.CallbackContext obj) => OnNextTabPerformed?.Invoke();
        private void PreviousTab_performed(InputAction.CallbackContext obj) => OnPreviousTabPerformed?.Invoke();

        private void OpenFrameSelection_performed(InputAction.CallbackContext obj) => OnOpenFrameSelectionPerformed?.Invoke();
        private void Confirm_performed(InputAction.CallbackContext obj) => OnConfirmPerformed?.Invoke();

        #endregion
    }
}