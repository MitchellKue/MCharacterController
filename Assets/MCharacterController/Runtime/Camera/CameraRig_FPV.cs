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

using UnityEngine;

namespace Kojiko.MCharacterController.Camera
{
    /// <summary>
    /// 1. STEP 1: Store references to the character root (yaw) and a pitch pivot.
    /// 2. STEP 2: Accumulate and clamp pitch angle, and apply yaw rotation to the character root.
    /// 3. STEP 3: Apply the resulting rotations each frame in response to look input.
    /// 4. STEP 4: Implement IAimLookRig for ADS control (FOV, offset, sensitivity multiplier).
    /// </summary>
    public class CameraRig_FPV : CameraRig_Base, IAimLookRig
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

        // Internal state
        private float _currentPitch;

        // NEW: cached base local position of the camera, for additive offset
        private Vector3 _baseCameraLocalPosition;
        private bool _hasBaseCameraLocalPosition;

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

                // NEW: cache base camera local position for additive offsets
                _baseCameraLocalPosition = _camera.transform.localPosition;
                _hasBaseCameraLocalPosition = true;
            }
            else
            {
                _hasBaseCameraLocalPosition = false;
            }

            // STEP 5: Initialize pitch state based on the current pitchRoot rotation.
            Vector3 euler = _pitchRoot.localEulerAngles;
            // Convert Unity's 0-360 representation to -180 to 180.
            _currentPitch = NormalizeAngle(euler.x);
        }

        public override void HandleLook(Vector2 lookAxis, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            // STEP 1: Compute scaled look delta using sensitivity + aim multiplier.
            float yawDelta = lookAxis.x * _sensitivityX * _aimSensitivityMultiplier;
            float pitchDelta = lookAxis.y * _sensitivityY * _aimSensitivityMultiplier;

            // STEP 2: Apply yaw rotation to the yaw root (character body).
            if (_yawRoot != null && Mathf.Abs(yawDelta) > Mathf.Epsilon)
            {
                _yawRoot.Rotate(Vector3.up, yawDelta, Space.World);
            }

            // STEP 3: Accumulate and clamp pitch rotation on the pitch root.
            if (_pitchRoot != null && Mathf.Abs(pitchDelta) > Mathf.Epsilon)
            {
                _currentPitch -= pitchDelta; // invert so moving mouse up looks up.
                _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);

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

        /// <summary>
        /// Converts an angle from [0, 360) range to [-180, 180) range.
        /// </summary>
        private static float NormalizeAngle(float angle)
        {
            // STEP 1: Ensure angle is within 0-360.
            while (angle > 360f) angle -= 360f;
            while (angle < 0f) angle += 360f;

            // STEP 2: Convert higher half to negative equivalent.
            if (angle > 180f)
            {
                angle -= 360f;
            }

            // STEP 3: Return the normalized angle in -180 to 180 range.
            return angle;
        }
    }
}