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
        public int[] WeaponIndicies;
        public int AbilityIndex;


        public int GetWeaponIndexForSlotIndex(WeaponSlotIndex slotIndex) => WeaponIndicies[(int)slotIndex - 1];


        public PlayerCustomisationState(ulong clientID) : this(clientID, 0, 0, new int[WeaponSlotIndex.Unset.GetMaxPossibleWeaponSlots()], 0, false) { }
        public PlayerCustomisationState(ulong clientID, int frameIndex, int legIndex, int[] weaponIndicies, int abilityIndex, bool isReady)
        {
            this.ClientID = clientID;
            this.IsReady = isReady;

            this.FrameIndex = frameIndex;
            this.LegIndex = legIndex;
            this.WeaponIndicies = weaponIndicies;
            this.AbilityIndex = abilityIndex;
        }

        public PlayerCustomisationState NewWithIsReady(bool isReadyValue)           { this.IsReady = isReadyValue;          return this; }

        public PlayerCustomisationState NewWithFrameIndex(int newValue)             { this.FrameIndex = newValue;           return this; }
        public PlayerCustomisationState NewWithLegIndex(int newValue)               { this.LegIndex = newValue;             return this; }
        public PlayerCustomisationState NewWithWeaponIndex(WeaponSlotIndex slotIndex, int newValue)     { this.WeaponIndicies[(int)slotIndex - 1] = newValue;   return this; }
        public PlayerCustomisationState NewWithAbilityIndex(int newValue)           { this.AbilityIndex = newValue;         return this; }

    
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientID);
            serializer.SerializeValue(ref IsReady);

            serializer.SerializeValue(ref FrameIndex);
            serializer.SerializeValue(ref LegIndex);
            serializer.SerializeValue(ref WeaponIndicies);
            serializer.SerializeValue(ref AbilityIndex);
        }
        public bool Equals(PlayerCustomisationState other)
        {
            return (ClientID, IsReady, FrameIndex, LegIndex, WeaponIndicies, AbilityIndex) == (other.ClientID, other.IsReady, other.FrameIndex, other.LegIndex, other.WeaponIndicies, other.AbilityIndex);
        }
    }
}