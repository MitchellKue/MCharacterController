using UnityEngine;
using Kojiko.MCharacterController.Core;


namespace Kojiko.MCharacterController.Debug
{
    [ExecuteAlways]
    public class MovementDirectionGizmos : MonoBehaviour
    {
        [SerializeField] private CharacterMotor _motor;

        [Header("Ellipse (Max Speed Shape)")]
        [SerializeField] private Color _ellipseColor = Color.cyan;
        [SerializeField, Min(8)] private int _segments = 64;

        [Header("Direction / Acceleration Line")]
        [SerializeField] private Color _directionColor = Color.yellow;
        [SerializeField] private Color _acceleratingColor = Color.green;
        [SerializeField] private Color _deceleratingColor = Color.red;

        [Tooltip("Color the direction line based on CurrentAcceleration.")]
        [SerializeField] private bool _colorByAcceleration = true;

        private CharacterController _controller;

        private void OnValidate()
        {
            if (_segments < 8) _segments = 8;
        }

        private void EnsureRefs()
        {
            if (_motor == null)
                _motor = GetComponent<CharacterMotor>();

            if (_motor != null && _controller == null)
                _controller = _motor.GetComponent<CharacterController>();
        }

        private void OnDrawGizmos()
        {
            DrawGizmosInternal();
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmosInternal();
        }

        private void DrawGizmosInternal()
        {
            EnsureRefs();
            if (_motor == null)
                return;

            // Center on character's feet on XZ plane
            Vector3 center = transform.position;
            if (_controller != null)
            {
                center = _controller.bounds.center;
                center.y = _controller.bounds.min.y;
            }

            DrawSpeedEllipse(center);
            DrawDirectionAndAccelerationLine(center);
        }

        private void DrawSpeedEllipse(Vector3 center)
        {
            float forwardSpeed = _motor.ForwardSpeed;
            float backwardSpeed = _motor.BackwardSpeed;
            float strafeSpeed = _motor.StrafeSpeed;

            // If all speeds are almost zero, nothing to draw.
            if (forwardSpeed < 0.01f && backwardSpeed < 0.01f && strafeSpeed < 0.01f)
                return;

            Gizmos.color = _ellipseColor;

            // Forward/backward/strafe are in the motor's local space:
            //   +local Z = forward radius
            //   -local Z = backward radius
            //   ±local X = strafe radius
            Vector3 localForward = transform.forward;
            localForward.y = 0f;
            localForward.Normalize();

            Vector3 localRight = transform.right;
            localRight.y = 0f;
            localRight.Normalize();

            // We build an ellipse where:
            //   at angle = 0   -> local +X (right)  => radius = strafeSpeed
            //   at angle = 90  -> local +Z (forward)=> radius = forwardSpeed
            //   at angle = 180 -> local -X (left)   => radius = strafeSpeed
            //   at angle = 270 -> local -Z (back)   => radius = backwardSpeed
            //
            // Between those points we smoothly interpolate.
           // float halfPi = 0.5f * Mathf.PI;
            float twoPi = 2f * Mathf.PI;
            float angleStep = twoPi / _segments;

            Vector3 firstPoint = Vector3.zero;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= _segments; i++)
            {
                float angle = i * angleStep; // 0..2π

                // Base unit circle (cos, sin).
                float xUnit = Mathf.Cos(angle); // side axis
                float zUnit = Mathf.Sin(angle); // forward/back axis

                // Determine radius along forward/back axis:
                //   positive zUnit => forward side, use forwardSpeed
                //   negative zUnit => backward side, use backwardSpeed
                float zRadius = zUnit >= 0f ? forwardSpeed : backwardSpeed;
                float xRadius = strafeSpeed;

                // Now scale the unit circle by these radii.
                float localX = xUnit * xRadius;
                float localZ = zUnit * zRadius; // sign already in zUnit

                // Convert from local XZ to world.
                Vector3 point =
                    center +
                    (localRight * localX) +
                    (localForward * localZ);

                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, point);
                }
                else
                {
                    firstPoint = point;
                }

                prevPoint = point;
            }

            Gizmos.DrawLine(prevPoint, firstPoint);
        }

        private void DrawDirectionAndAccelerationLine(Vector3 center)
        {
            if (!Application.isPlaying)
                return;

            Vector3 horizontalVel = _motor.HorizontalVelocity;
            horizontalVel.y = 0f;

            if (horizontalVel.sqrMagnitude <= 0.0001f)
                return;

            Color lineColor = _directionColor;

            if (_colorByAcceleration)
            {
                float accel = _motor.CurrentAcceleration;

                if (accel > 0.01f)
                    lineColor = _acceleratingColor;
                else if (accel < -0.01f)
                    lineColor = _deceleratingColor;
            }

            Gizmos.color = lineColor;
            Gizmos.DrawLine(center, center + horizontalVel);
        }
    }
}