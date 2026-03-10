// File: Runtime/Input/NewInputSystemSource.cs
// Namespace: Kojiko.MCharacterController.Input
//
// Summary:
// 1. Reads input from Unity's New Input System via a PlayerInput component.
// 2. Implements ICcInputSource so CharacterControllerRoot can consume input generically.
// 3. Bridges InputAction callbacks/values to simple properties.
//
// Dependencies:
// - UnityEngine.InputSystem.PlayerInput (on the same GameObject).
// - Actions expected in an action map named "Player":
//   - "Move"      (Vector2)
//   - "Look"      (Vector2)
//   - "Jump"      (Button)
//   - "Sprint"    (Button)
//   - "SwitchView" (Button)
//
// Used by:
// - Kojiko.MCharacterController.Core.CharacterControllerRoot.

using UnityEngine;
using UnityEngine.InputSystem;

namespace Kojiko.MCharacterController.Input
{
    /// <summary>
    /// 1. STEP 1: Get references to PlayerInput and relevant InputActions by name.
    /// 2. STEP 2: Read current input values (Move, Look, Jump, Sprint, SwitchView).
    /// 3. STEP 3: Expose these values through the ICcInputSource interface each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class NewInputSystem_Source : MonoBehaviour, ICcInputSource
    {
        [Header("Input Action Map / Action Names")]
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _switchViewActionName = "SwitchView";
        [SerializeField] private string _moveActionName = "Move";
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _sprintActionName = "Sprint";
        [SerializeField] private string _jumpActionName = "Jump";
        [SerializeField] private string _crouchActionName = "Crouch";

        // Internal references
        private PlayerInput _playerInput;
        private InputAction _switchViewAction;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction; 

        // Cached values
        private Vector2 _moveAxis;
        private Vector2 _lookAxis;
        private bool _switchViewPressed;
        private bool _sprintHeld;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _crouchPressed; 
        private bool _crouchHeld;  

        /// <inheritdoc />
        public Vector2 MoveAxis => _moveAxis;

        /// <inheritdoc />
        public Vector2 LookAxis => _lookAxis;


        /// <inheritdoc />
        public bool SwitchViewPressed => _switchViewPressed;


        /// <inheritdoc />
        public bool SprintHeld => _sprintHeld;

        /// <inheritdoc />
        public bool JumpPressed => _jumpPressed;

        /// <inheritdoc />
        public bool JumpHeld => _jumpHeld;

        /// <inheritdoc />
        public bool CrouchPressed => _crouchPressed; 

        /// <inheritdoc />
        public bool CrouchHeld => _crouchHeld;      

        private void Awake()
        {
            // STEP 1: Get the PlayerInput component on this GameObject.
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                UnityEngine.Debug.LogError("[NewInputSystemSource] PlayerInput component is required on the same GameObject.", this);
                enabled = false;
                return;
            }

            // STEP 2: Retrieve the specified action map.
            var actionMap = _playerInput.actions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (actionMap == null)
            {
                UnityEngine.Debug.LogError($"[NewInputSystemSource] Action map '{_actionMapName}' not found in PlayerInput actions.", this);
                enabled = false;
                return;
            }

            // STEP 3: Cache references to actions by their names.
            _moveAction = actionMap.FindAction(_moveActionName, throwIfNotFound: false);
            _lookAction = actionMap.FindAction(_lookActionName, throwIfNotFound: false);
            _jumpAction = actionMap.FindAction(_jumpActionName, throwIfNotFound: false);
            _sprintAction = actionMap.FindAction(_sprintActionName, throwIfNotFound: false);
            _switchViewAction = actionMap.FindAction(_switchViewActionName, throwIfNotFound: false);
            _crouchAction = actionMap.FindAction(_crouchActionName, throwIfNotFound: false); // NEW

            if (_moveAction == null || _lookAction == null)
            {
                UnityEngine.Debug.LogError("[NewInputSystemSource] Move and Look actions are required and must exist in the action map.", this);
                enabled = false;
                return;
            }

        }

        private void OnEnable()
        {
            // STEP 1: Enable the actions we care about when this component is enabled.
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
            _sprintAction?.Enable();
            _switchViewAction?.Enable();
            _crouchAction?.Enable();
        }

        private void OnDisable()
        {
            // STEP 1: Disable actions when this component is disabled to avoid leaks or warnings.
            _moveAction?.Disable();
            _lookAction?.Disable();
            _jumpAction?.Disable();
            _sprintAction?.Disable();
            _switchViewAction?.Disable();
            _crouchAction?.Disable();
        }

        private void Update()
        {
            // STEP 1: Read continuous axes (Move, Look).
            _moveAxis = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            _lookAxis = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

            // STEP 2: Compute button states (Jump, Sprint, SwitchView).
            // Jump pressed once per frame:
            _jumpPressed = _jumpAction != null && _jumpAction.WasPressedThisFrame();
            _jumpHeld = _jumpAction != null && _jumpAction.IsPressed();

            // Sprint held:
            _sprintHeld = _sprintAction != null && _sprintAction.IsPressed();

            // Switch view pressed once per frame:
            _switchViewPressed = _switchViewAction != null && _switchViewAction.WasPressedThisFrame();

            // STEP 3: Crouch
            _crouchPressed = _crouchAction != null && _crouchAction.WasPressedThisFrame();
            _crouchHeld = _crouchAction != null && _crouchAction.IsPressed();
        }
    }
}