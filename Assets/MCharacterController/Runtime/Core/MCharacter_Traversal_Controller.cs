// File: Runtime/Core/MCharacter_Traversal_Controller.cs
// Namespace: Kojiko.MCharacterController.Core
//
// Summary:
// 1. Manages high-level traversal sequences (e.g., zipline, ladder).
// 2. Drives character movement along predefined paths using the motor.
// 3. Exposes a simple public API (BeginZipline, BeginLadder, InterruptTraversal)
//    for abilities and triggers to start/stop traversal.
//
// Notes:
// - This is a Core helper, not an ability. Abilities or level logic decide when
//   traversal should start, in which direction, and with what parameters.

using UnityEngine;

namespace Kojiko.MCharacterController.Core
{
    /// <summary>
    /// Overall traversal state: None, Zipline, Ladder, etc.
    /// Extend as you add more traversal types (vault, mantle, ledges, etc.).
    /// </summary>
    public enum TraversalState
    {
        None = 0,
        Zipline = 10,
        Ladder = 20,
        // Future examples:
        // Vault = 30,
        // Mantle = 40,
    }

    /// <summary>
    /// Core traversal controller:
    /// 1. Owns traversal state and parameters (zipline, ladder, ...).
    /// 2. Advances traversal each frame in TickTraversal(deltaTime).
    /// 3. Uses the motor's TeleportToPoint (or velocity) to move the character.
    /// </summary>
    [DisallowMultipleComponent]
    public class MCharacter_Traversal_Controller : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------

        [Header("References")]
        [Tooltip("Reference to the character motor. If null, will be auto-fetched on Awake().")]
        [SerializeField] private MCharacter_Motor_Controller _motor;

        // --------------------------------------------------------------------
        // Runtime state
        // --------------------------------------------------------------------

        [Header("Debug / State")]
        [SerializeField] private TraversalState _state = TraversalState.None;
        /// <summary>
        /// Current traversal state (None, Zipline, Ladder, etc.).
        /// </summary>
        public TraversalState State => _state;

        /// <summary>
        /// Convenience: true when any traversal is active.
        /// </summary>
        public bool IsTraversing => _state != TraversalState.None;

        // --------------------------------------------------------------------
        // Zipline data
        // --------------------------------------------------------------------

        /// <summary>World-space start point of the zipline path.</summary>
        private Vector3 _SmoothMoveStart;
        /// <summary>World-space end point of the zipline path.</summary>
        private Vector3 _SmoothMoveEnd;
        /// <summary>Time in seconds the zipline traversal should take.</summary>
        private float _SmoothMoveDuration;
        /// <summary>Elapsed time since zipline traversal began.</summary>
        private float _SmoothMoveElapsed;
        /// <summary>Position curve for zipline (0..1 -> 0..1) for easing/speed variations.</summary>
        private AnimationCurve _zipPositionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // --------------------------------------------------------------------
        // Ladder data (step-based progression from bottom to top)
        // --------------------------------------------------------------------

        /// <summary>World-space bottom point of the ladder path.</summary>
        private Vector3 _stepMoveBottom;
        /// <summary>World-space top point of the ladder path.</summary>
        private Vector3 _stepMoverTop;
        /// <summary>Distance in meters per ladder "step".</summary>
        private float _ladderStepDistance;
        /// <summary>Time in seconds between ladder steps.</summary>
        private float _ladderStepTime;
        /// <summary>0..1 normalized progress along ladder (0 = bottom, 1 = top).</summary>
        private float _ladderProgress;
        /// <summary>Timer to accumulate deltaTime until next ladder step.</summary>
        private float _ladderStepTimer;
        /// <summary>1 for climbing up, -1 for climbing down.</summary>
        private int _ladderDirection;

        // --------------------------------------------------------------------
        // Unity lifecycle
        // --------------------------------------------------------------------

        private void Awake()
        {
            // 1. Cache motor reference if not set in inspector.
            if (_motor == null)
                _motor = GetComponent<MCharacter_Motor_Controller>();

            // 2. Ensure we start with a clean traversal state.
            _state = TraversalState.None;

            // 3. (Optional) Initialize any traversal-specific fields as needed.
        }

        // --------------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------------

