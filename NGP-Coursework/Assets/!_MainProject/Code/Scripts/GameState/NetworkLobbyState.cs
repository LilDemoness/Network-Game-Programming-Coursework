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


        public event System.Action<ulong, bool> OnClientChangedReadyState;


        private void Awake()
        {
            this.m_sessionPlayers = new NetworkList<SessionPlayerState>();
        }


        /// <summary>
        ///     An RPC to notify the server when a client changes their ready state.
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void ChangeReadyStateServerRpc(ulong clientId, bool newReadyState)
        {
            OnClientChangedReadyState?.Invoke(clientId, newReadyState);
        }


        /// <summary>
        ///     Describes one of the players in the session.
        /// </summary>
        public struct SessionPlayerState : INetworkSerializable, IEquatable<SessionPlayerState>
        {
            public ulong ClientId;

            private FixedPlayerName m_playerName;
            public string PlayerName
            {
                get => m_playerName;
                set => m_playerName = value;
            }

            public bool IsReady;


            public SessionPlayerState(ulong clientID, string name, bool isReady)
            {
                this.ClientId = clientID;
                this.m_playerName = new FixedPlayerName(name);
                this.IsReady = isReady;
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref m_playerName);
            }

            public bool Equals(SessionPlayerState other) => (this.ClientId, this.m_playerName) == (other.ClientId, other.m_playerName);
        }
    }
}