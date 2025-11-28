using Gameplay.Configuration;
using Infrastructure;
using Netcode.ConnectionManagement;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Networking.Transport;
using UnityEngine;
using VContainer;

namespace Gameplay.UI.MainMenu
{
    public class IPUIMediator : MonoBehaviour
    {
        public const string DEFAULT_IP = "127.0.0.1";
        public const int DEFAULT_PORT = 9998;

        [SerializeField] private CanvasGroup _canvasGroup;

        [Space(5)]
        [SerializeField] private TextMeshProUGUI _playerNameLabel;
        [SerializeField] private IPJoiningUI _ipJoiningUI;
        [SerializeField] private IPHostingUI _ipHostingUI;

        //[Space(5)]
        //[SerializeField] private UITinter _joinTabButtonHighlightTinter;
        //[SerializeField] private UITinter _joinTabButtonBlockerTinter;
        //[SerializeField] private UITinter _hostTabButtonHighlightTinter;
        //[SerializeField] private UITinter _hostTabButtonBlockerTinter;

        [Space(5)]
        [SerializeField] private GameObject _signInSpinner;

        [Space(5)]
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
            OpenCreateIPUI();
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
            EnableSignInSpinner();
            _connectionManager.StartHostIP(_playerNameLabel.text, ip, portInteger);
        }
        public void JoinWithIP(string ip, string port)
        {
            ValidateIpAndPort(ref ip, port, out int portInteger);

            // Perform the Join attempt.
            EnableSignInSpinner();
            _connectionManager.StartClientIP(_playerNameLabel.text, ip, portInteger);
            _ipConnectionWindow.ShowConnectionWindow();
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


        public void JoiningWindowCancelled()
        {
            DisableSignInSpinner();
            RequestShutdown();
        }

        public void EnableSignInSpinner() => _signInSpinner.SetActive(true);
        public void DisableSignInSpinner() => _signInSpinner.SetActive(false);
        
        private void RequestShutdown()
        {
            if (_connectionManager != null && _connectionManager.NetworkManager != null)
            {
                _connectionManager.RequestShutdown();
            }
        }

        /// <summary>
        ///     Randomise the player's name.
        /// </summary>
        public void RegenerateName()
        {
            _playerNameLabel.text = _nameGenerationData.GenerateRandomName();
        }

        public void OpenJoinIPUI()
        {
            _ipJoiningUI.Show();
            _ipHostingUI.Hide();

            //_joinTabButtonHighlightTinter.SetToColour(1);
            //_joinTabButtonBlockerTinter.SetToColour(1);
            //_hostTabButtonHighlightTinter.SetToColour(0);
            //_hostTabButtonBlockerTinter.SetToColour(0);
        }
        public void OpenCreateIPUI()
        {
            _ipJoiningUI.Hide();
            _ipHostingUI.Show();

            //_joinTabButtonHighlightTinter.SetToColour(0);
            //_joinTabButtonBlockerTinter.SetToColour(0);
            //_hostTabButtonHighlightTinter.SetToColour(1);
            //_hostTabButtonBlockerTinter.SetToColour(1);
        }


        public void Show()
        {
            // Show the UI.
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            // Ensure that the Sign-in Spinner starts hidden.
            DisableSignInSpinner();
        }
        public void Hide()
        {
            // Hide the UI.
            _canvasGroup.alpha = 0.0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }


        // To be called from the back/close UI button.
        public void CancelConnectingWindow()
        {
            RequestShutdown();
            _ipConnectionWindow.CancelConnectionWindow();
        }


        /// <summary>
        ///     Sanitize user IP address InputField box allowing only numbers and periods.<br/>
        ///     This also prevents undesirable invisible characters from being copy-pased accidentally.
        /// </summary>
        /// <param name="dirtyString"> The string to sanitize.</param>
        /// <returns> Sanitized text string.</returns>
        public static string SanitizeIP(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^0-9.]", "");
        }

        /// <summary>
        ///     Sanitize user port InputField box by allowing only numbers.<br/>
        ///     This also prevents undesirable invisible characters from being copy-pased accidentally.
        /// </summary>
        /// <param name="dirtyString"> The string to sanitize.</param>
        /// <returns> Sanitized text string.</returns>
        public static string SanitizePort(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^0-9]", "");
        }

        public static bool AreIPAddressAndPortValid(string ipAddress, string port)
        {
            bool portValid = ushort.TryParse(port, out ushort portNumber);
            return portValid && NetworkEndpoint.TryParse(ipAddress, portNumber, out var netowkrEndPoint);
        }
    }
}