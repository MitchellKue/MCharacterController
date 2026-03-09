// File: Runtime/Camera/FirstPersonCameraRig.cs
// Namespace: Kojiko.MCharacterController.Camera
//
// Summary:
// 1. Implements a simple FPS camera rig using yaw on the character root and pitch on a pivot.
// 2. Reads look input and rotates transforms accordingly with configurable sensitivity and clamping.
// 3. Designed to be attached to the Camera or a pivot under the character hierarchy.
//
// Dependencies:
// - Kojiko.MCharacterController.Camera.CameraRigBase (base class).
// - Kojiko.MCharacterController.Core.CharacterControllerRoot (calls Initialize/HandleLook).
// - A character root transform provided at initialization.

using UnityEngine;

namespace Kojiko.MCharacterController.Camera
{
    /// <summary>
    /// 1. STEP 1: Store references to the character root (yaw) and a pitch pivot.
    // 2. STEP 2: Accumulate and clamp pitch angle, and apply yaw rotation to the character root.
    // 3. STEP 3: Apply the resulting rotations each frame in response to look input.
    /// </summary>
    public class FirstPersonCameraRig : CameraRigBase
    {
        [Header("Transforms")]
        [Tooltip("Transform used for horizontal (yaw) rotation. Typically the character root.")]
        [SerializeField] private Transform _yawRoot;

        [Tooltip("Transform used for vertical (pitch) rotation. Typically a pivot under the camera root.")]
        [SerializeField] private Transform _pitchRoot;

        [Header("Sensitivity")]
        [SerializeField] private float _sensitivityX = 2f;
        [SerializeField] private float _sensitivityY = 2f;

        [Header("Pitch Limits")]
        [SerializeField] private float _minPitch = -80f;
        [SerializeField] private float _maxPitch = 80f;

        // Internal state
        private float _currentPitch;

        /// <inheritdoc />
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

            // STEP 3: Initialize pitch state based on the current pitchRoot rotation.
            Vector3 euler = _pitchRoot.localEulerAngles;
            // Convert Unity's 0-360 representation to -180 to 180.
            _currentPitch = NormalizeAngle(euler.x);
        }

        /// <inheritdoc />
        public override void HandleLook(Vector2 lookAxis, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            // STEP 1: Compute scaled look delta using sensitivity.
            float yawDelta = lookAxis.x * _sensitivityX;
            float pitchDelta = lookAxis.y * _sensitivityY;

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