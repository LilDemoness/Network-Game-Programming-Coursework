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

        private BuildData _localPlayerBuild
        {
            get => _playerBuilds[NetworkManager.LocalClientId];
            set => _playerBuilds[NetworkManager.LocalClientId] = value;
        }
        private Dictionary<ulong, BuildData> _playerBuilds;   // Synced by RPC calls.
        public Dictionary<ulong, BuildData> BuildData => _playerBuilds;

        // Stores a client's current builds for a given frame type index.
        private Dictionary<int, BuildData> _clientCachedBuilds = new Dictionary<int, BuildData>();


        // Events.
        public static event System.Action<ulong, BuildData> OnNonLocalClientPlayerBuildChanged;
        public static event System.Action<BuildData> OnLocalClientBuildChanged;

        public static event System.Action<ulong, BuildData> OnPlayerConnected;  // Remove?
        public static event System.Action<ulong> OnPlayerDisconnected;          // Remove?



        protected override void Awake()
        {
            base.Awake();
            _playerBuilds = new Dictionary<ulong, BuildData>();

            DontDestroyOnLoad(this.gameObject);
        }


        /// <summary>
        ///     Sets the player's build to a random value.
        /// </summary>
        [ContextMenu("Randomise Build")]
        private void RandomiseBuild()
        {
            int[] slottableDatas = new int[SlotIndexExtensions.GetMaxPossibleSlots()];
            for (int i = 0; i < SlotIndexExtensions.GetMaxPossibleSlots(); ++i)
                slottableDatas[i] = Random.Range(0, CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas.Length);

            _localPlayerBuild.ActiveFrameIndex =        Random.Range(0, CustomisationOptionsDatabase.AllOptionsDatabase.FrameDatas.Length);
            _localPlayerBuild.ActiveSlottableIndicies = slottableDatas;

            PlayerCustomisationManager_Server.Instance.SetBuildServerRpc(_localPlayerBuild);
        }


        /// <summary>
        ///     ClientRpc to update the local value of the corresponding player's build.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void UpdatePlayerBuildClientRpc(ulong clientID, BuildData newBuild) => UpdatePlayerBuild(clientID, newBuild);
        /// <summary>
        ///     Update the local value of the corresponding player's build.
        /// </summary>
        private void UpdatePlayerBuild(ulong clientID, BuildData newBuild)
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
            Dictionary<ulong, BuildData> newSyncedDictionary = syncedBuildData.ToDictionary();

            foreach (var kvp in newSyncedDictionary)
            {
                if (kvp.Key == NetworkManager.LocalClientId)
                {
                    HandleLocalPlayerStateChanged(kvp.Value);
                }
                else
                {
                    HandlePlayersStateChanged(kvp.Key, kvp.Value);
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
            _playerBuilds = syncedBuildData.ToDictionary();

            // Notify listeners that a new player has joined, and of their build data.
            foreach (var kvp in _playerBuilds)
            {
                Debug.Log("Adding client: " + kvp.Key);
                OnPlayerConnected?.Invoke(kvp.Key, kvp.Value);
                OnNonLocalClientPlayerBuildChanged?.Invoke(kvp.Key, kvp.Value);
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
            PlayerCustomisationManager_Server.Instance.SetBuildServerRpc(_localPlayerBuild);
        }
        /// <summary>
        ///     Update the local player's selected slottable for the passed slot, anticipating on the client and notifying the server.
        /// </summary>
        public void SelectSlottableData(SlotIndex slotIndex, int slottableDataIndex)
        {
            // Anticipate change locally.
            _localPlayerBuild.ActiveSlottableIndicies[slotIndex.GetSlotInteger()] = slottableDataIndex;

            // Notify server (We will sync build on receiving the subsequent ClientRpc call).
            PlayerCustomisationManager_Server.Instance.SetBuildServerRpc(_localPlayerBuild);
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
                _localPlayerBuild = new BuildData(newFrameIndex);
            }

            return true;
        }

        public int GetClientSelectedSlottableIndex(SlotIndex slotIndex) => _localPlayerBuild.GetSlottableDataIndex(slotIndex);

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
                _clientCachedBuilds[newBuild.ActiveFrameIndex] = newBuild;
            }

            // Update our local player state to match the server's.
            _localPlayerBuild = newBuild;


            // Notify listeners of the change.
            OnLocalClientBuildChanged?.Invoke(newBuild);
            OnNonLocalClientPlayerBuildChanged?.Invoke(NetworkManager.LocalClientId, newBuild);
        }
        /// <summary>
        ///     Handle the updating of a non-local player's build data.
        /// </summary>
        private void HandlePlayersStateChanged(ulong clientID, BuildData newBuild)
        {
            // Update the corresponding player state.
            _playerBuilds[clientID] = newBuild;

            // Update listeners (E.g. To update displayed graphics).
            OnNonLocalClientPlayerBuildChanged?.Invoke(clientID, newBuild);
        }



        /// <summary>
        ///     Called when a player joins the game.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void HandlePlayerConnectedClientRpc(ulong clientID, BuildData initialBuild)
        {
            OnPlayerConnected?.Invoke(clientID, initialBuild);

            _playerBuilds.Add(clientID, initialBuild);
            UpdatePlayerBuild(clientID, initialBuild);
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