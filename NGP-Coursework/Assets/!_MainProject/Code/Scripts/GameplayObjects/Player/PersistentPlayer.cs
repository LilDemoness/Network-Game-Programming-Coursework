using Gameplay.GameplayObjects.Character.Customisation.Data;
using Netcode.ConnectionManagement;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace Gameplay.GameplayObjects.Players
{
    /// <summary>
    ///     A NetworkBehaviour that represents a player's connection.<br/>
    ///     Contains multiple other NetworkBehaviours that should persist throughout the entire duration of a player's connection.
    /// </summary>
    /// <remarks>
    ///     We don't need to mark this object as a DontDestroyOnLoad object as Netcode will handle migrating this object between scene loads.
    /// </remarks>
    [RequireComponent(typeof(NetworkObject))]
    public class PersistentPlayer : NetworkBehaviour
    {
        public static PersistentPlayer LocalPersistentPlayer { get; private set; }

        [SerializeField] private PersistentPlayerRuntimeCollection _persistentPlayerRuntimeCollection;

        [SerializeField] private NetworkNameState _networkNameState;
        [SerializeField] private NetworkBuildState _networkBuildState;

        public NetworkVariable<int> PlayerNumber { get; } = new NetworkVariable<int>();
        public NetworkVariable<int> TeamIndex { get; } = new NetworkVariable<int>();

        #if UNITY_EDITOR

        [ContextMenu("Log Player Number")]
        private void Editor_LogPlayerNumber() => Debug.Log(PlayerNumber.Value);
        [ContextMenu("Log Team Index")]
        private void Editor_LogTeamIndex() => Debug.Log(TeamIndex.Value);

        #endif


        #region Public Accessors

        public NetworkNameState NetworkNameState => _networkNameState;
        public NetworkBuildState NetworkBuildState => _networkBuildState;

        #endregion

        public static event System.Action<BuildData> OnLocalPlayerBuildChanged;
        public static event System.Action<ulong, BuildData> OnPlayerBuildChanged;


        public override void OnNetworkSpawn()
        {
            // Name ourselves for easier viewing in the inspector.
            this.gameObject.name = "PersistentPlayer-" + OwnerClientId;

            // Add ourselves to the 'PersistentPlayerRuntimeCollection' for accessing from other scripts.
            //  This is done within OnNetworkSpawn as the NetworkBehaviour properties of this object are accessed when added to the collection.
            //  If we were to do this within Awake/OnEnable/Start, there would be a chance that these values are unset.
            _persistentPlayerRuntimeCollection.Add(this);

            if (IsServer)
            {
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    SessionPlayerData playerData = sessionPlayerData.Value;

                    // Load persistent data from the save data.
                    _networkNameState.Name.Value = playerData.PlayerName;
                    PlayerNumber.Value = playerData.PlayerNumber;
                    TeamIndex.Value = playerData.TeamIndex;
                    _networkBuildState.TrySetBuild(playerData.BuildData);
                }
            }
            if (IsOwner)
            {
                LocalPersistentPlayer = this;
            }

            // Subscribe to Events.
            _networkBuildState.OnBuildChanged += OnBuildChanged;
        }
        public override void OnNetworkDespawn()
        {
            if (LocalPersistentPlayer == this)
                LocalPersistentPlayer = null;

            RemovePersistentPlayer();
        }
        public override void OnDestroy()
        {
            RemovePersistentPlayer();
            base.OnDestroy();
        }


        /// <summary>
        ///     Remove this PersistentPlayer from the <see cref="PersistentPlayerRuntimeCollection"/> and (If the Server) update the saved
        ///     <see cref="SessionPlayerData"/> within the <see cref="SessionManager{T}"/> for retrieval if we reconnect.
        /// </summary>
        private void RemovePersistentPlayer()
        {
            _persistentPlayerRuntimeCollection.Remove(this);

            if (IsServer)
            {
                SavePlayerData();
            }
        }

        //  Server-only.
        public void SavePlayerData()
        {
            SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
            if (sessionPlayerData.HasValue)
            {
                SessionPlayerData playerData = sessionPlayerData.Value;

                playerData.PlayerName = _networkNameState.Name.Value;
                playerData.PlayerNumber = PlayerNumber.Value;
                playerData.TeamIndex = TeamIndex.Value;
                playerData.BuildData = _networkBuildState.BuildDataReference;
                
                // Update set value.
                SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
            }
        }


        private void OnBuildChanged(BuildData buildData)
        {
            if (IsLocalPlayer)
                OnLocalPlayerBuildChanged?.Invoke(buildData);

            OnPlayerBuildChanged?.Invoke(this.OwnerClientId, buildData);
        }
    }
}