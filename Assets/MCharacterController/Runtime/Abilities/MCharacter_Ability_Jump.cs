// File: Runtime/Abilities/Ability_Jump.cs
// Namespace: Kojiko.MCharacterController.Abilities
//
// Summary:
// - Handles jump input buffering and coyote time.
// - Calls the motor to set an upward vertical velocity when a jump triggers.
// - No variable-height logic (hold-to-jump-higher) yet.

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    [DisallowMultipleComponent]
    public class MCharacter_Ability_Jump : MonoBehaviour, ICharacterAbility
    {
        [Header("Jump Core")]

        [Tooltip("Upward jump speed in m/s at the start of the jump.")]
        [SerializeField]
        private float _jumpSpeed = 7.0f;

        [Tooltip("If true, only allow jumping when grounded or within coyote time.")]
        [SerializeField]
        private bool _requireGrounded = true;

        [Header("Coyote Time")]

        [Tooltip("Allow jumping a short time after leaving the ground (seconds). Set to 0 to disable.")]
        [SerializeField]
        private float _coyoteTime = 0.1f;

        [Header("Jump Buffer")]

        [Tooltip("Allow jump input slightly before landing (seconds). Set to 0 to disable.")]
        [SerializeField]
        private float _jumpBufferTime = 0.1f;

        [Header("Debug State")]
        [SerializeField]
        private bool _isJumping;

        [SerializeField]
        private bool _jumpQueued;

        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------

        private MCharacter_Motor_Controller _motor;
        private MCharacter_Root_Controller _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;

        // Timers
        private float _timeSinceLastGrounded;
        private float _timeSinceJumpPressed;
        private float _timeSinceJumpStart;

        /// <summary>
        /// True while the character is considered in a jump state (after jump triggered, before landing).
        /// </summary>
        public bool IsJumping => _isJumping;

        // --------------------------------------------------------------------
        // ICharacterAbility
        // --------------------------------------------------------------------

        public void Initialize(
            MCharacter_Motor_Controller motor,
            MCharacter_Root_Controller controllerRoot,
            ICcInputSource input,
            CameraRig_Base cameraRig)
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

            // ----------------------------------------------------------------
            // 1. Grounded / coyote tracking
            // ----------------------------------------------------------------
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

            // ----------------------------------------------------------------
            // 2. Jump input tracking (buffer)
            // ----------------------------------------------------------------
            if (_input.JumpPressed)
            {
                _timeSinceJumpPressed = 0f;
                _jumpQueued = true;
            }
            else
            {
                _timeSinceJumpPressed += deltaTime;
            }

            // Invalidate jump buffer when expired.
            if (_jumpBufferTime <= 0f || _timeSinceJumpPressed > _jumpBufferTime)
            {
                _jumpQueued = false;
            }

            // ----------------------------------------------------------------
            // 3. Check if we’re allowed to jump at all (global permission)
            // ----------------------------------------------------------------
            if (!IsJumpGloballyAllowed())
            {
                // If some other system disabled jumping, just clear queued input.
                _jumpQueued = false;
                return;
            }

            // ----------------------------------------------------------------
            // 4. Triggering a jump (grounded / coyote / buffer)
            // ----------------------------------------------------------------
            bool withinCoyote =
                _coyoteTime > 0f &&
                _timeSinceLastGrounded <= _coyoteTime;

            bool groundedOrCoyote =
                _motor.IsGrounded || (!_requireGrounded || withinCoyote);

            if (_requireGrounded && !groundedOrCoyote)
            {
                // Can't jump right now: still airborne and outside coyote window.
                // Keep the buffer active until it expires naturally.
            }
            else if (_jumpQueued)
            {
                // Perform the jump now.
                PerformJump();

                // Consume the queued jump so we don't retrigger multiple times.
                _jumpQueued = false;
                _timeSinceJumpPressed = float.PositiveInfinity;
            }

            // ----------------------------------------------------------------
            // 5. Advance jump timer if we're currently in a jump
            // ----------------------------------------------------------------
            if (_timeSinceJumpStart < float.PositiveInfinity)
            {
                _timeSinceJumpStart += deltaTime;
            }
        }

        public void PostStep(float deltaTime)
        {
            // Optional:
            // - Check for "just landed" by seeing IsGrounded go from false->true,
            //   and then trigger landing effects / animation events here.
        }

        // --------------------------------------------------------------------
        // Internal helpers
        // --------------------------------------------------------------------

        private void PerformJump()
        {
            // If your motor has any special rule (e.g., clear downward velocity first),
            // do it inside SetVerticalVelocity so this call stays simple.
            _motor.SetVerticalVelocity(_jumpSpeed);

            _isJumping = true;
            _timeSinceJumpStart = 0f;
        }

        private bool IsJumpGloballyAllowed()
        {
            // Hook for a motor-level or game-level permission flag.
            // Example if you add to the motor:
            //   public bool AllowJump { get; set; } = true;
            //
            // Then here:
            //   return _motor.AllowJump;
            //
            // For now, just always allow:
            return true;
        }

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