        /// <summary>
        /// Ticks the active traversal (if any).
        ///
        /// Typical call order from Root:
        /// 1. Process input & abilities (which may call BeginZipline/BeginLadder).
        /// 2. Perform normal motor.Step(...) if not traversing or as needed.
        /// 3. Call TickTraversal(deltaTime) to update traversal movement/position.
        /// </summary>
        public void TickTraversal(float deltaTime)
        {
            if (_state == TraversalState.None || deltaTime <= 0f)
                return;

            switch (_state)
            {
                case TraversalState.Zipline:
                    TickZipline(deltaTime);
                    break;

                case TraversalState.Ladder:
                    TickLadder(deltaTime);
                    break;
            }
        }

        /// <summary>
        /// Starts a zipline traversal between two world-space points.
        ///
        /// Steps:
        /// 1. Validate duration and distance.
        /// 2. Early-out if already traversing (or optionally interrupt).
        /// 3. Initialize zipline parameters and snap character to start.
        /// </summary>
        /// <param name="start">World-space start point of the zipline.</param>
        /// <param name="end">World-space end point of the zipline.</param>
        /// <param name="duration">Time in seconds the zipline should take.</param>
        /// <param name="curve">
        /// Optional position curve (0..1 -> 0..1). If null, linear motion is used.
        /// </param>
        /// <returns>True on success; false if arguments are invalid or traversal is in use.</returns>
        public bool BeginZipline(Vector3 start, Vector3 end, float duration, AnimationCurve curve = null)
        {
            // 1. Validate parameters.
            if (duration <= 0f)
                return false;

            if ((end - start).sqrMagnitude < 0.0001f)
                return false;

            // 2. If already traversing, either reject or interrupt (current approach: reject).
            if (IsTraversing)
                return false;

            // 3. Initialize zipline state and snap character to beginning.
            _SmoothMoveStart = start;
            _SmoothMoveEnd = end;
            _SmoothMoveDuration = duration;
            _SmoothMoveElapsed = 0f;

            _zipPositionCurve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);

            _state = TraversalState.Zipline;

            Vector3 direction = (end - start).normalized;
            Quaternion rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : transform.rotation;

            if (_motor != null)
            {
                _motor.TeleportToPoint(start, rotation);
            }

