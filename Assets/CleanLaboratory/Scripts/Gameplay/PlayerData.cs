using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanLaboratory.Gameplay
{
    public class PlayerData : NetworkBehaviour
    {
        public PlayerInput PlayerInput;
        public GameObject Camera;
        public PlayerMovementController PlayerMovementController;
        public GameObject PlayerGeometry;
        public GameObject PlayerCanvas;
        public PlayerActions PlayerActions;
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }
            PlayerInput.enabled = true;
            Camera.SetActive(true);
            PlayerMovementController.enabled = true;
            PlayerGeometry.SetActive(false);
            PlayerCanvas.SetActive(true);
            PlayerActions.enabled = true;

        }
    }

}
