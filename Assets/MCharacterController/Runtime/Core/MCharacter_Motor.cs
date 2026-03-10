// File: Runtime/Core/CharacterMotor.cs
// Namespace: Kojiko.MCharacterController.Core
//
// Summary:
// 1. Wraps Unity's CharacterController to handle movement and gravity.
// 2. Accepts a desired horizontal move direction from higher-level logic.
// 3. Applies directional speeds (forward, backward, strafe) and
//    acceleration/deceleration with reduced air acceleration.
// 4. Maintains grounded state and full velocity vector.
// 5. Exposes horizontal speed and scalar acceleration for debug systems.
// 6. Allows external systems (abilities) to inject extra horizontal/vertical velocity
//    via AddExternalHorizontalVelocity / AddExternalVerticalVelocity.

using UnityEngine;

namespace Kojiko.MCharacterController.Core
{
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class MCharacter_Motor : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [Tooltip("Maximum forward movement speed on the ground.")]
        [SerializeField] private float _forwardSpeed = 5f;

        [Tooltip("Maximum backward movement speed on the ground.")]
        [SerializeField] private float _backwardSpeed = 3f;

        [Tooltip("Maximum lateral (strafe) movement speed on the ground.")]
        [SerializeField] private float _strafeSpeed = 4f;

        private float _speedMultiplier = 1f;

        [Header("Horizontal Acceleration")]
        [Tooltip("Acceleration when speeding up or changing direction on the ground (m/s^2).")]
        [SerializeField] private float _acceleration = 20f;

        [Tooltip("Deceleration when slowing down or stopping on the ground (m/s^2).")]
        [SerializeField] private float _deceleration = 25f;

        [Tooltip("Multiplier applied to acceleration/deceleration while in the air.")]
        [SerializeField] private float _airAccelerationMultiplier = 0.5f;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -9.81f;

        [Tooltip("Small downward force to keep the character 'stuck' to the ground when grounded.")]
        [SerializeField] private float _groundedGravity = -2f;

        // --------------------------------------------------------------------
        // Public read-only state
        // --------------------------------------------------------------------

        /// <summary>
        /// True when the CharacterController reports that it is grounded.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// Full velocity vector in world space, including horizontal and vertical components.
        /// </summary>
        public Vector3 Velocity => _velocity;

        /// <summary>
        /// Horizontal velocity in world space (Y component is always 0).
        /// </summary>
        public Vector3 HorizontalVelocity => _currentHorizontalVelocity;

