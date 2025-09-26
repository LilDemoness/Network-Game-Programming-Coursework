using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private PlayerManager _playerPrefab;


    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        foreach (ClientData client in ServerManager.Instance.ClientData.Values)
        {
            Vector3 spawnPosition = Vector3.right * Mathf.Floor(client.ClientID) * 2.0f;

            PlayerManager playerInstance = Instantiate<PlayerManager>(_playerPrefab, spawnPosition, Quaternion.identity);
            playerInstance.NetworkObject.SpawnAsPlayerObject(client.ClientID);


            SetupPlayerClientRpc(client.ClientID, client.BuildData.ActiveFrameIndex, client.BuildData.ActiveLegIndex, client.BuildData.ActivePrimaryWeaponIndex, client.BuildData.ActiveSecondaryWeaponIndex, client.BuildData.ActiveTertiaryWeaponIndex, client.BuildData.ActiveAbilityIndex);
        }
    }

    [ClientRpc]
    private void SetupPlayerClientRpc(ulong clientID, int frameIndex, int legIndex, int primaryWeaponIndex, int secondaryWeaponIndex, int tertiaryWeaponIndex, int abilityIndex)
    {
        PlayerManager playerInstance = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerManager>();

        playerInstance.SetBuild(frameIndex, legIndex, primaryWeaponIndex, secondaryWeaponIndex, tertiaryWeaponIndex, abilityIndex);
    }
}
