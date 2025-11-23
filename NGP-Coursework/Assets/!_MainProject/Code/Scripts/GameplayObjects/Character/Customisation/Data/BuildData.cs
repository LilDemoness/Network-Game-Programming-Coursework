using System;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    public class BuildDataReference
    {
        // Data.
        private BuildDataState _data;

        // Accessors.
        public int ActiveFrameIndex
        {
            get => _data.ActiveFrameIndex;
            set => _data.ActiveFrameIndex = value;
        }
        public int[] ActiveSlottableIndicies    // Check if this causes duplication in memory.
        {
            get => _data.ActiveSlottableIndicies;
            set => _data.ActiveSlottableIndicies = value;
        }


        public BuildDataReference() : this(0) { }
        public BuildDataReference(int activeFrame) : this(new BuildDataState(activeFrame))
        { }
        public BuildDataReference(int activeFrame, int[] activeSlottableIndicies) : this(new BuildDataState(activeFrame, activeSlottableIndicies))
        { }
        public BuildDataReference(BuildDataState data) => this._data = data;


        public void SetBuildData(ref BuildDataState data) => this._data = data;


        public FrameData GetFrameData() => _data.GetFrameData();
        public SlottableData GetSlottableData(AttachmentSlotIndex slotIndex) => _data.GetSlottableData(slotIndex);
        public int GetSlottableDataIndex(AttachmentSlotIndex slotIndex) => _data.GetSlottableDataIndex(slotIndex);


        public ref BuildDataState GetBuildDataState() => ref this._data;
    }

    public struct BuildDataState : INetworkSerializable, IEquatable<BuildDataState>
    {
        public int ActiveFrameIndex;
        public int[] ActiveSlottableIndicies;

        public BuildDataState(int initialFrame)
        {
            this.ActiveFrameIndex = initialFrame;
            this.ActiveSlottableIndicies = new int[CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(initialFrame).AttachmentPoints.Length];
        }
        public BuildDataState(int initialFrame, int[] initialSlottables)
        {
            this.ActiveFrameIndex = initialFrame;
            this.ActiveSlottableIndicies = initialSlottables;
        }


        public FrameData GetFrameData() => CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(ActiveFrameIndex);
        public SlottableData GetSlottableData(AttachmentSlotIndex slotIndex) => slotIndex.GetSlotInteger() < ActiveSlottableIndicies.Length ? CustomisationOptionsDatabase.AllOptionsDatabase.GetSlottableData(ActiveSlottableIndicies[slotIndex.GetSlotInteger()]) : null;
        public int GetSlottableDataIndex(AttachmentSlotIndex slotIndex) => slotIndex.GetSlotInteger() < ActiveSlottableIndicies.Length ? ActiveSlottableIndicies[slotIndex.GetSlotInteger()] : throw new System.ArgumentOutOfRangeException("");


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ActiveFrameIndex);

            if (serializer.IsWriter && ActiveSlottableIndicies != null)
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();

                writer.WriteValueSafe(ActiveSlottableIndicies.Length);
                for (int i = 0; i < ActiveSlottableIndicies.Length; ++i)
                {
                    writer.WriteValueSafe(ActiveSlottableIndicies[i]);
                }
            }
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();

                reader.ReadValueSafe(out int length);
                ActiveSlottableIndicies = new int[length];

                for (int i = 0; i < length; ++i)
                {
                    reader.ReadValueSafe(out ActiveSlottableIndicies[i]);
                }
            }
        }

        public bool Equals(BuildDataState other) => (this.ActiveFrameIndex, this.ActiveSlottableIndicies) == (other.ActiveFrameIndex, other.ActiveSlottableIndicies);
    }
}