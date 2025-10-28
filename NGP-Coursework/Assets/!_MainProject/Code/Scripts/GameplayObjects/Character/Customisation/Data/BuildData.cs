using Unity.Netcode;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    public class BuildData : INetworkSerializable
    {
        // Data.
        private int _activeFrameIndex = 0;
        private int[] _activeSlottableIndicies = new int[0];

        // Accessors.
        public int ActiveFrameIndex
        {
            get => _activeFrameIndex;
            set => _activeFrameIndex = value;
        }
        public int[] ActiveSlottableIndicies    // Check if this causes duplication in memory.
        {
            get => _activeSlottableIndicies;
            set => _activeSlottableIndicies = value;
        }


        public BuildData() : this(0) { }
        public BuildData(int activeFrame)
        {
            this.ActiveFrameIndex = activeFrame;
            this.ActiveSlottableIndicies = new int[GetFrameData().AttachmentPoints.Length];
        }
        public BuildData(int activeFrame, int[] activeSlottableIndicies)
        {
            SetBuildData(activeFrame, activeSlottableIndicies);
        }
        public BuildData SetBuildData(int activeFrame, int[] activeSlottableIndicies)
        {
            // Set our build data.
            this.ActiveFrameIndex = activeFrame;
            this.ActiveSlottableIndicies = activeSlottableIndicies;

            // Return for fluent interface.
            return this;
        }


        public FrameData GetFrameData() => CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(ActiveFrameIndex);
        public SlottableData GetSlottableData(SlotIndex slotIndex) => slotIndex.GetSlotInteger() < ActiveSlottableIndicies.Length ? CustomisationOptionsDatabase.AllOptionsDatabase.GetSlottableData(ActiveSlottableIndicies[slotIndex.GetSlotInteger()]) : null;
        public int GetSlottableDataIndex(SlotIndex slotIndex) => slotIndex.GetSlotInteger() < ActiveSlottableIndicies.Length ? ActiveSlottableIndicies[slotIndex.GetSlotInteger()] : throw new System.ArgumentOutOfRangeException("");



        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _activeFrameIndex);

            if (serializer.IsWriter)
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();

                writer.WriteValueSafe(_activeSlottableIndicies.Length);
                for (int i = 0; i < _activeSlottableIndicies.Length; ++i)
                {
                    writer.WriteValueSafe(_activeSlottableIndicies[i]);
                }
            }
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();

                reader.ReadValueSafe(out int length);
                _activeSlottableIndicies = new int[length];

                for (int i = 0; i < length; ++i)
                {
                    reader.ReadValueSafe(out _activeSlottableIndicies[i]);
                }
            }
        }
    }
}