// File: Runtime/Debug/Visualization/MovementGizmo.cs
// Namespace: Kojiko.MCharacterController.Debug
//
// Summary:
// Draws a movement/rig arrow in the world using GL lines.
// - Direction: rigTransform.forward (character's yaw direction).
// - Length: proportional to |CharacterMotor.CurrentAcceleration|,
//           reaching MaxArrowLength at MaxAcceleration.
// - Color: configurable (default green).
//
// This component is meant for runtime debugging and can be included in builds.
// It does not rely on UnityEditor-only APIs.

using UnityEngine;
using Kojiko.MCharacterController.Core;

namespace Kojiko.MCharacterController.Debug
{
    /// <summary>
    /// Visualizes the character's movement acceleration as a 3D arrow.
    /// The arrow always points in the rig's forward direction, and its length
    /// scales with the magnitude of the current acceleration reported by CharacterMotor.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public class MovementGizmo : MonoBehaviour
    {
        [SerializeField] private Material _debugLineMaterial;

        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------

        [Header("References")]

        [Tooltip("Transform that defines the rig's forward direction (e.g., character root). "
               + "If left null, this component's transform is used.")]
        [SerializeField] private Transform _rigTransform;

        [Tooltip("World-space origin from which the arrow is drawn. "
               + "If left null, falls back to the rigTransform's position.")]
        [SerializeField] private Transform _arrowOrigin;

        // --------------------------------------------------------------------
        // Arrow configuration
        // --------------------------------------------------------------------

        [Header("Arrow Settings")]

        [Tooltip("Maximum length of the arrow when acceleration reaches MaxAcceleration.")]
        [SerializeField] private float _maxArrowLength = 1.5f;

        [Tooltip("Acceleration (in m/s^2) at which the arrow reaches MaxArrowLength.")]
        [SerializeField] private float _maxAcceleration = 10f;

        [Tooltip("Absolute acceleration value below which the arrow is not drawn, "
               + "to avoid jitter from very small changes.")]
        [SerializeField] private float _accelerationDeadzone = 0.05f;

        [Tooltip("Color of the movement/acceleration arrow. "
               + "Suggested: green to distinguish from camera arrow.")]
        [SerializeField] private Color _movementArrowColor = Color.green;

        [Tooltip("Relative size of the arrow head compared to the arrow length.")]
        [SerializeField] private float _headSize = 0.3f;

        // --------------------------------------------------------------------
        // General toggles
        // --------------------------------------------------------------------

        [Header("General")]

        [Tooltip("Enable or disable drawing of this movement gizmo at runtime.")]
        [SerializeField] private bool _gizmoEnabled = true;

        // --------------------------------------------------------------------
        // Internal state
        // --------------------------------------------------------------------

        /// <summary>
        /// Cached reference to the CharacterMotor on this GameObject.
        /// </summary>
        private CharacterMotor _motor;

        private void Awake()
        {
            // Cache the motor reference, required for acceleration data.
            _motor = GetComponent<CharacterMotor>();

            // If no rigTransform is specified, default to this transform.
            if (_rigTransform == null)
            {
                _rigTransform = transform;
            }
        }

        /// <summary>
        /// Unity callback invoked after all regular scene rendering is complete.
        /// We use this to draw GL lines so they appear over the scene.
        /// </summary>
        private void OnRenderObject()
        {
            // Early-out if this gizmo is disabled at runtime.
            if (!_gizmoEnabled)
                return;

            // Ensure we have all the data we need.
            if (_motor == null || _rigTransform == null)
                return;

            // Retrieve the scalar acceleration from the CharacterMotor.
            // We use the absolute value so braking and speeding up both produce a positive length,
            // but you could change this behavior if you want to distinguish them visually later.
            float accelMagnitude = Mathf.Abs(_motor.CurrentAcceleration);

            // Skip drawing for very small accelerations (to avoid micro arrows / noise).
            if (accelMagnitude < _accelerationDeadzone)
                return;

            // Validate configuration to avoid divisions by zero or negative lengths.
            if (_maxAcceleration <= 0f || _maxArrowLength <= 0f)
                return;

            // Normalize the acceleration into [0, 1] based on the configured maximum.
            float normalized = Mathf.Clamp01(accelMagnitude / _maxAcceleration);

            // Compute the final arrow length in world units.
            float arrowLength = normalized * _maxArrowLength;

            // Determine the origin of the arrow.
            Vector3 origin =
                _arrowOrigin != null
                    ? _arrowOrigin.position
                    : _rigTransform.position;

            // Determine the rig's forward direction (yaw direction).
            Vector3 forward = _rigTransform.forward.normalized;

            // If forward is degenerate, do not attempt to draw.
            if (forward.sqrMagnitude < 1e-4f)
                return;

            // Compute the arrow end position.
            Vector3 end = origin + forward * arrowLength;

            // Obtain the shared debug line material.
            Material lineMat = _debugLineMaterial;
            if (lineMat == null)
                return;

            // Instruct Unity to use this material for subsequent GL calls.
            lineMat.SetPass(0);

            // We draw in world space, so we use the identity matrix.
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            // Begin a GL.LINES batch, draw the arrow, then close the batch.
            GL.Begin(GL.LINES);
            DebugArrowDrawer.DrawArrow(origin, end, _movementArrowColor, _headSize);
            GL.End();

            GL.PopMatrix();
        }

        #region Public API (optional accessors)

        /// <summary>
        /// Public setter for enabling or disabling this gizmo at runtime.
        /// Useful for debug menus.
        /// </summary>
        /// <param name="enabled">True to draw the arrow, false to hide it.</param>
        public void SetGizmoEnabled(bool enabled)
        {
            _gizmoEnabled = enabled;
        }

        /// <summary>
        /// Allows external systems to update the arrow origin transform at runtime.
        /// For example, moving the anchor to a different part of the character.
        /// </summary>
        public void SetArrowOrigin(Transform origin)
        {
            _arrowOrigin = origin;
        }

        /// <summary>
        /// Allows external systems to update the rig transform (forward reference) at runtime.
        /// </summary>
        public void SetRigTransform(Transform rig)
        {
            _rigTransform = rig;
        }

        #endregion
    }
}