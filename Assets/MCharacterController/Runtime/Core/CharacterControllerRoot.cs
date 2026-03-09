// File: Runtime/Core/CharacterControllerRoot.cs
// Namespace: Kojiko.MCharacterController.Core
//
// Summary:
// 1. Orchestrates interaction between input, motor, and camera rig.
// 2. Reads ICcInputSource, forwards look to CameraRigBase, and computes world move direction.
// 3. Calls CharacterMotor.Step() every frame.
//
// Dependencies:
// - Kojiko.MCharacterController.Input.ICcInputSource (provided as a MonoBehaviour).
// - Kojiko.MCharacterController.Input.NewInputSystemSource (typical implementation).
// - Kojiko.MCharacterController.Core.CharacterMotor (movement).
// - Kojiko.MCharacterController.Camera.CameraRigBase (active camera rig).
//
// Usage:
// - Attach to the same GameObject as CharacterMotor and NewInputSystemSource.
// - Assign references in the inspector.

using UnityEngine;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Core
{
    /// <summary>
    /// 1. STEP 1: Resolve references to the motor, input source, and camera rig at startup.
    /// 2. STEP 2: Each frame, read input (MoveAxis, LookAxis) and update camera orientation.
    /// 3. STEP 3: Convert movement input into world-space direction and invoke CharacterMotor.Step().
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterControllerRoot : MonoBehaviour
    {
        [Header("Core References")]
        [Tooltip("Movement motor component responsible for handling CharacterController movement.")]
        [SerializeField] private CharacterMotor _motor;

        [Tooltip("Component that implements ICcInputSource (e.g., NewInputSystemSource).")]
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        [Tooltip("Active camera rig for this character (e.g., FirstPersonCameraRig).")]
        [SerializeField] private CameraRigBase _cameraRig;

        [Header("Abilities References")]
        [Tooltip("ability controller.")]
        [SerializeField] private CharacterAbilityController _abilityController;

        // Internal cached interface
        private ICcInputSource _inputSource;

        private void Awake()
        {
            // STEP 1: Validate and cache the motor reference.
            if (_motor == null)
            {
                _motor = GetComponent<CharacterMotor>();
            }

            if (_motor == null)
            {
                UnityEngine.Debug.LogError("[CharacterControllerRoot] CharacterMotor reference is missing.", this);
                enabled = false;
                return;
            }

            // STEP 2: Cast the provided MonoBehaviour to ICcInputSource.
            if (_inputSourceBehaviour == null)
            {
                // If not assigned, try to find any MonoBehaviour that implements ICcInputSource on this GameObject.
                _inputSourceBehaviour = GetComponent<MonoBehaviour>();
            }

            _inputSource = _inputSourceBehaviour as ICcInputSource;
            if (_inputSource == null)
            {
                UnityEngine.Debug.LogError("[CharacterControllerRoot] Input source must implement ICcInputSource.", this);
                enabled = false;
                return;
            }

            // STEP 3: Initialize the camera rig (if present) with this character's transform.
            if (_cameraRig != null)
            {
                _cameraRig.Initialize(transform);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[CharacterControllerRoot] No CameraRigBase assigned. Character will move but not look.", this);
            }

            // STEP 4: Initialize ability controller AFTER motor and input are ready.
            if (_abilityController != null)
            {
                UnityEngine.Debug.Log("[CharacterControllerRoot] Initializing ability controller.", this);
                _abilityController.Initialize(_motor, this, _inputSource, _cameraRig);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // STEP 1: Read input axes from the input source.
            Vector2 moveAxis = _inputSource.MoveAxis;
            Vector2 lookAxis = _inputSource.LookAxis;

            // STEP 2: Give the look input to the camera rig for yaw/pitch handling.
            if (_cameraRig != null)
            {
                _cameraRig.HandleLook(lookAxis, dt);
            }

            // STEP 3: Convert moveAxis to a world-space move direction based on character yaw.
            Vector3 moveDirection = TransformMoveInput(moveAxis);

            // STEP 3.5: Let abilities tweak moveDirection or other state before stepping the motor.
            _abilityController?.TickAbilities(dt, ref moveDirection);

            // STEP 4: Forward the computed move direction to the motor.
            _motor.Step(moveDirection, dt);

            // STEP 5: Let abilities react after the motor has stepped.
            _abilityController?.PostStepAbilities(dt);
        }

        /// <summary>
        /// Converts a 2D input vector (x = strafe, y = forward) into a world-space direction
        /// relative to the character's current yaw orientation.
        /// </summary>
        /// <param name="moveAxis">2D movement input.</param>
        private Vector3 TransformMoveInput(Vector2 moveAxis)
        {
            // STEP 1: Build a local-space direction with x = right, z = forward.
            Vector3 localDirection = new Vector3(moveAxis.x, 0f, moveAxis.y);

            // STEP 2: Transform local direction by the character's current rotation.
            Vector3 worldDirection = transform.TransformDirection(localDirection);

            // STEP 3: Ensure we only move on the XZ plane (no vertical from rotation).
            worldDirection.y = 0f;
            return worldDirection;
        }
    }
}