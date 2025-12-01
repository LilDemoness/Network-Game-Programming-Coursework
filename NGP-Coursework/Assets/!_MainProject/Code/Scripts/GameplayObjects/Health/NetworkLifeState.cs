using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects.Health
{
    /// <summary>
    ///     NetworkBehaviour containing a NetworkVariable that represents this object's life state.
    /// </summary>
    public class NetworkLifeState : NetworkBehaviour
    {
        [SerializeField] private NetworkVariable<LifeState> m_lifeState = new NetworkVariable<LifeState>(GameplayObjects.Character.LifeState.Alive);

        public NetworkVariable<LifeState> LifeState => m_lifeState;


        #if UNITY_EDITOR || DEVELOPMENT_BUILD

        /// <summary>
        ///     Indicates whether this character is in "God Mode" (Cannot be damaged).
        /// </summary>
        public NetworkVariable<bool> IsInGodMode { get; } = new NetworkVariable<bool>(false);

        #endif
    }
}