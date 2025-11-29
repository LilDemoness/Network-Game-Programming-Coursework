using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects
{
    public class NetworkBuildState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<BuildDataState> BuildData { get; } = new NetworkVariable<BuildDataState>();

        private BuildDataReference _buildDataReference;
        public BuildDataReference BuildDataReference => _buildDataReference;
        public event System.Action<BuildDataReference> OnBuildChanged;


        private void Awake()
        {
            _buildDataReference = new BuildDataReference(BuildData.Value);
            BuildData.OnValueChanged += OnBuildDataValueChanged;
        }
        public override void OnDestroy()
        {
            BuildData.OnValueChanged -= OnBuildDataValueChanged;
            base.OnDestroy();
        }
        

        private void OnBuildDataValueChanged(BuildDataState previousBuild, BuildDataState newBuild)
        {
            _buildDataReference.SetBuildData(ref newBuild);
            OnBuildChanged?.Invoke(_buildDataReference);
        }


        public void SetFrame(int frameIndex)
        {
            BuildDataState buildData = BuildData.Value;
            buildData.ActiveFrameIndex = frameIndex;
            BuildData.Value = buildData;
        }
        public int GetFrameIndex() => BuildData.Value.ActiveFrameIndex;

        public void SetSlottable(AttachmentSlotIndex slotIndex, int slottableIndex)
        {
            BuildDataState buildData = BuildData.Value;
            buildData.ActiveSlottableIndicies[slotIndex.GetSlotInteger()] = slottableIndex;
            BuildData.Value = buildData;
        }
        public int GetSlottableIndex(AttachmentSlotIndex slotIndex) => BuildData.Value.GetSlottableDataIndex(slotIndex);
    }
}