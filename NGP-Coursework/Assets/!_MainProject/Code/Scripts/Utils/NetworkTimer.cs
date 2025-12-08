using Unity.Netcode;
using UnityEngine;

namespace Utils
{
    /// <summary>
    ///     A network-synced timer.
    /// </summary>
    public class NetworkTimer : NetworkBehaviour
    {
        private bool _hasTimer;                     // Server-side.
        private bool _timerRunning;                 // Server-side.

        private float _actualTimeRemaining;         // Server-side.


        private float _timerPausedValue = -1;       // Client-side.
        private float _matchTimeCompleteEstimate;   // Client-side.
        public float RemainingMatchTimeEstimate => _timerPausedValue > 0.0 ? _timerPausedValue : Mathf.Max(_matchTimeCompleteEstimate - Time.time, 0.0f);


        public event System.Action OnTimerElapsed;  // Server-only.


        /// <summary>
        ///     Start a new timer.
        /// </summary>
        public void StartTimer(float timerDuration)
        {
            if (!IsSpawned)
                throw new System.Exception("You are trying to start a NetworkTimer before it has been spawned.");

            _hasTimer = true;
            _timerRunning = true;
            _actualTimeRemaining = timerDuration;
            StartTimerClientRpc(timerDuration);
        }
        /// <summary>
        ///     Stop the timer and trigger end events.
        /// </summary>
        public void EndTimer()
        {
            _hasTimer = false;
            _timerRunning = false;

            EndTimerClientRpc();
            OnTimerElapsed?.Invoke();
        }
        /// <summary>
        ///     Stop the timer without triggering any end events.
        /// </summary>
        public void CancelTimer()
        {
            _hasTimer = false;
            _timerRunning = false;
            CancelTimerClientRpc();
        }

        /// <summary>
        ///     Pause the timer.
        /// </summary>
        public void PauseTimer()
        {
            _timerRunning = false;
            PauseTimerClientRpc(_actualTimeRemaining);
        }
        /// <summary>
        ///     Resume the timer.
        /// </summary>
        public void ResumeTimer()
        {
            if (!_hasTimer)
                return;

            _timerRunning = true;
            ResumeTimerClientRpc(_actualTimeRemaining);
        }

        public void SyncGameTime(bool forceSync = false)
        {
            if (forceSync || (_hasTimer && _timerRunning))
            {
                SyncGameTimeClientRpc(_actualTimeRemaining);
            }
        }


        public void SetTimerRemainingTime(float newDuration)
        {
            _hasTimer = true;
            _timerRunning = true;
            _actualTimeRemaining = newDuration;
            SyncGameTime(true);
        }



        private void Update()
        {
            if (!IsServer)
                return;
            if (!_hasTimer || !_timerRunning)
                return;
            
            _actualTimeRemaining -= Time.deltaTime;

            if (_actualTimeRemaining <= 0.0f)
            {
                EndTimer();
            }
        }



        #region RPCs

        [Rpc(SendTo.ClientsAndHost)]
        private void StartTimerClientRpc(float timerDuration)
        {
            float timeRemainingEstimate = timerDuration - GetHalfRoundTripTime();  // Estimate the actual time that is on the server currently.
            _timerPausedValue = -1.0f;
            _matchTimeCompleteEstimate = Time.time + timeRemainingEstimate;
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void EndTimerClientRpc() => _timerPausedValue = 0.0f;
        [Rpc(SendTo.ClientsAndHost)]
        private void CancelTimerClientRpc() => _timerPausedValue = 0.0f;


        /// <summary>
        ///     Pause a timer on clients.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        private void PauseTimerClientRpc(float timeRemaining)
        {
            _timerPausedValue = timeRemaining;
        }
        /// <summary>
        ///     Resume a paused timer on clients.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        private void ResumeTimerClientRpc(float timeRemaining)
        {
            float timeRemainingEstimate = timeRemaining - GetHalfRoundTripTime();  // Estimate the actual time that is on the server currently.

            _matchTimeCompleteEstimate = Time.time + timeRemainingEstimate;
            _timerPausedValue = -1.0f;
        }


        /// <summary>
        ///     Re-sync the time remaining on clients.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        private void SyncGameTimeClientRpc(float timeRemaining)
        {
            float timeRemainingEstimate = timeRemaining - GetHalfRoundTripTime();  // Estimate the actual time that is on the server currently.
            _matchTimeCompleteEstimate = Time.time + timeRemainingEstimate;
        }

        #endregion


        // The estimated time for an RPC to arrive at the client.
        private float GetHalfRoundTripTime() => (NetworkManager.LocalTime.TimeAsFloat - NetworkManager.ServerTime.TimeAsFloat) / 2.0f;
    }
}