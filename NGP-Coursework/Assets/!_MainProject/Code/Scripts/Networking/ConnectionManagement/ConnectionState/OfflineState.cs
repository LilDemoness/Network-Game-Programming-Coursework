using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServices.Sessions;
using VContainer;
using Utils;
using SceneLoading;


namespace Netcode.ConnectionManagement
{
    /// <summary>
    ///     A Connection State corresponding to when the NetworkManager is shut down.<br/>
    ///     From this state we can transition ot the <see cref="ClientConnectingState"/> (If starting as a client), or the <see cref="StartingHostState"/> (If starting as a host)
    /// </summary>
    public class OfflineState : ConnectionState
    {
        [Inject]
        private MultiplayerServicesFacade _multiplayerServicesFacade;
        [Inject]
        private ProfileManager _profileManager;
        [Inject]
        private LocalSession _localSesson;


        public override void Enter()
        {
            _multiplayerServicesFacade.EndTracking();
            ConnectionManager.NetworkManager.Shutdown();

            SceneLoader.Instance.LoadNonGameplayScene(SceneLoader.NonGameplayScene.MainMenu, false);
        }

        public override void Exit() { }


        public override void StartClientIP(string playerName, string ipAddress, int port)
        {
            var connectionMethod = new ConnectionMethodIP(ipAddress, (ushort)port, ConnectionManager, _profileManager, playerName);
            ConnectionManager.ClientReconnecting.Configure(connectionMethod);
            ConnectionManager.ChangeState(ConnectionManager.ClientConnecting.Configure(connectionMethod));
        }
        public override void StartClientSession(string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(_multiplayerServicesFacade, ConnectionManager, _profileManager, playerName);
            ConnectionManager.ClientReconnecting.Configure(connectionMethod);
            ConnectionManager.ChangeState(ConnectionManager.ClientConnecting.Configure(connectionMethod));
        }

        public override void StartHostIP(string playerName, string ipAddress, int port)
        {
            var connectionMethod = new ConnectionMethodIP(ipAddress, (ushort)port, ConnectionManager, _profileManager, playerName);
            ConnectionManager.ChangeState(ConnectionManager.StartingHost.Configure(connectionMethod));
        }
        public override void StartHostSession(string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(_multiplayerServicesFacade, ConnectionManager, _profileManager, playerName);
            ConnectionManager.ChangeState(ConnectionManager.StartingHost.Configure(connectionMethod));
        }
    }
}