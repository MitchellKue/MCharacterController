// File: Runtime/Environment/SimpleClimbPair.cs
// Namespace: Kojiko.MCharacterController.Environment

using System.Collections;
using UnityEngine;
using Kojiko.MCharacterController.Core;

namespace Kojiko.MCharacterController.Environment
{
    /// <summary>
    /// Extremely simple climb/ladder behavior:
    /// - Two trigger colliders: bottomTrigger, topTrigger.
    /// - Two teleport points: bottomTeleportPoint, topTeleportPoint.
    /// - When the player enters a trigger and stays for climbDelaySeconds,
    ///   they are teleported to the opposite end (position + facing).
    /// - Walking out of the trigger before the delay cancels the teleport.
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
        [Tooltip("How long the player must stay inside a trigger to complete the climb (seconds).")]
        [SerializeField] private float _climbDelaySeconds = 0.5f;

    //   [Header("Target Filter")]
    //   [Tooltip("Tag used to identify the player object. If empty, any object with MCharacter_Motor is valid.")]
    //   [SerializeField] private string _playerTag = "Player";

        // Internal: track pending teleports per motor so we can cancel them.
        private class PendingTeleport
        {
            public Coroutine Coroutine;
            public Collider SourceTrigger;
        }

        // We can track at most one active player motor per climb volume in many games,
        // but this dictionary supports multiple if needed.
        private readonly System.Collections.Generic.Dictionary<MCharacter_Motor, PendingTeleport>
            _pendingTeleports = new System.Collections.Generic.Dictionary<MCharacter_Motor, PendingTeleport>();

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

        private void OnTriggerEnter(Collider other)
        {
            // This callback might not fire if the triggers are not on the same GameObject as this script.
            // To ensure consistent behavior, it's safer to put this script on a parent and have bottom/top
            // triggers as children with their own Collider components, with "Is Trigger" enabled.
        }

        /// <summary>
        /// Use trigger callbacks on the specific bottom/top trigger colliders.
        /// We expose these as public so they can be relayed from child components
        /// if needed.
        /// </summary>
        /// <param name="trigger">The trigger collider that was entered.</param>
        /// <param name="other">The collider that entered.</param>
        private void HandleTriggerEnter(Collider trigger, Collider other)
        {
            // Basic filter: tag and/or motor component.
        //   if (!string.IsNullOrEmpty(_playerTag) && !other.CompareTag(_playerTag))
        //        return;

            var motor = other.GetComponentInParent<MCharacter_Motor>();
            if (motor == null)
                return;

            // REQUIRE Ability_Climb to be present and enabled
            var abilityClimb = other.GetComponentInParent<Kojiko.MCharacterController.Abilities.Ability_Climb>();
            if (abilityClimb == null || !abilityClimb.AllowSimpleClimbVolumes)
                return;

            // If we already have a pending teleport for this motor, cancel it.
            CancelPendingTeleport(motor);

            // Decide target point based on which trigger was entered.
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
                return;

            // Start delayed teleport coroutine.
            Coroutine c = StartCoroutine(DelayedTeleport(motor, trigger, destination));
            _pendingTeleports[motor] = new PendingTeleport
            {
                Coroutine = c,
                SourceTrigger = trigger
            };
        }

        private void HandleTriggerExit(Collider trigger, Collider other)
        {
        //   if (!string.IsNullOrEmpty(_playerTag) && !other.CompareTag(_playerTag))
        //        return;

            var motor = other.GetComponentInParent<MCharacter_Motor>();
            if (motor == null)
                return;

            // We don't strictly need to check Ability_Climb here, since
            // we're just cancelling any pending teleport associated with this motor.
            if (_pendingTeleports.TryGetValue(motor, out var pending))
            {
                if (pending.SourceTrigger == trigger)
                {
                    CancelPendingTeleport(motor);
                }
            }
        }

        private IEnumerator DelayedTeleport(MCharacter_Motor motor, Collider sourceTrigger, Transform destination)
        {
            float delay = Mathf.Max(0f, _climbDelaySeconds);

            float elapsed = 0f;
            // We don't need to poll anything here; exit is handled by OnTriggerExit cancelling.
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;

                // If the motor object is destroyed mid-wait, bail out.
                if (motor == null)
                    yield break;

                yield return null;
            }

            // Still valid: perform teleport.
            if (motor != null && destination != null)
            {
                motor.TeleportToPoint(destination);
            }

            // Clean up the pending entry if it still exists.
            if (_pendingTeleports.ContainsKey(motor))
            {
                _pendingTeleports.Remove(motor);
            }
        }

        private void CancelPendingTeleport(MCharacter_Motor motor)
        {
            if (motor == null)
                return;

            if (_pendingTeleports.TryGetValue(motor, out var pending) && pending.Coroutine != null)
            {
                StopCoroutine(pending.Coroutine);
            }

            _pendingTeleports.Remove(motor);
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

        // ------------------------------------------------------------
        // Optional: Gizmos to visualize setup
        // ------------------------------------------------------------
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