// File: Runtime/Environment/SimpleZiplinePair.cs
// Namespace: Kojiko.MCharacterController.Environment

using System.Collections.Generic;
using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Abilities;

namespace Kojiko.MCharacterController.Environment
{
    /// <summary>
    /// Simple zipline volume:
    /// - Two triggers: bottom & top.
    /// - A line or direction from start to end.
    /// - Can auto-start when entering trigger, or wait for Interact.
    /// - Delegates the actual zipline motion to Ability_Zipline.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleZiplinePair : MonoBehaviour
    {
        public enum ZiplineStartMode
        {
            AutoOnEnter,   // Auto start as soon as you enter trigger (if ability allows).
            ManualInteract // Require Interact button while inside trigger.
        }

        [Header("Triggers (must be trigger colliders)")]
        [SerializeField] private Collider _bottomTrigger;
        [SerializeField] private Collider _topTrigger;

        [Header("Endpoints")]
        [Tooltip("Zipline start point (used when entering from bottom side).")]
        [SerializeField] private Transform _bottomPoint;

        [Tooltip("Zipline end point (used when entering from top side, or as the other end).")]
        [SerializeField] private Transform _topPoint;

        [Header("Settings")]
        [SerializeField] private ZiplineStartMode _startMode = ZiplineStartMode.ManualInteract;

        [Tooltip("Tag used to identify the player object. If empty, any object with MCharacter_Motor & Ability_Zipline is valid.")]
        [SerializeField] private string _playerTag = "Player";

        // Keep track of which motors are currently inside which trigger,
        // so manual mode can query "is in volume" when Interact is pressed.
        private readonly Dictionary<MCharacter_Motor, Collider> _motorsInTrigger =
            new Dictionary<MCharacter_Motor, Collider>();

        private void Reset()
        {
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
            if (_bottomPoint == null || _topPoint == null)
                return;
        }

        #region Trigger Relay API

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

        private void HandleTriggerEnter(Collider trigger, Collider other)
        {
            if (!string.IsNullOrEmpty(_playerTag) && !other.CompareTag(_playerTag))
                return;

            var motor = other.GetComponentInParent<MCharacter_Motor>();
            if (motor == null)
                return;

            var zipAbility = other.GetComponentInParent<Ability_Zipline>();
            if (zipAbility == null || !zipAbility.Enabled)
                return;

            // Remember which trigger this motor is in.
            _motorsInTrigger[motor] = trigger;

            if (_startMode == ZiplineStartMode.AutoOnEnter)
            {
                TryStartZipline(motor, zipAbility, trigger);
            }
        }

        private void HandleTriggerExit(Collider trigger, Collider other)
        {
            if (!string.IsNullOrEmpty(_playerTag) && !other.CompareTag(_playerTag))
                return;

            var motor = other.GetComponentInParent<MCharacter_Motor>();
            if (motor == null)
                return;

            // Leaving trigger cancels the possibility to start from here.
            if (_motorsInTrigger.ContainsKey(motor) && _motorsInTrigger[motor] == trigger)
            {
                _motorsInTrigger.Remove(motor);
            }
        }

        /// <summary>
        /// Used by Ability_Zipline in ManualInteract mode to attempt
        /// starting a zipline when Interact is pressed.
        /// </summary>
        public bool TryStartZiplineFromInteract(MCharacter_Motor motor, Ability_Zipline zipAbility)
        {
            if (motor == null || zipAbility == null || !zipAbility.Enabled)
                return false;

            if (_startMode != ZiplineStartMode.ManualInteract)
                return false;

            if (!_motorsInTrigger.TryGetValue(motor, out var trigger))
                return false;

            return TryStartZipline(motor, zipAbility, trigger);
        }

        private bool TryStartZipline(MCharacter_Motor motor, Ability_Zipline zipAbility, Collider trigger)
        {
            if (motor == null || zipAbility == null || !zipAbility.Enabled)
                return false;

            Transform startPoint = null;
            Transform endPoint = null;

            // Decide direction based on which trigger the player came from.
            if (trigger == _bottomTrigger)
            {
                startPoint = _bottomPoint;
                endPoint = _topPoint;
            }
            else if (trigger == _topTrigger)
            {
                startPoint = _topPoint;
                endPoint = _bottomPoint;
            }

            if (startPoint == null || endPoint == null)
                return false;

            return zipAbility.BeginZipline(this, motor, startPoint, endPoint);
        }

        #region Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_bottomPoint != null && _topPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_bottomPoint.position, _topPoint.position);

                // Draw spheres at endpoints
                Gizmos.DrawSphere(_bottomPoint.position, 0.08f);
                Gizmos.DrawSphere(_topPoint.position, 0.08f);

                // Draw arrows along the zipline direction (bottom -> top)
                Vector3 dir = (_topPoint.position - _bottomPoint.position);
                if (dir.sqrMagnitude > 0.001f)
                {
                    dir.Normalize();
                    DrawArrow(_bottomPoint.position, dir, 0.5f);
                }
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

        #endregion
    }
}