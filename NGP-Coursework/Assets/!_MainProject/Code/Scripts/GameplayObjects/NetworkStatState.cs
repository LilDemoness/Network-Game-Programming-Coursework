using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.NetworkedStats
{
    // Make generic versions of each?
    /// <summary>
    ///     NetworkBehaviour containing a NetworkVariable that represents a variable float statistic (Current Health, Heat, etc) on this character.
    /// </summary>
    public class NetworkStatState : NetworkBehaviour
    {
        public NetworkVariable<float> CurrentValue = new NetworkVariable<float>();
    }
}