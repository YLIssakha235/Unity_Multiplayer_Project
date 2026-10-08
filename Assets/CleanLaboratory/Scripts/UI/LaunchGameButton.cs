using Unity.Netcode;
using UnityEngine;
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

        if (IsServer)
        {
            NetworkManager.SceneManager.LoadScene(
                GameSceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );
        }
    }
}