using Gameplay.GameplayObjects.Character.Customisation.Data;
using System;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Common data and RPCs for the PreGameLobby state.
    /// </summary>
    public class NetworkLobbyState : NetworkBehaviour
    {
        private NetworkList<SessionPlayerState> m_sessionPlayers;
        public NetworkList<SessionPlayerState> SessionPlayers => m_sessionPlayers;

        public NetworkVariable<bool> IsStartingGame { get; } = new NetworkVariable<bool>(value: false);
        public NetworkVariable<bool> IsLobbyLocked { get; } = new NetworkVariable<bool>(value: false);


        public event System.Action<ulong, int, bool> OnClientChangedReadyState;


        private void Awake()
        {
            this.m_sessionPlayers = new NetworkList<SessionPlayerState>();
        }


        /// <summary>
        ///     An RPC to notify the server when a client changes their ready state.
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void ChangeReadyStateServerRpc(ulong clientId, int seatIndex, bool newReadyState)
        {
            OnClientChangedReadyState?.Invoke(clientId, seatIndex, newReadyState);
        }


        /// <summary>
        ///     Describes one of the players in the session.
        /// </summary>
        public struct SessionPlayerState : INetworkSerializable, IEquatable<SessionPlayerState>
        {
            public ulong ClientId;
            public int PlayerNumber;

            private FixedPlayerName m_playerName;
            public FixedPlayerName FixedPlayerName => m_playerName;
            public string PlayerName
            {
                get => m_playerName;
                set => m_playerName = value;
            }

            public bool IsReady;


            public SessionPlayerState(ulong clientId, int playerNumber, string name, bool isReady) : this(clientId, playerNumber, new FixedPlayerName(name), isReady)
            { }
            public SessionPlayerState(ulong clientId, int playerNumber, FixedPlayerName playerName, bool isReady)
            {
                this.ClientId = clientId;
                this.PlayerNumber = playerNumber;
                this.m_playerName = playerName;
                this.IsReady = isReady;
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref PlayerNumber);
                serializer.SerializeValue(ref m_playerName);
                serializer.SerializeValue(ref IsReady);
            }

            public bool Equals(SessionPlayerState other) => (this.ClientId, this.PlayerNumber, this.m_playerName, this.IsReady) == (other.ClientId, other.PlayerNumber, other.m_playerName, other.IsReady);
        }
    }
}