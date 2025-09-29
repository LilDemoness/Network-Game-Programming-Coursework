using System.Collections;
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


        /// <summary>
        ///     This event fires at the time when an action event is sent to the server.
        /// </summary>
        public event System.Action<ActionRequestData> ActionInputEvent;


        [SerializeField] private ServerCharacter _serverCharacter;


        private PlayerInputActions _inputActions;


        [Header("(Temp) Weapon Actions")]
        [SerializeField] private Weapon _primaryWeapon;
        [SerializeField] private Weapon _secondaryWeapon;
        [SerializeField] private Weapon _tertiaryWeapon;

        [Space(5)]
        [SerializeField] private ActionDefinition _testCancelAction;


        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                this.enabled = false;
                return;
            }


            // Setup our InputActionMap.
            CreateInputActions();
            StartCoroutine(InitialiseWeapons());
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


        // Move to a 'WeaponsManager' class?
        public IEnumerator InitialiseWeapons()
        {
            yield return null;
            foreach (var weaponAttachmentSlot in GetComponentsInChildren<Gameplay.GameplayObjects.Character.Customisation.Sections.WeaponAttachmentSlot>())
            {
                Debug.Log(weaponAttachmentSlot.name + " " + weaponAttachmentSlot.SlotIndex + " " + weaponAttachmentSlot.GetComponentInChildren<Weapon>());
                switch (weaponAttachmentSlot.SlotIndex)
                {
                    case 1: _primaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                    case 2: _secondaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                    case 3: _tertiaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                }
            }
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


        private void RequestAction(ActionType actionType, ActionDefinition actionState, Vector3 origin = default, Vector3 direction = default, int slotIdentifier = -1)
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
        }
        private void RequestActionForWeapon(Weapon weapon, int slot) => RequestAction(ActionType.StartShooting, weapon.WeaponData.AssociatedAction, weapon.GetAttackOrigin(), weapon.GetAttackDirection(), slot); 
        

        private void UsePrimaryWeapon_started(InputAction.CallbackContext obj) { if (_primaryWeapon != null) {RequestActionForWeapon(_primaryWeapon, 1); } }
        private void UsePrimaryWeapon_cancelled(InputAction.CallbackContext obj) { if (_primaryWeapon != null) { RequestAction(ActionType.StopShooting, _testCancelAction, slotIdentifier: 1); } }

        private void UseSecondaryWeapon_started(InputAction.CallbackContext obj) { if (_secondaryWeapon != null) { RequestActionForWeapon(_secondaryWeapon, 2); } }
        private void UseSecondaryWeapon_cancelled(InputAction.CallbackContext obj) { if (_secondaryWeapon != null) { RequestAction(ActionType.StopShooting, _testCancelAction, slotIdentifier: 2); } }

        private void UseTertiaryWeapon_started(InputAction.CallbackContext obj) { if (_tertiaryWeapon != null) { RequestActionForWeapon(_tertiaryWeapon, 3); } }
        private void UseTertiaryWeapon_cancelled(InputAction.CallbackContext obj) { if (_tertiaryWeapon != null) { RequestAction(ActionType.StopShooting, _testCancelAction, slotIdentifier: 3); } }
    }
}