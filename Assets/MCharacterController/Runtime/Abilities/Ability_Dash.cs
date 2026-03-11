// File: Runtime/Abilities/Ability_Dash.cs
// Namespace: Kojiko.MCharacterController.Abilities

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    /// <summary>
    /// Dash ability:
    /// - Moves the character from current position by a fixed dash distance
    ///   using a constant dash velocity.
    /// - Direction based on camera-relative move input.
    /// - Different dash distances for forward, backward, strafe.
    /// - Works on ground and/or in air via flags.
    /// - Cancels vertical velocity on start for a snappy combat feel.
    /// - Can be blocked while other abilities are active.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ability_Dash : MonoBehaviour, ICharacterAbility
    {
        [Header("General")]
        [SerializeField]
        [Tooltip("Enable/disable dash globally.")]
        private bool _enabled = true;

        [Header("Dash Distances (meters)")]
        [Tooltip("Dash distance when moving primarily forward.")]
        [SerializeField] private float _forwardDistance = 6f;

        [Tooltip("Dash distance when moving primarily backward.")]
        [SerializeField] private float _backwardDistance = 4f;

        [Tooltip("Dash distance when moving primarily left/right.")]
        [SerializeField] private float _strafeDistance = 5f;

        [Header("Dash Timing")]
        [Tooltip("How long the dash lasts in seconds. Shorter = snappier.")]
        [SerializeField] private float _dashDuration = 0.1f;

        [Header("Dash Speed")]
        [Tooltip("Global dash speed in m/s. This is the magnitude of dash velocity.")]
        [SerializeField] private float _dashVelocity = 20f;

        [Header("Cooldown")]
        [Tooltip("Cooldown between dash activations in seconds.")]
        [SerializeField] private float _cooldown = 0.5f;

        [Header("Direction Handling")]
        [Tooltip("If true, dash in the exact input direction (diagonals allowed).\n" +
                 "If false, only dash along the dominant axis (forward/back vs strafe).")]
        [SerializeField] private bool _allowDiagonalDash = false;

        [Header("State Restrictions")]
        [Tooltip("Can dash while grounded.")]
        [SerializeField] private bool _canGroundDash = true;

        [Tooltip("Can dash while in the air.")]
        [SerializeField] private bool _canAirDash = true;

        [Tooltip("If true, normal movement is ignored/overridden while dashing.")]
        [SerializeField] private bool _overrideMovementDuringDash = true;

        [Header("Vertical Handling")]
        [Tooltip("If true, vertical velocity is set to zero when dash starts.")]
        [SerializeField] private bool _cancelVerticalOnDashStart = true;

        [Header("Debug Gizmos")]
        [SerializeField]
        [Tooltip("Draw gizmos in the editor to visualize dash direction and distance.")]
        private bool _drawGizmos = true;

        [SerializeField]
        [Tooltip("Color of the gizmo when a dash is actively in progress.")]
        private Color _activeDashColor = Color.cyan;

        [SerializeField]
        [Tooltip("Color of the gizmo preview when dash is not active but could be triggered.")]
        private Color _previewDashColor = new Color(1f, 0.8f, 0.2f, 1f);

        [SerializeField]
        [Tooltip("Radius of the sphere drawn at the dash end point.")]
        private float _gizmoEndSphereRadius = 0.1f;

        // Core refs (from Initialize)
        private MCharacter_Motor _motor;
        private MCharacter_Controller_Root _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;

        private Transform _transform;

        // Internal state
        private bool _isDashing;
        private float _cooldownTimer;
        private float _dashTimeRemaining;

        private Vector3 _dashDirection;       // normalized world-space direction (XZ)
        private float _targetDashDistance;    // how far we want to travel this dash
        private float _distanceTraveled;      // how far we have moved so far this dash

        // Gizmo preview state (editor-only helpers)
        private Vector3 _previewDirection;
        private float _previewDistance;
        private bool _hasValidPreview;

        /// <summary>
        /// Set this from your ability controller to prevent dash while
        /// other exclusive abilities (zipline, etc.) are active.
        /// </summary>
        public bool IsBlockedByOtherAbilities { get; set; }

        public bool Enabled => _enabled;
        public bool IsDashing => _isDashing;
        public bool IsOnCooldown => _cooldownTimer > 0f;

        #region ICharacterAbility

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

            _transform = controllerRoot != null ? controllerRoot.transform : transform;

            _isDashing = false;
            _cooldownTimer = 0f;

            _dashDirection = Vector3.zero;
            _targetDashDistance = 0f;
            _distanceTraveled = 0f;

            _previewDirection = Vector3.zero;
            _previewDistance = 0f;
            _hasValidPreview = false;
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (!_enabled || deltaTime <= 0f || _motor == null || _input == null)
                return;

            // Update cooldown timer
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= deltaTime;
            }

            if (_isDashing)
            {
                TickDash(deltaTime, ref desiredMoveWorld);
            }
            else
            {
                TryStartDash();
                UpdatePreviewState();
            }
        }

        public void PostStep(float deltaTime)
        {
            // No-op currently. Hook SFX/VFX or state cleanup here if needed.
        }

        #endregion

        #region Dash Logic

        private void TryStartDash()
        {
            if (!_enabled || _isDashing)
                return;

            if (_cooldownTimer > 0f)
                return;

            if (IsBlockedByOtherAbilities)
                return;

            if (_motor == null || _input == null)
                return;

            // Ground / air permissions
            bool grounded = _motor.IsGrounded;
            if (grounded && !_canGroundDash)
                return;
            if (!grounded && !_canAirDash)
                return;

            // Requires dash input this frame
            if (!_input.DashPressed)
                return;

            // Requires movement input
            Vector2 moveInput = _input.MoveAxis;
            if (moveInput.sqrMagnitude < 0.0001f)
                return; // no movement -> no dash

            // Need a camera or some orientation basis
            Transform cameraTransform = GetCameraTransform();
            if (cameraTransform == null)
                return;

            // Compute dash direction from camera-relative input
            Vector3 dashDir = ComputeDashDirection(moveInput, cameraTransform);
            dashDir.y = 0f;

            if (dashDir.sqrMagnitude < 0.0001f)
                return;

            dashDir.Normalize();

            float dashDistance = GetDashDistanceForDirection(dashDir, cameraTransform);
            if (dashDistance <= 0f || _dashVelocity <= 0f)
                return;

            // Setup dash state
            _dashDirection = dashDir;
            _targetDashDistance = dashDistance;
            _distanceTraveled = 0f;

            // Dash timing
            _dashTimeRemaining = Mathf.Max(0.01f, _dashDuration); // avoid 0

            _isDashing = true;
            _cooldownTimer = _cooldown;

            // Snap vertical motion if configured
            if (_cancelVerticalOnDashStart)
            {
                _motor.SetVerticalVelocity(0f);
            }

            // Optional: clear external velocities for a very snappy dash
            // _motor.ClearExternalVelocities();
        }

        private void TickDash(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (_dashTimeRemaining <= 0f || _targetDashDistance <= 0f || _dashDirection.sqrMagnitude < 0.0001f)
            {
                EndDash();
                return;
            }

            // How much of the dash we complete this frame (0–1)
            float deltaFraction = deltaTime / _dashTimeRemaining;
            // Clamp to avoid overshooting
            if (deltaFraction > 1f) deltaFraction = 1f;

            // Distance to move this frame = fraction of remaining distance
            float remainingDistance = _targetDashDistance - _distanceTraveled;
            float frameDistance = remainingDistance * deltaFraction;

            if (frameDistance < 0f)
                frameDistance = 0f;

            // Convert to velocity for this frame
            // (distance / deltaTime = speed)
            Vector3 frameVelocity = (deltaTime > 0f)
                ? _dashDirection * (frameDistance / deltaTime)
                : Vector3.zero;

            if (_overrideMovementDuringDash)
            {
                desiredMoveWorld = frameVelocity;
            }
            else
            {
                desiredMoveWorld += frameVelocity;
            }

            _distanceTraveled += frameDistance;
            _dashTimeRemaining -= deltaTime;

            // End dash when we either consumed all distance or all time
            if (_distanceTraveled >= _targetDashDistance - 0.001f || _dashTimeRemaining <= 0f)
            {
                EndDash();
            }
        }

        private void EndDash()
        {
            _isDashing = false;
            _dashDirection = Vector3.zero;
            _targetDashDistance = 0f;
            _distanceTraveled = 0f;
        }

        #endregion

        #region Helpers

        private Transform GetCameraTransform()
        {
            // Prefer camera rig if it exposes a transform
            if (_cameraRig != null && _cameraRig.transform != null)
                return _cameraRig.transform;

            if (UnityEngine.Camera.main != null)
                return UnityEngine.Camera.main.transform;

            return null;
        }

        /// <summary>
        /// Compute dash direction from camera-relative move input.
        /// </summary>
        private Vector3 ComputeDashDirection(Vector2 moveInput, Transform cameraTransform)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 rawDir = (camForward * moveInput.y) + (camRight * moveInput.x);

            if (_allowDiagonalDash)
            {
                return rawDir;
            }

            if (rawDir.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            float forwardAmount = Vector3.Dot(rawDir, camForward);
            float rightAmount = Vector3.Dot(rawDir, camRight);

            float absForward = Mathf.Abs(forwardAmount);
            float absRight = Mathf.Abs(rightAmount);

            if (absForward >= absRight)
            {
                // Forward/backward
                return (forwardAmount >= 0f) ? camForward : -camForward;
            }
            else
            {
                // Strafe left/right
                return (rightAmount >= 0f) ? camRight : -camRight;
            }
        }

        /// <summary>
        /// Returns dash distance based on the dash direction (forward/back/strafe),
        /// using camera-forward as classification reference.
        /// </summary>
        private float GetDashDistanceForDirection(Vector3 dashDir, Transform cameraTransform)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            float forwardDot = Vector3.Dot(dashDir, camForward);
            float rightDot = Vector3.Dot(dashDir, camRight);

            float absForward = Mathf.Abs(forwardDot);
            float absRight = Mathf.Abs(rightDot);

            if (absForward >= absRight)
            {
                // Forward/backward
                return (forwardDot >= 0f) ? _forwardDistance : _backwardDistance;
            }
            else
            {
                // Left/right (same distance)
                return _strafeDistance;
            }
        }

        #endregion

        #region Preview / Gizmo Helpers

        /// <summary>
        /// Updates the "preview" dash direction + distance for gizmos
        /// when not currently dashing.
        /// </summary>
        private void UpdatePreviewState()
        {
            _hasValidPreview = false;
            _previewDirection = Vector3.zero;
            _previewDistance = 0f;

            if (!_drawGizmos || !_enabled || _isDashing || _motor == null || _input == null)
                return;

            if (_cooldownTimer > 0f)
                return;

            if (IsBlockedByOtherAbilities)
                return;

            bool grounded = _motor.IsGrounded;
            if (grounded && !_canGroundDash)
                return;
            if (!grounded && !_canAirDash)
                return;

            Vector2 moveInput = _input.MoveAxis;
            if (moveInput.sqrMagnitude < 0.0001f)
                return;

            Transform cameraTransform = GetCameraTransform();
            if (cameraTransform == null)
                return;

            Vector3 dir = ComputeDashDirection(moveInput, cameraTransform);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            dir.Normalize();

            float distance = GetDashDistanceForDirection(dir, cameraTransform);
            if (distance <= 0f)
                return;

            _previewDirection = dir;
            _previewDistance = distance;
            _hasValidPreview = true;
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_drawGizmos)
                return;

            Transform root = _controllerRoot != null ? _controllerRoot.transform : transform;
            if (root == null)
                return;

            Vector3 origin = root.position;
            origin.y += 0.05f; // small offset so lines don't clip into ground

            // Active dash gizmo
            if (_isDashing && _dashDirection.sqrMagnitude > 0.0001f && _targetDashDistance > 0f)
            {
                Gizmos.color = _activeDashColor;

                Vector3 end = origin + _dashDirection.normalized * _targetDashDistance;

                Gizmos.DrawLine(origin, end);
                Gizmos.DrawSphere(end, _gizmoEndSphereRadius);
            }
            // Preview gizmo
            else if (!_isDashing && _hasValidPreview &&
                     _previewDirection.sqrMagnitude > 0.0001f && _previewDistance > 0f)
            {
                Gizmos.color = _previewDashColor;

                Vector3 end = origin + _previewDirection.normalized * _previewDistance;

                Gizmos.DrawLine(origin, end);
                Gizmos.DrawSphere(end, _gizmoEndSphereRadius);
            }
        }
#endif

        #endregion
    }
}