using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace UserInput
{
    /// <summary>
    ///     Script handling the creation of a <see cref="PlayerInputActions"/> action map & relaying it's button presses through events.
    /// </summary>
    public class ClientInput : MonoBehaviour
    {
        // Prevent Multiple Instances
        private static ClientInput s_instance;


        public static Vector2 MovementInput { get; private set; }
        public static event System.Action OnMovementInputChanged;
        private static Vector2 s_previousMovementInput;

        public static Vector2 LookInput { get; private set; }


        #region Combat Events

        public static event System.Action<int> OnActivateSlotStarted;
        public static event System.Action<int> OnActivateSlotCancelled;

        #endregion

        #region UI Events


        public static event System.Action<Vector2> OnNavigatePerformed;
        
        // Tabs.
        public static event System.Action OnNextTabPerformed;
        public static event System.Action OnPreviousTabPerformed;

        // Confirmation.
        public static event System.Action OnConfirmPerformed;

        // Customisation UI.
        public static event System.Action OnOpenFrameSelectionPerformed;

        // Other.
        public static event System.Action OnToggleLeaderboardPerformed;

        #endregion

        #region Multiplayer Chat Events

        public static event System.Action OnOpenChatPerformed;
        public static event System.Action OnSubmitChatPerformed;
        public static event System.Action OnCancelChatPerformed;

        #endregion


        #region Input Device Type

        private const string MOUSE_AND_KEYBOARD_CONTROL_SCHEME_NAME = "MnK";
        private const string GAMEPAD_CONTROL_SCHEME_NAME = "Gamepad";

        private static InputControlScheme s_currentControlScheme = default;
        public static InputControl CurrentInputDevice {  get; private set; }
        public static event Action OnInputDeviceChanged;

        public enum DeviceType { MnK, Gamepad }
        public static DeviceType LastUsedDevice => s_currentControlScheme.name == MOUSE_AND_KEYBOARD_CONTROL_SCHEME_NAME ? DeviceType.MnK : DeviceType.Gamepad;
        public static string LastUsedDeviceName => s_currentControlScheme.name;

        #endregion


        static ClientInput()
        {
            InitialiseInputPrevention();
        }


        private static PlayerInputActions s_inputActions;
        private void Awake()
        {
            if (s_instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            else
            {
                s_instance = this;
                DontDestroyOnLoad(this.gameObject);
            }

            CreateInputActions();

            InputSystem.onDeviceChange += InputSystem_onDeviceChange;
            InputUser.onUnpairedDeviceUsed += InputUser_onUnpairedDeviceUsed;
            InputUser.listenForUnpairedDeviceActivity = 1;
        }
        private void OnDestroy()
        {
            if (s_instance != this)
                return; // Not the instance.
            
            if (s_inputActions != null)
                DestroyInputActions();  // Dispose of our InputActionMap.

            InputSystem.onDeviceChange -= InputSystem_onDeviceChange;
            InputUser.onUnpairedDeviceUsed -= InputUser_onUnpairedDeviceUsed;
        }
        private void CreateInputActions()
        {
            // Create the InputActionMap.
            s_inputActions = new PlayerInputActions();


            // Subscribe to Input Events.
            #region Combat Events

            s_inputActions.Combat.ActivateSlot0.started  += ActivateSlot0_started;
            s_inputActions.Combat.ActivateSlot0.canceled += ActivateSlot0_cancelled;

            s_inputActions.Combat.ActivateSlot1.started  += ActivateSlot1_started;
            s_inputActions.Combat.ActivateSlot1.canceled += ActivateSlot1_cancelled;

            s_inputActions.Combat.ActivateSlot2.started  += ActivateSlot2_started;
            s_inputActions.Combat.ActivateSlot2.canceled += ActivateSlot2_cancelled;

            s_inputActions.Combat.ActivateSlot3.started  += ActivateSlot3_started;
            s_inputActions.Combat.ActivateSlot3.canceled += ActivateSlot3_cancelled;

            #endregion

            #region UI Events

            s_inputActions.UI.OpenFrameSelection.performed += OpenFrameSelection_performed;
            s_inputActions.UI.Confirm.performed += Confirm_performed;
            s_inputActions.UI.NextTab.performed += NextTab_performed;
            s_inputActions.UI.PreviousTab.performed += PreviousTab_performed;
            s_inputActions.UI.Navigate.performed += Navigate_performed;
            s_inputActions.UI.ToggleLeaderboardUI.performed += ToggleLeaderboardUI_performed;

            #endregion

            #region Multiplayer Chat Events

            s_inputActions.MultiplayerChat.OpenChat.performed   += OpenChat_performed;
            s_inputActions.MultiplayerChat.SubmitChat.performed += SubmitChat_performed;
            s_inputActions.MultiplayerChat.CancelChat.performed += CancelChat_performed;

            #endregion


            // Enable the Input Actions.
            s_inputActions.Enable();

            // Ensure that the correct maps are enabled based on toggles.
            EnsureCorrectInputMapActivation();
        }
        private void DestroyInputActions()
        {
            // Unsubscribe from Input Events.
            #region Combat Events

            s_inputActions.Combat.ActivateSlot0.started  -= ActivateSlot0_started;
            s_inputActions.Combat.ActivateSlot0.canceled -= ActivateSlot0_cancelled;

            s_inputActions.Combat.ActivateSlot1.started  -= ActivateSlot1_started;
            s_inputActions.Combat.ActivateSlot1.canceled -= ActivateSlot1_cancelled;

            s_inputActions.Combat.ActivateSlot2.started  -= ActivateSlot2_started;
            s_inputActions.Combat.ActivateSlot2.canceled -= ActivateSlot2_cancelled;

            s_inputActions.Combat.ActivateSlot3.started  -= ActivateSlot3_started;
            s_inputActions.Combat.ActivateSlot3.canceled -= ActivateSlot3_cancelled;

            #endregion

            #region UI Events

            s_inputActions.UI.OpenFrameSelection.performed      -= OpenFrameSelection_performed;
            s_inputActions.UI.Confirm.performed                 -= Confirm_performed;
            s_inputActions.UI.NextTab.performed                 -= NextTab_performed;
            s_inputActions.UI.PreviousTab.performed             -= PreviousTab_performed;
            s_inputActions.UI.Navigate.performed                -= Navigate_performed;
            s_inputActions.UI.ToggleLeaderboardUI.performed     -= ToggleLeaderboardUI_performed;

            #endregion

            #region Multiplayer Chat Events

            s_inputActions.MultiplayerChat.OpenChat.performed   -= OpenChat_performed;
            s_inputActions.MultiplayerChat.SubmitChat.performed -= SubmitChat_performed;
            s_inputActions.MultiplayerChat.CancelChat.performed -= CancelChat_performed;

            #endregion


            // Dispose of the Input Actions.
            s_inputActions.Dispose();

            // Remove our Reference.
            s_inputActions = null;
        }


        private void Update()
        {
            if (s_inputActions == null)
                return;

            // Cache our movement input & notify listeners if it's changed since the last notification.
            MovementInput = s_inputActions.Movement.Movement.ReadValue<Vector2>();
            if (MovementInput != s_previousMovementInput)
            {
                OnMovementInputChanged?.Invoke();
                s_previousMovementInput = MovementInput;
            }

            LookInput = s_inputActions.Camera.LookInput.ReadValue<Vector2>();
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

        private void Navigate_performed(InputAction.CallbackContext obj) => OnNavigatePerformed?.Invoke(obj.ReadValue<Vector2>());
        private void NextTab_performed(InputAction.CallbackContext obj) => OnNextTabPerformed?.Invoke();
        private void PreviousTab_performed(InputAction.CallbackContext obj) => OnPreviousTabPerformed?.Invoke();

        private void OpenFrameSelection_performed(InputAction.CallbackContext obj) => OnOpenFrameSelectionPerformed?.Invoke();
        private void Confirm_performed(InputAction.CallbackContext obj) => OnConfirmPerformed?.Invoke();

        private void ToggleLeaderboardUI_performed(InputAction.CallbackContext obj) => OnToggleLeaderboardPerformed?.Invoke();

        #endregion

        #region Multiplayer Chat Event Functions

        private void OpenChat_performed(InputAction.CallbackContext obj) => OnOpenChatPerformed?.Invoke();
        private void SubmitChat_performed(InputAction.CallbackContext obj) => OnSubmitChatPerformed?.Invoke();
        private void CancelChat_performed(InputAction.CallbackContext obj) => OnCancelChatPerformed?.Invoke();

        #endregion
        



        #region Input Prevention

        private static void InitialiseInputPrevention()
        {
            s_movementPreventionDictionary = new Dictionary<Type, int>();
            s_cameraPreventionDictionary = new Dictionary<Type, int>();
            s_combatPreventionDictionary = new Dictionary<Type, int>();
            s_uiPreventionDictionary = new Dictionary<Type, int>();
        }
        /// <summary>
        ///     Reset all instances of input prevention.
        /// </summary>
        public static void ResetInputPrevention()
        {
            s_movementPreventionDictionary = new Dictionary<Type, int>();
            s_cameraPreventionDictionary = new Dictionary<Type, int>();
            s_combatPreventionDictionary = new Dictionary<Type, int>();
            s_uiPreventionDictionary = new Dictionary<Type, int>();
        }
        /// <summary>
        ///     Ensure that all Input Action Maps are correctly activated/deactivated based on their current preventions.
        /// </summary>
        public static void EnsureCorrectInputMapActivation()
        {
            if (s_inputActions == null)
                return; // No InputActions have been created.

            // Movement.
            if (s_movementPreventionDictionary.Count > 0)
                s_inputActions.Movement.Disable();
            else
                s_inputActions.Movement.Enable();


            // Camera.
            if (s_cameraPreventionDictionary.Count > 0)
                s_inputActions.Camera.Disable();
            else
                s_inputActions.Camera.Enable();


            // Combat.
            if (s_combatPreventionDictionary.Count > 0)
                s_inputActions.Combat.Disable();
            else
                s_inputActions.Combat.Enable();


            // UI.
            if (s_uiPreventionDictionary.Count > 0)
                s_inputActions.UI.Disable();
            else
                s_inputActions.UI.Enable();
        }


        [System.Serializable] [System.Flags]
        public enum ActionTypes
        {
            None = 0,

            Movement    = 1 << 0,   // Standard Character Movement (WASD/Stick)
            Camera      = 1 << 1,   // Camera Controls
            Combat      = 1 << 2,   // Weapon/Ability Activation
            UI          = 1 << 3,   // Main, Pause, and Customisation Menus
            MultiplayerChat = 1 << 4,   // Text and Voice Chat Input

            Respawning = ActionTypes.Movement | ActionTypes.Camera | ActionTypes.Combat,

            Everything = ~0
        }
        /// <summary>
        ///     Prevent the desired actions from being performed.
        /// </summary>
        /// <param name="lockingType"> The type of the object that is locking these actions.</param>
        /// <param name="actionsToLock"> The types of actions that we are locking.</param>
        public static void PreventActions(Type lockingType, ActionTypes actionsToLock)
        {
            if (actionsToLock.HasFlag(ActionTypes.Movement))
                PreventMovementActions(lockingType);
            if (actionsToLock.HasFlag(ActionTypes.Camera))
                PreventCameraActions(lockingType);
            if (actionsToLock.HasFlag(ActionTypes.Combat))
                PreventCombatActions(lockingType);
            if (actionsToLock.HasFlag(ActionTypes.UI))
                PreventUIActions(lockingType);
        }
        /// <summary>
        ///     Remove one count of prevention from the desired types of actions. <br/>
        ///     Enables the actions if there is no longer any types wishing for prevention.
        /// </summary>
        /// <param name="lockingType"> The type of the object that had locked these actions.</param>
        /// <param name="actionsToUnlock"> The types of actions that we are unlocking.</param>
        public static void RemoveActionPrevention(Type lockingType, ActionTypes actionsToUnlock)
        {
            if (actionsToUnlock.HasFlag(ActionTypes.Movement))
                RemoveMovementActionPrevention(lockingType);
            if (actionsToUnlock.HasFlag(ActionTypes.Camera))
                RemoveCameraActionPrevention(lockingType);
            if (actionsToUnlock.HasFlag(ActionTypes.Combat))
                RemoveCombatActionPrevention(lockingType);
            if (actionsToUnlock.HasFlag(ActionTypes.UI))
                RemoveUIActionPrevention(lockingType);
        }


#if UNITY_EDITOR

        [ContextMenu(itemName: "Display Active Locks")]
        private void DisplayLocks()
        {
            string movementPreventingTypes = string.Concat(s_movementPreventionDictionary.Keys);
            Debug.Log(s_movementPreventionDictionary.Count + "\n" + movementPreventingTypes);

            string cameraPreventingTypes = string.Concat(s_cameraPreventionDictionary.Keys);
            Debug.Log(s_cameraPreventionDictionary.Count + "\n" + cameraPreventingTypes);

            string combatPreventingTypes = string.Join(", ", s_combatPreventionDictionary.Keys);
            Debug.Log(s_combatPreventionDictionary.Count + "\n" + combatPreventingTypes);

            string uiPreventingTypes = string.Join(", ", s_uiPreventionDictionary.Keys);
            Debug.Log(s_uiPreventionDictionary.Count + "\n" + uiPreventingTypes);
        }

#endif



        #region Movement

        private static Dictionary<Type, int> s_movementPreventionDictionary;
        /// <summary>
        ///     Prevent the client from performing Movement actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is locking Movement actions.</param>
        public static void PreventMovementActions(Type lockingType)
        {
            if (!s_movementPreventionDictionary.TryAdd(lockingType, 1)) // If we have no kvp with key 'lockingType' initialise one with 1 count.
            {
                // We already have a kvp with key 'lockingType', so increment instead.
                ++s_movementPreventionDictionary[lockingType];
            }

            if (s_inputActions != null)
            {
                // At least one type is disabling movement controls.
                // Disable our 'Movement' map.
                s_inputActions.Movement.Disable();
            }
        }
        /// <summary>
        ///     Remove one count of the passed type from Movement action prevention.<br/>
        ///     Re-enables the Action Map if there are no longer any types wishing to disable Movement actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is stopping its Movement action locking.</param>
        public static void RemoveMovementActionPrevention(Type lockingType)
        {
            if (s_movementPreventionDictionary.ContainsKey(lockingType))
            {
                --s_movementPreventionDictionary[lockingType];

                if (s_movementPreventionDictionary[lockingType] <= 0)
                {
                    s_movementPreventionDictionary.Remove(lockingType);
                }
            }

            if (s_movementPreventionDictionary.Count == 0 && s_inputActions != null)
            {
                // There are no longer any types wishing to disable our movement controls.
                // Enable the 'Movement' map.
                s_inputActions.Movement.Enable();
            }
        }

        #endregion

        #region Camera

        private static Dictionary<Type, int> s_cameraPreventionDictionary;
        /// <summary>
        ///     Prevent the client from performing Camera actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is locking Camera actions.</param>
        public static void PreventCameraActions(Type lockingType)
        {
            if (!s_cameraPreventionDictionary.TryAdd(lockingType, 1)) // If we have no kvp with key 'lockingType' initialise one with 1 count.
            {
                // We already have a kvp with key 'lockingType', so increment instead.
                ++s_cameraPreventionDictionary[lockingType];
            }

            if (s_inputActions != null)
            {
                // At least one type is disabling camera controls.
                // Disable our 'Camera' map.
                s_inputActions.Camera.Disable();
            }
        }
        /// <summary>
        ///     Remove one count of the passed type from Camera action prevention.<br/>
        ///     Re-enables the Action Map if there are no longer any types wishing to disable Camera actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is stopping its Camera action locking.</param>
        public static void RemoveCameraActionPrevention(Type lockingType)
        {
            if (s_cameraPreventionDictionary.ContainsKey(lockingType))
            {
                --s_cameraPreventionDictionary[lockingType];

                if (s_cameraPreventionDictionary[lockingType] <= 0)
                {
                    s_cameraPreventionDictionary.Remove(lockingType);
                }
            }

            if (s_cameraPreventionDictionary.Count == 0 && s_inputActions != null)
            {
                // There are no longer any types wishing to disable our camera controls.
                // Enable the 'Camera' map.
                s_inputActions.Camera.Enable();
            }
        }

        #endregion

        #region Combat

        private static Dictionary<Type, int> s_combatPreventionDictionary;
        /// <summary>
        ///     Prevent the client from performing Combat actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is locking Combat actions.</param>
        public static void PreventCombatActions(Type lockingType)
        {
            if (!s_combatPreventionDictionary.TryAdd(lockingType, 1)) // If we have no kvp with key 'lockingType' initialise one with 1 count.
            {
                // We already have a kvp with key 'lockingType', so increment instead.
                ++s_combatPreventionDictionary[lockingType];
            }

            if (s_inputActions != null)
            {
                // At least one type is disabling combat controls.
                // Disable our 'Combat' map.
                s_inputActions.Combat.Disable();
            }
        }
        /// <summary>
        ///     Remove one count of the passed type from Combat action prevention.<br/>
        ///     Re-enables the Action Map if there are no longer any types wishing to disable Combat actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is stopping its Combat action locking.</param>
        public static void RemoveCombatActionPrevention(Type lockingType)
        {
            if (s_combatPreventionDictionary.ContainsKey(lockingType))
            {
                --s_combatPreventionDictionary[lockingType];

                if (s_combatPreventionDictionary[lockingType] <= 0)
                {
                    s_combatPreventionDictionary.Remove(lockingType);
                }
            }

            if (s_combatPreventionDictionary.Count == 0 && s_inputActions != null)
            {
                // There are no longer any types wishing to disable our combat controls.
                // Enable the 'Combat' map.
                s_inputActions.Combat.Enable();
            }
        }

        #endregion

        #region UI

        private static Dictionary<Type, int> s_uiPreventionDictionary;
        /// <summary>
        ///     Prevent the client from performing UI actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is locking UI actions.</param>
        public static void PreventUIActions(Type lockingType)
        {
            if (!s_uiPreventionDictionary.TryAdd(lockingType, 1)) // If we have no kvp with key 'lockingType' initialise one with 1 count.
            {
                // We already have a kvp with key 'lockingType', so increment instead.
                ++s_uiPreventionDictionary[lockingType];
            }

            if (s_inputActions != null)
            {
                // At least one type is disabling UI controls.
                // Disable our 'UI' map.
                s_inputActions.UI.Disable();
            }
        }
        /// <summary>
        ///     Remove one count of the passed type from UI action prevention.<br/>
        ///     Re-enables the Action Map if there are no longer any types wishing to disable UI actions.
        /// </summary>
        /// <param name="lockingType"> The type of the source object that is stopping its UI action locking.</param>
        public static void RemoveUIActionPrevention(Type lockingType)
        {
            if (s_uiPreventionDictionary.ContainsKey(lockingType))
            {
                --s_uiPreventionDictionary[lockingType];

                if (s_uiPreventionDictionary[lockingType] <= 0)
                {
                    s_uiPreventionDictionary.Remove(lockingType);
                }
            }

            if (s_uiPreventionDictionary.Count == 0 && s_inputActions != null)
            {
                // There are no longer any types wishing to disable our UI controls.
                // Enable the 'UI' map.
                s_inputActions.UI.Enable();
            }
        }

        #endregion

        #endregion


        #region Active Device Detection

        private void InputSystem_onDeviceChange(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
        {
            switch (inputDeviceChange)
            {
                case InputDeviceChange.Disconnected:
                    Debug.Log("User's device was disconnected");
                    break;
            }
        }
        private void InputUser_onUnpairedDeviceUsed(InputControl inputControl, UnityEngine.InputSystem.LowLevel.InputEventPtr inputEventPtr)
        {
            // Get the control scheme associated with the used device.
            InputControlScheme deviceControlScheme = s_inputActions.controlSchemes.Where(t => t.SupportsDevice(inputControl.device)).FirstOrDefault();
            CurrentInputDevice = inputControl.device;

            if (deviceControlScheme != default && deviceControlScheme != s_currentControlScheme)
            {
                // A input device belonging to a different control scheme has been used.
                s_currentControlScheme = deviceControlScheme;
                Debug.Log("New Used. Scheme Name: " + deviceControlScheme.name);

                OnInputDeviceChanged?.Invoke();
            }
        }

        #endregion


        public static InputAction GetSlotActivationAction(AttachmentSlotIndex slotIndex) => slotIndex switch
        {
            AttachmentSlotIndex.Primary => s_inputActions.Combat.ActivateSlot0,
            AttachmentSlotIndex.Secondary => s_inputActions.Combat.ActivateSlot1,
            AttachmentSlotIndex.Tertiary => s_inputActions.Combat.ActivateSlot2,
            AttachmentSlotIndex.Quaternary => s_inputActions.Combat.ActivateSlot3,

            _ => throw new System.NotImplementedException(),
        };
    }
}