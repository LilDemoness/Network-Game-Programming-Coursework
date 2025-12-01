using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Wrapper class for direct references to components relevant to movement & physics.<br/>
    ///     Each instance of PhysicsWrapper is registered to a static dictionary, indexed by the NetworkObject's ID.
    /// </summary>
    /// <remarks>
    ///     The root GameObject of a character may not always be the object that moves through the world, so this class gives others a quick reference to the character's in-game position.
    /// </remarks>
    public class PhysicsWrapper : NetworkBehaviour
    {
        private static Dictionary<ulong, PhysicsWrapper> s_physicsWrappers = new Dictionary<ulong, PhysicsWrapper>();
        private ulong _networkObjectId;


        [SerializeField] private Transform _transform;
        public Transform Transform => _transform;


        public override void OnNetworkSpawn()
        {
            _networkObjectId = this.NetworkObjectId;
            s_physicsWrappers.Add(_networkObjectId, this);
        }
        public override void OnNetworkDespawn() => RemovePhysicsWrapper();
        public override void OnDestroy()
        {
            base.OnDestroy();
            RemovePhysicsWrapper();
        }

        private void RemovePhysicsWrapper() => s_physicsWrappers.Remove(_networkObjectId);


        public static bool TryGetPhysicsWrapper(ulong networkObjectId, out PhysicsWrapper physicsWrapper) => s_physicsWrappers.TryGetValue(networkObjectId, out physicsWrapper);
    }
}