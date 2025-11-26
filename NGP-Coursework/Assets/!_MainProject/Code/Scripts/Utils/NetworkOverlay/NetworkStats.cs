using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Utils.Editor
{
    /// <summary>
    ///     A utility class to help with showing Network statistics at runtime.
    ///     This component attaches to any networked object and will spawn all required text and canvases.
    /// </summary>
    // Note: Check Unity for automatic support of this.
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkStats : NetworkBehaviour
    {
        // For values like RTT, an exponential moving average is a better indication of their actual value and fluctuates less.
        private struct ExponentialMovingAverageCalculator
        {
            private readonly float _alpha;
            private float _average;

            public float Average => _average;

            public ExponentialMovingAverageCalculator(float average)
            {
                _alpha = 2.0f / (MAX_WINDOW_SIZE + 1);
                _average = average;
            }

            public float NextValue(float value) => _average = (value - _average) * _alpha + _average;
        }


        // RTT:
        // - Client sends a ping RPC to the server and starts its timer.
        // - The server receives the ping and sends a pong request to the client.
        // - The client receives that poing response and stops its time.
        // The RPC value is using a moving average, so we don't have a value that moves too much, but is still reactive to RTT changes.


        private const int MAX_WINDOW_TIME_SECONDS = 3;  // It should take X seconds for the value to react to changes.
        private const float PING_INTERVAL_SECONDS = 0.1f;
        private const float MAX_WINDOW_SIZE = MAX_WINDOW_TIME_SECONDS / PING_INTERVAL_SECONDS;

        // Some games are less sensative to latency than others. For fast-paced games, latency above 100ms can become a challenge for players.
        private const float STRUGGLING_NETWORK_CONDITIONS_RTT_THRESHOLD = 80;
        private const float BAD_NETWORK_CONDITIONS_RTT_THRESHOLD = 150;

        private ExponentialMovingAverageCalculator _gameRTT = new ExponentialMovingAverageCalculator(0);
        private ExponentialMovingAverageCalculator _utpRTT = new ExponentialMovingAverageCalculator(0);

        private float _lastPingTime;
        private TextMeshProUGUI _statText;
        private TextMeshProUGUI _hostTypeText;
        private TextMeshProUGUI _badNetworkConditionsText;

        
        // When receiving pong client RPCs, we need to know when the initiating ping sent it so that we can calculate its individual RTT.
        private int _currentRTTPingId;
        private Dictionary<int, float> _pingHistoryStartTimes = new Dictionary<int, float>();

        private RpcParams _pongClientParams;
        private string _textToDisplay;


        public override void OnNetworkSpawn()
        {
            bool isClientOnly = IsClient && !IsServer;
            if (!IsOwner && isClientOnly)
            {
                // We don't want to track player ghost stats, only our own.
                this.enabled = false;
                return;
            }

            if (IsOwner)
            {
                CreateNetworkStatsText();
            }

            _pongClientParams = RpcTarget.Group(new[] { OwnerClientId }, RpcTargetUse.Persistent);
        }
        public override void OnNetworkDespawn()
        {
            if (_statText != null)
                Destroy(_statText.gameObject);

            if (_hostTypeText != null)
                Destroy(_hostTypeText.gameObject);

            if (_badNetworkConditionsText != null)
                Destroy(_badNetworkConditionsText.gameObject);
        }
        private void FixedUpdate()
        {
            if (!IsServer)
            {
                if (Time.realtimeSinceStartup - _lastPingTime > PING_INTERVAL_SECONDS)
                {
                    // Perform a Ping
                    // We could have had a ping/pong where the ping sends the pong and the pong sends the ping.
                    //  The issue with this is that the higher the latency, the lower the sampling would be,
                    //  and we require pings being sent at regular intervals.
                    ServerPingRpc(_currentRTTPingId);
                    _pingHistoryStartTimes[_currentRTTPingId] = Time.realtimeSinceStartup;
                    ++_currentRTTPingId;
                    _lastPingTime = Time.realtimeSinceStartup;

                    _utpRTT.NextValue(NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId));
                }

                if (_statText != null)
                {
                    // Display our RTT values.
                    _textToDisplay = $"RTT: {(_gameRTT.Average * 1000.0f).ToString("0")}ms;\nUTP RTT: {_utpRTT.Average.ToString("0")}ms";

                    // Determine colour based on condition thresholds.
                    _statText.color =
                        (_utpRTT.Average > BAD_NETWORK_CONDITIONS_RTT_THRESHOLD) ? Color.red
                        : (_utpRTT.Average > STRUGGLING_NETWORK_CONDITIONS_RTT_THRESHOLD) ? Color.yellow
                        : Color.white;
                }
            }
            else
            {
                _textToDisplay = $"Connected Players: {NetworkManager.Singleton.ConnectedClients.Count.ToString()}";
            }

            if (_statText != null)
            {
                _statText.text = _textToDisplay;
            }
        }


        /// <summary>
        ///     Create a UI Text Object and add it to the NetworkOverlay canvas.
        /// </summary>
        private void CreateNetworkStatsText()
        {
            Assert.IsNotNull(NetworkOverlay.Instance,
                "No NetworkOverlay object is within the scene. Add a NetworkOverlay prefab to the bootstrap scene!");

            string hostType = IsHost ? "Host" : IsClient ? "Client" : "Unknown";
            NetworkOverlay.Instance.AddTextToUI("UIHostTypeText", $"Type: {hostType}", out _hostTypeText);
            NetworkOverlay.Instance.AddTextToUI("UIStatText", "No Stat", out _statText);
            NetworkOverlay.Instance.AddTextToUI("UIBadConditionsText", "", out _badNetworkConditionsText);
        }


        [Rpc(SendTo.Server)]
        private void ServerPingRpc(int pingId, RpcParams serverParams = default)
        {
            ClientPongRpc(pingId, _pongClientParams);
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void ClientPongRpc(int pingId, RpcParams clientParams = default)
        {
            float startTime = _pingHistoryStartTimes[pingId];
            _pingHistoryStartTimes.Remove(pingId);
            _gameRTT.NextValue(Time.realtimeSinceStartup - startTime);
        }
    }
}