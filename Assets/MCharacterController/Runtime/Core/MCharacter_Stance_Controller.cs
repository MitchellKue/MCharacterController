// File: Runtime/Core/MCharacter_Stance_Controller.cs
// Namespace: Kojiko.MCharacterController.Core
//
// Summary:
// 1. Manages character stance state (Standing, Crouching, Prone, ...).
// 2. Adjusts CharacterController height/center & motor SpeedMultiplier.
// 3. Adjusts a visual body mesh (scale + vertical offset) per stance.
// 4. Adjusts a camera pivot (local Y offset) per stance.
// 5. Supports stance cycling (e.g., single button Standing -> Crouching -> Prone -> ...).
// 6. Raises an event whenever the stance successfully changes (for SFX, animations, etc.).
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kojiko.MCharacterController.Core
{
    /// <summary>
    /// Enumerates supported character stances.
    /// Standing, Crouching, and Prone are provided by default.
    /// </summary>
    public enum CharacterStance
    {
        Standing = 0,
        Crouching = 10,
        Prone = 20,
    }

    /// <summary>
    /// Core stance controller:
    /// 1. Owns the current stance (Standing / Crouching / Prone / ...).
    /// 2. Updates the CharacterController collider & motor SpeedMultiplier.
    /// 3. Updates a visual body mesh & a camera pivot per stance.
    /// 4. Provides cycling helpers for simple single-button stance progression.
    /// 5. Fires an event whenever the stance changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class MCharacter_Stance_Controller : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Nested types
        // --------------------------------------------------------------------

        /// <summary>
        /// Settings for a single stance:
        /// - Capsule height + center for CharacterController
        /// - Speed multiplier for motor
        /// - Visual body mesh: scale + local Y offset
        /// - Camera pivot: local Y offset
        /// </summary>
        [Serializable]
        public struct StanceSettings
        {
            [Header("Collider")]
            [Tooltip("Capsule height for this stance.")]
            public float Height;

            [Tooltip("Local center of the CharacterController for this stance.")]
            public Vector3 Center;

            [Header("Movement")]
            [Tooltip("Multiplier applied to the motor's SpeedMultiplier for this stance.")]
            public float SpeedMultiplier;

            [Header("Body Visual")]
            [Tooltip("Local scale applied to the body visual in this stance.")]
            public Vector3 BodyScale;

            [Tooltip("Additional offset (in meters) added to the body visual's original local Y position.")]
            public float BodyYOffset;

            [Header("Camera Pivot")]
            [Tooltip("Additional offset (in meters) added to the camera pivot's original local Y position.")]
            public float CameraPivotYOffset;
        }

        /// <summary>
        /// Event signature for stance changes.
        /// oldStance: stance we were in.
        /// newStance: stance we just switched to.
        /// </summary>
        public event Action<CharacterStance, CharacterStance> OnStanceChanged;

        // --------------------------------------------------------------------
        // Configuration
        // --------------------------------------------------------------------

        [Header("References")]
        [Tooltip("Reference to the character motor/controller that exposes SpeedMultiplier.")]
        [SerializeField] private MCharacter_Motor_Controller _motor;

        [Tooltip("Optional visual body mesh / capsule object to scale and move per stance.")]
        [SerializeField] private Transform _bodyVisual;

        [Tooltip("Optional camera pivot transform to move per stance (local Y offset).")]
        [SerializeField] private Transform _cameraPivot;

        [Header("Standing Settings")]
        [SerializeField]
        private StanceSettings _standingSettings = new StanceSettings
        {
            Height = 1.8f,
            Center = new Vector3(0f, 0.9f, 0f),
            SpeedMultiplier = 1f,
            BodyScale = Vector3.one,
            BodyYOffset = 0f,
            CameraPivotYOffset = 0f,
        };

        [Header("Crouching Settings")]
        [SerializeField]
        private StanceSettings _crouchingSettings = new StanceSettings
        {
            Height = 1.2f,
            Center = new Vector3(0f, 0.6f, 0f),
            SpeedMultiplier = 0.6f,
            BodyScale = new Vector3(1f, 0.6f, 1f),
            BodyYOffset = -0.15f,
            CameraPivotYOffset = -0.4f,
        };

        [Header("Prone Settings")]
        [SerializeField]
        private StanceSettings _proneSettings = new StanceSettings
        {
            Height = 0.6f,
            Center = new Vector3(0f, 0.3f, 0f),
            SpeedMultiplier = 0.4f,
            BodyScale = new Vector3(1f, 0.3f, 1f),
            BodyYOffset = -0.3f,
            CameraPivotYOffset = -0.7f,
        };

        [Header("Ceiling Check")]
        [Tooltip("Radius of the sphere used when checking if the character can stand up.")]
        [SerializeField] private float _headClearanceCheckRadius = 0.25f;

        [Tooltip("Layer mask used for stance ceiling checks. Default: collides with everything.")]
        [SerializeField] private LayerMask _ceilingCheckLayers = ~0;

        [Header("Stance Cycling")]
        [Tooltip("Ordered list of stances cycled through when using CycleToNext/Previous.\n" +
                 "For example: Standing -> Crouching -> Prone -> (wraps back).")]
        [SerializeField]
        private List<CharacterStance> _cycleOrder = new List<CharacterStance>
        {
            CharacterStance.Standing,
            CharacterStance.Crouching,
            CharacterStance.Prone
        };

        // --------------------------------------------------------------------
        // Runtime state
        // --------------------------------------------------------------------

        [Header("Debug / State")]
        [SerializeField] private CharacterStance _currentStance = CharacterStance.Standing;
        public CharacterStance CurrentStance => _currentStance;

        public bool IsStanding => _currentStance == CharacterStance.Standing;
        public bool IsCrouching => _currentStance == CharacterStance.Crouching;
        public bool IsProne => _currentStance == CharacterStance.Prone;

        private CharacterController _characterController;

        // Body visual original transform (used as baseline for per-stance offsets).
        private bool _bodyOriginalCached;
        private Vector3 _bodyOriginalLocalPos;
        private Vector3 _bodyOriginalLocalScale;

        // Camera pivot original transform (used as baseline for per-stance offsets).
        private bool _cameraPivotOriginalCached;
        private Vector3 _cameraPivotOriginalLocalPos;

        // ADDED: expose current index in cycle and total count, for external logic
        public int StanceIndex { get; private set; } = 0;          // ADDED
        public int StanceCount => _cycleOrder?.Count ?? 0;         // ADDED

        // --------------------------------------------------------------------
        // Unity lifecycle
        // --------------------------------------------------------------------

        private void Awake()
        {
            // 1. Cache references.
            _characterController = GetComponent<CharacterController>();
            if (_motor == null)
                _motor = GetComponent<MCharacter_Motor_Controller>();

            CacheBodyOriginalTransform();
            CacheCameraPivotOriginalTransform();
            EnsureValidCycleOrder();

            // 2. Apply initial stance configuration.
            ApplyStanceSettingsImmediate(_currentStance);
        }

        private void CacheBodyOriginalTransform()
        {
            if (_bodyVisual == null || _bodyOriginalCached)
                return;

            _bodyOriginalLocalPos = _bodyVisual.localPosition;
            _bodyOriginalLocalScale = _bodyVisual.localScale;
            _bodyOriginalCached = true;

            // If Standing BodyScale is identity but the mesh is not, assume current is "standing".
            if (_standingSettings.BodyScale == Vector3.one && _bodyOriginalLocalScale != Vector3.one)
            {
                _standingSettings.BodyScale = _bodyOriginalLocalScale;
            }
        }

        private void CacheCameraPivotOriginalTransform()
        {
            if (_cameraPivot == null || _cameraPivotOriginalCached)
                return;

            _cameraPivotOriginalLocalPos = _cameraPivot.localPosition;
            _cameraPivotOriginalCached = true;
        }

        // --------------------------------------------------------------------
        // Public API - Direct stance control
        // --------------------------------------------------------------------

        /// <summary>
        /// Attempts to switch to the requested stance.
        /// Returns true on success, false if the change is blocked (e.g., cannot stand).
        /// On success, applies stance settings and fires OnStanceChanged(old, new).
        /// </summary>
        public bool TrySetStance(CharacterStance newStance)
        {
            if (newStance == _currentStance)
                return true;

            if (IsTallerThanCurrent(newStance) && !CanStandUpTo(newStance))
            {
                return false;
            }

            var oldStance = _currentStance;
            _currentStance = newStance;

            // ADDED: keep StanceIndex in sync with _currentStance
            if (_cycleOrder != null && _cycleOrder.Count > 0)            // ADDED
            {                                                             // ADDED
                int idx = _cycleOrder.IndexOf(_currentStance);           // ADDED
                if (idx >= 0)                                            // ADDED
                    StanceIndex = idx;                                   // ADDED
            }                                                             // ADDED

            ApplyStanceSettingsImmediate(_currentStance);

            // Fire event after successful change.
            OnStanceChanged?.Invoke(oldStance, _currentStance);

            return true;
        }

        // --------------------------------------------------------------------
        // Public API - Cycling helpers (for single-button stance input)
        // --------------------------------------------------------------------

        public void CycleToNextStance()
        {
            if (_cycleOrder == null || _cycleOrder.Count == 0)
                return;

            int currentIndex = _cycleOrder.IndexOf(_currentStance);
            if (currentIndex < 0)
                currentIndex = 0;

            int nextIndex = (currentIndex + 1) % _cycleOrder.Count;
            CharacterStance nextStance = _cycleOrder[nextIndex];

            TrySetStance(nextStance);
        }

        public void CycleToPreviousStance()
        {
            if (_cycleOrder == null || _cycleOrder.Count == 0)
                return;

            int currentIndex = _cycleOrder.IndexOf(_currentStance);
            if (currentIndex < 0)
                currentIndex = 0;

            int prevIndex = (currentIndex - 1 + _cycleOrder.Count) % _cycleOrder.Count;
            CharacterStance prevStance = _cycleOrder[prevIndex];

            TrySetStance(prevStance);
        }

        // --------------------------------------------------------------------
        // Public API - Index-based helpers (ADDED)
        // --------------------------------------------------------------------

        /// <summary>
        /// Returns the stance at the given index in the cycle order.
        /// Safe to call only if 0 <= index &lt; StanceCount.
        /// </summary>
        public CharacterStance GetStanceByIndex(int index)              // ADDED
        {                                                               // ADDED
            return _cycleOrder[index];                                  // ADDED
        }                                                               // ADDED

        /// <summary>
        /// Convenience: minimum-index stance (usually Standing).
        /// </summary>
        public CharacterStance MinStance =>                             // ADDED
            StanceCount > 0 ? _cycleOrder[0] : _currentStance;          // ADDED

        /// <summary>
        /// Convenience: maximum-index stance (usually Prone).
        /// </summary>
        public CharacterStance MaxStance =>                             // ADDED
            StanceCount > 0 ? _cycleOrder[StanceCount - 1] : _currentStance; // ADDED

        // --------------------------------------------------------------------
        // Internal helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Applies stance settings to:
        /// 1. CharacterController (height + center)
        /// 2. Motor (speed multiplier)
        /// 3. Body visual (scale + Y offset)
        /// 4. Camera pivot (Y offset)
        /// </summary>
        private void ApplyStanceSettingsImmediate(CharacterStance stance)
        {
            StanceSettings settings = GetSettingsForStance(stance);

            // 1. CharacterController
            if (_characterController != null)
            {
                bool wasEnabled = _characterController.enabled;
                _characterController.enabled = false;

                _characterController.height = settings.Height;
                _characterController.center = settings.Center;

                _characterController.enabled = wasEnabled;
            }

            // 2. Motor speed multiplier.
            if (_motor != null)
            {
                _motor.SpeedMultiplier = settings.SpeedMultiplier;
            }

            // 3. Body visual.
            if (_bodyVisual != null)
            {
                CacheBodyOriginalTransform();

                _bodyVisual.localScale = settings.BodyScale;

                Vector3 pos = _bodyOriginalLocalPos;
                pos.y += settings.BodyYOffset;
                _bodyVisual.localPosition = pos;
            }

            // 4. Camera pivot.
            if (_cameraPivot != null)
            {
                CacheCameraPivotOriginalTransform();

                Vector3 cpPos = _cameraPivotOriginalLocalPos;
                cpPos.y += settings.CameraPivotYOffset;
                _cameraPivot.localPosition = cpPos;
            }
        }

        private StanceSettings GetSettingsForStance(CharacterStance stance)
        {
            switch (stance)
            {
                case CharacterStance.Crouching:
                    return _crouchingSettings;

                case CharacterStance.Prone:
                    return _proneSettings;

                case CharacterStance.Standing:
                default:
                    return _standingSettings;
            }
        }

        private bool IsTallerThanCurrent(CharacterStance target)
        {
            float currentHeight = GetSettingsForStance(_currentStance).Height;
            float targetHeight = GetSettingsForStance(target).Height;
            return targetHeight > currentHeight + 0.001f;
        }

        private bool CanStandUpTo(CharacterStance targetStance)
        {
            if (_characterController == null)
                return true;

            StanceSettings target = GetSettingsForStance(targetStance);

            float currentHeight = _characterController.height;
            float desiredHeight = target.Height;
            float castDistance = desiredHeight - currentHeight;

            if (castDistance <= 0f)
                return true;

            Vector3 origin = transform.position + target.Center;

            if (Physics.SphereCast(
                    origin,
                    _headClearanceCheckRadius,
                    Vector3.up,
                    out RaycastHit hit,
                    castDistance,
                    _ceilingCheckLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return true;
        }

        private void EnsureValidCycleOrder()
        {
            if (_cycleOrder == null || _cycleOrder.Count == 0)
            {
                _cycleOrder = new List<CharacterStance>
                {
                    CharacterStance.Standing,
                    CharacterStance.Crouching,
                    CharacterStance.Prone
                };
            }

            var seen = new HashSet<CharacterStance>();
            for (int i = _cycleOrder.Count - 1; i >= 0; i--)
            {
                if (seen.Contains(_cycleOrder[i]))
                {
                    _cycleOrder.RemoveAt(i);
                }
                else
                {
                    seen.Add(_cycleOrder[i]);
                }
            }

            if (!_cycleOrder.Contains(_currentStance))
            {
                _cycleOrder.Insert(0, _currentStance);
            }

            // ADDED: initialize StanceIndex from current stance
            int idx = _cycleOrder.IndexOf(_currentStance);   // ADDED
            if (idx >= 0)                                     // ADDED
                StanceIndex = idx;                            // ADDED
            else                                              // ADDED
                StanceIndex = 0;                              // ADDED
        }
    }
}