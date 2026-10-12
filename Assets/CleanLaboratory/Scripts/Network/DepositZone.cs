using Unity.Netcode;
using UnityEngine;
using CleanLaboratory.Gameplay;

public class DepositZone : NetworkBehaviour
{
    [SerializeField] private int pointsPerObject = 10;
    [SerializeField] private string targetTag = "Pickable"; // Tag de tes objets déposables

    private void OnTriggerEnter(Collider other)
    {
        // Règle 1 : Seul le serveur gère la logique de validation et de score
        if (!IsServer) return;

        // Vérifier si l'objet entrant est un objet ramassable valide
        if (!other.CompareTag(targetTag)) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return;

        // Identifier qui a déposé l'objet :
        // Le propriétaire de l'objet est généralement le joueur qui le tient/l'a lâché (netObj.OwnerClientId)
        ulong depositorClientId = netObj.OwnerClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(depositorClientId, out var client))
        {
            var playerScore = client.PlayerObject.GetComponent<PlayerScore>();
            if (playerScore != null)
            {
                // 1. Incrémenter le score
                playerScore.AddScore(pointsPerObject);

                // 2. Verrouiller définitivement l'objet via un ClientRpc
                LockObjectClientRpc(netObj.NetworkObjectId);
            }
        }
    }

    [ClientRpc]
    private void LockObjectClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            // A. Retirer le tag pour ne plus redéclencher le trigger
            netObj.gameObject.tag = "Untagged";

            // B. Désactiver le script d'interaction / ramassage
            // Remplace 'PickableItem' par le nom exact de ton script de ramassage (ex: Grabbable, Pickable, etc.)
            var pickableScript = netObj.GetComponent<MonoBehaviour>(); // Adapte au type exact de ton script
            /*
            var grabComponent = netObj.GetComponent<YourPickableScript>();
            if (grabComponent != null) grabComponent.enabled = false;
            */

            // C. Désactiver les colliders d'interaction ou figer le Rigidbody
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
            }

            // Optionnel : Désactiver le collider de l'objet pour qu'aucune main/rayon ne puisse le cibler
            Collider col = netObj.GetComponent<Collider>();
            if (col != null)
            {
                // Si tu veux qu'il reste visible mais plus cliquable/attrapable,
                // change son Layer vers "Default" ou désactive le composant interactable.
            }
        }
    }
}