        /// <summary>
        /// Current horizontal speed (magnitude of velocity on the XZ plane).
        /// </summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// Global multiplier applied to computed ground speeds (e.g. from sprint).
        /// Defaults to 1. Abilities can change this per-frame.
        /// </summary>
        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Max(0f, value); // no negative speeds
        }

        /// <summary>
        /// Current scalar acceleration in m/s^2, based on the change in horizontal
        /// speed over time. Positive when speeding up, negative when slowing down.
        /// </summary>
        public float CurrentAcceleration { get; private set; }

        /// <summary>
        /// Forward speed configured on the motor.
        /// </summary>
        public float ForwardSpeed => _forwardSpeed;

        /// <summary>
        /// Backward speed configured on the motor.
        /// </summary>
        public float BackwardSpeed => _backwardSpeed;

        /// <summary>
        /// Strafe (side) speed configured on the motor.
        /// </summary>
        public float StrafeSpeed => _strafeSpeed;

        /// <summary>
        /// Convenience: maximum of forward/backward/strafe speeds.
        /// </summary>
        public float MaxGroundSpeed => Mathf.Max(_forwardSpeed, _backwardSpeed, _strafeSpeed);

        // --------------------------------------------------------------------
        // Internal references
        // --------------------------------------------------------------------

        private CharacterController _characterController;

        // --------------------------------------------------------------------
        // Internal state
        // --------------------------------------------------------------------

        /// <summary>
        /// Internal velocity backing field for the public Velocity property.
        /// </summary>
        private Vector3 _velocity;

        /// <summary>
        /// Current horizontal velocity (XZ only).
        /// </summary>
        private Vector3 _currentHorizontalVelocity;

        /// <summary>
        /// Previous frame's horizontal velocity, used to compute acceleration.
        /// </summary>
        private Vector3 _prevHorizontalVelocity;

        /// <summary>
        /// Tracks whether _prevHorizontalVelocity has been initialized.
        /// </summary>
        private bool _hasPrevVelocity;

        // --------------------------------------------------------------------
        // External velocity contributions
        // --------------------------------------------------------------------

        /// <summary>
        /// External horizontal velocity (XZ) added on top of _currentHorizontalVelocity
        /// for this frame only. Cleared at the end of Step().
        /// </summary>
        private Vector3 _externalHorizontalVelocity;

        /// <summary>
        /// External vertical velocity offset added on top of vertical velocity
        /// for this frame only. Cleared at the end of Step().
        /// </summary>
        private float _externalVerticalVelocityOffset;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            _velocity = Vector3.zero;
            _currentHorizontalVelocity = Vector3.zero;
            _prevHorizontalVelocity = Vector3.zero;
            _hasPrevVelocity = false;

            CurrentSpeed = 0f;
            CurrentAcceleration = 0f;

            _speedMultiplier = 1f;

            _externalHorizontalVelocity = Vector3.zero;
            _externalVerticalVelocityOffset = 0f;
        }

        /// <summary>
        /// Called every frame by higher-level logic to advance movement.
        /// </summary>
        /// <param name="desiredMoveWorld">
        /// Desired horizontal move vector in world space (Y should be 0).
        /// Typically derived from input + camera orientation. This can be
        /// non-normalized; only the direction is used here.
        /// </param>
        /// <param name="deltaTime">Frame delta time.</param>
        public void Step(Vector3 desiredMoveWorld, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            // 1. Grounded state
            IsGrounded = _characterController.isGrounded;

            // Ensure desired move has no vertical component.
            desiredMoveWorld.y = 0f;

            // 2. Horizontal velocity (with acceleration model)
            Vector3 currentHorizontal = _currentHorizontalVelocity;
            Vector3 targetHorizontal = ComputeTargetHorizontalVelocity(desiredMoveWorld);

            float accelMultiplier = IsGrounded ? 1f : _airAccelerationMultiplier;
            float accel = _acceleration * accelMultiplier;
            float decel = _deceleration * accelMultiplier;

            Vector3 newHorizontal = MoveHorizontalTowards(
                currentHorizontal,
                targetHorizontal,
                accel,
                decel,
                deltaTime
            );

            // Apply external horizontal contribution for this frame.
            newHorizontal += _externalHorizontalVelocity;

            _currentHorizontalVelocity = newHorizontal;

            // 3. Vertical velocity (gravity)
            float verticalVelocity = _velocity.y;

            if (IsGrounded && verticalVelocity < 0f)
            {
                // Small downward force keeps the character snapped to ground surfaces.
                verticalVelocity = _groundedGravity;
            }
            else
            {
                verticalVelocity += _gravity * deltaTime;
            }

            // Apply external vertical offset for this frame.
            verticalVelocity += _externalVerticalVelocityOffset;

            // 4. Combine horizontal + vertical into final velocity.
            _velocity = new Vector3(newHorizontal.x, verticalVelocity, newHorizontal.z);

            // 5. Update debug/telemetry values.
            UpdateSpeedAndAcceleration(deltaTime);

            // 6. Move CharacterController.
            Vector3 motion = _velocity * deltaTime;
            _characterController.Move(motion);

            // 7. Clear external contributions so abilities must re-apply each frame.
            ClearExternalVelocities();
        }

        /// <summary>
        /// Allows external systems (e.g., jump ability) to override the vertical velocity.
        /// External vertical offsets are still added after this inside Step().
        /// </summary>
        public void SetVerticalVelocity(float newVerticalVelocity)
        {
            Vector3 horizontal = _currentHorizontalVelocity;
            _velocity = new Vector3(horizontal.x, newVerticalVelocity, horizontal.z);
        }

        // --------------------------------------------------------------------
        // External velocity helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Adds an external horizontal velocity contribution in world space (XZ).
        /// Added on top of the motor's internal horizontal velocity for this frame only.
        /// Should be called every frame the effect applies.
        /// </summary>
        public void AddExternalHorizontalVelocity(Vector3 horizontalVelocity)
        {
            horizontalVelocity.y = 0f;
            _externalHorizontalVelocity += horizontalVelocity;
        }

        /// <summary>
        /// Adds an external vertical velocity offset (e.g., knock-up or push-down).
        /// Added on top of the current vertical velocity for this frame only.
        /// Should be called every frame the effect applies.
        /// </summary>
        public void AddExternalVerticalVelocity(float verticalVelocity)
        {
            _externalVerticalVelocityOffset += verticalVelocity;
        }

        /// <summary>
        /// Clears all accumulated external velocity contributions (horizontal and vertical).
        /// Called automatically at the end of Step().
        /// </summary>
        public void ClearExternalVelocities()
        {
            _externalHorizontalVelocity = Vector3.zero;
            _externalVerticalVelocityOffset = 0f;
        }

        // Optional debug accessors if you want them:
        public Vector3 ExternalHorizontalVelocity => _externalHorizontalVelocity;
        public float ExternalVerticalVelocityOffset => _externalVerticalVelocityOffset;

        // --------------------------------------------------------------------
        // Horizontal movement helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Computes the target horizontal velocity based on input direction and
        /// forward/backward/strafe speeds.
        /// </summary>
        private Vector3 ComputeTargetHorizontalVelocity(Vector3 desiredMoveWorld)
        {
            // No input -> no target horizontal motion.
            if (desiredMoveWorld.sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            // Direction on the XZ plane.
            Vector3 dir = desiredMoveWorld.normalized;

            // Project onto local forward/right to determine intent.
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            float forwardDot = Vector3.Dot(dir, forward); // > 0 forward, < 0 backward
            float rightDot = Vector3.Dot(dir, right);     // > 0 right,   < 0 left

            // Cache multiplier
            float mult = _speedMultiplier;

            // Determine directional speeds with multiplier applied.
            float forwardSpeed =
                forwardDot > 0f ? _forwardSpeed * mult :
                (forwardDot < 0f ? _backwardSpeed * mult : 0f);

            float strafeSpeed =
                Mathf.Abs(rightDot) > 0.0001f ? _strafeSpeed * mult : 0f;

            // Convert the directional intent into a target velocity vector.
            Vector3 forwardComponent = forward * (forwardDot * forwardSpeed);
            Vector3 strafeComponent = right * (rightDot * strafeSpeed);

            Vector3 target = forwardComponent + strafeComponent;
            return target;
        }

        /// <summary>
        /// Moves the current horizontal velocity toward target, using different
        /// rates for acceleration and deceleration.
        /// </summary>
        private static Vector3 MoveHorizontalTowards(
            Vector3 current,
            Vector3 target,
            float accel,
            float decel,
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return current;

            float maxAccelDelta = accel * deltaTime;
            float maxDecelDelta = decel * deltaTime;

            float currentSpeed = current.magnitude;
            float targetSpeed = target.magnitude;

            // Case 1: No target input -> decelerate to stop.
            if (targetSpeed <= 0.000001f)
            {
                if (currentSpeed <= 0f)
                    return Vector3.zero;

                float newSpeed = Mathf.Max(currentSpeed - maxDecelDelta, 0f);
                return currentSpeed > 0f
                    ? current * (newSpeed / currentSpeed)
                    : Vector3.zero;
            }

            // Normalize directions.
            Vector3 targetDir = target / targetSpeed;
            Vector3 currentDir = currentSpeed > 0f
                ? current / Mathf.Max(currentSpeed, 0.000001f)
                : targetDir;

            float directionDot = Vector3.Dot(currentDir, targetDir);

            // Case 2: Starting from rest -> accelerate toward target.
            if (currentSpeed <= 0f)
            {
                float newSpeed = Mathf.Min(targetSpeed, maxAccelDelta);
                return targetDir * newSpeed;
            }

            // Case 3: Roughly same direction -> accelerate/decelerate toward target speed.
            if (directionDot > 0f)
            {
                float speedDelta = targetSpeed - currentSpeed;
                float maxDelta = Mathf.Min(maxAccelDelta, Mathf.Abs(speedDelta));

                float newSpeed = currentSpeed + Mathf.Sign(speedDelta) * maxDelta;
                return targetDir * newSpeed;
            }

            // Case 4: Opposite or very different direction.
            // First brake in current direction.
            float speedAfterBrake = currentSpeed - maxDecelDelta;

            if (speedAfterBrake > 0f)
            {
                // Still moving in old direction; just reduce magnitude.
                return currentDir * speedAfterBrake;
            }

            // We effectively came to a stop (or overshot slightly below 0).
            // Use any "overshoot" + accel to start in target direction.
            float overshoot = -speedAfterBrake; // how far below zero we went
            float accelBudget = Mathf.Max(0f, maxAccelDelta - overshoot);

            float newSpeedTarget = Mathf.Min(targetSpeed, accelBudget);
            return targetDir * newSpeedTarget;
        }

        // --------------------------------------------------------------------
        // Debug / telemetry helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Computes CurrentSpeed and CurrentAcceleration based on the current
        /// horizontal velocity and previous frame's horizontal velocity.
        /// </summary>
        private void UpdateSpeedAndAcceleration(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                CurrentAcceleration = 0f;
                return;
            }

            Vector3 currentHorizontal = _currentHorizontalVelocity;

            CurrentSpeed = currentHorizontal.magnitude;

            if (!_hasPrevVelocity)
            {
                CurrentAcceleration = 0f;
                _prevHorizontalVelocity = currentHorizontal;
                _hasPrevVelocity = true;
                return;
            }

            float prevSpeed = _prevHorizontalVelocity.magnitude;
            float speedDelta = CurrentSpeed - prevSpeed;
            CurrentAcceleration = speedDelta / deltaTime;

            _prevHorizontalVelocity = currentHorizontal;
        }
    }
}