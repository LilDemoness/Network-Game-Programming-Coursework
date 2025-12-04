using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Utils
{
    /// <summary>
    ///     A NetworkBehaviour containing only one NetworkVariableString which represents this object's name.
    /// </summary>
    public class NetworkNameState : NetworkBehaviour
    {
        [ReadOnly]
        public NetworkVariable<FixedPlayerName> Name { get; set; } = new NetworkVariable<FixedPlayerName>();
    }

    /// <summary>
    ///     Wrapping a FixedString so that if we want to change the player max name length in the future, we need to only do it once.
    /// </summary>
    public struct FixedPlayerName : INetworkSerializable
    {
        private FixedString32Bytes _name;

        public FixedPlayerName(string initialValue)
        {
            Debug.Log(initialValue);
            _name = new FixedString32Bytes(initialValue);
            Debug.Log(_name.ToString());
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _name);
        }

        public override string ToString() => _name.Value.ToString();

        public static implicit operator string(FixedPlayerName s) => s.ToString();
        public static implicit operator FixedPlayerName(string s) => new FixedPlayerName(s);
    }
}