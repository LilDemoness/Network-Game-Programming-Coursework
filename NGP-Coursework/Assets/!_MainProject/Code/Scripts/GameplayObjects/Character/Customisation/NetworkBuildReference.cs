using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    public class NetworkBuildReference : NetworkBehaviour
    {
        public NetworkVariable<ulong> NetworkReferenceObjectId;
        private NetworkBuildState _referencedNetworkBuildState;

        public NetworkBuildState Reference => _referencedNetworkBuildState;

        public event System.Action<BuildData> OnBuildChanged;


        public override void OnNetworkSpawn()
        {
            NetworkReferenceObjectId.OnValueChanged += OnNetworkReferenceIdChanged;
            OnNetworkReferenceIdChanged(0, NetworkReferenceObjectId.Value);
        }
        public override void OnNetworkDespawn()
        {
            if (NetworkReferenceObjectId != null)
                NetworkReferenceObjectId.OnValueChanged -= OnNetworkReferenceIdChanged;
        }


        private void OnNetworkReferenceIdChanged(ulong _, ulong newValue)
        {
            if (_referencedNetworkBuildState != null)
                _referencedNetworkBuildState.OnBuildChanged -= InvokeOnBuildChanged;

            if (!NetworkManager.SpawnManager.SpawnedObjects[newValue].TryGetComponent<NetworkBuildState>(out _referencedNetworkBuildState))
                throw new System.Exception("Referenced Object ID doesn't contain a NetworkBuildState object");

            _referencedNetworkBuildState.OnBuildChanged += InvokeOnBuildChanged;
            OnBuildChanged?.Invoke(_referencedNetworkBuildState.BuildDataReference);
        }


        private void InvokeOnBuildChanged(BuildData buildData) => OnBuildChanged?.Invoke(buildData);
    }
}