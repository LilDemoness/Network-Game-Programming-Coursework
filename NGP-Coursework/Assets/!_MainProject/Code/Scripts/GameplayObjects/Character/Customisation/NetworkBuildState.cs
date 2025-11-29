using Gameplay.GameplayObjects.Character.Customisation.Data;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects
{
    public class NetworkBuildState : NetworkBehaviour
    {
        [field:SerializeField]
        public NetworkVariable<int> ActiveFrameIndex { get; set; } = new NetworkVariable<int>();
        [field:SerializeField]
        public NetworkList<int> ActiveSlottableIndicies { get; set; } = new NetworkList<int>(new int[CustomisationOptionsDatabase.MAX_SLOTTABLE_DATAS]);


        private BuildData _buildDataReference;
        public BuildData BuildDataReference => _buildDataReference;
        public event System.Action<BuildData> OnBuildChanged;


        private void Awake()
        {
            _buildDataReference = new BuildData(0);

            SubscribeToNetworkEvents();
        }
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                InitialiseBuildState();
        }
        public override void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();

            base.OnDestroy();
        }

        private void SubscribeToNetworkEvents()
        {
            ActiveFrameIndex.OnValueChanged += ActiveFrameIndex_OnValueChanged;
            ActiveSlottableIndicies.OnListChanged += ActiveSlottableIndicies_OnListChanged;
        }
        private void UnsubscribeFromNetworkEvents()
        {
            ActiveFrameIndex.OnValueChanged -= ActiveFrameIndex_OnValueChanged;
            ActiveSlottableIndicies.OnListChanged -= ActiveSlottableIndicies_OnListChanged;
        }

        private void InitialiseBuildState()
        {
            _cachedBuildData = new Dictionary<int, int[]>()
            {
                { 0, new int[CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(0).AttachmentPoints.Length] }
            };
            Debug.Log("Setting Build");

            SetBuildServerRpc(0, new int[CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(0).AttachmentPoints.Length]);
        }


        private void ActiveFrameIndex_OnValueChanged(int previousValue, int newFrameIndex)
        {
            _buildDataReference.SetFrameDataIndex(newFrameIndex);
            OnBuildChanged?.Invoke(_buildDataReference);
        }
        private void ActiveSlottableIndicies_OnListChanged(NetworkListEvent<int> changeEvent)
        {
            _buildDataReference.ActiveSlottableIndicies[changeEvent.Index] = changeEvent.Value;
            CacheSlottableIndex(changeEvent.Index, changeEvent.Value);

            OnBuildChanged?.Invoke(_buildDataReference);
        }
        private void CacheSlottableIndex(int index, int value)
        {
            if (_cachedBuildData.ContainsKey(ActiveFrameIndex.Value))
            {
                Debug.Log("Has Key. Set Index: " + index);
                _cachedBuildData[ActiveFrameIndex.Value][index] = value;
            }
        }



        public void SelectFrame(int frameIndex)
        {
            // Load cached build.
            int[] loadedFrameData = GetCachedBuildData(frameIndex);

            // Notify Server.
            SetBuildServerRpc(frameIndex, loadedFrameData);
        }
        private int[] GetCachedBuildData(int frameIndex)
        {
            // To-do: Implement Caching.
            //  Note: We were having issue of values getting overriden when swapping frames, leading to inconsistent setting.
            return new int[CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(frameIndex).AttachmentPoints.Length];

            if (_cachedBuildData.TryGetValue(frameIndex, out int[] cachedSlottables))
            {
                string slottablesString = "";
                for(int i = 0; i < cachedSlottables.Length; ++i)
                    slottablesString += cachedSlottables[i] + ",";
                Debug.Log("Loading Cache: " + slottablesString);
                return cachedSlottables;
            }
            else
            {
                Debug.Log("Caching");
                _cachedBuildData.Add(frameIndex, new int[CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(frameIndex).AttachmentPoints.Length]);
                return _cachedBuildData[frameIndex];
            }
        }
        private Dictionary<int, int[]> _cachedBuildData = new Dictionary<int, int[]>();

        public void SelectSlottableData(AttachmentSlotIndex slotIndex, int slottableDataIndex) => SetSlottableServerRpc(slotIndex, slottableDataIndex);


        [Rpc(SendTo.Server)]
        private void SetBuildServerRpc(int frameIndex, int[] slottableIndicies)
        {
            // Unsubscribe from NetworkEvent Notifications to prevent multiple OnBuildChanged calls when we only need one.
            UnsubscribeFromNetworkEvents();

            // Update NetworkVariables.
            ActiveFrameIndex.Value = frameIndex;
            for (int i = 0; i < slottableIndicies.Length; ++i)
                ActiveSlottableIndicies[i] = slottableIndicies[i];
            
            // Update Rereference.
            _buildDataReference.SetFrameDataIndex(frameIndex);
            _buildDataReference.SetActiveSlottableDataIndicies(slottableIndicies);

            // Notify Listeners.
            OnBuildChanged?.Invoke(_buildDataReference);
            // Resubscribe to NetworkEvent Notifications.
            SubscribeToNetworkEvents();
        }
        [Rpc(SendTo.Server)]
        public void SetSlottableServerRpc(AttachmentSlotIndex slotIndex, int slottableIndex) => ActiveSlottableIndicies[slotIndex.GetSlotInteger()] = slottableIndex;



        public int GetFrameIndex() => ActiveFrameIndex.Value;
        public int GetSlottableIndex(AttachmentSlotIndex slotIndex) => ActiveSlottableIndicies[slotIndex.GetSlotInteger()];
    }
}