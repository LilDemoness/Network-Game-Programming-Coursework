using Gameplay.Actions.Definitions;
using Gameplay.Actions;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character
{
    public class ServerWeaponController : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;

        
        private Weapon[] _activationSlots;


        [SerializeField] private CancelAction _cancelFiringAction;


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
            _activationSlots = new Weapon[attachmentSlots.Length];
            foreach (var weaponAttachmentSlot in attachmentSlots)
            {
                Weapon weapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>();
                if (weapon == null)
                    continue;

                Debug.Log(weaponAttachmentSlot.name + " is equipped in Slot " + weaponAttachmentSlot.SlotIndex + ". Weapon Data: " + weapon.WeaponData.name);
                switch (weaponAttachmentSlot.SlotIndex)
                {
                    case SlotIndex.PrimaryWeapon: _activationSlots[0] = weapon; break;
                    case SlotIndex.SecondaryWeapon: _activationSlots[1] = weapon; break;
                    case SlotIndex.TertiaryWeapon: _activationSlots[2] = weapon; break;
                }
            }

            // Initialise Weapons (Or their Actions) on Client too?
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