            return true;
        }

        /// <summary>
        /// Starts a ladder traversal between two points, with step-based motion.
        ///
        /// Steps:
        /// 1. Validate step distance/time and ladder length.
        /// 2. Early-out if already traversing.
        /// 3. Initialize ladder parameters and snap character to start or end.
        /// </summary>
        /// <param name="bottom">World-space bottom of the ladder.</param>
        /// <param name="top">World-space top of the ladder.</param>
        /// <param name="stepDistance">Vertical distance per ladder step (meters).</param>
        /// <param name="stepTime">Time between ladder steps (seconds).</param>
        /// <param name="startAtBottom">True to start at bottom, false to start at top.</param>
        /// <param name="direction">
        /// +1 to climb up, -1 to climb down. Direction is clamped to ±1.
        /// </param>
        /// <returns>True on success; false if arguments are invalid or traversal is in use.</returns>
        public bool BeginLadder(
            Vector3 bottom,
            Vector3 top,
            float stepDistance,
            float stepTime,
            bool startAtBottom,
            int direction)
        {
            // 1. Validate parameters.
            if (stepDistance <= 0f || stepTime <= 0f)
                return false;

            if ((top - bottom).sqrMagnitude < 0.0001f)
                return false;

            // 2. If already traversing, reject ladder request for now.
            if (IsTraversing)
                return false;

            // 3. Initialize ladder fields + snap to chosen start.
            _stepMoveBottom = bottom;
            _stepMoverTop = top;
            _ladderStepDistance = stepDistance;
            _ladderStepTime = stepTime;

            _ladderDirection = Mathf.Sign(direction) >= 0 ? 1 : -1;

            _ladderProgress = startAtBottom ? 0f : 1f;
            _ladderStepTimer = 0f;

            Vector3 startPos = Vector3.Lerp(_stepMoveBottom, _stepMoverTop, _ladderProgress);

            // Ladder "up" direction = vector from bottom to top.
            Vector3 upDir = (_stepMoverTop - _stepMoveBottom).normalized;
            if (upDir.sqrMagnitude < 0.0001f)
                upDir = Vector3.up;

            // Compute a stable forward direction for the character on the ladder
            // (simple cross with world right as a placeholder).
            Vector3 forwardDir = Vector3.Cross(upDir, Vector3.right);
            if (forwardDir.sqrMagnitude < 0.0001f)
                forwardDir = Vector3.forward;

            _state = TraversalState.Ladder;

            if (_motor != null)
            {
                _motor.TeleportToPoint(startPos, Quaternion.LookRotation(forwardDir, upDir));
            }

            return true;
        }

        /// <summary>
        /// Immediately stops any active traversal and leaves the character
        /// at its current position.
        ///
        /// Steps:
        /// 1. If not traversing, early-out.
        /// 2. Clear traversal state to None.
        /// 3. (Future) Optional: notify abilities/animation about traversal end.
        /// </summary>
        public void InterruptTraversal()
        {
            // 1. Early-out if already idle.
            if (!IsTraversing)
                return;

            // 2. Clear traversal state.
            _state = TraversalState.None;

            // 3. (Optional) Additional cleanup hooks can be added here if needed.
        }

        // --------------------------------------------------------------------
        // Zipline updates
        // --------------------------------------------------------------------

        /// <summary>
        /// Advances zipline traversal by deltaTime.
        ///
        /// Steps:
        /// 1. Update elapsed time and compute normalized t (0..1).
        /// 2. Evaluate the zipline curve to get eased position along the line.
        /// 3. Teleport motor to new position and orientation.
        /// </summary>
        private void TickZipline(float deltaTime)
        {
            if (_motor == null)
                return;

            // 1. Advance elapsed time and compute normalized parameter.
            _SmoothMoveElapsed += deltaTime;
            float t = Mathf.Clamp01(_SmoothMoveElapsed / _SmoothMoveDuration);

            // 2. Use the curve to get non-linear position if desired.
            float curveT = _zipPositionCurve != null
                ? _zipPositionCurve.Evaluate(t)
                : t;

            Vector3 position = Vector3.Lerp(_SmoothMoveStart, _SmoothMoveEnd, curveT);
            Vector3 direction = (_SmoothMoveEnd - _SmoothMoveStart).normalized;
            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;

            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            // 3. Teleport motor along the path.
            _motor.TeleportToPoint(position, rotation);

            // End traversal once we reach the end.
            if (t >= 1f)
            {
                _state = TraversalState.None;
            }
        }

        // --------------------------------------------------------------------
        // Ladder updates
        // --------------------------------------------------------------------

        /// <summary>
        /// Advances ladder traversal by deltaTime, moving the character in steps.
        ///
        /// Steps:
        /// 1. Accumulate step timer; early-out if we haven't reached the next step time.
        /// 2. Compute next ladder progress based on step distance and direction.
        /// 3. Teleport the motor to the new ladder position/orientation.
        /// </summary>
        private void TickLadder(float deltaTime)
        {
            if (_motor == null)
                return;

            // 1. Accumulate time toward the next step.
            _ladderStepTimer += deltaTime;
            if (_ladderStepTimer < _ladderStepTime)
                return;

            _ladderStepTimer -= _ladderStepTime;

            // 2. Compute progress increment based on configured step distance.
            float totalHeight = Vector3.Distance(_stepMoveBottom, _stepMoverTop);
            float stepFrac = _ladderStepDistance / Mathf.Max(totalHeight, 0.0001f);

            _ladderProgress += stepFrac * _ladderDirection;
            _ladderProgress = Mathf.Clamp01(_ladderProgress);

            Vector3 pos = Vector3.Lerp(_stepMoveBottom, _stepMoverTop, _ladderProgress);

            Vector3 upDir = (_stepMoverTop - _stepMoveBottom).normalized;
            if (upDir.sqrMagnitude < 0.0001f)
                upDir = Vector3.up;

            Vector3 forwardDir = Vector3.Cross(upDir, Vector3.right);
            if (forwardDir.sqrMagnitude < 0.0001f)
                forwardDir = Vector3.forward;

            _motor.TeleportToPoint(pos, Quaternion.LookRotation(forwardDir, upDir));

            // 3. If we've reached either end, complete traversal.
            if (_ladderProgress <= 0f || _ladderProgress >= 1f)
            {
                _state = TraversalState.None;
            }
        }
    }
}