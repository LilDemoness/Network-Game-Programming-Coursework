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
        private const int PRIMARY_WEAPON_SLOT_IDENTIFIER = 1;

        private Weapon _secondaryWeapon;
        private const int SECONDARY_WEAPON_SLOT_IDENTIFIER = 2;

        private Weapon _tertiaryWeapon;
        private const int TERTIARY_WEAPON_SLOT_IDENTIFIER = 3;


        [SerializeField] private ActionDefinition _cancelFiringActionDefinition;


        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }

            Debug.Log(this.OwnerClientId);

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
                    case 1: _primaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                    case 2: _secondaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                    case 3: _tertiaryWeapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>(); break;
                }
            }

            // Initialise Weapons (Or their Actions) on Client too?
        }


        [ServerRpc]
        public void StartFiringPrimaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._primaryWeapon != null)
            {
                StartFiringWeapon(_primaryWeapon, PRIMARY_WEAPON_SLOT_IDENTIFIER);
            }
        }
        [ServerRpc]
        public void StopFiringPrimaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId)
                StopFiringWeapon(PRIMARY_WEAPON_SLOT_IDENTIFIER);
        }
        [ServerRpc]
        public void StartFiringSecondaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._secondaryWeapon != null)
            {
                StartFiringWeapon(_secondaryWeapon, SECONDARY_WEAPON_SLOT_IDENTIFIER);
            }
        }
        [ServerRpc]
        public void StopFiringSecondaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId)
                StopFiringWeapon(SECONDARY_WEAPON_SLOT_IDENTIFIER);
        }
        [ServerRpc]
        public void StartFiringTertiaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._tertiaryWeapon != null)
            {
                StartFiringWeapon(_tertiaryWeapon, TERTIARY_WEAPON_SLOT_IDENTIFIER);
            }
        }
        [ServerRpc]
        public void StopFiringTertiaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId)
                StopFiringWeapon(TERTIARY_WEAPON_SLOT_IDENTIFIER);
        }


        private void StartFiringWeapon(Weapon weapon, int slotIdentifier)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(weapon.WeaponData.AssociatedAction);

            // Setup the ActionRequestData.
            actionRequestData.OriginTransform = weapon.GetAttackOriginTransform();
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