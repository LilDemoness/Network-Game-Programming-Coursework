using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Gameplay.Actions;
using Gameplay.GameplayObjects.Players;
using Gameplay.GameplayObjects.Character.Customisation.Sections;

namespace Gameplay.GameplayObjects.Character
{
    public class ServerWeaponController : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;
        [SerializeField] private Player _playerManager;

        
        private SlotGFXSection[] _activationSlots = new SlotGFXSection[0];
        private bool[] _activationRequests = new bool[0];


        private void Awake()
        {
            _playerManager.OnThisPlayerBuildUpdated += OnPlayerBuildChanged;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            _playerManager.OnThisPlayerBuildUpdated -= OnPlayerBuildChanged;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }
        }

        private void OnPlayerBuildChanged() => OnPlayerBuildChanged(_playerManager.GetActivationSlots());
        private void OnPlayerBuildChanged(SlotGFXSection[] activationSlots)
        {
            // Populate our Attachment Slots
            _activationSlots = activationSlots;

            // We use a bool[] to track our activation requests.
            _activationRequests = new bool[_activationSlots.Length];
        }


        [Rpc(SendTo.Server)]
        public void ActivateSlotServerRpc(int slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.
            if (slotIndex >= _activationSlots.Length)
                return; // Outwith our slot count.

            // Valid activation input.
            StartUsingSlottable(_activationSlots[slotIndex], slotIndex.ToSlotIndex());
        }
        [Rpc(SendTo.Server)]
        public void DeactivateSlotServerRpc(int slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.
            if (slotIndex >= _activationSlots.Length)
                return; // Outwith our slot count.

            // Valid deactivation input.
            StopUsingSlottable(slotIndex.ToSlotIndex());
        }

        private void StartUsingSlottable(SlotGFXSection weapon, AttachmentSlotIndex attachmentSlotIndex)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(weapon.SlottableData.AssociatedAction);

            // Setup the ActionRequestData.
            actionRequestData.IActionSourceObjectID = weapon.GetAbilitySourceObjectId();
            actionRequestData.AttachmentSlotIndex = attachmentSlotIndex;


            if (_serverCharacter.ActionPlayer.IsActionOnCooldown(actionRequestData.ActionID, actionRequestData.AttachmentSlotIndex))
            {
                // Our action is currently on cooldown. Cache our desire to activate this action.
                _activationRequests[attachmentSlotIndex.GetSlotInteger()] = true;
            }
            else
            {
                // Request to play our action.
                _activationRequests[attachmentSlotIndex.GetSlotInteger()] = false;
                _serverCharacter.PlayActionServerRpc(actionRequestData);
            }
        }
        private void StopUsingSlottable(AttachmentSlotIndex attachmentSlotIndex)
        {
            _activationRequests[attachmentSlotIndex.GetSlotInteger()] = false;
            if (_activationSlots[attachmentSlotIndex.GetSlotInteger()].SlottableData.AssociatedAction.ActivationStyle != ActionActivationStyle.Held)
                return; // Don't cancel this action on release.


            // Cancel the action triggered from this slot.
            _serverCharacter.CancelActionBySlotServerRpc(attachmentSlotIndex);
        }


        private void Update()
        {
            for(int i = 0; i < _activationRequests.Length; ++i)
            {
                if (_activationRequests[i] == true)
                {
                    StartUsingSlottable(_activationSlots[i], i.ToSlotIndex());
                }
            }
        }


        public SlotGFXSection[] GetActivationSlots() => _activationSlots;
    }
}