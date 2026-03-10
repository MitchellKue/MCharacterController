// File: Runtime/Abilities/SprintAbility.cs
// Namespace: Kojiko.MCharacterController.Abilities
//
// Summary:
// 1. Scales the desired movement direction when sprint input is held.
// 2. Optionally restricts sprinting to mostly-forward movement.
// 3. Optionally modifies acceleration/deceleration while sprinting.
//
// Requirements:
// - CharacterControllerRoot uses CharacterAbilityController and calls TickAbilities()
//   with the desiredMoveWorld vector by ref BEFORE calling CharacterMotor.Step().
// - An ICcInputSource implementation (e.g., NewInputSystemSource) that exposes SprintHeld.
//
// Usage:
// - Add SprintAbility to the same GameObject as CharacterControllerRoot/CharacterAbilityController.
// - Ensure CharacterAbilityController is present and has this component in its list or auto-discovers it.
// - Tune sprint multipliers and angle constraints in the inspector.

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    /// <summary>
    /// Sprint ability:
    /// - While the sprint input is held and the character is moving (optionally mostly forward),
    ///   scales the desired horizontal movement direction to increase effective speed.
    /// - Optionally modifies acceleration/deceleration multipliers during sprint.
    /// 
    /// NOTE:
    /// This implementation is intentionally conservative: it only scales the *input-side*
    /// desiredMoveWorld vector. It does NOT mutate CharacterMotor fields directly, which
    /// keeps the motor generic and avoids extra coupling.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ability_Sprint : MonoBehaviour, ICharacterAbility
    {
        [Header("Activation")]

        [Tooltip("If true, sprint only applies when there is sufficient movement input.")]
        [SerializeField]
        private bool _requireMoveInput = true;

        [Tooltip("Minimum desired move magnitude (0-1-ish) to allow sprinting.")]
        [SerializeField]
        private float _minMoveMagnitudeToSprint = 0.1f;

        [Tooltip("If true, restrict sprint to mostly-forward movement relative to character yaw.")]
        [SerializeField]
        private bool _restrictToForward = true;

        [Tooltip("Maximum allowed angle (in degrees) between desired move direction and character forward for sprint to apply.")]
        [SerializeField]
        [Range(0f, 90f)]
        private float _maxForwardAngle = 45f;

        [Header("Sprint Tuning")]

        [Tooltip("Multiplier applied to desired move vector while sprinting.\n" +
                 "Example: 1.5 means 50% faster than the base movement.")]
        [SerializeField]
        private float _sprintSpeedMultiplier = 1.5f;

        [Tooltip("Optional multiplier applied to acceleration while sprinting.\n" +
                 "Set to 1 to leave acceleration unchanged.\n" +
                 "NOTE: This currently only affects input-side magnitude; hook into the motor if you want deeper control.")]
        [SerializeField]
        private float _accelerationMultiplierWhileSprinting = 1.0f;

        [Header("State (Debug)")]
        private bool _isSprinting;

        // --------------------------------------------------------------------
        // Internal references
        // --------------------------------------------------------------------

        private MCharacter_Motor _motor;
        private MCharacter_Controller_Root _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;
        private Transform _characterTransform;

        // Cached angle value in radians for faster checks.
        private float _maxForwardCosine;

        // --------------------------------------------------------------------
        // ICharacterAbility implementation
        // --------------------------------------------------------------------

        /// <inheritdoc />
        public void Initialize(
            MCharacter_Motor motor,
            MCharacter_Controller_Root controllerRoot,
            ICcInputSource input,
            CameraRig_Base cameraRig)
        {
            _motor = motor;
            _controllerRoot = controllerRoot;
            _input = input;
            _cameraRig = cameraRig;

            if (controllerRoot != null)
            {
                _characterTransform = controllerRoot.transform;
            }

            _maxForwardCosine = Mathf.Cos(_maxForwardAngle * Mathf.Deg2Rad);
        }

        /// <inheritdoc />
        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (deltaTime <= 0f)
            {
                _isSprinting = false;
                _motor.SpeedMultiplier = 1f; // ensure reset
                return;
            }

            if (_input == null || _motor == null || _characterTransform == null)
            {
                _isSprinting = false;
                _motor.SpeedMultiplier = 1f;
                return;
            }

            // Early out if sprint is not held.
            if (!_input.SprintHeld)
            {
                _isSprinting = false;
                _motor.SpeedMultiplier = 1f;
                return;
            }

            // Optionally require some movement input so we don't sprint while standing still.
            float desiredSqrMag = desiredMoveWorld.sqrMagnitude;
            if (_requireMoveInput && desiredSqrMag < _minMoveMagnitudeToSprint * _minMoveMagnitudeToSprint)
            {
                _isSprinting = false;
                _motor.SpeedMultiplier = 1f;
                return;
            }

            // Optionally require that movement direction is roughly forward.
            if (_restrictToForward && desiredSqrMag > 0.0001f)
            {
                Vector3 desiredDir = desiredMoveWorld.normalized;

                Vector3 forward = _characterTransform.forward;
                forward.y = 0f;
                forward.Normalize();

                float dot = Vector3.Dot(desiredDir, forward);
                if (dot < _maxForwardCosine)
                {
                    _isSprinting = false;
                    _motor.SpeedMultiplier = 1f;
                    return;
                }
            }

            // At this point, sprint is active for this frame.
            _isSprinting = true;

            // Simple: directly boost motor speed.
            _motor.SpeedMultiplier = _sprintSpeedMultiplier;

        }

        /// <inheritdoc />
        public void PostStep(float deltaTime)
        {
            // Currently unused for sprint; could be used for:
            // - Draining a stamina resource over time while sprinting.
            // - Playing footstep or breathing audio based on sprint state.
        }

        // --------------------------------------------------------------------
        // Public API (optional helpers)
        // --------------------------------------------------------------------

        /// <summary>
        /// True if sprint conditions were met for the last Tick() call.
        /// This can be used by other systems (UI, VFX, SFX) to react to sprinting.
        /// </summary>
        public bool IsSprinting => _isSprinting;
    }
}