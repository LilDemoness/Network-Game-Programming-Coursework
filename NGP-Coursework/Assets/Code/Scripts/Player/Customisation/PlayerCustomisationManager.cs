using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCustomisationManager : NetworkBehaviour
{
    [SerializeField] private PlayerCustomisationOptionsDatabase _optionsDatabase;
    [SerializeField] private PlayerCustomisationUI _playerCustomisationUI;
    private NetworkList<PlayerCustomisationState> _players;


    public static event System.Action<ulong, PlayerCustomisationState> OnPlayerCustomisationStateChanged;


    private void Awake()
    {
        _players = new NetworkList<PlayerCustomisationState>();
    }


    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            _playerCustomisationUI.Setup(this, NetworkManager.Singleton.LocalClientId, _optionsDatabase);

            _players.OnListChanged += HandlePlayersStateChanged;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            // Ensure that we're accounting for already connected clients.
            foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                HandleClientConnected(client.ClientId);
            }
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            _players.OnListChanged -= HandlePlayersStateChanged;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }


    private void HandleClientConnected(ulong clientID)
    {
        // Ensure we've not already added this client.
        for(int i = 0; i < _players.Count; ++i)
            if (_players[i].ClientID == clientID)
                return;

        // Add the client.
        _players.Add(new PlayerCustomisationState(clientID));
        Debug.Log("Players Count: " + _players.Count);
    }
    private void HandleClientDisconnected(ulong clientID)
    {
        // Remove the player who just left from our '_players' list.
        for(int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == clientID)
            {
                _players.RemoveAt(i);
                return;
            }
        }
    }



#region Changing Selected Elements

#region Frame

    public void SelectNextFrame() => SelectFrameServerRpc(isIncrement: transform);
    public void SelectPreviousFrame() => SelectFrameServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectFrameServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for(int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].FrameIndex + (isIncrement ? 1 : -1), _optionsDatabase.FrameDatas.Length);
                _players[i] = _players[i].NewWithFrameIndex(newValue);
            }
        }
    }

#endregion

#region Leg

    public void SelectNextLeg() => SelectLegServerRpc(isIncrement: true);
    public void SelectPreviousLeg() => SelectLegServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectLegServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].LegIndex + (isIncrement ? 1 : -1), _optionsDatabase.LegDatas.Length);
                _players[i] = _players[i].NewWithLegIndex(newValue);
            }
        }
    }

#endregion

#region Weapons

    // Primary.
    public void SelectNextPrimaryWeapon() => SelectPrimaryWeaponServerRpc(isIncrement: true);
    public void SelectPreviousPrimaryWeapon() => SelectPrimaryWeaponServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectPrimaryWeaponServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].PrimaryWeaponIndex + (isIncrement ? 1 : -1), _optionsDatabase.WeaponDatas.Length);
                _players[i] = _players[i].NewWithPrimaryWeaponIndex(newValue);
            }
        }
    }


    // Secondary.
    public void SelectNextSecondaryWeapon() => SelectSecondaryWeaponServerRpc(isIncrement: true);
    public void SelectPreviousSecondaryWeapon() => SelectSecondaryWeaponServerRpc(isIncrement: false);
    
    [ServerRpc(RequireOwnership = false)]
    private void SelectSecondaryWeaponServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].SecondaryWeaponIndex + (isIncrement ? 1 : -1), _optionsDatabase.WeaponDatas.Length);
                _players[i] = _players[i].NewWithSecondaryWeaponIndex(newValue);
            }
        }
    }


    // Tertiary.
    public void SelectNextTertiaryWeapon() => SelectTertiaryWeaponServerRpc(isIncrement: true);
    public void SelectPreviousTertiaryWeapon() => SelectTertiaryWeaponServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectTertiaryWeaponServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].TertiaryWeaponIndex + (isIncrement ? 1 : -1), _optionsDatabase.WeaponDatas.Length);
                _players[i] = _players[i].NewWithTertiaryWeaponIndex(newValue);
            }
        }
    }

#endregion

#region Ability

    public void SelectNextAbility() => SelectAbilityServerRpc(isIncrement: true);
    public void SelectPreviousAbility() => SelectAbilityServerRpc(isIncrement: false);
    [ServerRpc(RequireOwnership = false)]
    private void SelectAbilityServerRpc(bool isIncrement, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            if (_players[i].ClientID == serverRpcParams.Receive.SenderClientId)
            {
                int newValue = Loop(_players[i].AbilityIndex + (isIncrement ? 1 : -1), _optionsDatabase.AbilityDatas.Length);
                _players[i] = _players[i].NewWithAbilityIndex(newValue);
            }
        }
    }

#endregion

    private int Loop(int value, int maxValueExclusive)
    {
        if (value >= maxValueExclusive)
            return 0;
        else if (value < 0)
            return maxValueExclusive - 1;
        else
            return value;
    }

    #endregion


    private void HandlePlayersStateChanged(NetworkListEvent<PlayerCustomisationState> changeEvent)
    {
        for (int i = 0; i < _players.Count; ++i)
        {
            Debug.Log($"{i}: {_players[i].ClientID} frame: {_players[i].FrameIndex}");

            OnPlayerCustomisationStateChanged?.Invoke(_players[i].ClientID, _players[i]);
        }
    }
}
