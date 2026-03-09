// File: Runtime/Debug/Visualization/CameraGizmo.cs
// Namespace: Kojiko.MCharacterController.Debug
//
// Summary:
// Draws a camera forward arrow in the world using GL lines.
// - Direction: cameraTransform.forward (includes both yaw and pitch).
// - Length: fixed (CameraArrowLength).
// - Color: configurable (default blue).
//
// This component is meant for runtime debugging and can be included in builds.
// It does not rely on UnityEditor-only APIs.

using UnityEngine;

namespace Kojiko.MCharacterController.Debug
{
    /// <summary>
    /// Visualizes the camera's forward direction as a 3D arrow.
    /// The arrow indicates the full look direction (yaw + pitch) of the camera.
    /// </summary>
    public class CameraGizmo : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------
        [SerializeField] private Material _debugLineMaterial;

        [Header("References")]

        [Tooltip("Camera whose forward direction is visualized by this gizmo.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("World-space origin from which the camera arrow is drawn. "
               + "If left null, falls back to this component's transform position.")]
        [SerializeField] private Transform _arrowOrigin;

        // --------------------------------------------------------------------
        // Arrow configuration
        // --------------------------------------------------------------------

        [Header("Arrow Settings")]

        [Tooltip("Fixed length of the camera direction arrow in world units.")]
        [SerializeField] private float _cameraArrowLength = 1.5f;

        [Tooltip("Color of the camera direction arrow. "
               + "Suggested: blue to distinguish from movement arrow.")]
        [SerializeField] private Color _cameraArrowColor = Color.blue;

        [Tooltip("Relative size of the arrow head compared to the arrow length.")]
        [SerializeField] private float _headSize = 0.3f;

        // --------------------------------------------------------------------
        // General toggles
        // --------------------------------------------------------------------

        [Header("General")]

        [Tooltip("Enable or disable drawing of this camera gizmo at runtime.")]
        [SerializeField] private bool _gizmoEnabled = true;

        private void OnRenderObject()
        {
            // Early-out if disabled at runtime.
            if (!_gizmoEnabled)
                return;

            // Camera transform must be assigned to know which direction to draw.
            if (_cameraTransform == null)
                return;

            // Non-positive length results in no visible arrow.
            if (_cameraArrowLength <= 0f)
                return;

            // Determine the origin position of the arrow.
            Vector3 origin =
                _arrowOrigin != null
                    ? _arrowOrigin.position
                    : transform.position;

            // Compute the camera's forward direction (includes pitch and yaw).
            Vector3 forward = _cameraTransform.forward.normalized;

            // If forward is degenerate, skip drawing.
            if (forward.sqrMagnitude < 1e-4f)
                return;

            // Compute the end position based on the fixed arrow length.
            Vector3 end = origin + forward * _cameraArrowLength;

            // Acquire the shared debug line material.
            Material lineMat = _debugLineMaterial;
            if (lineMat == null)
                return;

            // Bind this material for GL rendering.
            lineMat.SetPass(0);

            // Draw in world space.
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            // Issue GL line commands to draw the arrow.
            GL.Begin(GL.LINES);
            DebugArrowDrawer.DrawArrow(origin, end, _cameraArrowColor, _headSize);
            GL.End();

            GL.PopMatrix();
        }

        #region Public API (optional accessors)

        /// <summary>
        /// Public setter for enabling or disabling this gizmo at runtime.
        /// </summary>
        public void SetGizmoEnabled(bool enabled)
        {
            _gizmoEnabled = enabled;
        }

        /// <summary>
        /// Allows changing which camera is used for the forward direction.
        /// </summary>
        /// <param name="cameraTransform">Transform of the camera to visualize.</param>
        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>
        /// Allows changing the arrow origin at runtime (e.g., moving it to a new anchor).
        /// </summary>
        /// <param name="origin">New world-space origin transform.</param>
        public void SetArrowOrigin(Transform origin)
        {
            _arrowOrigin = origin;
        }

        #endregion
    }
}