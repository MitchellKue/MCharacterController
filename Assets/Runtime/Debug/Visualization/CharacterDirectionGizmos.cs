// File: Runtime/Debug/Visualization/CharacterDirectionGizmos.cs
// Namespace: Kojiko.MCharacterController.Debug
//
// Summary:
// Draws two always-visible gizmo lines in the Scene view:
// 1. A movement line from the character body forward (body forward direction).
// 2. A camera line showing the camera's yaw + pitch direction.
// Each line has a wire sphere at its endpoint.
// Uses only Unity built-in Gizmos; no custom shaders or GL calls.
//
// Usage:
// - Attach to the character root GameObject (or any GameObject you prefer).
// - Set "Movement Transform" to the character body transform (or leave null to use this GameObject).
// - Set "Camera Transform" to the camera (or camera rig pivot) whose forward direction you want to visualize.
// - Ensure the Scene view Gizmos toggle is enabled to see lines and spheres.

using UnityEngine;

namespace Kojiko.MCharacterController.Debug
{
    /// <summary>
    /// Draws simple debug gizmos for character movement and camera direction:
    /// - Movement line: body forward direction.
    /// - Camera line: camera forward direction (pitch + yaw).
    /// Each line is capped with a wire sphere.
    /// </summary>
    [ExecuteAlways] // Also draws in edit mode
    public class CharacterDirectionGizmos : MonoBehaviour
    {
        [Header("References")]

        [Tooltip("Transform used for movement direction (usually the character body root).\n" +
                 "If null, this component's transform is used.")]
        [SerializeField]
        private Transform _movementTransform;

        [Tooltip("Transform of the camera or camera rig pivot whose forward direction you want to visualize.")]
        [SerializeField]
        private Transform _cameraTransform;

        [Header("Appearance")]

        [Tooltip("Length of the movement direction line.")]
        [SerializeField]
        private float _movementLineLength = 2f;

        [Tooltip("Length of the camera direction line.")]
        [SerializeField]
        private float _cameraLineLength = 2f;

        [Tooltip("Radius of the wire sphere at the end of each line.")]
        [SerializeField]
        private float _endpointSphereRadius = 0.1f;

        [Tooltip("Color of the movement (body forward) line.")]
        [SerializeField]
        private Color _movementColor = Color.green;

        [Tooltip("Color of the camera (pitch + yaw) line.")]
        [SerializeField]
        private Color _cameraColor = Color.cyan;

        private void OnDrawGizmos()
        {
            DrawGizmosInternal();
        }

        // If you ever want them only when selected, you can move DrawGizmosInternal into OnDrawGizmosSelected instead.
        private void OnDrawGizmosSelected()
        {
            // Intentionally left empty: we want gizmos always visible via OnDrawGizmos().
        }

        /// <summary>
        /// Internal method to draw body forward and camera forward lines with endpoint spheres.
        /// </summary>
        private void DrawGizmosInternal()
        {
            // Resolve body transform (movement reference).
            Transform body = _movementTransform != null ? _movementTransform : transform;

            // 1) Movement line: body forward
            if (body != null)
            {
                Vector3 origin = body.position;
                Vector3 dir = body.forward.normalized;
                Vector3 end = origin + dir * _movementLineLength;

                Gizmos.color = _movementColor;
                Gizmos.DrawLine(origin, end);
                Gizmos.DrawWireSphere(end, _endpointSphereRadius);
            }

            // 2) Camera line: camera forward (includes pitch + yaw)
            if (_cameraTransform != null)
            {
                // Option A: camera line from camera position
                Vector3 origin = _cameraTransform.position;

                // Option B (uncomment if you prefer camera line starting at the body instead):
                // if (body != null) origin = body.position;

                Vector3 dir = _cameraTransform.forward.normalized;
                Vector3 end = origin + dir * _cameraLineLength;

                Gizmos.color = _cameraColor;
                Gizmos.DrawLine(origin, end);
                Gizmos.DrawWireSphere(end, _endpointSphereRadius);
            }
        }
    }
}