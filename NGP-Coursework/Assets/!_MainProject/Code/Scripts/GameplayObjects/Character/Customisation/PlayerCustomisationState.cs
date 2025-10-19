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
        public int PrimaryWeaponIndex;
        public int SecondaryWeaponIndex;
        public int TertiaryWeaponIndex;
        public int AbilityIndex;



        public PlayerCustomisationState(ulong clientID) : this(clientID, 0, 0, 0, 0, 0, 0, false) { }
        public PlayerCustomisationState(ulong clientID, int frameIndex, int legIndex, int primaryWeaponIndex, int secondaryWeaponIndex, int tertiaryWeaponIndex, int abilityIndex, bool isReady)
        {
            this.ClientID = clientID;
            this.IsReady = isReady;

            this.FrameIndex = frameIndex;
            this.LegIndex = legIndex;
            this.PrimaryWeaponIndex = primaryWeaponIndex;
            this.SecondaryWeaponIndex = secondaryWeaponIndex;
            this.TertiaryWeaponIndex = tertiaryWeaponIndex;
            this.AbilityIndex = abilityIndex;
        }

        public PlayerCustomisationState NewWithIsReady(bool isReadyValue)           { this.IsReady = isReadyValue;          return this; }

        public PlayerCustomisationState NewWithFrameIndex(int newValue)             { this.FrameIndex = newValue;           return this; }
        public PlayerCustomisationState NewWithLegIndex(int newValue)               { this.LegIndex = newValue;             return this; }
        public PlayerCustomisationState NewWithPrimaryWeaponIndex(int newValue)     { this.PrimaryWeaponIndex = newValue;   return this; }
        public PlayerCustomisationState NewWithSecondaryWeaponIndex(int newValue)   { this.SecondaryWeaponIndex = newValue; return this; }
        public PlayerCustomisationState NewWithTertiaryWeaponIndex(int newValue)    { this.TertiaryWeaponIndex = newValue;  return this; }
        public PlayerCustomisationState NewWithAbilityIndex(int newValue)           { this.AbilityIndex = newValue;         return this; }

    
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientID);
            serializer.SerializeValue(ref IsReady);

            serializer.SerializeValue(ref FrameIndex);
            serializer.SerializeValue(ref LegIndex);
            serializer.SerializeValue(ref PrimaryWeaponIndex);
            serializer.SerializeValue(ref SecondaryWeaponIndex);
            serializer.SerializeValue(ref TertiaryWeaponIndex);
            serializer.SerializeValue(ref AbilityIndex);
        }
        public bool Equals(PlayerCustomisationState other)
        {
            return (this.ClientID == other.ClientID)
                && (this.IsReady == other.IsReady)
                && (this.FrameIndex == other.FrameIndex)
                && (this.LegIndex == other.LegIndex)
                && (this.PrimaryWeaponIndex == other.PrimaryWeaponIndex)
                && (this.SecondaryWeaponIndex == other.SecondaryWeaponIndex)
                && (this.TertiaryWeaponIndex == other.TertiaryWeaponIndex)
                && (this.AbilityIndex == other.AbilityIndex);
        }
    }
}