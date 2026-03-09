// File: Runtime/Debug/Visualization/MovementDirectionGizmos.cs
// Namespace: Kojiko.MCharacterController.Debug
//
// Summary:
// Draws a gizmo line representing the character's current movement velocity,
// using data exposed by CharacterMotor. The line's direction is the current
// horizontal movement direction, and its length is proportional to the
// current horizontal speed. A wire sphere is drawn at the end.
// Uses only Unity built-in Gizmos; no custom shaders or GL calls.
// The CharacterMotor does NOT know about this component.
//
// Usage:
// - Attach this to the character root GameObject (or body).
// - Assign "Motor" to the CharacterMotor on the same character.
// - Optionally assign "Origin Transform" if the arrow should start somewhere
//   other than the motor's transform (e.g., a visual mesh root).
// - Ensure the Scene view Gizmos toggle is enabled.

using UnityEngine;
using Kojiko.MCharacterController.Core;

namespace Kojiko.MCharacterController.Debug
{
    /// <summary>
    /// Visualizes CharacterMotor.HorizontalVelocity as a gizmo arrow:
    /// - Line origin: Origin transform (or motor transform).
    /// - Line direction: motor.HorizontalVelocity.normalized.
    /// - Line length: proportional to motor.CurrentSpeed.
    /// </summary>
    [ExecuteAlways]
    public class MovementDirectionGizmos : MonoBehaviour
    {
        [Header("References")]

        [Tooltip("CharacterMotor providing velocity data for visualization.")]
        [SerializeField]
        private CharacterMotor _motor;

        [Tooltip("Optional origin transform for the velocity arrow.\n" +
                 "If null, uses the motor's transform.")]
        [SerializeField]
        private Transform _originTransform;

        [Header("Line Scaling")]

        [Tooltip("Optional maximum speed used to normalize line length.\n" +
                 "If > 0, line length will be (speed / maxSpeed) * MaxLineLength.\n" +
                 "If <= 0, line length will be (speed * LineLengthScale).")]
        [SerializeField]
        private float _maxSpeed = 0f;

        [Tooltip("If MaxSpeed <= 0, lineLength = speed * LineLengthScale.")]
        [SerializeField]
        private float _lineLengthScale = 0.25f;

        [Tooltip("If MaxSpeed > 0, this is the line length at MaxSpeed.")]
        [SerializeField]
        private float _maxLineLength = 2f;

        [Header("Appearance")]

        [Tooltip("Radius of the wire sphere at the end of the line.")]
        [SerializeField]
        private float _endpointSphereRadius = 0.1f;

        [Tooltip("Color of the movement velocity line.")]
        [SerializeField]
        private Color _lineColor = Color.green;

        private void OnDrawGizmos()
        {
            DrawVelocityGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            // Intentionally left empty; gizmos are always drawn from OnDrawGizmos.
        }

        /// <summary>
        /// Draws the movement velocity line and endpoint sphere based on
        /// CharacterMotor.HorizontalVelocity and CharacterMotor.CurrentSpeed.
        /// </summary>
        private void DrawVelocityGizmos()
        {
            if (_motor == null)
                return;

            Transform originTransform = _originTransform != null
                ? _originTransform
                : _motor.transform;

            if (originTransform == null)
                return;

            Vector3 horizontalVelocity = _motor.HorizontalVelocity;
            float speed = _motor.CurrentSpeed;

            // If speed is essentially zero, skip drawing.
            if (speed <= 0.0001f || horizontalVelocity.sqrMagnitude <= 0.000001f)
                return;

            // Compute line length from speed.
            float lineLength;
            if (_maxSpeed > 0f)
            {
                float t = Mathf.Clamp01(speed / _maxSpeed);
                lineLength = t * _maxLineLength;
            }
            else
            {
                lineLength = speed * _lineLengthScale;
            }

            Vector3 origin = originTransform.position;
            Vector3 dir = horizontalVelocity.normalized;
            Vector3 end = origin + dir * lineLength;

            Gizmos.color = _lineColor;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, _endpointSphereRadius);
        }
    }
}