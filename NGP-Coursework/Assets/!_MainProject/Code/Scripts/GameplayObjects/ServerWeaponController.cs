using Gameplay.Actions.Definitions;
using Gameplay.Actions;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Character.Customisation.Sections;

namespace Gameplay.GameplayObjects.Character
{
    public class ServerWeaponController : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;

        
        private SlotGFXSection[] _activationSlots;


        private void Awake()
        {
            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged += OnPlayerBuildChanged;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            PlayerCustomisationManager.OnNonLocalClientPlayerBuildChanged -= OnPlayerBuildChanged;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }

            // Initialise our Weapons (To-do: Improve this pls).
            StartCoroutine(InitialiseWeaponsAfterFrame());
        }
        private IEnumerator InitialiseWeaponsAfterFrame()
        {
            // Wait for Setup.
            yield return null;

            // Get our Attachment Slots
            SlottableDataSlot[] attachmentSlots = GetComponentsInChildren<SlottableDataSlot>();

            // Determine the number of active attachment slots.
            int activeSlots = 0;
            for(int i = 0; i < attachmentSlots.Length; ++i)
            {
                if (attachmentSlots[i].isActiveAndEnabled)
                    ++activeSlots;
            }

            // Populate our Attachment Slots
            _activationSlots = new SlotGFXSection[activeSlots];
            foreach (var attachmentSlot in attachmentSlots)
            {
                if (!attachmentSlot.isActiveAndEnabled)
                    continue;   // The attachment slot is inactive.

                foreach (SlotGFXSection slotSection in attachmentSlot.GetComponentsInChildren<SlotGFXSection>())
                {
                    if (!slotSection.isActiveAndEnabled)
                        continue;   // The slot section is inactive.

                    Debug.Log(attachmentSlot.name + " is equipped in Slot " + attachmentSlot.SlotIndex + ". Data Data: " + slotSection.SlottableData.Name);
                    _activationSlots[attachmentSlot.SlotIndex.GetSlotInteger()] = slotSection;
                    break;  // Fails to account for duplicates.
                }
            }

            // Initialise Weapons (Or their Actions) on Client too?
        }


        private void OnPlayerBuildChanged(ulong clientID, BuildData newBuild)
        {
            if (clientID != _serverCharacter.OwnerClientId)
                return;

            // To-do: Improve this (E.g. Having it be not tied to activation state).
            StartCoroutine(InitialiseWeaponsAfterFrame());
        }


        [Rpc(SendTo.Server)]
        public void ActivateSlotServerRpc(int slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.

            if (slotIndex >= _activationSlots.Length)
                return; // Outwith our slot count.

            StartUsingSlottable(_activationSlots[slotIndex], slotIndex.ToSlotIndex());
        }
        [Rpc(SendTo.Server)]
        public void DeactivateSlotServerRpc(int slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.

            if (slotIndex >= _activationSlots.Length)
                return; // Outwith our slot count.

            StopUsingSlottable(slotIndex.ToSlotIndex());
        }


        private void StartUsingSlottable(SlotGFXSection weapon, SlotIndex slotIndex)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(weapon.SlottableData.AssociatedAction);

            // Setup the ActionRequestData.
            actionRequestData.OriginTransformID = weapon.GetAbilityOriginTransformID();
            actionRequestData.Position = weapon.GetAbilityLocalOffset();
            actionRequestData.Direction = weapon.GetAbilityLocalDirection();
            actionRequestData.SlotIdentifier = (int)slotIndex;

            // Request to play our action.
            _serverCharacter.PlayActionServerRpc(actionRequestData);
        }
        private void StopUsingSlottable(SlotIndex slotIndex)
        {
            if (_activationSlots[slotIndex.GetSlotInteger()].SlottableData.AssociatedAction.ActivationStyle != ActionActivationStyle.Held)
                return; // Don't cancel this action on release.

            // Cancel the action triggered from this slot.
            _serverCharacter.CancelActionBySlotServerRpc((int)slotIndex);
        }
    }
}