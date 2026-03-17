// File: Runtime/Abilities/ExternalForcesAbility.cs
// Namespace: Kojiko.MCharacterController.Abilities
//
// Summary:
// - Allows external systems to push or knock back the character via impulses/forces.
// - Integrates with the movement pipeline as an ICharacterAbility.
// - Routes contributions explicitly through CharacterMotor.AddExternal*Velocity.
//
// Usage:
// - Add this component to the GameObject with CharacterControllerRoot / CharacterAbilityController.
// - Ensure CharacterAbilityController discovers it (auto or explicit list).
// - From gameplay code, call AddImpulse(...) or AddForce(..., duration).

using UnityEngine;
using System.Collections.Generic;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    [DisallowMultipleComponent]
    public class MCharacter_Ability_ExternalForces : MonoBehaviour, ICharacterAbility
    {
        [Header("General")]

        [Tooltip("If true, external forces are damped over time instead of staying constant.")]
        [SerializeField]
        private bool _useDamping = true;

        [Tooltip("Damping rate (per second) applied to persistent forces when Use Damping is true.\n" +
                 "Higher values = forces fade out more quickly.")]
        [SerializeField]
        private float _dampingRate = 4f;

        [Tooltip("Optional maximum horizontal speed contributed by external forces (m/s).\n" +
                 "Set to 0 or negative to disable clamping.")]
        [SerializeField]
        private float _maxExternalHorizontalSpeed = 0f;

        [Tooltip("Optional maximum vertical speed contributed by external forces (m/s).\n" +
                 "Set to 0 or negative to disable clamping.")]
        [SerializeField]
        private float _maxExternalVerticalSpeed = 0f;

        [Header("Debug State")]
        [SerializeField] private Vector3 _currentAccumulatedForce;       // world-space horizontal
        [SerializeField] private float _currentVerticalVelocityOffset;   // vertical
        [SerializeField] private int _activeTimedForceCount;

        // --------------------------------------------------------------------
        // Types for queued external contributions
        // --------------------------------------------------------------------

        private struct TimedForce
        {
            public Vector3 Force;      // world-space contribution (m/s)
            public float RemainingTime;
        }

        // Core refs
        private MCharacter_Motor_Controller _motor;
        private MCharacter_Root_Controller _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;

        // Queues/state
        private readonly List<TimedForce> _timedForces = new();
        private Vector3 _frameImpulse;         // one-frame (instant) horizontal component, m/s
        private float _frameVerticalImpulse;   // explicit vertical one-frame addition, m/s

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

            _timedForces.Clear();
            _frameImpulse = Vector3.zero;
            _frameVerticalImpulse = 0f;
            _currentAccumulatedForce = Vector3.zero;
            _currentVerticalVelocityOffset = 0f;
            _activeTimedForceCount = 0;
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (deltaTime <= 0f || _motor == null)
                return;

            // 1. Integrate timed forces over time.
            UpdateTimedForces(deltaTime);

            // 2. Compute total horizontal / vertical external contributions for this frame.
            Vector3 totalHorizontal = _frameImpulse + _currentAccumulatedForce;
            float totalVertical = _frameVerticalImpulse + _currentVerticalVelocityOffset;

            // Optional clamping
            if (_maxExternalHorizontalSpeed > 0f)
            {
                float sqrMax = _maxExternalHorizontalSpeed * _maxExternalHorizontalSpeed;
                if (totalHorizontal.sqrMagnitude > sqrMax)
                {
                    totalHorizontal = totalHorizontal.normalized * _maxExternalHorizontalSpeed;
                }
            }

            if (_maxExternalVerticalSpeed > 0f)
            {
                totalVertical = Mathf.Clamp(
                    totalVertical,
                    -_maxExternalVerticalSpeed,
                    _maxExternalVerticalSpeed
                );
            }

            // 3. Route contributions explicitly through CharacterMotor.

            // Horizontal: only XZ, added as external velocity.
            totalHorizontal.y = 0f;
            if (totalHorizontal.sqrMagnitude > 0.000001f)
            {
                _motor.AddExternalHorizontalVelocity(totalHorizontal);
            }

            // Vertical: added as external vertical velocity.
            if (Mathf.Abs(totalVertical) > 0.0001f)
            {
                _motor.AddExternalVerticalVelocity(totalVertical);
            }

            // 4. Clear one-frame impulses after they're applied.
            _frameImpulse = Vector3.zero;
            _frameVerticalImpulse = 0f;
        }

        public void PostStep(float deltaTime)
        {
            // Not used currently; reserved for post-move behaviour if needed.
        }

        // --------------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------------

        /// <summary>
        /// Adds an instantaneous impulse to the character in world space.
        /// Interpreted as an immediate velocity change (m/s) for this frame.
        /// Use this for one-shot events like explosions or knockback hits.
        /// </summary>
        /// <param name="impulseWorld">World-space impulse (m/s).</param>
        /// <param name="includeVertical">If true, Y component will affect vertical velocity.</param>
        public void AddImpulse(Vector3 impulseWorld, bool includeVertical = true)
        {
            // Horizontal part
            Vector3 horizontal = impulseWorld;
            horizontal.y = 0f;
            _frameImpulse += horizontal;

            // Vertical part
            if (includeVertical)
            {
                _frameVerticalImpulse += impulseWorld.y;
            }
        }

        /// <summary>
        /// Adds a force over time (world space). This is integrated every Tick
        /// for the given duration (seconds), optionally with damping.
        /// Use this for wind, conveyor belts, or persistent knockbacks.
        /// </summary>
        /// <param name="forceWorld">Effective force; treated as velocity contribution (m/s).</param>
        /// <param name="duration">Duration in seconds. If &lt;= 0, this does nothing.</param>
        public void AddForce(Vector3 forceWorld, float duration)
        {
            if (duration <= 0f || forceWorld.sqrMagnitude <= 0.000001f)
                return;

            _timedForces.Add(new TimedForce
            {
                Force = forceWorld,
                RemainingTime = duration
            });

            _activeTimedForceCount = _timedForces.Count;
        }

        /// <summary>
        /// Clears all active timed forces and queued impulses.
        /// </summary>
        public void ClearAllForces()
        {
            _timedForces.Clear();
            _frameImpulse = Vector3.zero;
            _frameVerticalImpulse = 0f;
            _currentAccumulatedForce = Vector3.zero;
            _currentVerticalVelocityOffset = 0f;
            _activeTimedForceCount = 0;
        }

        // --------------------------------------------------------------------
        // Internal helpers
        // --------------------------------------------------------------------

        private void UpdateTimedForces(float deltaTime)
        {
            if (_timedForces.Count == 0)
            {
                _currentAccumulatedForce = Vector3.zero;
                _currentVerticalVelocityOffset = 0f;
                _activeTimedForceCount = 0;
                return;
            }

            Vector3 totalForce = Vector3.zero;

            // Walk the list once; collect active forces and update remaining times.
            for (int i = _timedForces.Count - 1; i >= 0; i--)
            {
                TimedForce tf = _timedForces[i];
                tf.RemainingTime -= deltaTime;

                if (tf.RemainingTime <= 0f)
                {
                    _timedForces.RemoveAt(i);
                    continue;
                }

                // Optionally apply damping each step.
                if (_useDamping && _dampingRate > 0f)
                {
                    float dampFactor = Mathf.Exp(-_dampingRate * deltaTime);
                    tf.Force *= dampFactor;
                }

                _timedForces[i] = tf;
                totalForce += tf.Force;
            }

            _activeTimedForceCount = _timedForces.Count;

            // Interpret totalForce as "extra velocity" for this frame.
            Vector3 horizontal = totalForce;
            horizontal.y = 0f;

            float vertical = totalForce.y;

            _currentAccumulatedForce = horizontal;
            _currentVerticalVelocityOffset = vertical;
        }
    }
}