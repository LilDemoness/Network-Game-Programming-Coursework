using System;
using Unity.Netcode;

namespace Utils
{
    /// <summary>
    ///     Utility class to give non-NetworkBehaviour classes access to NetworkObject-based events.<br/>
    ///     Useful for classes that only want to exist on the Server/Clients, as normal removal would mess with NetworkBehaviour indexing.
    /// </summary>
    public class NetcodeHooks : NetworkBehaviour
    {
        public event Action OnNetworkSpawnHook;
        public event Action OnNetworkDespawnHook;


        public override void OnNetworkSpawn() => OnNetworkSpawnHook?.Invoke();
        public override void OnNetworkDespawn() => OnNetworkDespawnHook?.Invoke();
    }
}