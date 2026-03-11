// File: Runtime/Camera/FirstPersonCameraRig.cs
// Namespace: Kojiko.MCharacterController.Camera
//
// Summary:
// 1. Implements a simple FPS camera rig using yaw on the character root and pitch on a pivot.
// 2. Reads look input and rotates transforms accordingly with configurable sensitivity and clamping.
// 3. Implements IAimLookRig so abilities (e.g., aiming) can:
//    - Drive FOV (BaseFOV + SetFOV)
//    - Apply a *relative* (additive) local aim offset
//    - Apply an aim sensitivity multiplier.
// 4. Implements IClimbLookRig so abilities (e.g., climbing) can:
//    - Clamp yaw around a surface forward direction while climbing.
//    - Override pitch limits while climbing.

using UnityEngine;

namespace Kojiko.MCharacterController.Camera
{
    /// <summary>
    /// 1. STEP 1: Store references to the character root (yaw) and a pitch pivot.
    /// 2. STEP 2: Accumulate and clamp pitch angle, and apply yaw rotation to the character root.
    /// 3. STEP 3: Apply the resulting rotations each frame in response to look input.
    /// 4. STEP 4: Implement IAimLookRig for ADS control (FOV, offset, sensitivity multiplier).
    /// 5. STEP 5: Implement IClimbLookRig for climb-time yaw/pitch clamping.
    /// </summary>
    public class CameraRig_FPV : CameraRig_Base, IAimLookRig, IClimbLookRig
    {
        [Header("Transforms")]
        [Tooltip("Transform used for horizontal (yaw) rotation. Typically the character root.")]
        [SerializeField] private Transform _yawRoot;

        [Tooltip("Transform used for vertical (pitch) rotation. Typically a pivot under the camera root.")]
        [SerializeField] private Transform _pitchRoot;

        [Tooltip("Camera transform we want to manipulate for FOV and local offset.")]
        [SerializeField] private UnityEngine.Camera _camera;

        [Header("Sensitivity")]
        [SerializeField] private float _sensitivityX = 2f;
        [SerializeField] private float _sensitivityY = 2f;

        [Tooltip("Current multiplier applied to sensitivity (e.g., for ADS).")]
        [SerializeField] private float _aimSensitivityMultiplier = 1f;

        [Header("Pitch Limits")]
        [SerializeField] private float _minPitch = -80f;
        [SerializeField] private float _maxPitch = 80f;

        [Header("FOV")]
        [Tooltip("Base (hip-fire) FOV. If <= 0 at runtime, we'll grab from the camera.")]
        [SerializeField] private float _baseFOV = 0f;

        [Header("Aim Offset")]
        [Tooltip("Current local offset applied by abilities (e.g., aiming). This is *additive* on top of the camera's base local position.")]
        [SerializeField] private Vector3 _aimOffset;

        // Internal look state
        private float _currentPitch;

        // NEW: cached base local position of the camera, for additive offset
        private Vector3 _baseCameraLocalPosition;
        private bool _hasBaseCameraLocalPosition;

        // NEW: yaw state so we can clamp around climb surfaces
        private float _currentYaw;

        // ----------------------------
        // IAimLookRig implementation
        // ----------------------------

        /// <summary>
        /// IAimLookRig implementation: Base FOV property.
        /// </summary>
        public float BaseFOV
        {
            get => _baseFOV;
            set
            {
                _baseFOV = Mathf.Max(1f, value);
                // We don't automatically force the camera to this FOV here;
                // Ability_Aiming_FPS or calling code should drive SetFOV.
            }
        }

        // ----------------------------
        // IClimbLookRig implementation
        // ----------------------------

        [Header("Climb Constraints (Runtime Debug)")]
        [SerializeField, Tooltip("True while climb constraints are active.")]
        private bool _climbConstraintsActive;

        [SerializeField, Tooltip("Surface forward direction used while climbing (debug).")]
        private Vector3 _climbSurfaceForward = Vector3.forward;

        [SerializeField, Tooltip("Max yaw offset from surface forward (degrees) while climbing.")]
        private float _climbMaxYawFromSurface = 45f;

        [SerializeField, Tooltip("Minimum pitch (degrees) while climbing.")]
        private float _climbMinPitch = -60f;

        [SerializeField, Tooltip("Maximum pitch (degrees) while climbing.")]
        private float _climbMaxPitch = 60f;

        public bool ClimbConstraintsActive => _climbConstraintsActive;

        public void SetClimbConstraints(
            bool enabled,
            Vector3 surfaceForward,
            float maxYawFromSurface,
            float minPitch,
            float maxPitch)
        {
            _climbConstraintsActive = enabled;

            if (!enabled)
            {
                // When disabling, we simply resume using the normal pitch limits.
                return;
            }

            // Normalize surface forward on XZ.
            surfaceForward.y = 0f;
            if (surfaceForward.sqrMagnitude < 0.0001f)
            {
                surfaceForward = Vector3.forward;
            }

            _climbSurfaceForward = surfaceForward.normalized;
            _climbMaxYawFromSurface = Mathf.Max(0f, maxYawFromSurface);
            _climbMinPitch = minPitch;
            _climbMaxPitch = maxPitch;

            // Optionally: snap yaw toward surface forward when enabling.
            if (_yawRoot != null)
            {
                // Compute yaw of surface forward.
                float surfaceYaw = Mathf.Atan2(_climbSurfaceForward.x, _climbSurfaceForward.z) * Mathf.Rad2Deg;

                // Get current yaw from yawRoot.
                Vector3 yawEuler = _yawRoot.eulerAngles;
                _currentYaw = NormalizeAngle(yawEuler.y);

                // Clamp yaw immediately within new bounds.
                float minYaw = surfaceYaw - _climbMaxYawFromSurface;
                float maxYaw = surfaceYaw + _climbMaxYawFromSurface;

                _currentYaw = Mathf.Clamp(_currentYaw, minYaw, maxYaw);
                yawEuler.y = _currentYaw;
                _yawRoot.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            }
        }

