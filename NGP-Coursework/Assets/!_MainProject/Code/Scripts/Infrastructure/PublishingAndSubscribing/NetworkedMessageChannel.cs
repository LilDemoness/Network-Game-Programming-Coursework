using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using VContainer;

namespace Infrastructure
{
    /// <summary>
    ///     A type of message channel which allows the server to publish a message that will be send to all clients as well as being published locally.<br/>
    ///     Clients and the server can both subscribe to it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetworkedMessageChannel<T> : MessageChannel<T> where T : unmanaged, INetworkSerializeByMemcpy
    {

    }
}