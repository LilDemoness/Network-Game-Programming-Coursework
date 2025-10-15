using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Health
{
    /// <summary>
    ///     NetworkBehaviour containing a NetworkVariable that represents this object's health.
    /// </summary>
    public class NetworkHealthState : NetworkBehaviour
    {
        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    }
}