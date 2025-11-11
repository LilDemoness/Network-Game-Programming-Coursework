using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    /// <summary>
    ///     Handles the processing, storing, and eventual relaying of player customisation data. Data is server-side only.
    /// </summary>
    // Note: All functions should be server-only.
    public class PlayerCustomisationManager_Server : NetworkSingleton<PlayerCustomisationManager_Server>
    {
        private Dictionary<ulong, BuildData> _playerBuilds = new Dictionary<ulong, BuildData>();    // Elements are created when players join and removed when they leave.
        /// <summary>
        ///     All player builds as synced on the server.
        /// </summary>
        /// <remarks> Will be empty on non-server clients.</remarks>
        public Dictionary<ulong, BuildData> PlayerBuilds => _playerBuilds;


        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }

            // Subscribe to connection events.
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectedCallback;
        }
        public override void OnNetworkDespawn()
        {
            if (!IsServer)
                return;


            // Unsubscribe to connection events.
            NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectedCallback;
        }


        #region Player Connecting/Disconnecting

        /// <summary>
        ///     Syncs the current build data with the new player.<br/>
        ///     Adds the new player to our build datas and notifies all clients of their arrival.
        /// </summary>
        private void NetworkManager_OnClientConnectedCallback(ulong clientId)
        {
            if (_playerBuilds.ContainsKey(clientId))
                throw new System.Exception("The same client has been added twice."); // We've already added this client. Throw an exception.

            Debug.Log("Client Connected: " + clientId);

            // Notify the new client of all existing clients.
            InitialiseClientBuildData(clientId);


            // Create the new player's data
            _playerBuilds.Add(clientId, GetDefaultValidBuild());
            
            // Notify all clients (Including the new one) of the new client.
            PlayerCustomisationManager.Instance.HandlePlayerConnectedClientRpc(clientId, _playerBuilds[clientId]);
        }
        /// <summary>
        ///     Removes the player from our builds and notifies all clients of their disconnect.
        /// </summary>
        private void NetworkManager_OnClientDisconnectedCallback(ulong clientId)
        {
            // Remove the player who just left from our stored builds.
            _playerBuilds.Remove(clientId);

            // Notify the clients of the disconnect.
            PlayerCustomisationManager.Instance.HandlePlayerDisconnectedClientRpc(clientId);
        }

        #endregion


        #region Initial Syncing

        /// <summary>
        ///     Initialises a client's build datas by sending all the current values to them.
        /// </summary>
        private void InitialiseClientBuildData(ulong targetClientId)
        {
            PlayerCustomisationManager.Instance.InitialiseBuildDataClientRpc(
                syncedBuildData: new AllBuildSyncData(_playerBuilds),
                clientRpcParams: new RpcParams
                {
                    Send = new RpcSendParams
                    {
                        Target = RpcTarget.Single(targetClientId, RpcTargetUse.Temp)
                    }
                }
            );
        }

        #endregion


        #region Altering Builds

        /// <summary>
        ///     Set the build data of the associated client on the server if it is valid. Syncs all clients whether valid or not
        /// </summary>
        /// <param name="newBuild"> The build we are wishing to validate and set.</param>
        /// <remarks> Assumes that an entry in <see cref="_playerBuilds"/> exists for the sender's clientID</remarks>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SetBuildServerRpc(BuildData newBuild, RpcParams rpcParams = default) => SetBuild(rpcParams.Receive.SenderClientId, newBuild);

        /// <inheritdoc cref="SetBuildServerRpc(BuildData, RpcParams)"/>
        /// <remarks> Assumes that an entry in <see cref="_playerBuilds"/> exists for the passed clientID.</remarks>
        private void SetBuild(ulong clientId, BuildData newBuild)
        {
            // Only update the build if it is valid.
            //  If it is invalid, we will still update the clients to put them back into line with the server.
            if (ValidateBuild(newBuild))
            {
                _playerBuilds[clientId] = newBuild;
            }

            // Update clients.
            PlayerCustomisationManager.Instance.UpdatePlayerBuildClientRpc(clientId, newBuild);
        }

        #endregion


        #region Build Validation

        private bool ValidateFrameIndex(int frameIndex) => true;
        private bool ValidateSlottableIndex(AttachmentSlotIndex slotIndex, int slottableIndex) => true;
        private bool ValidateBuild(BuildData buildData) 
        {
            if (!ValidateFrameIndex(buildData.ActiveFrameIndex))
                return false;

            for(int i = 0; i < buildData.ActiveSlottableIndicies.Length; ++i)
            {
                if (!ValidateSlottableIndex(i.ToSlotIndex(), buildData.ActiveSlottableIndicies[i]))
                    return false;
            }

            // Selected Frame and Slottables are valid.
            return true;
        }


        /// <summary>
        ///     Determines and returns the first possible valid build.
        /// </summary>
        private BuildData GetDefaultValidBuild() => new BuildData();

        #endregion
    }



    /// <summary>
    ///     A NetworkSerializeable Data Container for syncing a [ClientId -> BuildData] dictionary across the network.
    /// </summary>
    public struct AllBuildSyncData : INetworkSerializable
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
            for (int i = 0; i < _clientIDs.Length; ++i)
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
}