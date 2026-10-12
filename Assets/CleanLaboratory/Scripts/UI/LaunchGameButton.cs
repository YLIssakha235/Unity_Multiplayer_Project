using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LaunchGameButton : NetworkBehaviour
{
    public string GameSceneName = "Laboratory";

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"LaunchGameButton spawned - IsServer = {IsServer}");

        if (!IsServer)
        {
            GetComponentInParent<Canvas>().gameObject.SetActive(false);
            return;
        }

        GetComponent<Button>().onClick.AddListener(LaunchGame);
    }

    private void LaunchGame()
    {
        Debug.Log("LAUNCH GAME CLICKED");

        // 1. Security Check: Only the Server/Host is allowed to clean up and launch the game
        if (!IsServer) return;

        // 2. Find all objects in the scene that have the LobbyObject "sticky note"
        LobbyObject[] objectsToClean = FindObjectsOfType<LobbyObject>();

        foreach (LobbyObject obj in objectsToClean)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();

            // If it is a NetworkObject and is currently spawned, despawn it
            if (netObj != null && netObj.IsSpawned)
            {
                // Passing 'true' tells Netcode to despawn AND destroy the GameObject on all clients
                netObj.Despawn(true);
            }
        }

        // 3. Finally, load the Game scene for all connected players
        // (Make sure your scene is exactly named "Game" in your Build Settings)
        NetworkManager.Singleton.SceneManager.LoadScene("Laboratory", LoadSceneMode.Single);
    }
}
