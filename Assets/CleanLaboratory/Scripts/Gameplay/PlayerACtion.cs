using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAction : NetworkBehaviour
{
    // Rentre la variable accessible par les autres scripts
    public static bool IsCursorMenuOpen { get; private set; } = false;

    void Update()
    {
        if (!IsOwner)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // On inverse l'état
            IsCursorMenuOpen = !IsCursorMenuOpen;

            if (IsCursorMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // SÉCURITÉ : Si le menu est censé être ouvert, on force le curseur 
        // à rester libre à chaque frame pour contrer les autres scripts.
        if (IsCursorMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
