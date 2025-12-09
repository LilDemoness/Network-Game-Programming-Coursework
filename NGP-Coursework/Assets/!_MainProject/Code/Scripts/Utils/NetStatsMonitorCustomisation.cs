using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utils
{
    /// <summary>
    ///     Toggles the Network Stats Monitor View when a specified Input Action is performed.
    /// </summary>
    public class NetStatsMonitorCustomisation : MonoBehaviour
    {
        [SerializeField] private RuntimeNetStatsMonitor _monitor;
        [SerializeField] private InputActionReference _toggleNetworkStateAction;

        private void Start()
        {
            _monitor.Visible = false;
            _toggleNetworkStateAction.action.performed += OnToggleNetworkStateAction_performed;
        }
        private void OnDestroy()
        {
            _toggleNetworkStateAction.action.performed += OnToggleNetworkStateAction_performed;   
        }


        private void OnToggleNetworkStateAction_performed(InputAction.CallbackContext ctx)
        {
            // Toggle Visibility.
            //  Note: Using "Visible" rather than "Enabled" to make sure that it keeps updating in the background, ensuring that when we toggle visibility our values are up to date.
            _monitor.Visible = !_monitor.Visible;
        }
    }
}