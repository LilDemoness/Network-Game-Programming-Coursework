using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace Gameplay.Messages
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public struct CheatUsedMessage : INetworkSerializeByMemcpy
    {
        private FixedString32Bytes _cheatUsed;
        private FixedPlayerName _cheaterName;

        public string CheatUsed => _cheatUsed.ToString();
        public string CheaterName => _cheaterName.ToString();

        public CheatUsedMessage(string cheatUsed, string cheaterName)
        {
            this._cheatUsed = cheatUsed;
            this._cheaterName = cheaterName;
        }
    }
#endif
}