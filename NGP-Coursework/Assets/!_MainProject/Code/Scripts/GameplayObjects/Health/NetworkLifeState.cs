using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Health
{
    /// <summary>
    ///     NetworkBehaviour containing a NetworkVariable that represents this object's life state.
    /// </summary>
    public class NetworkLifeState : NetworkBehaviour
    {
        public NetworkVariable<bool> IsDead = new NetworkVariable<bool>();
    }
}