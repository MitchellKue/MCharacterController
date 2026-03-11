// File: Runtime/Environment/ClimbVolume.cs
// Namespace: Kojiko.MCharacterController.Environment

using UnityEngine;

namespace Kojiko.MCharacterController.Environment
{
    /// <summary>
    /// Marks a region as climbable (ladder, vine, cliff).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ClimbVolume : MonoBehaviour
    {
        [Header("Climb Volume")]
        [Tooltip("Trigger collider that defines the climbable region. Must be a trigger.")]
        [SerializeField] private Collider _volumeCollider;

        [Tooltip("World-space transform representing the bottom of the climb path.")]
        [SerializeField] private Transform _bottomPoint;

        [Tooltip("World-space transform representing the top of the climb path.")]
        [SerializeField] private Transform _topPoint;

        [Tooltip("Up direction of the climbable surface. Usually world up (0,1,0) for ladders.")]
        [SerializeField] private Vector3 _climbUpDirection = Vector3.up;

        [Tooltip("Forward direction of the surface the player faces while climbing.")]
        [SerializeField] private Vector3 _surfaceForward = Vector3.forward;

        [Header("Camera Constraints")]
        [Tooltip("Allowed yaw offset relative to surface forward while climbing (degrees).")]
        [SerializeField] private float _maxYawFromSurface = 45f;

        [Tooltip("Min pitch (degrees) allowed while climbing.")]
        [SerializeField] private float _minPitch = -60f;

        [Tooltip("Max pitch (degrees) allowed while climbing.")]
        [SerializeField] private float _maxPitch = 60f;

        private void Reset()
        {
            _volumeCollider = GetComponent<Collider>();
            if (_volumeCollider != null)
                _volumeCollider.isTrigger = true;

            _climbUpDirection = Vector3.up;
            _surfaceForward = transform.forward;
        }

        public Collider VolumeCollider => _volumeCollider;
        public Transform BottomPoint => _bottomPoint;
        public Transform TopPoint => _topPoint;

        public Vector3 ClimbUpDirection
        {
            get
            {
                if (_climbUpDirection.sqrMagnitude < 0.0001f)
                    return Vector3.up;
                return _climbUpDirection.normalized;
            }
        }

        public Vector3 SurfaceForward
        {
            get
            {
                if (_surfaceForward.sqrMagnitude < 0.0001f)
                    return transform.forward.normalized;
                return _surfaceForward.normalized;
            }
        }

        public float MaxYawFromSurface => _maxYawFromSurface;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;

        public float GetBottomHeight() =>
            _bottomPoint != null ? _bottomPoint.position.y : transform.position.y;

        public float GetTopHeight() =>
            _topPoint != null ? _topPoint.position.y : transform.position.y + 2f;

        /// <summary>
        /// Returns a point on the climb line (bottom->top) projected from a given world position.
        /// </summary>
        public Vector3 GetClosestPointOnClimbLine(Vector3 worldPosition)
        {
            if (_bottomPoint == null || _topPoint == null)
                return transform.position;

            Vector3 a = _bottomPoint.position;
            Vector3 b = _topPoint.position;
            Vector3 ab = b - a;
            float abSqr = ab.sqrMagnitude;
            if (abSqr < 0.0001f)
                return a;

            float t = Vector3.Dot(worldPosition - a, ab) / abSqr;
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }

        // -------------------------------------------------
        // Gizmos
        // -------------------------------------------------

        private void OnDrawGizmos()
        {
            DrawGizmosInternal(selected: false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmosInternal(selected: true);
        }

        private void DrawGizmosInternal(bool selected)
        {
            // Slightly brighter when selected
            Color baseColor = selected ? new Color(1f, 1f, 0f, 1f) : new Color(1f, 1f, 0f, 0.7f);
            Gizmos.color = baseColor;

            // Draw volume bounds (wireframe)
            if (_volumeCollider != null)
            {
                Matrix4x4 cached = Gizmos.matrix;
                Gizmos.matrix = _volumeCollider.transform.localToWorldMatrix;

                if (_volumeCollider is BoxCollider box)
                {
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (_volumeCollider is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
                else if (_volumeCollider is CapsuleCollider capsule)
                {
                    // Approximate with a wire cube for simplicity
                    Vector3 size = new Vector3(capsule.radius * 2f, capsule.height, capsule.radius * 2f);
                    Gizmos.DrawWireCube(capsule.center, size);
                }

                Gizmos.matrix = cached;
            }

            // Draw bottom/top points & line between them (the climb line)
            Vector3 bottomPos = _bottomPoint != null ? _bottomPoint.position : transform.position;
            Vector3 topPos = _topPoint != null ? _topPoint.position : transform.position + Vector3.up * 2f;

            // Bottom sphere
            Gizmos.color = baseColor;
            Gizmos.DrawWireSphere(bottomPos, 0.1f);

            // Top sphere
            Gizmos.DrawWireSphere(topPos, 0.1f);

            // Wire between bottom & top
            Gizmos.DrawLine(bottomPos, topPos);

            // Draw climb up direction arrow from middle
            Vector3 mid = (bottomPos + topPos) * 0.5f;
            Vector3 upDir = ClimbUpDirection;
            float arrowLength = 0.75f;
            DrawArrow(mid, upDir, arrowLength, baseColor);

            // Draw surface forward direction arrow from middle
            Vector3 surfaceFwd = SurfaceForward;
            float surfArrowLength = 0.75f;
            DrawArrow(mid, surfaceFwd, surfArrowLength, baseColor);

#if UNITY_EDITOR
            // Labels when selected (editor only)
            if (selected)
            {
                UnityEditor.Handles.color = baseColor;

                UnityEditor.Handles.Label(bottomPos, " Bottom");
                UnityEditor.Handles.Label(topPos, " Top");

                UnityEditor.Handles.Label(
                    mid + upDir * (arrowLength + 0.1f),
                    " Climb Up");

                UnityEditor.Handles.Label(
                    mid + surfaceFwd * (surfArrowLength + 0.1f),
                    " Surface Fwd");
            }
#endif
        }

        private static void DrawArrow(Vector3 origin, Vector3 direction, float length, Color color)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();
            Vector3 end = origin + direction * length;

            Gizmos.color = color;
            Gizmos.DrawLine(origin, end);

            // Simple arrow head
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
    }
}