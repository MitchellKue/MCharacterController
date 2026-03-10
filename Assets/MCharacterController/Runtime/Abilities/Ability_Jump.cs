// File: Runtime/Abilities/JumpAbility.cs
using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    /// <summary>
    /// Simple jump ability with:
    /// - Ground / coyote time.
    /// - Jump buffer.
    /// NO variable-height logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ability_Jump : MonoBehaviour, ICharacterAbility
    {
        [Header("Jump Core")]

        [Tooltip("Upward jump speed in m/s at the start of the jump.")]
        [SerializeField] private float _jumpSpeed = 7.0f;

        [Tooltip("If true, only allow jumping when grounded or within coyote time.")]
        [SerializeField] private bool _requireGrounded = true;

        [Header("Coyote Time")]

        [Tooltip("Allow jumping a short time after leaving the ground (seconds). Set to 0 to disable.")]
        [SerializeField] private float _coyoteTime = 0.1f;

        [Header("Jump Buffer")]

        [Tooltip("Allow jump input slightly before landing (seconds). Set to 0 to disable.")]
        [SerializeField] private float _jumpBufferTime = 0.1f;

        [Header("Debug State")]
        [SerializeField] private bool _isJumping;
        [SerializeField] private bool _jumpQueued;

        private MCharacter_Motor _motor;
        private MCharacter_Controller_Root _controllerRoot;
        private ICcInputSource _input;
        private CameraRigBase _cameraRig;

        private float _timeSinceLastGrounded = 0f;
        private float _timeSinceJumpPressed = float.PositiveInfinity;
        private float _timeSinceJumpStart = float.PositiveInfinity;

        /// <summary>
        /// True while the character is considered in a jump state (after jump triggered, before landing).
        /// </summary>
        public bool IsJumping => _isJumping;

        // --------------------------------------------------------------------
        // ICharacterAbility
        // --------------------------------------------------------------------

        public void Initialize(
            MCharacter_Motor motor,
            MCharacter_Controller_Root controllerRoot,
            ICcInputSource input,
            CameraRigBase cameraRig)
        {
            _motor = motor;
            _controllerRoot = controllerRoot;
            _input = input;
            _cameraRig = cameraRig;

            ResetState();
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (deltaTime <= 0f || _motor == null || _input == null)
            {
                _isJumping = false;
                return;
            }

            // --------------------------------------------------------------
            // 1. Grounded / coyote tracking.
            // --------------------------------------------------------------
            if (_motor.IsGrounded)
            {
                _timeSinceLastGrounded = 0f;

                // Touching the ground resets jump state.
                _isJumping = false;
                _timeSinceJumpStart = float.PositiveInfinity;
            }
            else
            {
                _timeSinceLastGrounded += deltaTime;
            }

            // --------------------------------------------------------------
            // 2. Jump input tracking (buffer).
            // --------------------------------------------------------------
            if (_input.JumpPressed)
            {
                _timeSinceJumpPressed = 0f;
                _jumpQueued = true;
            }
            else
            {
                _timeSinceJumpPressed += deltaTime;
            }

            // Invalidate buffer when expired.
            if (_jumpBufferTime <= 0f || _timeSinceJumpPressed > _jumpBufferTime)
            {
                _jumpQueued = false;
            }

            // --------------------------------------------------------------
            // 3. Triggering a jump (ground / coyote / buffer).
            // --------------------------------------------------------------
            bool withinCoyote = _coyoteTime > 0f && _timeSinceLastGrounded <= _coyoteTime;
            bool groundedOrCoyote = _motor.IsGrounded || withinCoyote;

            if (_requireGrounded && !groundedOrCoyote)
            {
                // Can't jump if not grounded or within coyote time.
            }
            else if (_jumpQueued)
            {
                // Perform the jump now.
                _motor.SetVerticalVelocity(_jumpSpeed);
                _isJumping = true;
                _timeSinceJumpStart = 0f;

                // Consume the queued jump so we don't retrigger multiple times.
                _jumpQueued = false;
                _timeSinceJumpPressed = float.PositiveInfinity;
            }

            // Advance jump timer if we're currently in a jump.
            if (_timeSinceJumpStart < float.PositiveInfinity)
            {
                _timeSinceJumpStart += deltaTime;
            }
        }

        public void PostStep(float deltaTime)
        {
            // Currently unused; can be used for landing effects, etc.
        }

        // --------------------------------------------------------------------
        // Internal helpers
        // --------------------------------------------------------------------

        private void ResetState()
        {
            _isJumping = false;
            _jumpQueued = false;

            _timeSinceLastGrounded = 0f;
            _timeSinceJumpPressed = float.PositiveInfinity;
            _timeSinceJumpStart = float.PositiveInfinity;
        }
    }
}