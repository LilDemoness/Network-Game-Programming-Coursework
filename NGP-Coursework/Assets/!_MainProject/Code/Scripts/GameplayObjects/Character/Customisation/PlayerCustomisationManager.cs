using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    /// <summary>
    ///     Handles the processing, storing, and eventual relaying of player customisation data when in the customisation menu.
    /// </summary>
    public class PlayerCustomisationManager : NetworkSingleton<PlayerCustomisationManager>
    {
        [SerializeField] private CustomisationOptionsDatabase _optionsDatabase;

        private BuildData _clientBuild
        {
            get => _syncedPlayerBuilds[NetworkManager.LocalClientId];
            set => _syncedPlayerBuilds[NetworkManager.LocalClientId] = value;
        }
        private Dictionary<ulong, BuildData> _syncedPlayerBuilds;   // Synced by RPC calls.
        public Dictionary<ulong, BuildData> BuildData => _syncedPlayerBuilds;

        // Stores a client's current builds for a given frame type index.
        private Dictionary<int, BuildData> _clientCachedBuilds = new Dictionary<int, BuildData>();


        // Events.
        public static event System.Action<ulong, BuildData> OnNonLocalClientPlayerBuildChanged;
        public static event System.Action<BuildData> OnLocalClientBuildChanged;

        public static event System.Action<ulong> OnPlayerDisconnected;  // Remove.



        protected override void Awake()
        {
            base.Awake();
            _syncedPlayerBuilds = new Dictionary<ulong, BuildData>();

            DontDestroyOnLoad(this.gameObject);
        }


        [ContextMenu("Randomise Build")]
        private void RandomiseBuild()
        {
            int[] slottableDatas = new int[SlotIndexExtensions.GetMaxPossibleSlots()];
            for(int i = 0; i < SlotIndexExtensions.GetMaxPossibleSlots(); ++i)
                slottableDatas[i] = Random.Range(0, CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas.Length);

            _clientBuild.SetBuildData(
                activeFrame: Random.Range(0, CustomisationOptionsDatabase.AllOptionsDatabase.FrameDatas.Length),
                activeSlottableIndicies: slottableDatas);

            AlterPlayerBuildServerRpc(_clientBuild);
        }


        [Rpc(SendTo.Server)]
        public void AlterPlayerBuildServerRpc(BuildData newBuild, RpcParams rpcParams = default)
        {
            // If we are ONLY the server (NOT the Host too), then update our cached value.
            if (!IsHost)
            {
                if (_syncedPlayerBuilds.TryAdd(rpcParams.Receive.SenderClientId, newBuild))
                {
                    _syncedPlayerBuilds[rpcParams.Receive.SenderClientId] = newBuild;
                }
            }


            AlterPlayerBuildClientRpc(rpcParams.Receive.SenderClientId, newBuild);
        }
        [Rpc(SendTo.ClientsAndHost)]
        public void AlterPlayerBuildClientRpc(ulong clientID, BuildData newBuild)
        {
            if (clientID == NetworkManager.LocalClientId)
            {
                // The local client has been changed.
                HandleLocalPlayerStateChanged(newBuild);
            }
            else
            {
                // A non-local client has been changed.
                HandlePlayersStateChanged(clientID, newBuild);
            }
        }


        private struct AllBuildSyncData : INetworkSerializable
        {
            private ulong[] _clientIDs;
            private BuildData[] _buildDatas;

            public AllBuildSyncData(Dictionary<ulong, BuildData> input)
            {
                _clientIDs = new ulong[input.Count];
                _buildDatas = new BuildData[input.Count];

                int index = 0;
                foreach (var kvp in input)
                {
                    _clientIDs[index] = kvp.Key;
                    _buildDatas[index] = kvp.Value;
                    ++index;
                }
            }

            public Dictionary<ulong, BuildData> ToDictionary()
            {
                Dictionary<ulong, BuildData> outputDictionary = new Dictionary<ulong, BuildData>(capacity: _clientIDs.Length);
                for(int i = 0; i < _clientIDs.Length; ++i)
                {
                    outputDictionary.Add(_clientIDs[i], _buildDatas[i]);
                }
                return outputDictionary;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _clientIDs);
                serializer.SerializeValue(ref _buildDatas);
            }
        }
        [Rpc(SendTo.Server)]
        private void RequestFullBuildDataSyncServerRpc(ulong targetClientID)
        {
            SyncAllBuildDataClientRpc(
                syncedBuildData: new AllBuildSyncData(_syncedPlayerBuilds),
                clientRpcParams: new RpcParams
                    {
                        Send = new RpcSendParams
                        {
                            Target = RpcTarget.Single(targetClientID, RpcTargetUse.Temp)
                        }
                    }
            );
        }
        [Rpc(SendTo.ClientsAndHost, AllowTargetOverride = true)]
        private void SyncAllBuildDataClientRpc(AllBuildSyncData syncedBuildData, RpcParams clientRpcParams = default)
        {
            Dictionary<ulong, BuildData> newSyncedDictionary = syncedBuildData.ToDictionary();

            foreach(var kvp in newSyncedDictionary)
            {
                if(kvp.Key == NetworkManager.LocalClientId)
                {
                    HandleLocalPlayerStateChanged(kvp.Value);
                }
                else
                {
                    HandlePlayersStateChanged(kvp.Key, kvp.Value);
                }
            }

            Debug.Log("Received All Build Data: " + _syncedPlayerBuilds.Count);
        }


        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += Server_HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += Server_HandleClientDisconnected;

                // Ensure that we're accounting for already connected clients.
                foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    Server_HandleClientConnected(client.ClientId);
                }
            }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= Server_HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= Server_HandleClientDisconnected;
            }
        }


        private void Server_HandleClientConnected(ulong clientID)
        {
            // Ensure we've not already added this client.
            if (_syncedPlayerBuilds.ContainsKey(clientID))
                return;

            // Notify the New Client of Existing Clients (Called before adding to prevent duplicate addition of the new client).
            RequestFullBuildDataSyncServerRpc(clientID);

            // Add the client.
            _syncedPlayerBuilds.Add(clientID, new BuildData());
            Debug.Log("Players Count: " + _syncedPlayerBuilds.Count);

            // Notify All Clients of the New Client.
            AlterPlayerBuildClientRpc(clientID, _syncedPlayerBuilds[clientID]);
        }
        private void Server_HandleClientDisconnected(ulong clientID)
        {
            // Remove the player who just left from our dictionary.
            _syncedPlayerBuilds.Remove(clientID);

            // Notify the Clients of the disconnect.
            HandleClientDisconnectClientRpc(clientID);
        }



    #region Changing Selected Elements

    #region Frame

        public void SelectNextFrame() => SelectFrame(MathUtils.Loop(_clientBuild.ActiveFrameIndex + 1, _optionsDatabase.FrameDatas.Length));
        public void SelectPreviousFrame() => SelectFrame(MathUtils.Loop(_clientBuild.ActiveFrameIndex - 1, _optionsDatabase.FrameDatas.Length));
        public void SelectFrame(int selectedFrameIndex)
        {
            TrySetBuildToCachedValue(selectedFrameIndex);
            SelectFrameServerRpc(_clientBuild.ActiveFrameIndex);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SelectFrameServerRpc(int selectedFrameIndex, RpcParams rpcParams = default)
        {
            if (!_syncedPlayerBuilds.ContainsKey(rpcParams.Receive.SenderClientId))
                throw new System.Exception($"We haven't set up a Customisation State for ClientID {rpcParams.Receive.SenderClientId}");

            _syncedPlayerBuilds[rpcParams.Receive.SenderClientId].ActiveFrameIndex = selectedFrameIndex;
            AlterPlayerBuildClientRpc(rpcParams.Receive.SenderClientId, _syncedPlayerBuilds[rpcParams.Receive.SenderClientId]);
        }

        /// <summary>
        ///     Attempts to load the cached value of a build if the new index doesn't match the current build index.
        /// </summary>
        private void TrySetBuildToCachedValue(int newFrameIndex)
        {
            if (_clientBuild.ActiveFrameIndex == newFrameIndex)
                return;

            // We've changed our frame.
            if (_clientCachedBuilds.ContainsKey(newFrameIndex))
            {
                // We have cached data, so load our cached data.
                _clientBuild = _clientCachedBuilds[newFrameIndex];
            }
            else
            {
                // We've not used this frame before and so have nothing to load.
                // Create a empty BuildData for this frame (Resets Slottables as desired).
                _clientBuild = new BuildData(newFrameIndex);
            }
        }

    #endregion

    #region Slottable Data (Weapons & Abilities)

        public void SelectSlottableData(SlotIndex slotIndex, int slottableDataIndex) => SelectSlottableDataServerRpc(slotIndex, slottableDataIndex);

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SelectSlottableDataServerRpc(SlotIndex slotIndex, int slottableDataIndex, RpcParams rpcParams = default)
        {
            if (!_syncedPlayerBuilds.ContainsKey(rpcParams.Receive.SenderClientId))
                throw new System.Exception($"We haven't set up a Customisation State for ClientID {rpcParams.Receive.SenderClientId}");

            // Remove once we've fixed our UI?
            /*bool isValid = false;
            foreach(SlottableData validSlottableData in _optionsDatabase.FrameDatas[customisationState.FrameIndex].AttachmentPoints[slotIndex.GetSlotInteger()].ValidSlottableDatas)
            {
                if (validSlottableData == _optionsDatabase.GetSlottableData(slottableDataIndex))
                {
                    isValid = true;
                    break;
                }
            }
            if (!isValid)
                throw new System.ArgumentException($"Slottable Data Index {slottableDataIndex} isn't supported by \"{_optionsDatabase.FrameDatas[customisationState.FrameIndex].name}\"'s attach point {slotIndex.GetSlotInteger()}");*/

            _syncedPlayerBuilds[rpcParams.Receive.SenderClientId].ActiveSlottableIndicies[slotIndex.GetSlotInteger()] = slottableDataIndex;
            AlterPlayerBuildClientRpc(rpcParams.Receive.SenderClientId, _syncedPlayerBuilds[rpcParams.Receive.SenderClientId]);
        }

        public int GetClientSelectedSlottableIndex(SlotIndex slotIndex) => _clientBuild.GetSlottableDataIndex(slotIndex);

#endregion

        #endregion


        /// <summary>
        ///     Handle the updating of the local client's Build Data.
        ///     Includes caching the BuildData for the active state, and loading cached data if we swapped frames.
        /// </summary>
        private void HandleLocalPlayerStateChanged(BuildData newBuild)
        {
            // Update our cached BuildData for this Frame Index.
            if (!_clientCachedBuilds.TryAdd(newBuild.ActiveFrameIndex, newBuild))
            {
                Debug.Log("Updating Cached State");
                _clientCachedBuilds[newBuild.ActiveFrameIndex] = newBuild;
            }


            // Update the corresponding other player state.
            _clientBuild = newBuild;

            // Notify listeners of the change.
            OnLocalClientBuildChanged?.Invoke(newBuild);
            OnNonLocalClientPlayerBuildChanged?.Invoke(NetworkManager.LocalClientId, newBuild);
        }
        private void HandlePlayersStateChanged(ulong clientID, BuildData newBuild)
        {
            // Update the corresponding player state.
            if (!_syncedPlayerBuilds.TryAdd(clientID, newBuild))
                _syncedPlayerBuilds[clientID] = newBuild;

            // Update listeners (E.g. To update displayed graphics).
            OnNonLocalClientPlayerBuildChanged?.Invoke(clientID, newBuild);
        }



        [Rpc(SendTo.ClientsAndHost)]
        private void HandleClientConnectedClientRpc(ulong clientID, BuildData initialBuild)
        {
            /*AddPlayerInstance(initialState.ClientID);
            AlterPlayerState(ref initialState);*/
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void HandleClientDisconnectClientRpc(ulong clientID)
        {
            OnPlayerDisconnected?.Invoke(clientID);
        }
    }
}