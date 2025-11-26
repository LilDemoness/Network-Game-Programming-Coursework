using Gameplay.Configuration;
using Infrastructure;
using Netcode.ConnectionManagement;
using TMPro;
using UnityEngine;
using VContainer;

namespace Gameplay.UI.MainMenu
{
    public class IPUIMediator : MonoBehaviour
    {
        public const string DEFAULT_IP = "127.0.0.1";
        public const int DEFAULT_PORT = 9998;

        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private TextMeshProUGUI _playerNameLabel;
        [SerializeField] private IPJoiningUI _ipJoiningUI;
        [SerializeField] private IPHostingUI _ipHostingUI;

        [SerializeField] private UITinter _joinTabButtonHighlightTinter;
        [SerializeField] private UITinter _joinTabButtonBlockerTinter;
        [SerializeField] private UITinter _hostTabButtonHighlightTinter;
        [SerializeField] private UITinter _hostTabButtonBlockerTinter;

        [SerializeField] private GameObject _signInSpinner;

        [SerializeField] private IPConnectionWindow _ipConnectionWindow;

        [Inject]
        private NameGenerationData _nameGenerationData;
        [Inject]
        private ConnectionManager _connectionManager;


        public IPHostingUI IPHostingUI => _ipHostingUI;

        private ISubscriber<ConnectStatus> _connectStatusSubscriber;


        [Inject]
        private void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSubscriber)
        {
            this._connectStatusSubscriber = connectStatusSubscriber;
            _connectStatusSubscriber.Subscribe(OnConnectStatusMessage);
        }


        private void Awake()
        {
            Hide();
        }
        private void Start()
        {
            // Show 'Create IP' as default.
            ToggleCreateIPUI();
            RegenerateName();
        }
        private void OnDestroy()
        {
            if (_connectStatusSubscriber != null)
                _connectStatusSubscriber.Unsubscribe(OnConnectStatusMessage);
        }


        private void OnConnectStatusMessage(ConnectStatus connectStatus)
        {
            DisableSignInSpinner();
        }

        public void HostIPRequest(string ip, string port)
        {
            ValidateIpAndPort(ref ip, port, out int portInteger);

            // Perform the Host attempt.
            _signInSpinner.SetActive(true);
            _connectionManager.StartHostIP(_playerNameLabel.text, ip, portInteger);
        }
        public void JoinWithIP(string ip, string port)
        {
            ValidateIpAndPort(ref ip, port, out int portInteger);

            // Perform the Join attempt.
            _signInSpinner.SetActive(true);
            _connectionManager.StartClientIP(_playerNameLabel.text, ip, portInteger);
            _ipConnectionWindow.ShowConnectingWindow();
        }


        private void ValidateIpAndPort(ref string ip, string port, out int portInteger)
        {
            // Port Validation.
            int.TryParse(port, out portInteger);
            if (portInteger <= 0)
            {
                // Invalid port. Set to default.
                portInteger = DEFAULT_PORT;
            }

            // IP Address Validation.
            ip = string.IsNullOrEmpty(ip) ? DEFAULT_IP : ip;
        }
    }
}