using System;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Character.Customisation
{
    /// <summary>
    ///     A network-serializeable struct which stores build data for the various players.
    /// </summary>
    /*public struct PlayerCustomisationState : INetworkSerializable, IEquatable<PlayerCustomisationState>
    {
        public ulong ClientID;
        public bool IsReady;
    
        public int FrameIndex;
        public int LegIndex;
        public int[] SlottableDataIndicies;
        public void SetSlottableDataIndexForSlot(SlotIndex slotIndex, int newValue) => this.SlottableDataIndicies[slotIndex.GetSlotInteger()] = newValue;
        public int GetSlottableDataIndexForSlot(SlotIndex slotIndex) => this.SlottableDataIndicies[slotIndex.GetSlotInteger()];



        public PlayerCustomisationState(ulong clientID) : this(clientID, 0, 0, false) { }
        public PlayerCustomisationState(ulong clientID, int frameIndex, int legIndex, bool isReady, int[] slottableDataIndicies) : this(clientID, frameIndex, legIndex, isReady, ConvertIntArrayToSlottableIntTuple(slottableDataIndicies)) { }
        public PlayerCustomisationState(ulong clientID, int frameIndex, int legIndex, bool isReady, params(SlotIndex, int)[] param)
        {
            this.ClientID = clientID;
            this.IsReady = isReady;

            this.FrameIndex = frameIndex;
            this.LegIndex = legIndex;
            this.SlottableDataIndicies = new int[SlotIndexExtensions.GetMaxPossibleSlots()];
            foreach((SlotIndex slot, int setValue) slotInfo in param)
            {
                SetSlottableDataIndexForSlot(slotInfo.slot, slotInfo.setValue);
            }
        }


        /// <summary>
        ///     Convert an int array to an (SlotIndex, int) tuple array, where SlotIndex corresponds to the position of the integer in the array.
        /// </summary>
        private static (SlotIndex, int)[] ConvertIntArrayToSlottableIntTuple(in int[] dataIndicies)
        {
            (SlotIndex, int)[] output = new (SlotIndex, int)[dataIndicies.Length];
            for(int i = 0; i < dataIndicies.Length; ++i)
                output[i] = (i.ToSlotIndex(), dataIndicies[i]);
            

            return output;
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
    }*/
}