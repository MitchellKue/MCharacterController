// File: Runtime/Core/CharacterMotor.cs
// Namespace: Kojiko.MCharacterController.Core
//
// Summary:
// 1. Wraps Unity's CharacterController to handle movement and gravity.
// 2. Receives desired movement directions from CharacterControllerRoot each frame.
// 3. Maintains grounded and velocity state for use by abilities and other systems.
// 4. Computes horizontal speed and scalar acceleration for debug/telemetry systems.
// 5. NEW: Exposes horizontal velocity as a public read-only property for visualization.

using UnityEngine;

namespace Kojiko.MCharacterController.Core
{
    /// <summary>
    /// 1. STEP 1: Accept a desired move direction from higher-level logic (e.g., input + camera).
    /// 2. STEP 2: Apply gravity and grounded logic, building a final velocity vector.
    /// 3. STEP 3: Move the CharacterController component with this velocity each frame.
    /// 4. Additionally: Exposes horizontal speed and scalar acceleration for debug systems.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

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
        /// NEW:
        /// Horizontal velocity in world space (Y component is always 0).
        /// This is useful for visualization systems that only care about
        /// movement along the ground plane.
        /// </summary>
        public Vector3 HorizontalVelocity => _currentHorizontalVelocity; // NEW

        /// <summary>
        /// Current horizontal speed (magnitude of Velocity on the XZ plane).
        /// Useful for debug and UI systems that want a stable "move speed" value.
        /// </summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// Current scalar acceleration in m/s^2, based on the change in horizontal
        /// speed over time. Positive when speeding up, negative when slowing down.
        /// This is intentionally a scalar to make it easy for debug visualization
        /// systems (e.g., gizmos, graphs) to interpret and display.
        /// </summary>
        public float CurrentAcceleration { get; private set; }

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
        /// NEW:
        /// Current horizontal velocity (XZ only), cached so HorizontalVelocity
        /// can be exposed without recomputing each time.
        /// </summary>
        private Vector3 _currentHorizontalVelocity; // NEW

        /// <summary>
        /// Previous frame's horizontal velocity, used to compute acceleration.
        /// Only the XZ components are used for the acceleration calculation.
        /// </summary>
        private Vector3 _prevHorizontalVelocity;

        /// <summary>
        /// Tracks whether _prevHorizontalVelocity has been initialized.
        /// </summary>
        private bool _hasPrevVelocity;

        private void Awake()
        {
            // STEP 1: Cache reference to the CharacterController.
            _characterController = GetComponent<CharacterController>();

            // STEP 2: Initialize internal velocity.
            _velocity = Vector3.zero;

            // NEW: Initialize horizontal and acceleration-related state.
            _currentHorizontalVelocity = Vector3.zero; // NEW
            _prevHorizontalVelocity = Vector3.zero;
            _hasPrevVelocity = false;
            CurrentSpeed = 0f;
            CurrentAcceleration = 0f;
        }

        /// <summary>
        /// Called every frame by CharacterControllerRoot to advance movement.
        /// </summary>
        /// <param name="desiredMoveWorld">
        /// Desired horizontal move direction in world space (y should be 0).
        /// </param>
        /// <param name="deltaTime">
        /// Time step for this frame.
        /// </param>
        public void Step(Vector3 desiredMoveWorld, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            // STEP 1: Check if the character is grounded using CharacterController.
            IsGrounded = _characterController.isGrounded;

            // STEP 2: Compute horizontal velocity based on desired move direction and speed.
            // Ensure desiredMoveWorld has no vertical component.
            desiredMoveWorld.y = 0f;
            var desiredHorizontal = desiredMoveWorld.normalized * _moveSpeed;

            // Preserve vertical velocity from previous frame.
            var verticalVelocity = _velocity.y;

            // STEP 3: Update vertical velocity with gravity or grounded gravity.
            if (IsGrounded && verticalVelocity < 0f)
            {
                // Small downward force keeps the character grounded.
                verticalVelocity = _groundedGravity;
            }
            else
            {
                verticalVelocity += _gravity * deltaTime;
            }

            // STEP 4: Build the final velocity vector with horizontal + vertical parts.
            _velocity = new Vector3(desiredHorizontal.x, verticalVelocity, desiredHorizontal.z);

            // NEW: Cache the horizontal component for HorizontalVelocity.
            _currentHorizontalVelocity = new Vector3(_velocity.x, 0f, _velocity.z); // NEW

            // NEW STEP 4.5: Update speed and acceleration for this frame.
            UpdateSpeedAndAcceleration(deltaTime);

            // STEP 5: Move the CharacterController by velocity * deltaTime.
            var motion = _velocity * deltaTime;
            _characterController.Move(motion);
        }

        /// <summary>
        /// Allows external systems (e.g., jump abilities) to override the vertical velocity.
        /// </summary>
        /// <param name="newVerticalVelocity">
        /// The new vertical component for the internal velocity vector.
        /// </param>
        public void SetVerticalVelocity(float newVerticalVelocity)
        {
            // STEP 1: Read current horizontal components.
            var horizontal = new Vector3(_velocity.x, 0f, _velocity.z);

            // STEP 2: Combine horizontal with the provided vertical value.
            _velocity = horizontal + Vector3.up * newVerticalVelocity;

            // STEP 3: This will be applied on the next Step() call.
        }

        /// <summary>
        /// Computes CurrentSpeed and CurrentAcceleration based on the current
        /// internal velocity and the previous frame's horizontal velocity.
        /// This method should be called exactly once per Step() call, after
        /// _velocity has been updated for this frame.
        /// </summary>
        /// <param name="deltaTime">
        /// Time step used for this movement step.
        /// </param>
        private void UpdateSpeedAndAcceleration(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                // Degenerate case: cannot compute acceleration without a positive time step.
                CurrentAcceleration = 0f;
                return;
            }

            // Use the cached horizontal velocity.
            var currentHorizontal = _currentHorizontalVelocity; // NEW

            // Compute current horizontal speed magnitude.
            CurrentSpeed = currentHorizontal.magnitude;

            if (!_hasPrevVelocity)
            {
                // First frame: we do not yet have a baseline to compute acceleration from.
                CurrentAcceleration = 0f;
                _prevHorizontalVelocity = currentHorizontal;
                _hasPrevVelocity = true;
                return;
            }

            // Compute previous frame's horizontal speed.
            float prevSpeed = _prevHorizontalVelocity.magnitude;

            // Change in speed over this time step.
            float speedDelta = CurrentSpeed - prevSpeed;

            // Acceleration = delta speed / delta time (m/s^2).
            CurrentAcceleration = speedDelta / deltaTime;

            // Cache this frame's horizontal velocity for next frame's computation.
            _prevHorizontalVelocity = currentHorizontal;
        }
    }
}