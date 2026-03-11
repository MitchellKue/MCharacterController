// File: Runtime/Abilities/Ability_Climb.cs
// Namespace: Kojiko.MCharacterController.Abilities

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;
using Kojiko.MCharacterController.Environment;

namespace Kojiko.MCharacterController.Abilities
{
    public enum ClimbMode
    {
        Disabled,       // No climb at all, even if ability is present.
        SimpleTeleport, // Use SimpleClimbPair volumes only (teleport).
        FullClimb       // Use ClimbVolume with full climb movement.
    }

    [DisallowMultipleComponent]
    public class Ability_Climb : MonoBehaviour, ICharacterAbility
    {
        [Header("General")]
        [SerializeField]
        [Tooltip("Enable/disable climbing globally.")]
        private bool _enabled = true;

        [Header("Mode")]
        [SerializeField]
        [Tooltip("Which climb behavior this ability should use.")]
        private ClimbMode _mode = ClimbMode.SimpleTeleport;

        [Tooltip("Layers considered climbable when raycasting / searching for climb volumes.")]
        [SerializeField] private LayerMask _climbLayerMask = ~0;

        [Tooltip("Distance in front of the character to search for climb volumes when doing a forward check.")]
        [SerializeField] private float _forwardCheckDistance = 0.8f;

        [Tooltip("Radius to search for climb volumes when using TryBeginClimb().")]
        [SerializeField] private float _searchRadius = 1.0f;

        [Header("Climb Speeds")]
        [Tooltip("Speed moving up the climb volume (m/s).")]
        [SerializeField] private float _climbUpSpeed = 2.5f;

        [Tooltip("Speed moving down the climb volume (m/s).")]
        [SerializeField] private float _climbDownSpeed = 2.0f;

        [Tooltip("How quickly we snap to the climb surface horizontally.")]
        [SerializeField] private float _horizontalSnapSpeed = 20f;

        [Header("Detection")]
        [Tooltip("If true, pressing Interact will try to begin climbing the best nearby climb volume.")]
        [SerializeField] private bool _interactToClimb = true;

        [Tooltip("Maximum angle (degrees) allowed between character forward and climb surface forward when starting a climb.")]
        [SerializeField] private float _maxStartAngle = 60f;

        [Header("Debug (Read Only)")]
        [SerializeField, Tooltip("Currently in climbing state.")]
        private bool _isClimbing;

        [SerializeField, Tooltip("Current climb volume being used.")]
        private ClimbVolume _currentVolume;

        // Core refs
        private MCharacter_Motor _motor;
        private MCharacter_Controller_Root _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;
        private IClimbLookRig _climbLookRig;

        private Transform _transform;

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
            _climbLookRig = cameraRig as IClimbLookRig;

            _transform = controllerRoot != null ? controllerRoot.transform : transform;

            _isClimbing = false;
            _currentVolume = null;
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (!_enabled || deltaTime <= 0f)
                return;

            // SIMPLE TELEPORT MODE: Interact to use SimpleClimbPair
            if (_mode == ClimbMode.SimpleTeleport)
            {
                if (_input != null && _input.InteractPressed && _motor != null)
                {
                    TryUseSimpleClimbPairs();
                }

                // Simple teleport doesn't override normal motor movement.
                return;
            }

            // FULL CLIMB MODE:
            if (_mode != ClimbMode.FullClimb)
                return;

            if (_isClimbing)
            {
                TickClimbing(deltaTime, ref desiredMoveWorld);
            }
            else
            {
                if (_interactToClimb && _input != null && _input.InteractPressed)
                {
                    if (!TryBeginClimb(_searchRadius))
                    {
                        ClimbVolume volume = FindClimbVolumeInFront();
                        if (volume != null && CanStartOnVolume(volume))
                        {
                            BeginClimb(volume);
                        }
                    }
                }
            }
        }

        public void PostStep(float deltaTime)
        {
            if (!_enabled || deltaTime <= 0f)
                return;

            if (_mode != ClimbMode.FullClimb)
                return;

            if (_isClimbing)
            {
                CheckTopBottomExit();
            }
        }

        #endregion

        #region Core Climb Logic

        private void TickClimbing(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (_input == null || _currentVolume == null || _motor == null)
            {
                EndClimb();
                return;
            }

            // Exit via jump
            if (_input.JumpPressed)
            {
                EndClimb();
                return;
            }

            // Still close to volume?
            if (!IsStillInVolume(_currentVolume))
            {
                EndClimb();
                return;
            }

            // Disable normal gravity by zeroing vertical velocity first.
            _motor.SetVerticalVelocity(0f);

            // Move along climb up/down direction based on vertical input.
            Vector2 moveAxis = _input.MoveAxis;
            float verticalInput = moveAxis.y;

            float speed =
                verticalInput > 0f ? _climbUpSpeed :
                verticalInput < 0f ? _climbDownSpeed : 0f;

            float climbVelocity = speed * verticalInput; // signed
            _motor.AddExternalVerticalVelocity(climbVelocity);

            // Snap horizontally to climb line and face surface
            SnapToClimbLine(deltaTime);
            FaceSurfaceForward(deltaTime);

            // Prevent standard movement input from affecting horizontal motion.
            desiredMoveWorld = Vector3.zero;
        }

