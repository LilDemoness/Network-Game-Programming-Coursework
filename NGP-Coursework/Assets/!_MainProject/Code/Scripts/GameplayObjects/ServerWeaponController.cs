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

        
        private Weapon _primaryWeapon;
        private Weapon _secondaryWeapon;
        private Weapon _tertiaryWeapon;


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
            foreach (var weaponAttachmentSlot in GetComponentsInChildren<Customisation.Sections.SlottableDataSlot>())
            {
                Weapon weapon = weaponAttachmentSlot.GetComponentInChildren<Weapon>();
                if (weapon == null)
                    continue;

                Debug.Log(weaponAttachmentSlot.name + " is equipped in Slot " + weaponAttachmentSlot.SlotIndex + ". Weapon Data: " + weapon.WeaponData.name);
                switch (weaponAttachmentSlot.SlotIndex)
                {
                    case SlotIndex.PrimaryWeapon: _primaryWeapon = weapon; break;
                    case SlotIndex.SecondaryWeapon: _secondaryWeapon = weapon; break;
                    case SlotIndex.TertiaryWeapon: _tertiaryWeapon = weapon; break;
                }
            }

            // Initialise Weapons (Or their Actions) on Client too?
        }


        [Rpc(SendTo.Server)]
        public void PlayActionForSlotIndexServerRpc(SlotIndex slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return;


        }

        [ServerRpc]
        public void StartFiringPrimaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._primaryWeapon != null)
            {
                StartFiringWeapon(_primaryWeapon, (int)SlotIndex.PrimaryWeapon);
            }
        }
        [ServerRpc]
        public void StopFiringPrimaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._primaryWeapon != null)
                StopFiringWeapon((int)SlotIndex.PrimaryWeapon);
        }
        [ServerRpc]
        public void StartFiringSecondaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._secondaryWeapon != null)
            {
                StartFiringWeapon(_secondaryWeapon, (int)SlotIndex.SecondaryWeapon);
            }
        }
        [ServerRpc]
        public void StopFiringSecondaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._secondaryWeapon != null)
                StopFiringWeapon((int)SlotIndex.SecondaryWeapon);
        }
        [ServerRpc]
        public void StartFiringTertiaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._tertiaryWeapon != null)
            {
                StartFiringWeapon(_tertiaryWeapon, (int)SlotIndex.TertiaryWeapon);
            }
        }
        [ServerRpc]
        public void StopFiringTertiaryWeaponServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == this.OwnerClientId && this._tertiaryWeapon != null)
                StopFiringWeapon((int)SlotIndex.TertiaryWeapon);
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
            _serverCharacter.PlayActionServerRpc(actionRequestData);
        }
        private void StopFiringWeapon(int slotIdentifier) => _serverCharacter.CancelActionBySlotServerRpc(slotIdentifier);
    }
}