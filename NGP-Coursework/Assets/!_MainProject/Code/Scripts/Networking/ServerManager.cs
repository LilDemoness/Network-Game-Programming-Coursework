using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Customisation.Data;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }

    private bool _hasGameStarted;
    public Dictionary<ulong, ClientData> ClientData { get; private set; }


    #if UNITY_EDITOR
    public UnityEditor.SceneAsset GameplaySceneAsset;
    private void OnValidate()
    {
        if (GameplaySceneAsset != null)
        {
            m_gameplaySceneName = GameplaySceneAsset.name;
        }
    }
#endif
    [SerializeField] private string m_gameplaySceneName;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // An instance of ServerManager already exists, and is not us.
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += OnNetworkReady;

        ClientData = new Dictionary<ulong, ClientData>();

        NetworkManager.Singleton.StartHost();
    }
    public void StartServer()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += OnNetworkReady;

        ClientData = new Dictionary<ulong, ClientData>();

        NetworkManager.Singleton.StartServer();
    }
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (_hasGameStarted)
        {
            // We have already started the game.
            response.Approved = false;
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        ClientData[request.ClientNetworkId] = new ClientData(request.ClientNetworkId);

        Debug.Log($"Added Client {request.ClientNetworkId}");
    }
    private void OnNetworkReady()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        //NetworkManager.Singleton.SceneManager.LoadScene(
    }

    private void OnClientDisconnect(ulong clientID)
    {
        if (ClientData.ContainsKey(clientID))
        {
            if (ClientData.Remove(clientID))
            {
                Debug.Log($"Removed Client {clientID}");
            }
        }
    }


    public void SetBuild(ulong clientID, BuildData newBuildData)
    {
        if (ClientData.TryGetValue(clientID, out ClientData data))
        {
            data.BuildData = newBuildData;
        }
    }
    public void StartGame()
    {
        this._hasGameStarted = true;

        Debug.Log("Starting Game");
        var status = NetworkManager.Singleton.SceneManager.LoadScene(m_gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to load {m_gameplaySceneName} " +
                    $"with a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }
}
