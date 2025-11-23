using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    /// <summary>
    ///     Handles the processing, and caching of player customisation data when in the customisation menu for clients.
    /// </summary>
    public class PlayerCustomisationManager : NetworkSingleton<PlayerCustomisationManager>
    {
        [SerializeField] private CustomisationOptionsDatabase _optionsDatabase;

        private BuildDataReference _localPlayerBuild
        {
            get => _playerBuilds[NetworkManager.LocalClientId];
            set => _playerBuilds[NetworkManager.LocalClientId] = value;
        }
        private Dictionary<ulong, BuildDataReference> _playerBuilds;   // Synced by RPC calls.
        public Dictionary<ulong, BuildDataReference> BuildData => _playerBuilds;
            
        // Stores a client's current builds for a given frame type index.
        private Dictionary<int, BuildDataReference> _clientCachedBuilds = new Dictionary<int, BuildDataReference>();


        // Events.
        public static event System.Action<ulong, BuildDataReference> OnPlayerBuildChanged;
        public static event System.Action<BuildDataReference> OnLocalClientBuildChanged;

        public static event System.Action<ulong, BuildDataReference> OnPlayerConnected;  // Remove?
        public static event System.Action<ulong> OnPlayerDisconnected;          // Remove?



        protected override void Awake()
        {
            base.Awake();
            _playerBuilds = new Dictionary<ulong, BuildDataReference>();

            DontDestroyOnLoad(this.gameObject);
        }


        /// <summary>
        ///     Sets the player's build to a random value.
        /// </summary>
        [ContextMenu("Randomise Build")]
        private void RandomiseBuild()
        {
            int[] slottableDatas = new int[AttachmentSlotIndexExtensions.GetMaxPossibleSlots()];
            for (int i = 0; i < AttachmentSlotIndexExtensions.GetMaxPossibleSlots(); ++i)
                slottableDatas[i] = Random.Range(0, CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas.Length);

            _localPlayerBuild.ActiveFrameIndex =        Random.Range(0, CustomisationOptionsDatabase.AllOptionsDatabase.FrameDatas.Length);
            _localPlayerBuild.ActiveSlottableIndicies = slottableDatas;

            PlayerCustomisationManager_Server.Instance.SetBuildServerRpc(_localPlayerBuild.GetBuildDataState());
        }


        /// <summary>
        ///     ClientRpc to update the local value of the corresponding player's build.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void UpdatePlayerBuildClientRpc(ulong clientID, BuildDataState newBuild) => UpdatePlayerBuild(clientID, newBuild);
        /// <summary>
        ///     Update the local value of the corresponding player's build.
        /// </summary>
        private void UpdatePlayerBuild(ulong clientID, BuildDataState newBuild)
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


        /// <summary>
        ///     ClientRpc fully sync all data for a client.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost, AllowTargetOverride = true)]
        public void SyncAllBuildDataClientRpc(AllBuildSyncData syncedBuildData, RpcParams clientRpcParams = default)
        {
            for(int i = 0; i < syncedBuildData.Length; ++i)
            {
                if (syncedBuildData.GetClientId(i) == NetworkManager.LocalClientId)
                {
                    HandleLocalPlayerStateChanged(syncedBuildData.GetBuildDataState(i));
                }
                else
                {
                    HandlePlayersStateChanged(syncedBuildData.GetClientId(i), syncedBuildData.GetBuildDataState(i));
                }
            }

            Debug.Log("Received All Build Data: " + _playerBuilds.Count);
        }
        /// <summary>
        ///     ClientRpc to initialise the build data of a client when they join.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost, AllowTargetOverride = true)]
        public void InitialiseBuildDataClientRpc(AllBuildSyncData syncedBuildData, RpcParams clientRpcParams = default)
        {
            // Update our cached build data.
            _playerBuilds = new Dictionary<ulong, BuildDataReference>(syncedBuildData.Length);
            for (int i = 0; i < syncedBuildData.Length; ++i)
            {
                // Cache values for multiple uses.
                ulong clientId = syncedBuildData.GetClientId(i);
                BuildDataReference buildData = new BuildDataReference(syncedBuildData.GetBuildDataState(i));
                Debug.Log("Adding client: " + clientId);

                // Add our player's build to our cached builds.
                _playerBuilds.Add(clientId, buildData);

                // Notify listeners that a new player has joined, and of their build data.
                OnPlayerConnected?.Invoke(clientId, buildData);
                OnPlayerBuildChanged?.Invoke(clientId, buildData);
            }

            Debug.Log("Received All Build Data: " + _playerBuilds.Count);
        }



        #region Changing Selected Elements

        /// <summary>
        ///     Update the local player's selected frame, anticipating on the client and notifying the server.
        /// </summary>
        public void SelectFrame(int selectedFrameIndex)
        {
            // Anticipate change locally.
            TrySetBuildToCachedValue(selectedFrameIndex);

            // Notify server (We will sync build on receiving the subsequent ClientRpc call).
            PlayerCustomisationManager_Server.Instance.SetBuildServerRpc(_localPlayerBuild.GetBuildDataState());
        }
        /// <summary>
        ///     Update the local player's selected slottable for the passed attachment slot, anticipating on the client and notifying the server.
        /// </summary>
        public void SelectSlottableData(AttachmentSlotIndex slotIndex, int slottableDataIndex)
        {
            // Anticipate change locally.
            _localPlayerBuild.ActiveSlottableIndicies[slotIndex.GetSlotInteger()] = slottableDataIndex;

            // Notify server (We will sync build on receiving the subsequent ClientRpc call).
            PlayerCustomisationManager_Server.Instance.SetBuildServerRpc(_localPlayerBuild.GetBuildDataState());
        }


        /// <summary>
        ///     Attempts to load the cached value of a build if the new index doesn't match the current build index.
        /// </summary>
        private bool TrySetBuildToCachedValue(int newFrameIndex)
        {
            if (_localPlayerBuild.ActiveFrameIndex == newFrameIndex)
                return false;

            // We've changed our frame.
            if (_clientCachedBuilds.ContainsKey(newFrameIndex))
            {
                // We have cached data, so load our cached data.
                Debug.Log("Loaded BuildData");
                _localPlayerBuild = _clientCachedBuilds[newFrameIndex];
            }
            else
            {
                // We've not used this frame before and so have nothing to load.
                // Create a empty BuildData for this frame (Resets Slottables as desired).
                Debug.Log("Created New BuildData");
                _localPlayerBuild = new BuildDataReference(newFrameIndex);
            }

            return true;
        }

        public int GetClientSelectedSlottableIndex(AttachmentSlotIndex slotIndex) => _localPlayerBuild.GetSlottableDataIndex(slotIndex);

        #endregion


        /// <summary>
        ///     Handle the updating of the local client's Build Data.
        ///     Includes caching the BuildData for the active state, and loading cached data if we swapped frames.
        /// </summary>
        private void HandleLocalPlayerStateChanged(BuildDataState newBuildState)
        {
            // Update our cached BuildData for this Frame Index.
            if (_clientCachedBuilds.TryGetValue(newBuildState.ActiveFrameIndex, out BuildDataReference buildData))
            {
                buildData.SetBuildData(ref newBuildState);
            }
            else
            {
                _clientCachedBuilds[newBuildState.ActiveFrameIndex] = new BuildDataReference(newBuildState.ActiveFrameIndex);
            }

            // Update our local player state to match the server's.
            _localPlayerBuild.SetBuildData(ref newBuildState);


            // Notify listeners of the change.
            OnLocalClientBuildChanged?.Invoke(_localPlayerBuild);
            OnPlayerBuildChanged?.Invoke(NetworkManager.LocalClientId, _localPlayerBuild);
        }
        /// <summary>
        ///     Handle the updating of a non-local player's build data.
        /// </summary>
        private void HandlePlayersStateChanged(ulong clientID, BuildDataState newBuildState)
        {
            // Update the corresponding player state.
            _playerBuilds[clientID].SetBuildData(ref newBuildState);

            // Update listeners (E.g. To update displayed graphics).
            OnPlayerBuildChanged?.Invoke(clientID, _playerBuilds[clientID]);
        }



        /// <summary>
        ///     Called when a player joins the game.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void HandlePlayerConnectedClientRpc(ulong clientID, BuildDataState initialBuildState)
        {
            _playerBuilds.Add(clientID, new BuildDataReference(initialBuildState));

            OnPlayerConnected?.Invoke(clientID, _playerBuilds[clientID]);
            UpdatePlayerBuild(clientID, initialBuildState);
        }
        /// <summary>
        ///     Called when a player leaves the game.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void HandlePlayerDisconnectedClientRpc(ulong clientID)
        {
            OnPlayerDisconnected?.Invoke(clientID);
        }
    }
}