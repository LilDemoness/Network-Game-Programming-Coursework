using Unity.Netcode;
using Utils;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Messages
{
    public struct LifeStateChangedEventMessage : INetworkSerializeByMemcpy
    {
        public LifeState NewLifeState;
        public FixedPlayerName CharacterName;
    }
}