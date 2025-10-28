using System;
using Unity.Netcode;

public struct PlayerLobbyState : INetworkSerializable, IEquatable<PlayerLobbyState>
{
    public ulong ClientID;
    public bool IsReady;


    public PlayerLobbyState NewWithIsReady(bool isReady) { this.IsReady = isReady; return this; }


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientID);
        serializer.SerializeValue(ref IsReady);
    }
    public bool Equals(PlayerLobbyState other)
    {
        return (ClientID, IsReady) == (other.ClientID, other.IsReady);
    }
}
