// File: Runtime/Core/CharacterControllerRoot.cs
// Namespace: Kojiko.MCharacterController.Core
//
// (ORIGINAL COMMENT BLOCK UNCHANGED)

using UnityEngine;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;
using Kojiko.MCharacterController.Abilities;
using UnityEngine.Windows;

namespace Kojiko.MCharacterController.Core
{
    [DisallowMultipleComponent]
    public class MCharacter_Root_Controller : MonoBehaviour
    {
        [Header("Core References")]
        [Tooltip("Movement motor component responsible for handling CharacterController movement.")]
        [SerializeField] private MCharacter_Motor_Controller _motor;

        [Tooltip("Component that implements ICcInputSource (e.g., NewInputSystemSource).")]
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        [Tooltip("Active camera rig for this character (e.g., FirstPersonCameraRig).")]
        [SerializeField] private CameraRig_Base _cameraRig;

        [Header("Abilities References")]
        [Tooltip("ability controller.")]
        [SerializeField] private MCharacter_Ability_Controller _abilityController;

        // stance
        [SerializeField] private MCharacter_Stance_Controller _stanceController;
        [SerializeField] private float _stanceHoldTime = 0.3f;   // seconds required to hold
        //[SerializeField] private float _stanceHoldCooldown = 0.2f; // min time between "held" actions

        private float _stanceHoldTimer = 0f;       // runtime timer while crouch is held
        private bool _stanceHoldTriggered = false; // ensures only one action per hold


        // Internal cached interface
        private ICcInputSource _inputSource;

        private void Awake()
        {
            // STEP 1: Validate and cache the motor reference.
            if (_motor == null)
            {
                _motor = GetComponent<MCharacter_Motor_Controller>();
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

            // ADDED: ensure stanceController reference if not assigned
            if (_stanceController == null) // ADDED
            {                               // ADDED
                _stanceController = GetComponent<MCharacter_Stance_Controller>(); // ADDED
            }                               // ADDED

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

            // 1. Handle stance input directly.
            if (_stanceController != null)
            {
                // -----------------------------
                // Press: simple cycle
                // -----------------------------
                if (_inputSource.CrouchPressed)
                {
                    // Single button cycles: Standing -> Crouching -> Prone -> Crouching -> Standing...
                    _stanceController.CycleToNextStance();
                }

                // -----------------------------
                // Held: snap to min / max stance, once per hold
                // -----------------------------
                if (_inputSource.CrouchHeld)
                {
                    // Advance hold timer while button is held
                    _stanceHoldTimer += Time.deltaTime;

                    // If we've already triggered this hold, do nothing more
                    if (!_stanceHoldTriggered && _stanceHoldTimer >= _stanceHoldTime)
                    {
                        int index = _stanceController.StanceIndex;
                        int count = _stanceController.StanceCount;

                        if (count > 0)
                        {
                            // If not at 0 and button is held past threshold, go to min stance (typically Standing)
                            if (index != 0)
                            {
                                _stanceController.TrySetStance(CharacterStance.Standing);
                            }
                            // Else if not at last index and button is held, go to max stance (typically Prone)
                            else if (count > 1 && index != count - 1)
                            {
                                _stanceController.TrySetStance(CharacterStance.Prone);
                            }
                        }

                        // Mark that we've fired for this continuous hold
                        _stanceHoldTriggered = true;
                    }
                }
                else
                {
                    // Button released: reset for next hold
                    _stanceHoldTimer = 0f;
                    _stanceHoldTriggered = false;
                }
            }

            // STEP 3.5: Let abilities tweak moveDirection or other state before stepping the motor.
            _abilityController?.TickAbilities(dt, ref moveDirection);

            // STEP 4: Forward the computed move direction to the motor.
            _motor.Step(moveDirection, dt);

            // STEP 5: Let abilities react after the motor has stepped.
            _abilityController?.PostStepAbilities(dt);
        }


        #region MOVEMENT
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

        #endregion

        #region CAMERA


        #endregion
    }
}