        // ----------------------------
        // CameraRig_Base
        // ----------------------------

        public override void Initialize(Transform characterRoot)
        {
            // STEP 1: If yawRoot is not explicitly assigned, default to the provided character root.
            if (_yawRoot == null)
            {
                _yawRoot = characterRoot;
            }

            // STEP 2: If pitchRoot is not assigned, default to this GameObject's transform.
            if (_pitchRoot == null)
            {
                _pitchRoot = transform;
            }

            // STEP 3: Grab camera reference if not assigned.
            if (_camera == null)
            {
                _camera = GetComponentInChildren<UnityEngine.Camera>();
            }

            // STEP 4: Initialize base FOV.
            if (_camera != null)
            {
                if (_baseFOV <= 0f)
                {
                    _baseFOV = _camera.fieldOfView;
                }

                // Cache base camera local position for additive offsets
                _baseCameraLocalPosition = _camera.transform.localPosition;
                _hasBaseCameraLocalPosition = true;
            }
            else
            {
                _hasBaseCameraLocalPosition = false;
            }

            // STEP 5: Initialize pitch state based on the current pitchRoot rotation.
            Vector3 pitchEuler = _pitchRoot.localEulerAngles;
            _currentPitch = NormalizeAngle(pitchEuler.x);

            // STEP 6: Initialize yaw state based on current yawRoot rotation.
            if (_yawRoot != null)
            {
                Vector3 yawEuler = _yawRoot.eulerAngles;
                _currentYaw = NormalizeAngle(yawEuler.y);
            }
            else
            {
                _currentYaw = 0f;
            }

            // Initially no climb constraints.
            _climbConstraintsActive = false;
        }

        public override void HandleLook(Vector2 lookAxis, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            // STEP 1: Compute scaled look delta using sensitivity + aim multiplier.
            float yawDelta = lookAxis.x * _sensitivityX * _aimSensitivityMultiplier;
            float pitchDelta = lookAxis.y * _sensitivityY * _aimSensitivityMultiplier;

            // --------------------
            // Yaw handling
            // --------------------
            if (_yawRoot != null && Mathf.Abs(yawDelta) > Mathf.Epsilon)
            {
                // Accumulate yaw
                _currentYaw += yawDelta;

                if (_climbConstraintsActive)
                {
                    // Compute surface yaw
                    float surfaceYaw = Mathf.Atan2(_climbSurfaceForward.x, _climbSurfaceForward.z) * Mathf.Rad2Deg;
                    float minYaw = surfaceYaw - _climbMaxYawFromSurface;
                    float maxYaw = surfaceYaw + _climbMaxYawFromSurface;

                    _currentYaw = Mathf.Clamp(_currentYaw, minYaw, maxYaw);
                }

                // Apply yaw only around global up
                _yawRoot.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            }

            // --------------------
            // Pitch handling
            // --------------------
            if (_pitchRoot != null && Mathf.Abs(pitchDelta) > Mathf.Epsilon)
            {
                // Invert so moving mouse up looks up.
                _currentPitch -= pitchDelta;

                // Choose which pitch limits to use.
                float minPitch = _climbConstraintsActive ? _climbMinPitch : _minPitch;
                float maxPitch = _climbConstraintsActive ? _climbMaxPitch : _maxPitch;

                _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

                Vector3 euler = _pitchRoot.localEulerAngles;
                euler.x = _currentPitch;
                _pitchRoot.localEulerAngles = euler;
            }

            // STEP 4: Apply aim offset (local) to the camera if present, ADDITIVELY.
            if (_camera != null && _hasBaseCameraLocalPosition)
            {
                _camera.transform.localPosition = _baseCameraLocalPosition + _aimOffset;
            }
        }

        // ----------------------------
        // IAimLookRig
        // ----------------------------

        /// <summary>
        /// IAimLookRig: set the current field of view (in degrees).
        /// </summary>
        public void SetFOV(float fov)
        {
            if (_camera == null)
                return;

            _camera.fieldOfView = Mathf.Max(1f, fov);
        }

        /// <summary>
        /// IAimLookRig: set the local aim offset (additive on top of base local position).
        /// </summary>
        public void SetAimOffset(Vector3 offset)
        {
            _aimOffset = offset;
        }

        /// <summary>
        /// IAimLookRig: set the current sensitivity multiplier (e.g., when ADS).
        /// </summary>
        public void SetAimSensitivityMultiplier(float multiplier)
        {
            _aimSensitivityMultiplier = Mathf.Max(0.01f, multiplier);
        }

        // ----------------------------
        // Helpers
        // ----------------------------

        /// <summary>
        /// Converts an angle from [0, 360) range to [-180, 180) range.
        /// </summary>
        private static float NormalizeAngle(float angle)
        {
            while (angle > 360f) angle -= 360f;
            while (angle < 0f) angle += 360f;

            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }
    }
}