// File: Runtime/Abilities/Ability_Aiming_FPS.cs
// Namespace: Kojiko.MCharacterController.Abilities
//
// Summary (Part 1 - camera/input side only):
// - Uses ICcInputSource.AimHeld to toggle an aiming state.
// - Smoothly blends camera FOV between hip and ADS via IAimLookRig.
// - Smoothly blends an ADS camera offset via IAimLookRig.
// - Smoothly blends a look-sensitivity multiplier via IAimLookRig.
// - Does NOT touch weapon spread/recoil/etc. (that lives in your weapon/combat systems).

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    [DisallowMultipleComponent]
    public class Ability_FPV_AimDownSight : MonoBehaviour, ICharacterAbility
    {
        [Header("General")]
        [Tooltip("If false, aiming is disabled and we always blend back to hip-fire.")]
        [SerializeField] private bool _enabled = true;

        [Tooltip("Hold-to-aim (true) or toggle-to-aim (false). Currently only Hold uses input; Toggle is for future AimPressed support.")]
        [SerializeField] private bool _holdToAim = true;

        [Header("FOV")]
        [Tooltip("Hip-fire FOV (degrees). If <= 0, use camera rig's BaseFOV as hip-fire.")]
        [SerializeField] private float _hipFireFOV = 0f;

        [Tooltip("ADS FOV (degrees). Smaller = more zoom.")]
        [SerializeField] private float _adsFOV = 55f;

        [Tooltip("Time in seconds to blend between hip and ADS FOV.")]
        [SerializeField] private float _fovBlendTime = 0.15f;

        [Header("Sensitivity")]
        [Tooltip("Hip-fire look sensitivity multiplier (usually 1).")]
        [SerializeField, Range(0.01f, 5f)]
        private float _hipSensitivityMultiplier = 1f;

        [Tooltip("ADS look sensitivity multiplier (usually < 1).")]
        [SerializeField, Range(0.01f, 5f)]
        private float _adsSensitivityMultiplier = 0.6f;

        [Tooltip("Time in seconds to blend sensitivity when entering/leaving ADS.")]
        [SerializeField] private float _sensitivityBlendTime = 0.1f;

        [Header("Camera Offset (Optional)")]
        [Tooltip("Local camera offset to apply in ADS (meaning is up to IAimLookRig implementation).")]
        [SerializeField] private Vector3 _adsCameraOffset = new Vector3(0.05f, -0.02f, 0.06f);

        [Tooltip("Time in seconds to blend camera offset when entering/leaving ADS.")]
        [SerializeField] private float _offsetBlendTime = 0.15f;

        [Header("Debug (Read Only)")]
        [SerializeField, Tooltip("Current aiming intent (input-side).")]
        private bool _isAiming;

        [SerializeField, Tooltip("0 = fully hip, 1 = fully ADS.")]
        private float _aimBlend;

        // Core refs
        private MCharacter_Motor _motor;
        private MCharacter_Controller_Root _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;
        private IAimLookRig _aimLookRig;

        // Cached base FOV from rig
        private float _rigBaseFOV;
        private bool _hasRigBaseFOV;

        // Smooth blend velocities
        private float _aimBlendVel_FOV;
        private float _aimBlendVel_Sens;
        private float _aimBlendVel_Offset;

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

            _aimLookRig = cameraRig as IAimLookRig;

            if (_aimLookRig != null)
            {
                _rigBaseFOV = Mathf.Max(1f, _aimLookRig.BaseFOV);
                _hasRigBaseFOV = true;

                if (_hipFireFOV <= 0f)
                {
                    _hipFireFOV = _rigBaseFOV;
                }
                else
                {
                    _aimLookRig.BaseFOV = _hipFireFOV;
                    _rigBaseFOV = _hipFireFOV;
                }

                // Ensure we start in hip-fire state visually.
                _aimLookRig.SetFOV(_hipFireFOV);
                _aimLookRig.SetAimOffset(Vector3.zero);
                _aimLookRig.SetAimSensitivityMultiplier(_hipSensitivityMultiplier);
            }
            else
            {
                _hasRigBaseFOV = false;
            }

            _isAiming = false;
            _aimBlend = 0f;
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (deltaTime <= 0f)
                return;

            HandleInput();
            UpdateBlending(deltaTime);
        }

        public void PostStep(float deltaTime)
        {
            // No post-step logic needed for aiming (Part 1).
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            if (!_enabled || _input == null)
            {
                _isAiming = false;
                return;
            }

            if (_holdToAim)
            {
                // Simple: hold-to-aim uses AimHeld directly.
                _isAiming = _input.AimHeld;
            }
            else
            {
                // Toggle mode: currently no AimPressed on ICcInputSource.
                // We could implement this later once you add a pressed property.
                // For now, we just treat toggle as hold to keep behaviour predictable.
                _isAiming = _input.AimHeld;
            }
        }

        #endregion

        #region Blending

        private void UpdateBlending(float deltaTime)
        {
            float target = (_enabled && _isAiming) ? 1f : 0f;

            // FOV blend
            if (_aimLookRig != null)
            {
                _aimBlend = SmoothDamp01(_aimBlend, target, ref _aimBlendVel_FOV, _fovBlendTime, deltaTime);

                float hipFOV = (_hipFireFOV > 0f) ? _hipFireFOV :
                    (_hasRigBaseFOV ? _rigBaseFOV : 75f);
                float adsFOV = Mathf.Max(1f, _adsFOV);

                float currentFOV = Mathf.Lerp(hipFOV, adsFOV, _aimBlend);
                _aimLookRig.SetFOV(currentFOV);
            }
            else
            {
                _aimBlend = SmoothDamp01(_aimBlend, target, ref _aimBlendVel_FOV, _fovBlendTime, deltaTime);
            }

            // Sensitivity blend
            if (_aimLookRig != null)
            {
                float sensBlend = SmoothDamp01(_aimBlend, target, ref _aimBlendVel_Sens, _sensitivityBlendTime, deltaTime);
                float currentSens = Mathf.Lerp(_hipSensitivityMultiplier, _adsSensitivityMultiplier, sensBlend);
                _aimLookRig.SetAimSensitivityMultiplier(currentSens);
            }

            // Offset blend
            if (_aimLookRig != null)
            {
                float offsetBlend = SmoothDamp01(_aimBlend, target, ref _aimBlendVel_Offset, _offsetBlendTime, deltaTime);
                Vector3 offset = Vector3.Lerp(Vector3.zero, _adsCameraOffset, offsetBlend);
                _aimLookRig.SetAimOffset(offset);
            }
        }

        private static float SmoothDamp01(float current, float target, ref float velocity, float smoothTime, float deltaTime)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);

            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

            float change = current - target;
            float temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;

            float output = target + (change + temp) * exp;
            return Mathf.Clamp01(output);
        }

        #endregion

        #region Public API (for weapons / buffs / higher systems)

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled)
                _isAiming = false;
        }

        /// <summary>
        /// Force aim state from outside (e.g., scripted zoom).
        /// </summary>
        public void SetAiming(bool aiming)
        {
            if (!_enabled && aiming)
                return;

            _isAiming = aiming;
        }

        /// <summary>
        /// Configure hip/ADS FOV at runtime (e.g., per-weapon zoom).
        /// </summary>
        public void SetFOVSettings(float hipFOV, float adsFOV)
        {
            _hipFireFOV = Mathf.Max(1f, hipFOV);
            _adsFOV = Mathf.Max(1f, adsFOV);

            if (_aimLookRig != null)
            {
                _aimLookRig.BaseFOV = _hipFireFOV;
                _rigBaseFOV = _hipFireFOV;
            }
        }

        /// <summary>
        /// Configure hip + ADS sensitivity multipliers at runtime.
        /// </summary>
        public void SetSensitivitySettings(float hipMultiplier, float adsMultiplier)
        {
            _hipSensitivityMultiplier = Mathf.Clamp(hipMultiplier, 0.01f, 5f);
            _adsSensitivityMultiplier = Mathf.Clamp(adsMultiplier, 0.01f, 5f);
        }

        /// <summary>
        /// Configure ADS camera offset at runtime (per-weapon offsets, buffs, etc.).
        /// </summary>
        public void SetADSOffset(Vector3 offset)
        {
            _adsCameraOffset = offset;
        }

        /// <summary>
        /// Current aiming (input-side) flag.
        /// </summary>
        public bool IsAiming => _isAiming;

        /// <summary>
        /// Blend factor for smooth transitions: 0 = hip, 1 = ADS.
        /// </summary>
        public float AimBlend => _aimBlend;

        #endregion
    }
}