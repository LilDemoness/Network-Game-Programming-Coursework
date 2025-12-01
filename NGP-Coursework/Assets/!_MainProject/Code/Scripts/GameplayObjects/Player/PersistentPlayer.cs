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

        public int PlayerNumber { get; set; }


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
            //  If we were to do this within Awake/OnEnable/Start, there would be a change that these values are unset.
            _persistentPlayerRuntimeCollection.Add(this);

            if (IsServer)
            {
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    SessionPlayerData playerData = sessionPlayerData.Value;

                    _networkNameState.Name.Value = playerData.PlayerName;
                    // Cache Build Data?
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
            base.OnDestroy();
            RemovePersistentPlayer();
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
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    SessionPlayerData playerData = sessionPlayerData.Value;

                    playerData.PlayerName = _networkNameState.Name.Value;
                    // Build Data?

                    // Update set value.
                    SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
                }
            }
        }


        private void OnBuildChanged(BuildData buildData)
        {
            Debug.Log("Build Changed: " + buildData.ActiveSlottableIndicies[0]);
            if (IsLocalPlayer)
                OnLocalPlayerBuildChanged?.Invoke(buildData);

            OnPlayerBuildChanged?.Invoke(this.OwnerClientId, buildData);
        }
    }
}