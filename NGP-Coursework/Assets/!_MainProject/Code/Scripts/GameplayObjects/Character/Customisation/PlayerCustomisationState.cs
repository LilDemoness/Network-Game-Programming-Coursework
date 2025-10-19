using System;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    /// <summary>
    ///     A network-serializeable struct which stores build data for the various players.
    /// </summary>
    public struct PlayerCustomisationState : INetworkSerializable, IEquatable<PlayerCustomisationState>
    {
        public ulong ClientID;
        public bool IsReady;
    
        public int FrameIndex;
        public int LegIndex;
        public int[] SlottableDataIndicies;
        public void SetSlottableDataIndexForSlot(SlotIndex slotIndex, int newValue) => this.SlottableDataIndicies[(int)slotIndex - 1] = newValue;
        public int GetSlottableDataIndexForSlot(SlotIndex slotIndex) => this.SlottableDataIndicies[(int)slotIndex - 1];



        public PlayerCustomisationState(ulong clientID) : this(clientID, 0, 0, false) { }
        public PlayerCustomisationState(ulong clientID, int frameIndex, int legIndex, bool isReady, params(SlotIndex, int)[] param)
        {
            this.ClientID = clientID;
            this.IsReady = isReady;

            this.FrameIndex = frameIndex;
            this.LegIndex = legIndex;
            this.SlottableDataIndicies = new int[SlotIndex.Unset.GetMaxPossibleSlots()];
            foreach((SlotIndex slot, int setValue) slotInfo in param)
            {
                SetSlottableDataIndexForSlot(slotInfo.slot, slotInfo.setValue);
            }
        }

        public PlayerCustomisationState NewWithIsReady(bool isReadyValue)           { this.IsReady = isReadyValue;          return this; }

        public PlayerCustomisationState NewWithFrameIndex(int newValue)             { this.FrameIndex = newValue;           return this; }
        public PlayerCustomisationState NewWithLegIndex(int newValue)               { this.LegIndex = newValue;             return this; }
        public PlayerCustomisationState NewWithSlottableDataValue(SlotIndex slotIndex, int newValue) { this.SetSlottableDataIndexForSlot(slotIndex, newValue); return this; }

        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientID);
            serializer.SerializeValue(ref IsReady);

            serializer.SerializeValue(ref FrameIndex);
            serializer.SerializeValue(ref LegIndex);
            serializer.SerializeValue(ref SlottableDataIndicies);
        }
        public bool Equals(PlayerCustomisationState other)
        {
            return (ClientID, IsReady, FrameIndex, LegIndex, SlottableDataIndicies) == (other.ClientID, other.IsReady, other.FrameIndex, other.LegIndex, other.SlottableDataIndicies);
        }
    }
    public struct SlotIndexToSelectedIndexWrapper : INetworkSerializable, IEquatable<SlotIndexToSelectedIndexWrapper>
    {
        public SlotIndex SlotIndex;
        public int SelectedIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SlotIndex);
            serializer.SerializeValue(ref SelectedIndex);
        }
        public bool Equals(SlotIndexToSelectedIndexWrapper other)
        {
            return (SlotIndex, SelectedIndex) == (other.SlotIndex, other.SelectedIndex);
        }
    }
}