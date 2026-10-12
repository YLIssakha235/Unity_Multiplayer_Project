using Unity.Netcode;
using UnityEngine;

namespace CleanLaboratory.Gameplay
{
    public class PlayerSpawnManager : MonoBehaviour
    {
        [SerializeField]
        private Transform[] spawnPoints;

        public Transform GetSpawnPoint(int index)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;

            return spawnPoints[index % spawnPoints.Length];
        }


        private void Start()
        {
            Debug.Log("FP2-1 : PlayerSpawnManager démarré");

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            StartCoroutine(PlacePlayersAfterLoad());
        }

        private System.Collections.IEnumerator PlacePlayersAfterLoad()
        {
            // Wait until the scene is loaded
            yield return null;

            OnSceneLoaded(
                "Laboratory",
                UnityEngine.SceneManagement.LoadSceneMode.Single,
                new System.Collections.Generic.List<ulong>(NetworkManager.Singleton.ConnectedClients.Keys),
                new System.Collections.Generic.List<ulong>()
            );
            
            
        }

        private void OnSceneLoaded(
            string sceneName,
            UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
            System.Collections.Generic.List<ulong> clientsCompleted,
            System.Collections.Generic.List<ulong> clientsTimedOut)

        {
            if(sceneName != "Laboratory")
                return;

            Debug.Log("FP2-1 : Laboratory chargé, placement des joueurs");

            int index = 0;

            foreach (ulong clientId in clientsCompleted)
            {
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                    continue;

                if (client.PlayerObject == null)
                    continue;

                Transform point = GetSpawnPoint(index++);

                if (point == null)
                    continue;   

                var movement = client.PlayerObject.GetComponent<PlayerMovementController>();

                if (movement != null)
                {
                    movement.TeleportToSpawnPointClientRpc(point.position, point.rotation);
                }
            }
        }

        // FP2-1 nettoye l'abonnement lors de la destruction du manager
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
            }
        }
    }
}