using TMPro;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Utils.Editor
{
    public class NetworkLatencyWarning : MonoBehaviour
    {
        [SerializeField] private NetworkSimulator _networkSimulator;

        private TextMeshProUGUI _latencyText;
        bool _latencyTextCreated;

        private Color _textColor = Color.red;

        private bool _artificialLatancyEnabled;


        private void Update()
        {
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
            {
                // Adding this preprocessor directive check since the UnityTransform simulator tools only injects latency into #UNITY_EDITOR or #DEVELOPMENT_BUILD
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                INetworkSimulatorPreset currentSimulationPreset = _networkSimulator.CurrentPreset;
                _artificialLatancyEnabled = currentSimulationPreset.PacketDelayMs > 0.0f
                    || currentSimulationPreset.PacketJitterMs > 0.0f
                    || currentSimulationPreset.PacketLossInterval > 0.0f
                    || currentSimulationPreset.PacketLossPercent > 0;
#else
                _artificialLatancyEnabled = false;
#endif

                if (_artificialLatancyEnabled)
                {
                    if (!_latencyTextCreated)
                    {
                        _latencyTextCreated = true;
                        CreateLatencyText();
                    }

                    _textColor.a = Mathf.PingPong(Time.time, 1.0f);
                    _latencyText.color = _textColor;
                }
            }
            else
            {
                _artificialLatancyEnabled = false;
            }

            // Destroy our artificial latency text if we aren't using artificial latency.
            if (!_artificialLatancyEnabled)
            {
                if (_latencyTextCreated)
                {
                    _latencyTextCreated = false;
                    Destroy(_latencyText);
                }
            }
        }

        /// <summary>
        ///     Creates a UI text object to display the Artificial Latency and adds it to the NetworkOverlay canvas.
        /// </summary>
        private void CreateLatencyText()
        {
            Assert.IsNotNull(NetworkOverlay.Instance,
                "No NetworkOverlay object is within the scene. Add a NetworkOverlay prefab to the bootstrap scene!");
            
            NetworkOverlay.Instance.AddTextToUI("UILatencyWarningText", "Network Latency Enabled", out _latencyText);
        }
    }
}