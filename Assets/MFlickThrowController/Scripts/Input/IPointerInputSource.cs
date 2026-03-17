using UnityEngine;

namespace FlickThrowSystem.Input
{
    public interface IPointerInputSource
    {
        bool IsPressedThisFrame();
        bool IsHeld();
        bool IsReleasedThisFrame();

        Vector2 GetPosition();
    }
}