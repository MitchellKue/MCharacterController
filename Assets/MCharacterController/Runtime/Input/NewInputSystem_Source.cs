// File: Runtime/Input/NewInputSystemSource.cs
// Namespace: Kojiko.MCharacterController.Input

using UnityEngine;
using UnityEngine.InputSystem;

namespace Kojiko.MCharacterController.Input
{
    [DisallowMultipleComponent]
    public class NewInputSystem_Source : MonoBehaviour, ICcInputSource
    {
        [Header("Input Action Map")]
        [SerializeField] private string _actionMapName = "Player";
        private PlayerInput _playerInput;

        [Header("Interact")]
        [SerializeField] private string _interactActionName = "Interact";
        private InputAction _interactAction;
        private bool _interactPressed;
        private bool _interactHeld;
        public bool InteractPressed => _interactPressed;
        public bool InteractHeld => _interactHeld;

        [Header("Move")]
        [SerializeField] private string _moveActionName = "Move";
        private InputAction _moveAction;
        private Vector2 _moveAxis;
        public Vector2 MoveAxis => _moveAxis;

        [Header("Sprint")]
        [SerializeField] private string _sprintActionName = "Sprint";
        private InputAction _sprintAction;
        private bool _sprintHeld;
        public bool SprintHeld => _sprintHeld;

        [Header("Look")]
        [SerializeField] private string _lookActionName = "Look";
        private InputAction _lookAction;
        private Vector2 _lookAxis;
        public Vector2 LookAxis => _lookAxis;


        [Header("Switch View Mode")]
        [SerializeField] private string _switchViewActionName = "SwitchView";
        private InputAction _switchViewAction;
        private bool _switchViewPressed;
        public bool SwitchViewPressed => _switchViewPressed;

        [Header("Aim Down sight")]
        [SerializeField] private string _aimActionName = "AimDownSight";
        private InputAction _aimAction;
        private bool _aimHeld;
        private bool _aimPressed;
        public bool AimHeld => _aimHeld;
        public bool AimPressed => _aimPressed;

        [Header("Jump")]
        [SerializeField] private string _jumpActionName = "Jump";
        private InputAction _jumpAction;
        private bool _jumpPressed;
        private bool _jumpHeld;
        public bool JumpPressed => _jumpPressed;
        public bool JumpHeld => _jumpHeld;

        [Header("Crouch")]
        [SerializeField] private string _crouchActionName = "Crouch";
        private InputAction _crouchAction;
        private bool _crouchPressed;
        private bool _crouchHeld;
        public bool CrouchPressed => _crouchPressed;
        public bool CrouchHeld => _crouchHeld;

        [Header("Dash")]
        [SerializeField] private string _dashActionName = "Dash";
        private InputAction _dashAction;
        private bool _dashPressed;
        public bool DashPressed => _dashPressed;




        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                UnityEngine.Debug.LogError("[NewInputSystemSource] PlayerInput component is required on the same GameObject.", this);
                enabled = false;
                return;
            }

            var actionMap = _playerInput.actions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (actionMap == null)
            {
                UnityEngine.Debug.LogError($"[NewInputSystemSource] Action map '{_actionMapName}' not found in PlayerInput actions.", this);
                enabled = false;
                return;
            }

            _switchViewAction = actionMap.FindAction(_switchViewActionName, throwIfNotFound: false);

            _interactAction = actionMap.FindAction(_interactActionName, throwIfNotFound: false);
            
            _jumpAction = actionMap.FindAction(_jumpActionName, throwIfNotFound: false);
            _sprintAction = actionMap.FindAction(_sprintActionName, throwIfNotFound: false);
            _dashAction = actionMap.FindAction(_dashActionName, throwIfNotFound: false);
            _crouchAction = actionMap.FindAction(_crouchActionName, throwIfNotFound: false);
            _aimAction = actionMap.FindAction(_aimActionName, throwIfNotFound: false);

            _moveAction = actionMap.FindAction(_moveActionName, throwIfNotFound: false);
            _lookAction = actionMap.FindAction(_lookActionName, throwIfNotFound: false);
            if (_moveAction == null || _lookAction == null)
            {
                UnityEngine.Debug.LogError("[NewInputSystemSource] Move and Look actions are required and must exist in the action map.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
            _sprintAction?.Enable();
            _dashAction?.Enable();
            _switchViewAction?.Enable();
            _crouchAction?.Enable();
            _aimAction?.Enable(); 
            _interactAction?.Enable(); 
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _lookAction?.Disable();
            _jumpAction?.Disable();
            _sprintAction?.Disable();
            _dashAction?.Disable();
            _switchViewAction?.Disable();
            _crouchAction?.Disable();
            _aimAction?.Disable(); 
            _interactAction?.Disable(); 
        }

        private void Update()
        {
            _interactPressed = _interactAction != null && _interactAction.WasPressedThisFrame();
            _interactHeld = _interactAction != null && _interactAction.IsPressed();

            _moveAxis = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            _lookAxis = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

            _jumpPressed = _jumpAction != null && _jumpAction.WasPressedThisFrame();
            _jumpHeld = _jumpAction != null && _jumpAction.IsPressed();

            _sprintHeld = _sprintAction != null && _sprintAction.IsPressed();

            _dashPressed = _dashAction != null && _dashAction.WasPressedThisFrame();

            _switchViewPressed = _switchViewAction != null && _switchViewAction.WasPressedThisFrame();

            _crouchPressed = _crouchAction != null && _crouchAction.WasPressedThisFrame();
            _crouchHeld = _crouchAction != null && _crouchAction.IsPressed();

            _aimPressed = _aimAction != null && _aimAction.WasPressedThisFrame();
            _aimHeld = _aimAction != null && _aimAction.IsPressed();
        }
    }
}