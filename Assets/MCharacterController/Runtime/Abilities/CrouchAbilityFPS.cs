// File: Runtime/Abilities/CrouchAbilityFPS.cs

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    /// <summary>
    /// FPS-style crouch:
    /// - Smoothly lerps CharacterController height and camera height.
    /// - Optionally scales a visual "body" (capsule mesh) for crouch.
    /// - Optional air-crouch.
    /// - Option to use input as toggle or hold.
    /// - Reduces movement speed while crouched by a multiplier.
    /// - Prevents uncrouching when blocked by a low ceiling.
    /// </summary>
    [DisallowMultipleComponent]
    public class CrouchAbilityFPS : MonoBehaviour, ICharacterAbility
    {
        [Header("Crouch Settings")]

        [Tooltip("Standing height for the CharacterController (meters).")]
        [SerializeField] private float _standingHeight = 1.8f;

        [Tooltip("Crouched height for the CharacterController (meters).")]
        [SerializeField] private float _crouchedHeight = 1.0f;

        [Tooltip("Speed at which we lerp height/camera/body between standing and crouched.")]
        [SerializeField] private float _crouchLerpSpeed = 10f;

        [Tooltip("Movement speed factor while crouched. 0.5 = half speed.")]
        [SerializeField, Range(0.1f, 1f)] private float _crouchSpeedMultiplier = 0.5f;

        [Tooltip("Allow entering crouch while in the air.")]
        [SerializeField] private bool _allowAirCrouch = true;

        [Tooltip("If true, crouch input acts as a toggle. If false, crouch is hold-to-crouch.")]
        [SerializeField] private bool _useToggleInput = false;

        [Header("Ceiling Check")]

        [Tooltip("Extra distance to keep from the ceiling when checking if we can stand.")]
        [SerializeField] private float _uncrouchSkin = 0.02f;

        [Tooltip("Physics layers to check when testing if we can stand up.")]
        [SerializeField] private LayerMask _ceilingLayers = ~0;

        [Header("Camera")]

        [Tooltip("Camera transform to move vertically while crouching (e.g., FPS camera).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Local Y position of camera when standing.")]
        [SerializeField] private float _standingCameraLocalY = 0.9f;

        [Tooltip("Local Y position of camera when crouched.")]
        [SerializeField] private float _crouchedCameraLocalY = 0.5f;

        [Header("Visual Body")]

        [Tooltip("Optional visual capsule/body to scale when crouching.")]
        [SerializeField] private Transform _bodyVisual;

        [Tooltip("Local scale of the body visual when standing.")]
        [SerializeField] private Vector3 _standingBodyScale = Vector3.one;

        [Tooltip("Local scale of the body visual when crouched.")]
        [SerializeField] private Vector3 _crouchedBodyScale = new Vector3(1f, 0.6f, 1f);

        [Header("Debug State")]
        [SerializeField] private bool _isCrouched;
        [SerializeField] private bool _isCrouchingTransition; // in the middle of lerp

        private CharacterMotor _motor;
        private CharacterControllerRoot _controllerRoot;
        private ICcInputSource _input;
        private CameraRigBase _cameraRig;
        private CharacterController _characterController;

        // Target state we are moving toward (true = crouched, false = standing)
        private bool _targetCrouchedState;

        // Cached original CharacterController center so we keep feet on ground.
        private Vector3 _originalCenter;

        // Cached initial body scale if not explicitly set.
        private bool _bodyScaleInitialized;

        // Cache last uncrouch-blocked state to avoid spamming logs.
        private bool _loggedMissingControllerWarning;

        public bool IsCrouched => _isCrouched;

        // --------------------------------------------------------------------
        // ICharacterAbility
        // --------------------------------------------------------------------

        public void Initialize(
            CharacterMotor motor,
            CharacterControllerRoot controllerRoot,
            ICcInputSource input,
            CameraRigBase cameraRig)
        {
            _motor = motor;
            _controllerRoot = controllerRoot;
            _input = input;
            _cameraRig = cameraRig;

            _characterController = motor != null
                ? motor.GetComponent<CharacterController>()
                : null;

            if (_characterController == null)
            {
                UnityEngine.Debug.LogError("[CrouchAbilityFPS] Requires a CharacterController on the CharacterMotor GameObject.", this);
                enabled = false;
                return;
            }

            _originalCenter = _characterController.center;

            // Initialize controller to standing
            _characterController.height = _standingHeight;
            _characterController.center = new Vector3(
                _originalCenter.x,
                _standingHeight * 0.5f,
                _originalCenter.z);

            // Initialize camera to standing height
            if (_cameraTransform != null)
            {
                var localPos = _cameraTransform.localPosition;
                localPos.y = _standingCameraLocalY;
                _cameraTransform.localPosition = localPos;
            }

            // Initialize body visual scale
            if (_bodyVisual != null)
            {
                if (!_bodyScaleInitialized)
                {
                    // If user left _standingBodyScale as (1,1,1), assume current scale is the "standing" scale.
                    if (_standingBodyScale == Vector3.one && _bodyVisual.localScale != Vector3.one)
                    {
                        _standingBodyScale = _bodyVisual.localScale;
                    }

                    _bodyScaleInitialized = true;
                }

                _bodyVisual.localScale = _standingBodyScale;
            }

            _isCrouched = false;
            _targetCrouchedState = false;
            _isCrouchingTransition = false;
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (deltaTime <= 0f || _motor == null || _input == null || _characterController == null)
                return;

            HandleInput();
            UpdateCrouchTransition(deltaTime);
            ApplyCrouchSpeed(ref desiredMoveWorld);
        }

        public void PostStep(float deltaTime)
        {
            // Not used for now
        }

        // --------------------------------------------------------------------
        // Internal logic
        // --------------------------------------------------------------------

        private void HandleInput()
        {
            bool crouchPressed = _input.CrouchPressed;
            bool crouchHeld = _input.CrouchHeld;

            if (_useToggleInput)
            {
                // Toggle mode: pressing crouch flips the target state.
                if (crouchPressed)
                {
                    // If we disallow air-crouch, only allow toggling ON when grounded.
                    if (!_allowAirCrouch && !_motor.IsGrounded && !_targetCrouchedState)
                    {
                        // Ignore attempt to enter crouch in air.
                        return;
                    }

                    // If we are currently crouched and want to stand, first check ceiling.
                    if (_targetCrouchedState && !CanUncrouch())
                    {
                        // Block uncrouch; stay crouched.
                        _targetCrouchedState = true;
                        return;
                    }

                    _targetCrouchedState = !_targetCrouchedState;
                }
            }
            else
            {
                // Hold mode: target crouch state is directly tied to input hold.
                bool desiredCrouch = crouchHeld;

                if (!_allowAirCrouch && !_motor.IsGrounded && desiredCrouch)
                {
                    // Can't crouch in air, so treat as not crouched while airborne.
                    desiredCrouch = false;
                }

                // If we're currently crouched and input wants us to stand, check ceiling.
                if (!desiredCrouch && _targetCrouchedState && !CanUncrouch())
                {
                    // Block uncrouch; ignore request.
                    desiredCrouch = true;
                }

                _targetCrouchedState = desiredCrouch;
            }
        }

        private void UpdateCrouchTransition(float deltaTime)
        {
            // Decide target height and camera Y
            float targetHeight = _targetCrouchedState ? _crouchedHeight : _standingHeight;
            float currentHeight = _characterController.height;

            float targetCameraY = _targetCrouchedState ? _crouchedCameraLocalY : _standingCameraLocalY;
            float currentCameraY = _cameraTransform != null
                ? _cameraTransform.localPosition.y
                : 0f;

            // Decide target body scale
            Vector3 targetBodyScale = _targetCrouchedState ? _crouchedBodyScale : _standingBodyScale;
            Vector3 currentBodyScale = _bodyVisual != null ? _bodyVisual.localScale : Vector3.one;

            bool heightChanged = !Mathf.Approximately(currentHeight, targetHeight);

            // Lerp controller height / center
            if (heightChanged)
            {
                _isCrouchingTransition = true;

                float newHeight = Mathf.Lerp(currentHeight, targetHeight, _crouchLerpSpeed * deltaTime);
                _characterController.height = newHeight;

                // Adjust center so feet stay on the ground (center at half height).
                _characterController.center = new Vector3(
                    _originalCenter.x,
                    newHeight * 0.5f,
                    _originalCenter.z);
            }
            else
            {
                // Don't immediately clear transition; we may still be lerping camera/body.
                _isCrouchingTransition = false;
            }

            // Lerp camera height if we have a camera transform.
            if (_cameraTransform != null)
            {
                float newCameraY = Mathf.Lerp(currentCameraY, targetCameraY, _crouchLerpSpeed * deltaTime);
                var localPos = _cameraTransform.localPosition;
                localPos.y = newCameraY;
                _cameraTransform.localPosition = localPos;

                // If camera hasn't reached target yet, we're still transitioning.
                if (!Mathf.Approximately(newCameraY, targetCameraY))
                    _isCrouchingTransition = true;
            }

            // Lerp visual body scale if assigned.
            if (_bodyVisual != null)
            {
                Vector3 newScale = Vector3.Lerp(currentBodyScale, targetBodyScale, _crouchLerpSpeed * deltaTime);
                _bodyVisual.localScale = newScale;

                // If body hasn't reached target scale yet, we're still transitioning.
                if ((newScale - targetBodyScale).sqrMagnitude > 0.0001f)
                    _isCrouchingTransition = true;
            }

            // Update final crouched flag for debug
            _isCrouched = _targetCrouchedState && !_isCrouchingTransition;
        }

        private void ApplyCrouchSpeed(ref Vector3 desiredMoveWorld)
        {
            if (_targetCrouchedState || _isCrouchingTransition)
            {
                desiredMoveWorld *= _crouchSpeedMultiplier;
            }
        }

        /// <summary>
        /// Returns true if there is enough space above the character to stand up
        /// to _standingHeight without intersecting geometry.
        /// </summary>
        private bool CanUncrouch()
        {
            if (_characterController == null)
            {
                if (!_loggedMissingControllerWarning)
                {
                    UnityEngine.Debug.LogError("[CrouchAbilityFPS] Cannot perform ceiling check: CharacterController is missing.", this);
                    _loggedMissingControllerWarning = true;
                }
                return true; // Fallback: don't block uncrouch if we can't check.
            }

            float currentHeight = _characterController.height;

            // If we're already at (or near) standing height, nothing to do.
            if (currentHeight >= _standingHeight - 0.001f)
                return true;

            // Compute the capsule we WANT to occupy when standing.
            Vector3 worldCenter = _characterController.transform.TransformPoint(_characterController.center);

            float radius = _characterController.radius;
            float standingHalfHeight = _standingHeight * 0.5f;

            // Bottom and top points of the standing capsule
            Vector3 bottom = worldCenter - Vector3.up * (standingHalfHeight - radius);
            Vector3 top = worldCenter + Vector3.up * (standingHalfHeight - radius);

            // We cast a very short distance (just skin) to detect overlap.
            float castDistance = _uncrouchSkin;

            // If this capsule cast hits something, we can't stand.
            bool hit = Physics.CapsuleCast(
                point1: bottom,
                point2: top,
                radius: radius,
                direction: Vector3.up,
                maxDistance: castDistance,
                layerMask: _ceilingLayers,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore);

            return !hit;
        }
    }
}