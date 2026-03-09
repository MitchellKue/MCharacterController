// File: Runtime/Debug/Visualization/CameraDirectionGizmos.cs
// Namespace: Kojiko.MCharacterController.Debug
//
// Summary:
// Draws a gizmo line representing the camera's forward direction (yaw + pitch)
// and a wire sphere at the end of that line.
// Uses only Unity built-in Gizmos; no custom shaders or GL calls.
//
// Usage:
// - Attach this to any GameObject (often the same as the character root).
// - Set "Camera Transform" to the actual camera or camera rig pivot.
// - Optionally choose where the line should start (camera position or character).
// - Ensure the Scene view Gizmos toggle is enabled to see the line and sphere.

using UnityEngine;

namespace Kojiko.MCharacterController.Debug
{
    /// <summary>
    /// Draws a camera direction gizmo:
    /// - A line from a chosen origin in the direction of the camera's forward vector.
    /// - A wire sphere at the line's end.
    /// </summary>
    [ExecuteAlways] // Also draws in edit mode
    public class CameraDirectionGizmos : MonoBehaviour
    {
        [Header("References")]

        [Tooltip("Transform of the camera or camera rig pivot whose forward direction to visualize.")]
        [SerializeField]
        private Transform _cameraTransform;

        [Tooltip("Optional origin transform for the gizmo line (e.g., character body).\n" +
                 "If null, the line originates from the camera position.")]
        [SerializeField]
        private Transform _originTransform;

        [Header("Appearance")]

        [Tooltip("Length of the camera direction line.")]
        [SerializeField]
        private float _lineLength = 2f;

        [Tooltip("Radius of the wire sphere at the end of the line.")]
        [SerializeField]
        private float _endpointSphereRadius = 0.1f;

        [Tooltip("Color of the camera direction line.")]
        [SerializeField]
        private Color _lineColor = Color.cyan;

        private void OnDrawGizmos()
        {
            DrawCameraGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            // Intentionally left empty; gizmos are always drawn from OnDrawGizmos.
        }

        /// <summary>
        /// Draws the camera direction line and endpoint sphere.
        /// </summary>
        private void DrawCameraGizmos()
        {
            if (_cameraTransform == null)
                return;

            // Origin:
            // - If _originTransform is set, use its position (e.g., character body).
            // - Otherwise, use the camera's position.
            Vector3 origin = _originTransform != null
                ? _originTransform.position
                : _cameraTransform.position;

            Vector3 dir = _cameraTransform.forward.normalized;
            Vector3 end = origin + dir * _lineLength;

            Gizmos.color = _lineColor;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, _endpointSphereRadius);
        }
    }
}