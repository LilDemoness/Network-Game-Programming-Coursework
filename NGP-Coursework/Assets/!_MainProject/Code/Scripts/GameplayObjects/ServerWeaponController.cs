using Gameplay.Actions;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character
{
    public class ServerWeaponController : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;

        
        private Weapon _primaryWeapon;
        private Weapon _secondaryWeapon;
        private Weapon _tertiaryWeapon;


        [SerializeField] private ActionDefinition _cancelFiringActionDefinition;


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
            foreach (var weaponAttachmentSlot in GetComponentsInChildren<Customisation.Sections.WeaponAttachmentSlot>())
            {
                Debug.Log(weaponAttachmentSlot.name + " " + weaponAttachmentSlot.SlotIndex + " " + weaponAttachmentSlot.GetComponentInChildren<Weapon>());
                switch (weaponAttachmentSlot.SlotIndex)
                {
                    case (int)WeaponSlotIndex.Primary: _primaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                    case (int)WeaponSlotIndex.Secondary: _secondaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                    case (int)WeaponSlotIndex.Tertiary: _tertiaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                }
            }

            // Initialise Weapons (Or their Actions) on Client too?
        }


        [ServerRpc]
        public void StartFiringPrimaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._primaryWeapon != null)
            {
                StartFiringWeapon(_primaryWeapon, (int)WeaponSlotIndex.Primary);
            }
        }
        [ServerRpc]
        public void StopFiringPrimaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId)
                StopFiringWeapon((int)WeaponSlotIndex.Primary);
        }
        [ServerRpc]
        public void StartFiringSecondaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._secondaryWeapon != null)
            {
                StartFiringWeapon(_secondaryWeapon, (int)WeaponSlotIndex.Secondary);
            }
        }
        [ServerRpc]
        public void StopFiringSecondaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId)
                StopFiringWeapon((int)WeaponSlotIndex.Secondary);
        }
        [ServerRpc]
        public void StartFiringTertiaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._tertiaryWeapon != null)
            {
                StartFiringWeapon(_tertiaryWeapon, (int)WeaponSlotIndex.Tertiary);
            }
        }
        [ServerRpc]
        public void StopFiringTertiaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId)
                StopFiringWeapon((int)WeaponSlotIndex.Tertiary);
        }


        private void StartFiringWeapon(Weapon weapon, int slotIdentifier)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(weapon.WeaponData.AssociatedAction);

            // Setup the ActionRequestData.
            actionRequestData.OriginTransformID = weapon.GetAttackOriginTransformID();
            actionRequestData.Position = weapon.GetAttackLocalOffset();
            actionRequestData.Direction = weapon.GetAttackLocalDirection();
            actionRequestData.SlotIdentifier = slotIdentifier;

            // Request to play our action.
            _serverCharacter.PlayActionLocalCallRpc(actionRequestData);
        }
        private void StopFiringWeapon(int slotIdentifier)
        {
            // Create and Setup the ActionRequestData.
            ActionRequestData actionRequestData = ActionRequestData.Create(_cancelFiringActionDefinition);
            actionRequestData.SlotIdentifier = slotIdentifier;

            // Request to play our action.
            _serverCharacter.PlayActionLocalCallRpc(actionRequestData);
        }
    }
}