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

        [SerializeField] private string _interactActionName = "Interact";

        [Header("Base Locomotion")]
        [SerializeField] private string _moveActionName = "Move";
        [SerializeField] private string _sprintActionName = "Sprint";

        
        [Header("Base Camera")]
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _switchViewActionName = "SwitchView";

        [Header("Extra Abilities")]
        [SerializeField] private string _aimActionName = "AimDownSight";
        [SerializeField] private string _jumpActionName = "Jump";
        [SerializeField] private string _crouchActionName = "Crouch";


        // Internal references
        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private InputAction _switchViewAction;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _aimAction;

        // Cached values
        private Vector2 _moveAxis;
        private Vector2 _lookAxis;
        private bool _switchViewPressed;
        private bool _sprintHeld;
        //
        private bool _interactPressed;
        private bool _interactHeld;
        //
        private bool _jumpPressed;
        private bool _jumpHeld;
        //
        private bool _crouchPressed;
        private bool _crouchHeld;
        //
        private bool _aimHeld; 
        private bool _aimPressed; 

        public Vector2 MoveAxis => _moveAxis;
        public Vector2 LookAxis => _lookAxis;

        public bool SwitchViewPressed => _switchViewPressed;
        public bool SprintHeld => _sprintHeld;

        //
        public bool InteractPressed =>  _interactPressed;
        public bool InteractHeld =>  _interactHeld;
        //
        public bool JumpPressed => _jumpPressed;
        public bool JumpHeld => _jumpHeld;
        //
        public bool CrouchPressed => _crouchPressed;
        public bool CrouchHeld => _crouchHeld;
        //
        public bool AimHeld => _aimHeld;
        public bool AimPressed => _aimPressed;

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

            _switchViewPressed = _switchViewAction != null && _switchViewAction.WasPressedThisFrame();

            _crouchPressed = _crouchAction != null && _crouchAction.WasPressedThisFrame();
            _crouchHeld = _crouchAction != null && _crouchAction.IsPressed();

            _aimPressed = _aimAction != null && _aimAction.WasPressedThisFrame();
            _aimHeld = _aimAction != null && _aimAction.IsPressed();
        }
    }
}