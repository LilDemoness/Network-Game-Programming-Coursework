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
            public AttachmentSlotIndex AttachmentSlotIndex;
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


        [SerializeField] private ServerSlotController _serverSlotController;

        
        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                this.enabled = false;
                return;
            }


            // Subscribe to Client Inputs.
            SubscribeToClientInput();
        }
        public override void OnNetworkDespawn()
        {
            if (!IsClient || !IsOwner)
                return;

            // Unsubscribe from Client Inputs.
            UnsubscribeFromClientInput();
        }
        private void SubscribeToClientInput()
        {
            ClientInput.OnMovementInputChanged += ClientInput_OnMovementInputChanged;

            ClientInput.OnActivateSlotStarted += ClientInput_OnActivateSlotStarted;
            ClientInput.OnActivateSlotCancelled += ClientInput_OnActivateSlotCancelled;
        }
        private void UnsubscribeFromClientInput()
        {
            ClientInput.OnMovementInputChanged -= ClientInput_OnMovementInputChanged;

            ClientInput.OnActivateSlotStarted -= ClientInput_OnActivateSlotStarted;
            ClientInput.OnActivateSlotCancelled -= ClientInput_OnActivateSlotCancelled;
        }

        private void ClientInput_OnMovementInputChanged()
        {
            // Only notify the server of movement input CHANGES to reduce bandwidth usage while still being pretty reliable and responsive.
            // Deferred to the next Update so that rapid changes in input only send the relevant one.
            _movementInput = ClientInput.MovementInput;
            _hasMoveRequest = true;
        }
        private void ClientInput_OnActivateSlotStarted(int slotIndex) => _serverSlotController.ActivateSlot(slotIndex);//_serverWeaponController.ActivateSlotServerRpc(slotIndex);
        private void ClientInput_OnActivateSlotCancelled(int slotIndex) => _serverSlotController.DeactivateSlotServerRpc(slotIndex);
        


        private void FixedUpdate()
        {
            // Send Non-client Only Input Requests to the Server.
            // Play All ActionRequests (In FIFO order).
            // Note: Currently unused as the ServerSlotController is handling Slottable Activation and we are sending Movement requests separately.
            /*for (int i = 0; i < _actionRequestCount; ++i)
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
                        data.AttachmentSlotIndex = _actionRequests[i].AttachmentSlotIndex;

                        // Send our Input.
                        SendInput(data);
                        break;
                }
            }

            _actionRequestCount = 0;*/


            if (_hasMoveRequest)
            {
                if ((Time.time - _lastSendMoveTime) > MAX_MOVEMENT_RATE_SECONDS)
                {
                    // Process our move request.
                    _hasMoveRequest = false;
                    _lastSendMoveTime = Time.time;

                    _serverCharacter.SendCharacterMovementInputServerRpc(_movementInput);
                }
            }
        }

        /// <summary>
        ///     Send the ActionRequestData to the server to be processed.
        /// </summary>
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
    }
}