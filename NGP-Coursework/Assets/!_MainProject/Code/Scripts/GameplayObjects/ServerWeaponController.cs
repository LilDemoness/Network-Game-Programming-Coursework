using Gameplay.Actions.Definitions;
using Gameplay.Actions;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character
{
    public class ServerWeaponController : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;

        
        private Weapon[] _activationSlots;


        [SerializeField] private CancelAction _cancelFiringAction;


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

            // Get our Weapons
            Customisation.Sections.SlottableDataSlot[] attachmentSlots = GetComponentsInChildren<Customisation.Sections.SlottableDataSlot>();
            int activeSlots = 0;
            for(int i = 0; i < attachmentSlots.Length; ++i)
                if (attachmentSlots[i].isActiveAndEnabled)
                    ++activeSlots;
            _activationSlots = new Weapon[activeSlots];
            foreach (var weaponAttachmentSlot in attachmentSlots)
            {
                if (!weaponAttachmentSlot.isActiveAndEnabled)
                    continue;

                foreach (Weapon weapon in weaponAttachmentSlot.GetComponentsInChildren<Weapon>())
                {
                    if (!weapon.isActiveAndEnabled)
                        continue;

                    Debug.Log(weaponAttachmentSlot.name + " is equipped in Slot " + weaponAttachmentSlot.SlotIndex + ". Weapon Data: " + weapon.WeaponData.name);
                    switch (weaponAttachmentSlot.SlotIndex)
                    {
                        case SlotIndex.PrimaryWeapon: _activationSlots[0] = weapon; break;
                        case SlotIndex.SecondaryWeapon: _activationSlots[1] = weapon; break;
                        case SlotIndex.TertiaryWeapon: _activationSlots[2] = weapon; break;
                    }
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

            StartFiringWeapon(_activationSlots[slotIndex], slotIndex.ToSlotIndex());
        }
        [Rpc(SendTo.Server)]
        public void DeactivateSlotServerRpc(int slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.

            if (slotIndex >= _activationSlots.Length)
                return; // Outwith our slot count.

            StopFiringWeapon(slotIndex.ToSlotIndex());
        }


        private void StartFiringWeapon(Weapon weapon, SlotIndex slotIndex)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(weapon.WeaponData.AssociatedAction);

            // Setup the ActionRequestData.
            actionRequestData.OriginTransformID = weapon.GetAttackOriginTransformID();
            actionRequestData.Position = weapon.GetAttackLocalOffset();
            actionRequestData.Direction = weapon.GetAttackLocalDirection();
            actionRequestData.SlotIdentifier = (int)slotIndex;

            // Request to play our action.
            _serverCharacter.PlayActionServerRpc(actionRequestData);
        }
        private void StopFiringWeapon(SlotIndex slotIndex) => _serverCharacter.CancelActionBySlotServerRpc((int)slotIndex);
    }
}