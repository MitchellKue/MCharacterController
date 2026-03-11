// File: Runtime/Environment/SimpleClimbPair.cs
// Namespace: Kojiko.MCharacterController.Environment

using System.Collections.Generic;
using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Abilities;

namespace Kojiko.MCharacterController.Environment
{
    /// <summary>
    /// Extremely simple climb/ladder behavior:
    /// - Two trigger colliders: bottomTrigger, topTrigger.
    /// - Two teleport points: bottomTeleportPoint, topTeleportPoint.
    /// - When the player is inside one of the triggers and presses Interact
    ///   (via Ability_Climb in SimpleTeleport mode), they are teleported
    ///   to the opposite end (position + facing).
    ///
    /// This is purely visual / positional: no climbing animation logic here.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleClimbPair : MonoBehaviour
    {
        [Header("Triggers (must be trigger colliders)")]
        [SerializeField] private Collider _bottomTrigger;
        [SerializeField] private Collider _topTrigger;

        [Header("Teleport Points")]
        [Tooltip("Destination when coming from the top trigger.")]
        [SerializeField] private Transform _bottomTeleportPoint;

        [Tooltip("Destination when coming from the bottom trigger.")]
        [SerializeField] private Transform _topTeleportPoint;

        [Header("Timing")]
        [Tooltip("Optional delay before teleporting after Interact is pressed (seconds).")]
        [SerializeField] private float _climbDelaySeconds = 0.0f;

        // Track which motors are currently inside which trigger.
        private readonly Dictionary<MCharacter_Motor, Collider> _motorsInTrigger =
            new Dictionary<MCharacter_Motor, Collider>();

        private void Reset()
        {
            // Try to auto-assign triggers from children on reset.
            Collider[] childColliders = GetComponentsInChildren<Collider>();
            foreach (var col in childColliders)
            {
                if (!col.isTrigger)
                    continue;

                if (_bottomTrigger == null)
                    _bottomTrigger = col;
                else if (_topTrigger == null && col != _bottomTrigger)
                    _topTrigger = col;
            }
        }

        private void OnValidate()
        {
            if (_climbDelaySeconds < 0f)
                _climbDelaySeconds = 0f;
        }

        /// <summary>
        /// Called by a relay when something enters one of the triggers.
        /// </summary>
        private void HandleTriggerEnter(Collider trigger, Collider other)
        {
            var motor = other.GetComponentInParent<MCharacter_Motor>();
            if (motor == null)
                return;

            // REQUIRE Ability_Climb to be present and allow simple climb volumes
            var abilityClimb = other.GetComponentInParent<Ability_Climb>();
            if (abilityClimb == null || !abilityClimb.AllowSimpleClimbVolumes)
                return;

            // Register that this motor is inside this trigger.
            _motorsInTrigger[motor] = trigger;
        }

        /// <summary>
        /// Called by a relay when something exits one of the triggers.
        /// </summary>
        private void HandleTriggerExit(Collider trigger, Collider other)
        {
            var motor = other.GetComponentInParent<MCharacter_Motor>();
            if (motor == null)
                return;

            if (_motorsInTrigger.TryGetValue(motor, out var storedTrigger))
            {
                if (storedTrigger == trigger)
                {
                    _motorsInTrigger.Remove(motor);
                }
            }
        }

        /// <summary>
        /// Called (e.g. by Ability_Climb in SimpleTeleport mode) when the player
        /// presses Interact. If the motor is currently in one of this pair's
        /// triggers, perform the appropriate teleport.
        /// </summary>
        /// <returns>True if a teleport was performed or scheduled.</returns>
        public bool TryUseSimpleClimb(MCharacter_Motor motor)
        {
            if (motor == null)
                return false;

            if (!_motorsInTrigger.TryGetValue(motor, out var trigger))
                return false;

            Transform destination = null;
            if (trigger == _bottomTrigger)
            {
                destination = _topTeleportPoint;
            }
            else if (trigger == _topTrigger)
            {
                destination = _bottomTeleportPoint;
            }

            if (destination == null)
                return false;

            if (_climbDelaySeconds <= 0f)
            {
                // Instant teleport
                motor.TeleportToPoint(destination);
            }
            else
            {
                // Optional small delay (no need to track/cancel like before,
                // because Interact is a one-shot action).
                StartCoroutine(DelayedTeleport(motor, destination, _climbDelaySeconds));
            }

            return true;
        }

        private System.Collections.IEnumerator DelayedTeleport(
            MCharacter_Motor motor,
            Transform destination,
            float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                t += Time.deltaTime;

                if (motor == null || destination == null)
                    yield break;

                yield return null;
            }

            if (motor != null && destination != null)
            {
                motor.TeleportToPoint(destination);
            }
        }

        #region Trigger Relays

        public void BottomTriggerEnter(Collider trigger, Collider other)
        {
            if (trigger == _bottomTrigger)
                HandleTriggerEnter(trigger, other);
        }

        public void BottomTriggerExit(Collider trigger, Collider other)
        {
            if (trigger == _bottomTrigger)
                HandleTriggerExit(trigger, other);
        }

        public void TopTriggerEnter(Collider trigger, Collider other)
        {
            if (trigger == _topTrigger)
                HandleTriggerEnter(trigger, other);
        }

        public void TopTriggerExit(Collider trigger, Collider other)
        {
            if (trigger == _topTrigger)
                HandleTriggerExit(trigger, other);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color bottomColor = new Color(0.2f, 0.8f, 1f, 0.8f); // cyan-ish
            Color topColor = new Color(1f, 0.8f, 0.2f, 0.8f);    // orange-ish

            // Bottom teleport
            if (_bottomTeleportPoint != null)
            {
                Gizmos.color = bottomColor;
                Gizmos.DrawSphere(_bottomTeleportPoint.position, 0.08f);
                DrawArrow(_bottomTeleportPoint.position, _bottomTeleportPoint.forward, 0.4f);
            }

            // Top teleport
            if (_topTeleportPoint != null)
            {
                Gizmos.color = topColor;
                Gizmos.DrawSphere(_topTeleportPoint.position, 0.08f);
                DrawArrow(_topTeleportPoint.position, _topTeleportPoint.forward, 0.4f);
            }

            // Line between teleports for visual pairing
            if (_bottomTeleportPoint != null && _topTeleportPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_bottomTeleportPoint.position, _topTeleportPoint.position);
            }
        }

        private static void DrawArrow(Vector3 origin, Vector3 direction, float length)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();
            Vector3 end = origin + direction * length;
            Gizmos.DrawLine(origin, end);

            const float headSize = 0.15f;
            Vector3 right = Vector3.Cross(direction, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(direction, Vector3.right);
            right.Normalize();
            Vector3 up = Vector3.Cross(right, direction);

            Vector3 headBase = end - direction * headSize;
            Gizmos.DrawLine(end, headBase + right * headSize * 0.5f);
            Gizmos.DrawLine(end, headBase - right * headSize * 0.5f);
            Gizmos.DrawLine(end, headBase + up * headSize * 0.5f);
            Gizmos.DrawLine(end, headBase - up * headSize * 0.5f);
        }
#endif
    }
}