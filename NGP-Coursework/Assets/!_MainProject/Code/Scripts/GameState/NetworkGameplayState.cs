using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Common data and RPCs for the Gameplay states (General for different GameModes).
    /// </summary>
    public class NetworkGameplayState : NetworkBehaviour
    {
        /*
        Contents:
            - Match Time Syncing
            - Scores? (Stored in PersistentGameState?)
         */

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
    }
}