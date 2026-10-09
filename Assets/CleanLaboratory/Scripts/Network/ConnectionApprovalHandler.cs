using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; // FP2-7

/// <summary>
/// Connection Approval Handler Component
/// </summary>
/// <remarks>
/// This should be placed on the same GameObject as the NetworkManager.
/// It automatically declines the client connection for example purposes.
/// </remarks>
public class ConnectionApprovalHandler : MonoBehaviour
{
    private NetworkManager m_NetworkManager;

    public int MaxNumberOfPlayers = 6;
    private int _numberOfPlayers = 0;

    // FP2-7: block connections once the game has started
    public string GameSceneName = "Laboratory";
    private bool _gameStarted = false;

    private void Start()
    {
        m_NetworkManager = GetComponent<NetworkManager>();

        if (m_NetworkManager != null)
        {
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            m_NetworkManager.ConnectionApprovalCallback += CheckApprovalCallback;
            m_NetworkManager.OnServerStarted += OnServerStarted;   // FP2-7
            m_NetworkManager.OnServerStopped += OnServerStopped;   // FP2-7
        }

        if (MaxNumberOfPlayers == 0)
        {
            MaxNumberOfPlayers++;
        }
    }

    private void CheckApprovalCallback(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        bool isApproved = true;

        _numberOfPlayers++;

        if (_numberOfPlayers > MaxNumberOfPlayers)
        {
            isApproved = false;
            response.Reason = "Too many players in lobby!";
        }

        // FP2-7: game already started → refuse
        if (_gameStarted)
        {
            isApproved = false;
            response.Reason = "The game has already started!";
        }

        response.Approved = isApproved;
        response.CreatePlayerObject = isApproved;
        response.Position = new Vector3(0, 3, 0);
    }

    private void OnClientDisconnectCallback(ulong clientID)
    {
        if (!m_NetworkManager.IsServer &&
            m_NetworkManager.DisconnectReason != string.Empty &&
            !m_NetworkManager.IsApproved)
        {
            Debug.Log(
                $"Approval Declined Reason: {m_NetworkManager.DisconnectReason}"
            );
        }

        _numberOfPlayers--;
    }

    // ===== FP2-7: know when the game starts =====

    private void OnServerStarted()
    {
        // The network SceneManager only exists once the server is started
        m_NetworkManager.SceneManager.OnLoad += OnSceneLoadStarted;
    }

    private void OnSceneLoadStarted(ulong clientId, string sceneName, LoadSceneMode mode, AsyncOperation op)
    {
        // The host launched the game → the game scene starts loading
        if (sceneName == GameSceneName && !_gameStarted)
        {
            _gameStarted = true;
            Debug.Log("[FP2-7] Game started: new connections are now refused.");
        }
    }

    private void OnServerStopped(bool wasHost)
    {
        // Reset for the next session
        _gameStarted = false;
        if (m_NetworkManager.SceneManager != null)
            m_NetworkManager.SceneManager.OnLoad -= OnSceneLoadStarted;
    }
}