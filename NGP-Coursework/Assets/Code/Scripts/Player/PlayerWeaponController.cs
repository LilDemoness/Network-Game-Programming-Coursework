using UnityEngine;
using Unity.Netcode;

namespace Player
{
    /// <summary>
    ///     A script to process Player Input into triggering Weapon functions.
    /// </summary>
    public class PlayerWeaponController : NetworkBehaviour
    {
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private Weapon[] _weapons;
        private int _weaponCount;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;


            UpdateWeaponCount();
            SubscribeToInput();
        }
        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
                return;


            UnsubscribeFromInput();
        }


        [ContextMenu("Update Weapon Count")]
        private void UpdateWeaponCount() => _weaponCount = _weapons.Length;


        #region Receiving Input Functions

        private void SubscribeToInput()
        {
            _playerInput.OnUsePrimaryWeaponStarted += PlayerInput_OnUsePrimaryWeaponStarted;
            _playerInput.OnUsePrimaryWeaponCancelled += PlayerInput_OnUsePrimaryWeaponCancelled;

            _playerInput.OnUseSecondaryWeaponStarted += PlayerInput_OnUseSecondaryWeaponStarted;
            _playerInput.OnUseSecondaryWeaponCancelled += PlayerInput_OnUseSecondaryWeaponCancelled;

            _playerInput.OnUseTertiaryWeaponStarted += PlayerInput_OnUseTertiaryWeaponStarted;
            _playerInput.OnUseTertiaryWeaponCancelled += PlayerInput_OnUseTertiaryWeaponCancelled;
        }
        private void UnsubscribeFromInput()
        {
            _playerInput.OnUsePrimaryWeaponStarted -= PlayerInput_OnUsePrimaryWeaponStarted;
            _playerInput.OnUsePrimaryWeaponCancelled -= PlayerInput_OnUsePrimaryWeaponCancelled;

            _playerInput.OnUseSecondaryWeaponStarted -= PlayerInput_OnUseSecondaryWeaponStarted;
            _playerInput.OnUseSecondaryWeaponCancelled -= PlayerInput_OnUseSecondaryWeaponCancelled;

            _playerInput.OnUseTertiaryWeaponStarted -= PlayerInput_OnUseTertiaryWeaponStarted;
            _playerInput.OnUseTertiaryWeaponCancelled -= PlayerInput_OnUseTertiaryWeaponCancelled;
        }


        private void PlayerInput_OnUsePrimaryWeaponStarted() => StartFiringServerRpc(0, NetworkManager.Singleton.LocalClientId);
        private void PlayerInput_OnUsePrimaryWeaponCancelled() => StopFiringServerRpc(0, NetworkManager.Singleton.LocalClientId);

        private void PlayerInput_OnUseSecondaryWeaponStarted() => StartFiringServerRpc(1, NetworkManager.Singleton.LocalClientId);
        private void PlayerInput_OnUseSecondaryWeaponCancelled() => StopFiringServerRpc(1, NetworkManager.Singleton.LocalClientId);

        private void PlayerInput_OnUseTertiaryWeaponStarted() => StartFiringServerRpc(2, NetworkManager.Singleton.LocalClientId);
        private void PlayerInput_OnUseTertiaryWeaponCancelled() => StopFiringServerRpc(2, NetworkManager.Singleton.LocalClientId);

        #endregion


        private void OnStaggered() => InterruptFiringServerRpc(NetworkManager.Singleton.LocalClientId);
        


        [ServerRpc]
        private void StartFiringServerRpc(int weaponIndex, ulong triggeringClientID)
        {
            //Debug.Log($"Player {triggeringClientID} Started Firing");
            StartFiringClientRpc(weaponIndex);
        }
        [ClientRpc]
        private void StartFiringClientRpc(int weaponIndex)
        {
            if (_weaponCount > weaponIndex)
                _weapons[weaponIndex].StartFiring();
        }
        [ServerRpc]
        private void StopFiringServerRpc(int weaponIndex, ulong triggeringClientID)
        {
            //Debug.Log($"Player {triggeringClientID} Stopped Firing");
            StopFiringClientRpc(weaponIndex);
        }
        [ClientRpc]
        private void StopFiringClientRpc(int weaponIndex)
        {
            if (_weaponCount > weaponIndex)
                _weapons[weaponIndex].StopFiring();
        }
        [ServerRpc]
        private void InterruptFiringServerRpc(ulong triggeringClientID)
        {
            //Debug.Log($"Player {triggeringClientID} Was Interrupted");
            InterruptFiringClientRpc();
        }
        [ClientRpc]
        private void InterruptFiringClientRpc()
        {
            for (int i = 0; i < _weaponCount; ++i)
            {
                _weapons[i].InterruptFiring();
            }
        }
    }
}


[System.Serializable]
public class Weapon
{
    // Data.
    [SerializeField] private WeaponData _weaponData;

    // Per-Instance Variables.



    // Base Functions.
    public void StartFiring() { }
    public void StopFiring() { }
    public void InterruptFiring() { }
}