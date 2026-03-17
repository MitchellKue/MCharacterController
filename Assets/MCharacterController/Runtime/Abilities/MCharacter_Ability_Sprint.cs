// File: Runtime/Abilities/Ability_Sprint.cs
// Namespace: Kojiko.MCharacterController.Abilities
//
// Summary:
// - Interprets sprint input and movement direction.
// - Applies a speed multiplier to the motor while sprinting.
// - Optionally restricts sprint to mostly-forward movement and sufficient input magnitude.

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    [DisallowMultipleComponent]
    public class MCharacter_Ability_Sprint : MonoBehaviour, ICharacterAbility
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

        [Tooltip("Speed multiplier applied to the motor while sprinting.\n" +
                 "Example: 1.5 means 50% faster than the base movement.")]
        [SerializeField]
        private float _sprintSpeedMultiplier = 1.5f;

        
        [Header("Debug State")]
        private bool _isSprinting;

        // --------------------------------------------------------------------
        // Internal references
        // --------------------------------------------------------------------

        private MCharacter_Motor_Controller _motor;
        private MCharacter_Root_Controller _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;
        private Transform _characterTransform;

        // ADDED: stance ref so sprint can auto-stand
        private MCharacter_Stance_Controller _stanceController; // ADDED

        // Cached cosine threshold for the forward-angle check.
        private float _maxForwardCosine;

        // --------------------------------------------------------------------
        // ICharacterAbility implementation
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
            _characterTransform = controllerRoot != null ? controllerRoot.transform : null;

            _maxForwardCosine = Mathf.Cos(_maxForwardAngle * Mathf.Deg2Rad);

            // ADDED: grab stance controller from the same root object (if present)
            if (_controllerRoot != null) // ADDED
            {                            // ADDED
                _stanceController = _controllerRoot.GetComponent<MCharacter_Stance_Controller>(); // ADDED
            }                            // ADDED
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            // Safety / early outs.
            if (deltaTime <= 0f || _motor == null || _input == null || _characterTransform == null)
            {
                StopSprinting();
                return;
            }

            // If the motor has a global sprint-permission flag and it's off, respect it.
            // (If you don't have AllowSprint on the motor, just remove this block.)
            if (HasSprintPermissionFlag() && !IsSprintAllowedOnMotor())
            {
                StopSprinting();
                return;
            }

            // Sprint input not held: no sprint.
            if (!_input.SprintHeld)
            {
                StopSprinting();
                return;
            }

            // ADDED: if crouched/prone and sprint is held, auto-stand
            if (_stanceController != null) // ADDED
            {                               // ADDED
                if (_stanceController.IsCrouching || _stanceController.IsProne) // ADDED
                {                                                               // ADDED
                    _stanceController.TrySetStance(CharacterStance.Standing);   // ADDED
                }                                                               // ADDED
            }                                                                   // ADDED

            float desiredSqrMag = desiredMoveWorld.sqrMagnitude;

            // Optionally require some movement so we don't sprint while standing still.
            if (_requireMoveInput && desiredSqrMag < _minMoveMagnitudeToSprint * _minMoveMagnitudeToSprint)
            {
                StopSprinting();
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
                    StopSprinting();
                    return;
                }
            }

            // All conditions met: apply sprint for this frame.
            StartOrMaintainSprinting();
        }

        public void PostStep(float deltaTime)
        {
            // Optional:
            // - Drain stamina while _isSprinting.
            // - Trigger footstep, breathing, camera FOV, etc.
        }

        // --------------------------------------------------------------------
        // Internal helpers
        // --------------------------------------------------------------------

        private void StartOrMaintainSprinting()
        {
            _isSprinting = true;

            // Core: directly modify the motor's speed multiplier.
            // Motor will handle clamping/etc. if you implement AllowSprint logic there.
            _motor.SpeedMultiplier = _sprintSpeedMultiplier;

            // If you later wire acceleration into the motor, you could also do:
            // _motor.AccelerationMultiplier = _accelerationMultiplierWhileSprinting;
        }

        private void StopSprinting()
        {
            if (!_isSprinting && Mathf.Approximately(_motor.SpeedMultiplier, 1f))
                return; // already in non-sprint state

            _isSprinting = false;
            _motor.SpeedMultiplier = 1f;

            // If you add an acceleration multiplier to the motor, reset it here:
            // _motor.AccelerationMultiplier = 1f;
        }

        // If you add an AllowSprint flag to the motor, this is how you can check it
        // without hard-coding a direct field name in multiple places.

        private bool HasSprintPermissionFlag()
        {
            // If your motor always has AllowSprint, this can just return true.
            // Keeping it as a method makes it easy to stub out in editor/variants.
            return true;
        }

        private bool IsSprintAllowedOnMotor()
        {
            // If your motor exposes a public bool AllowSprint, use it here.
            // Replace this with your actual property.
            // Example:
            // return _motor.AllowSprint;

            // Placeholder if you haven't added it yet:
            return true;
        }

        // --------------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------------

        /// <summary>
        /// True if sprint conditions were met for the last Tick() call.
        /// Use this from UI/VFX/SFX/etc. to react to sprinting.
        /// </summary>
        public bool IsSprinting => _isSprinting;
    }
}