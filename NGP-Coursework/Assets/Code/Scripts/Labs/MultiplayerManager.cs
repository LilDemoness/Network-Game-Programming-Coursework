using UnityEngine;
using Unity.Netcode;

public class MultiplayerManager : MonoBehaviour
{
    private void OnGUI()
    {
        if (NetworkManager.Singleton == null || ServerManager.Instance == null)
            return;

        GUILayout.BeginArea(new Rect(10.0f, 10.0f, 300.0f, 300.0f));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // We haven't yet started the game.
            ShowStartButtons();
        }
        else
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }


    /// <summary>
    ///     Shows buttons to start a game as a Host, Client, or Server.
    /// </summary>
    private static void ShowStartButtons()
    {
        if (GUILayout.Button("Host"))
            ServerManager.Instance.StartHost();
        if (GUILayout.Button("Client"))
            NetworkManager.Singleton.StartClient();
        if (GUILayout.Button("Server"))
            ServerManager.Instance.StartServer();
    }
    /// <summary>
    ///     Shows the Network Transport Type and whether this game is a Host, Client, or Server.
    /// </summary>
    private static void StatusLabels()
    {
        string mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}