        #endregion

        #region Volume / Detection

        private ClimbVolume FindClimbVolumeInFront()
        {
            Vector3 origin = _transform.position + Vector3.up * 1.0f;
            Vector3 fwd = _transform.forward;

            if (Physics.Raycast(origin, fwd, out RaycastHit hit, _forwardCheckDistance, _climbLayerMask, QueryTriggerInteraction.Collide))
            {
                var volume = hit.collider.GetComponentInParent<ClimbVolume>();
                if (volume != null)
                    return volume;
            }

            return null;
        }

        private bool IsStillInVolume(ClimbVolume volume)
        {
            if (volume == null)
                return false;

            Collider col = volume.VolumeCollider;
            if (col == null)
                return false;

            Vector3 closest = col.ClosestPoint(_transform.position);
            float distSqr = (closest - _transform.position).sqrMagnitude;
            return distSqr < 1.0f; // tweak threshold
        }

        private bool CanStartOnVolume(ClimbVolume volume)
        {
            if (volume == null)
                return false;

            // Basic forward alignment check similar to auto-start, but
            // only used when Interact is pressed.
            Vector3 charFwd = _transform.forward;
            charFwd.y = 0f;
            charFwd.Normalize();

            Vector3 surfaceFwd = volume.SurfaceForward;
            surfaceFwd.y = 0f;
            surfaceFwd.Normalize();

            if (charFwd.sqrMagnitude < 0.0001f || surfaceFwd.sqrMagnitude < 0.0001f)
                return true; // if something is degenerate, don't block interaction

            float angle = Vector3.Angle(charFwd, surfaceFwd);
            return angle <= _maxStartAngle;
        }

        private void TryUseSimpleClimbPairs()
        {
            // Strategy similar to zipline: find all SimpleClimbPair and
            // let them decide if this motor is inside their triggers.
            var pairs = FindObjectsOfType<SimpleClimbPair>();
            foreach (var pair in pairs)
            {
                if (pair.TryUseSimpleClimb(_motor))
                {
                    // Once one pair handles it, we can stop.
                    break;
                }
            }
        }

        #endregion

        #region Snapping / Orientation

        private void SnapToClimbLine(float deltaTime)
        {
            if (_currentVolume == null || _motor == null)
                return;

            Vector3 pos = _transform.position;
            Vector3 targetOnLine = _currentVolume.GetClosestPointOnClimbLine(pos);

            // Keep vertical where the motor is handling it; snap XZ only.
            targetOnLine.y = pos.y;

            Vector3 toTarget = targetOnLine - pos;
            Vector3 horizontalOffset = new Vector3(toTarget.x, 0f, toTarget.z);

            Vector3 snapVelocity = horizontalOffset / Mathf.Max(deltaTime, 0.0001f);
            snapVelocity = Vector3.ClampMagnitude(snapVelocity, _horizontalSnapSpeed);

            _motor.AddExternalHorizontalVelocity(snapVelocity);
        }

        private void FaceSurfaceForward(float deltaTime)
        {
            if (_currentVolume == null)
                return;

            Vector3 surfaceForward = _currentVolume.SurfaceForward;
            surfaceForward.y = 0f;
            if (surfaceForward.sqrMagnitude < 0.0001f)
                return;

            surfaceForward.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(surfaceForward, Vector3.up);
            _transform.rotation = Quaternion.Slerp(
                _transform.rotation,
                targetRot,
                deltaTime * 15f); // tweak rotation speed
        }

        #endregion

        #region Start / End / Bounds

        private void BeginClimb(ClimbVolume volume)
        {
            if (volume == null || _isClimbing)
                return;

            if (!CanStartOnVolume(volume))
                return;

            _isClimbing = true;
            _currentVolume = volume;

            // Snap immediately to climb line XZ.
            Vector3 pos = _transform.position;
            Vector3 snapPos = volume.GetClosestPointOnClimbLine(pos);
            snapPos.y = pos.y;
            _transform.position = snapPos;

            _motor.SetVerticalVelocity(0f);
            _motor.ClearExternalVelocities();

            // Enable camera constraints if supported.
            if (_climbLookRig != null)
            {
                _climbLookRig.SetClimbConstraints(
                    true,
                    volume.SurfaceForward,
                    volume.MaxYawFromSurface,
                    volume.MinPitch,
                    volume.MaxPitch);
            }
        }

        private void EndClimb()
        {
            if (!_isClimbing)
                return;

            _isClimbing = false;
            _currentVolume = null;

            // Let motor resume normal behavior. Gravity is always re-applied by the motor.

            if (_climbLookRig != null)
            {
                _climbLookRig.SetClimbConstraints(false, Vector3.forward, 180f, -89f, 89f);
            }
        }

