using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAction : NetworkBehaviour
{
    void Update()
    {
        if (!IsOwner)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}