using Unity.Netcode;
using Utils;
using Gameplay.GameplayObjects.Character;
using System;

namespace Gameplay.Messages
{
    //public struct LifeStateChangedEventMessage : INetworkSerializable
    public struct LifeStateChangedEventMessage : INetworkSerializeByMemcpy
    {
        public ulong OriginCharacterObjectId;
        public ulong InflicterObjectId;
        public bool HasInflicter;

        public LifeState NewLifeState;
        public FixedPlayerName CharacterName;


        public LifeStateChangedEventMessage(ulong originObjectId, ulong? inflicterObjectId, LifeState newLifeState, FixedPlayerName characterName)
        {
            this.OriginCharacterObjectId = originObjectId;
            if (inflicterObjectId.HasValue)
            {
                this.InflicterObjectId = inflicterObjectId.Value;
                this.HasInflicter = true;
            }
            else
            {
                this.InflicterObjectId = 0;
                this.HasInflicter = false;
            }

            this.NewLifeState = newLifeState;
            this.CharacterName = characterName;
        }


        /*public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref OriginCharacterObjectId);
            serializer.SerializeValue(ref InflicterObjectId);
            serializer.SerializeValue(ref NewLifeState);
            serializer.SerializeValue(ref CharacterName);
        }*/
    }
}