        private void CheckTopBottomExit()
        {
            if (_currentVolume == null || !_isClimbing)
                return;

            float y = _transform.position.y;
            float bottomY = _currentVolume.GetBottomHeight();
            float topY = _currentVolume.GetTopHeight();
            const float tolerance = 0.1f;

            if (y > topY + tolerance || y < bottomY - tolerance)
            {
                EndClimb();
            }
        }

        #endregion

        #region Public API

        public bool Enabled => _enabled;
        public bool IsClimbing => _isClimbing;
        public ClimbVolume CurrentVolume => _currentVolume;

        /// <summary>
        /// Allows the ability to set the current climb mode model
        /// </summary>
        public ClimbMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>
        /// Simple climb (teleport-based SimpleClimbPair volumes) is allowed
        /// when the ability is enabled and the mode is SimpleTeleport.
        /// </summary>
        public bool AllowSimpleClimbVolumes =>
            _enabled && _mode == ClimbMode.SimpleTeleport;

        [ContextMenu("Switch to simple Climb")]
        public void SwitchToSimpleClimb()
        {
            Mode = ClimbMode.SimpleTeleport;
        }

        [ContextMenu("Switch to full Climb")]
        public void SwitchToFullClimb()
        {
            Mode = ClimbMode.FullClimb;
        }

        [ContextMenu("Disable climbing")]
        public void DisableClimb()
        {
            Mode = ClimbMode.Disabled;
        }

        [ContextMenu("Forcefully end climbing")]
        public void ForceEndClimb()
        {
            EndClimb();
        }

        /// <summary>
        /// Attempts to begin climbing the best ClimbVolume within the given radius.
        /// Returns true if a climb was started.
        /// </summary>
        public bool TryBeginClimb(float radius)
        {
            if (!_enabled || _isClimbing || _mode != ClimbMode.FullClimb)
                return false;

            float r = radius > 0f ? radius : _searchRadius;

            Collider[] hits = Physics.OverlapSphere(
                _transform.position,
                r,
                _climbLayerMask,
                QueryTriggerInteraction.Collide);

            float bestDistSqr = float.MaxValue;
            ClimbVolume best = null;

            foreach (var col in hits)
            {
                if (col == null) continue;

                var volume = col.GetComponentInParent<ClimbVolume>();
                if (volume == null) continue;

                if (!CanStartOnVolume(volume))
                    continue;

                float distSqr = (col.bounds.ClosestPoint(_transform.position) - _transform.position).sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = volume;
                }
            }

            if (best != null)
            {
                BeginClimb(best);
                return true;
            }

            return false;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Only draw if we have some sort of transform
            Transform t = _controllerRoot != null ? _controllerRoot.transform : transform;
            if (t == null)
                return;

            // Use same height offset as forward raycast
            Vector3 origin = t.position + Vector3.up * 1.0f;
            Vector3 forward = t.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = t.forward;

            forward.Normalize();

            float maxAngle = Mathf.Clamp(_maxStartAngle, 0f, 180f);
            float distance = Mathf.Max(0.1f, _forwardCheckDistance);

            Color baseColor = new Color(1f, 1f, 0f, 0.9f);
            Gizmos.color = baseColor;

            // Draw cone as a set of rays forming a fan + an arc
            DrawCone(origin, forward, Vector3.up, maxAngle, distance, baseColor);
        }

        private static void DrawCone(
            Vector3 origin,
            Vector3 forward,
            Vector3 up,
            float angleDeg,
            float length,
            Color color)
        {
            Gizmos.color = color;

            // Ensure orthonormal basis
            forward.Normalize();
            up = up.sqrMagnitude < 0.0001f ? Vector3.up : up.normalized;

            Vector3 right = Vector3.Cross(forward, up);
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(forward, Vector3.right);
            }
            right.Normalize();
            up = Vector3.Cross(right, forward);

            // Main center ray
            Gizmos.DrawLine(origin, origin + forward * length);

            // Edge directions on the horizontal plane
            Vector3 leftDir = Quaternion.AngleAxis(-angleDeg * 0.5f, up) * forward;
            Vector3 rightDir = Quaternion.AngleAxis(angleDeg * 0.5f, up) * forward;

            Vector3 left = origin + leftDir * length;
            Vector3 rightV = origin + rightDir * length;

            Gizmos.DrawLine(origin, left);
            Gizmos.DrawLine(origin, rightV);
            Gizmos.DrawLine(left, rightV);

#if UNITY_EDITOR
            // Small arc to visualize the angle
            const int segments = 24;
            Vector3 prev = origin + (Quaternion.AngleAxis(-angleDeg * 0.5f, up) * forward) * length;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float currentAngle = Mathf.Lerp(-angleDeg * 0.5f, angleDeg * 0.5f, t);
                Vector3 dir = Quaternion.AngleAxis(currentAngle, up) * forward;
                Vector3 point = origin + dir * length;
                Gizmos.DrawLine(prev, point);
                prev = point;
            }

            // Angle label in scene view
            Vector3 labelPos = origin + forward * (length * 0.7f) + up * 0.1f;
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(labelPos, $" Climb Start Cone ({angleDeg:0}° / {length:0.0}m)");
#endif
        }

        #endregion
    }
}