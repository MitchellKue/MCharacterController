using UnityEngine;
using UnityEngine.InputSystem;

namespace FlickThrowSystem.Input
{
    public class MousePointerInputSource : IPointerInputSource
    {
        private Mouse mouse;

        public MousePointerInputSource()
        {
            mouse = Mouse.current;
        }

        public bool IsPressedThisFrame()
        {
            return mouse.leftButton.wasPressedThisFrame;
        }

        public bool IsHeld()
        {
            return mouse.leftButton.isPressed;
        }

        public bool IsReleasedThisFrame()
        {
            return mouse.leftButton.wasReleasedThisFrame;
        }

        public Vector2 GetPosition()
        {
            return mouse.position.ReadValue();
        }
    }
}