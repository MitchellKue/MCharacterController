using UnityEngine;
using UnityEngine.InputSystem;

namespace FlickThrowSystem.Input
{
    public class GamepadStickInputSource : IPointerInputSource
    {
        private Gamepad gamepad;

        private Vector2 virtualPosition;
        private float sensitivity = 1000f;

        public GamepadStickInputSource()
        {
            gamepad = Gamepad.current;
        }

        public bool IsPressedThisFrame()
        {
            return gamepad.rightTrigger.wasPressedThisFrame;
        }

        public bool IsHeld()
        {
            return gamepad.rightTrigger.isPressed;
        }

        public bool IsReleasedThisFrame()
        {
            return gamepad.rightTrigger.wasReleasedThisFrame;
        }

        public Vector2 GetPosition()
        {
            Vector2 delta =
                gamepad.rightStick.ReadValue() *
                sensitivity *
                Time.deltaTime;

            virtualPosition += delta;

            return virtualPosition;
        }
    }
}