using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CleanLaboratory.Gameplay
{
    public class ActionInput : MonoBehaviour
    {
        [Header("Player Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Commands")]

        public UnityEvent Fire1;

        // FP1-8 : événement appelé lorsqu'on appuie sur la touche Drop (F)
        public UnityEvent Drop;

        public void OnFire1(InputValue input)
        {
            Fire1.Invoke();
        }

        // FP1-8 : reçoit l'action "Drop" de l'Input System
        // et déclenche l'événement configuré dans l'Inspector
        public void OnDrop(InputValue input)
        {
            Drop.Invoke();
        }

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            LookInput(value.Get<Vector2>());
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }
    }
}