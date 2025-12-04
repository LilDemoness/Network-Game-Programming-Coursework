using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameState
{
    public abstract class NetworkGameplayState : NetworkBehaviour
    {
        [SerializeField] private PersistentPlayerRuntimeCollection _persistentPlayerCollection;


        private float _matchTimeCompleteEstimate;
        public float RemainingMatchTimeEstimate => _matchTimeCompleteEstimate - Time.time;

        public void SyncGameTime(float timeRemaining) => SyncGameTimeClientRpc(NetworkManager.ServerTime.TimeAsFloat, timeRemaining);
        [Rpc(SendTo.ClientsAndHost)]
        private void SyncGameTimeClientRpc(float serverSendTime, float timeRemaining)
        {
            float serverTimeDelta = NetworkManager.ServerTime.TimeAsFloat - serverSendTime;   // Time that it took for this RPC to arrive at the client.
            float timeRemainingEstimate = timeRemaining - serverTimeDelta;  // Estimate the actual time that is on the server currently.

            _matchTimeCompleteEstimate = Time.time + timeRemainingEstimate;
        }


        public abstract void Initialise(ServerCharacter[] playerCharacters, ServerCharacter[] npcCharacters);
        public abstract void AddPlayer(ServerCharacter playerCharacter);
        public abstract void AddNPC(ServerCharacter npcCharacter);


        public abstract void OnPlayerLeft(ulong clientId);
        public void OnPlayerReconnected(ulong clientId, ServerCharacter newServerCharacter) => OnPlayerReconnected(GetPlayerIndex(clientId), newServerCharacter);
        public abstract void OnPlayerReconnected(int playerIndex, ServerCharacter newServerCharacter);


        protected int GetPlayerIndex(ulong clientId)
        {
            if (!_persistentPlayerCollection.TryGetPlayer(clientId, out PersistentPlayer persistentPlayer))
                throw new System.Exception($"No PersistentPlayer found for Client {clientId}");

            return persistentPlayer.PlayerNumber;
        }
        protected int GetTeamIndex(ulong clientId)
        {
            if (!_persistentPlayerCollection.TryGetPlayer(clientId, out PersistentPlayer persistentPlayer))
                throw new System.Exception($"No PersistentPlayer found for Client {clientId}");

            return persistentPlayer.TeamIndex;
        }


        /// <summary>
        ///     Increment the score of the passed ServerCharacter, if possible.
        /// </summary>
        public abstract void IncrementScore(ServerCharacter serverCharacter);


        /// <summary>
        ///     Server-only function to save this NetworkGameplayState's data to the Persistent State.
        /// </summary>
        public abstract void SavePersistentData(ref PersistentGameState persistentGameState);
    }
}