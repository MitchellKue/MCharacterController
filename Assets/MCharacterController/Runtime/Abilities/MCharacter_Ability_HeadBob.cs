// File: Runtime/Abilities/HeadBobAbility.cs
// Namespace: Kojiko.MCharacterController.Abilities
//
// Summary:
// - Applies a sinusoidal head bob offset to a camera transform based on movement speed.
// - Uses CharacterMotor speed + grounded state, and (optionally) Sprint/Crouch abilities.
// - Designed to be plugged into CharacterAbilityController.
//
// Requirements:
// - CharacterAbilityController initializes this ability with a CharacterMotor and camera rig.
// - A camera Transform assigned; typically the same one used by CrouchAbilityFPS.

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    [DisallowMultipleComponent]
    public class MCharacter_Ability_HeadBob : MonoBehaviour, ICharacterAbility
    {
        [Header("Camera Target")]

        [Tooltip("Camera transform to apply head bobbing to (local position offset).")]
        [SerializeField]
        private Transform _cameraTransform;

        [Header("General")]

        [Tooltip("Minimum horizontal speed before head bob starts (m/s).")]
        [SerializeField]
        private float _minSpeedForBob = 0.1f;

        [Tooltip("How quickly bob intensity fades in/out when starting/stopping movement.")]
        [SerializeField]
        private float _bobEnableLerpSpeed = 10f;

        [Tooltip("Global strength multiplier for all bobbing.")]
        [SerializeField]
        private float _globalBobMultiplier = 1f;

        [Header("Walk Bob")]

        [Tooltip("Vertical bob amplitude while walking.")]
        [SerializeField]
        private float _walkVerticalAmplitude = 0.03f;

        [Tooltip("Horizontal (side-to-side) bob amplitude while walking.")]
        [SerializeField]
        private float _walkHorizontalAmplitude = 0.02f;

        [Tooltip("Bob frequency (cycles per second) while walking.")]
        [SerializeField]
        private float _walkFrequency = 1.8f;

        [Header("Sprint Bob")]

        [Tooltip("Vertical bob amplitude while sprinting.")]
        [SerializeField]
        private float _sprintVerticalAmplitude = 0.06f;

        [Tooltip("Horizontal (side-to-side) bob amplitude while sprinting.")]
        [SerializeField]
        private float _sprintHorizontalAmplitude = 0.035f;

        [Tooltip("Bob frequency (cycles per second) while sprinting.")]
        [SerializeField]
        private float _sprintFrequency = 2.5f;

        [Header("Crouch Modifiers")]

        [Tooltip("Multiplier applied to bob amplitude while crouched.")]
        [SerializeField]
        private float _crouchAmplitudeMultiplier = 0.6f;

        [Tooltip("Multiplier applied to bob frequency while crouched.")]
        [SerializeField]
        private float _crouchFrequencyMultiplier = 0.8f;

        [Header("Debug State")]
        [SerializeField] private bool _isBobbing;
        [SerializeField] private float _currentBobIntensity;
        [SerializeField] private float _currentPhase;

        // Core refs
        private MCharacter_Motor_Controller _motor;
        private MCharacter_Root_Controller _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;

        // Optional ability refs (auto-detected)
        private MCharacter_Ability_Sprint _sprintAbility;
        //private MCharacter_Ability_FPV_Crouch _crouchAbility;   // no longer using crouch ability

        // Baseline local position for the camera (no bob).
        private Vector3 _baseCameraLocalPos;
        private bool _initializedCameraBase;

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

            // Camera reference: if not explicitly assigned, try to find one.
            if (_cameraTransform == null)
            {
                // Try to find a camera child under the controller root.
                var cam = controllerRoot != null
                    ? controllerRoot.GetComponentInChildren<UnityEngine.Camera>()
                    : null;

                if (cam != null)
                {
                    _cameraTransform = cam.transform;
                }
            }

            if (_cameraTransform != null)
            {
                _baseCameraLocalPos = _cameraTransform.localPosition;
                _initializedCameraBase = true;
            }
            else
            {
                _initializedCameraBase = false;
                UnityEngine.Debug.LogWarning(
                    "[HeadBobAbility] No camera transform assigned or found; head bob will be disabled.",
                    this);
            }

            // Try to auto-find related abilities on the same GameObject.
            if (controllerRoot != null)
            {
                // Typically abilities are on the same GameObject as CharacterControllerRoot.
                _sprintAbility = controllerRoot.GetComponent<MCharacter_Ability_Sprint>();
                //_crouchAbility = controllerRoot.GetComponent<Ability_FPV_Crouch>();
            }

            _currentPhase = 0f;
            _currentBobIntensity = 0f;
            _isBobbing = false;
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (deltaTime <= 0f || _motor == null || !_initializedCameraBase)
                return;

            // Determine if we should bob at all (speed & grounded).
            float speed = _motor.CurrentSpeed;
            bool movingEnough = speed >= _minSpeedForBob;
            bool grounded = _motor.IsGrounded;

            bool shouldBob = grounded && movingEnough;

            // Smoothly blend bob intensity on/off.
            float targetIntensity = shouldBob ? 1f : 0f;
            _currentBobIntensity = Mathf.Lerp(
                _currentBobIntensity,
                targetIntensity,
                _bobEnableLerpSpeed * deltaTime
            );

            _isBobbing = _currentBobIntensity > 0.001f;

            if (!_isBobbing)
            {
                // If not bobbing (or almost zero intensity), just reset to base.
                _cameraTransform.localPosition = _baseCameraLocalPos;
                return;
            }

            // Decide if we're in sprint mode (if SprintAbility exists).
            bool isSprinting = _sprintAbility != null && _sprintAbility.IsSprinting;

            // Pick base amplitudes/frequency for walk vs sprint.
            float baseVertAmp = isSprinting ? _sprintVerticalAmplitude : _walkVerticalAmplitude;
            float baseHorAmp = isSprinting ? _sprintHorizontalAmplitude : _walkHorizontalAmplitude;
            float baseFreq = isSprinting ? _sprintFrequency : _walkFrequency;

            // Apply crouch modifiers if crouch ability is present and active.
            //bool isCrouched = _crouchAbility != null && _crouchAbility.IsCrouched;
            //if (isCrouched)
            //{
            //    baseVertAmp *= _crouchAmplitudeMultiplier;
            //    baseHorAmp *= _crouchAmplitudeMultiplier;
            //    baseFreq *= _crouchFrequencyMultiplier;
            //}

            // Scale by global multiplier and current intensity.
            float verticalAmplitude = baseVertAmp * _globalBobMultiplier * _currentBobIntensity;
            float horizontalAmplitude = baseHorAmp * _globalBobMultiplier * _currentBobIntensity;
            float frequency = baseFreq;

            // Advance phase: frequency is cycles per second.
            _currentPhase += frequency * deltaTime * Mathf.PI * 2f;

            // Wrap phase to avoid growing too large.
            if (_currentPhase > Mathf.PI * 2f)
                _currentPhase -= Mathf.PI * 2f;

            // Compute offsets:
            // - Vertical: simple sin wave.
            // - Horizontal: cos wave (90° out of phase) for side-to-side motion.
            float verticalOffset = Mathf.Sin(_currentPhase) * verticalAmplitude;
            float horizontalOffset = Mathf.Cos(_currentPhase * 0.5f) * horizontalAmplitude;

            // Apply offsets relative to the camera's local right & up.
            Vector3 right = _cameraTransform.right;
            Vector3 up = _cameraTransform.up;

            // Work in local space: start from base position, then move along local axes.
            Vector3 offset =
                (transform.InverseTransformDirection(right) * horizontalOffset) +
                (transform.InverseTransformDirection(up) * verticalOffset);

            _cameraTransform.localPosition = _baseCameraLocalPos + offset;
        }

        public void PostStep(float deltaTime)
        {
            // Not used currently.
        }

        // --------------------------------------------------------------------
        // Public getters (optional)
        // --------------------------------------------------------------------

        /// <summary>
        /// True if head bob is currently active (intensity > ~0).
        /// </summary>
        public bool IsBobbing => _isBobbing;

        /// <summary>
        /// Current normalized bob intensity (0..1).
        /// </summary>
        public float CurrentBobIntensity => _currentBobIntensity;
    }
}