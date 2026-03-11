// File: Runtime/Abilities/Ability_Zipline.cs
// Namespace: Kojiko.MCharacterController.Abilities

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Environment;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    [DisallowMultipleComponent]
    public class Ability_Zipline : MonoBehaviour, ICharacterAbility
    {
        [Header("General")]
        [SerializeField]
        [Tooltip("Enable/disable zipline globally.")]
        private bool _enabled = true;

        [Tooltip("How fast the character moves along the zipline (m/s).")]
        [SerializeField] private float _ziplineSpeed = 8f;

        [Tooltip("How strong gravity is while zipping (if you want a slight pull down).")]
        [SerializeField] private float _ziplineGravity = 0f;

        [Header("Exit")]
        [Tooltip("If true, pressing Jump exits the zipline early.")]
        [SerializeField] private bool _allowJumpExit = true;

        [Tooltip("Additional forward impulse when exiting the zipline.")]
        [SerializeField] private float _exitForwardImpulse = 3f;

        // Core refs
        private MCharacter_Motor _motor;
        private MCharacter_Controller_Root _controllerRoot;
        private ICcInputSource _input;
        private CameraRig_Base _cameraRig;

        private Transform _transform;

        // State
        private bool _isZipping;
        private SimpleZiplinePair _currentZipline;
        private Vector3 _startPos;
        private Vector3 _endPos;
        private float _t; // 0..1 along zipline

        public bool Enabled => _enabled;
        public bool IsZipping => _isZipping;
        public SimpleZiplinePair CurrentZipline => _currentZipline;

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

            _transform = controllerRoot != null ? controllerRoot.transform : transform;

            _isZipping = false;
            _currentZipline = null;
        }

        public void Tick(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (!_enabled || deltaTime <= 0f)
                return;

            if (_isZipping)
            {
                TickZipline(deltaTime, ref desiredMoveWorld);
            }
            else
            {
                // Manual start from within volume via Interact:
                // In auto mode, the SimpleZiplinePair calls us directly.
                if (_input != null && _input.InteractPressed)
                {
                    TryBeginZiplineFromInteract();
                }
            }
        }

        public void PostStep(float deltaTime)
        {
            // No-op for now
        }

        #endregion

        #region Zipline Logic

        internal bool BeginZipline(
            SimpleZiplinePair zipline,
            MCharacter_Motor motor,
            Transform startPoint,
            Transform endPoint)
        {
            if (!_enabled || _isZipping)
                return false;

            if (zipline == null || motor == null || startPoint == null || endPoint == null)
                return false;

            // Snap to start point, face along direction of line
            Vector3 startPos = startPoint.position;
            Vector3 endPos = endPoint.position;
            Vector3 dir = endPos - startPos;
            if (dir.sqrMagnitude < 0.0001f)
                return false;

            dir.Normalize();
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);

            _motor.TeleportToPoint(startPos, look);
            _motor.ClearExternalVelocities();
            _motor.SetVerticalVelocity(0f);

            _currentZipline = zipline;
            _startPos = startPos;
            _endPos = endPos;
            _t = 0f;
            _isZipping = true;

            return true;
        }

        private void TickZipline(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            if (_motor == null)
            {
                EndZipline(false);
                return;
            }

            // Exit via jump if allowed
            if (_allowJumpExit && _input != null && _input.JumpPressed)
            {
                ExitWithImpulse();
                return;
            }

            // Advance along line
            float lineLength = (_endPos - _startPos).magnitude;
            float travelSpeed = Mathf.Max(0.01f, _ziplineSpeed);

            float deltaT = (travelSpeed * deltaTime) / Mathf.Max(lineLength, 0.01f);
            _t += deltaT;

            if (_t >= 1f)
            {
                // Reached end
                _t = 1f;
                Vector3 endPos = Vector3.Lerp(_startPos, _endPos, _t);
                Vector3 dir = (_endPos - _startPos).normalized;
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);

                _motor.TeleportToPoint(endPos, look);
                EndZipline(false);
                return;
            }

            // Move motor along zipline
            Vector3 currentPos = Vector3.Lerp(_startPos, _endPos, _t);
            Vector3 direction = (_endPos - _startPos).normalized;
            Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);

            _motor.TeleportToPoint(currentPos, lookRot);

            // Optionally, apply slight gravity if you want a subtle sag feeling;
            // for now we just set vertical velocity and let motor handle it.
            if (_ziplineGravity != 0f)
            {
                _motor.SetVerticalVelocity(_ziplineGravity * deltaTime);
            }

            // Prevent normal move from affecting zipline
            desiredMoveWorld = Vector3.zero;
        }

        private void ExitWithImpulse()
        {
            if (_motor == null)
            {
                EndZipline(false);
                return;
            }

            Vector3 dir = (_endPos - _startPos).normalized;
            Vector3 horizontalDir = new Vector3(dir.x, 0f, dir.z).normalized;

            // Give a small forward push and resume normal gravity
            if (horizontalDir.sqrMagnitude > 0.0001f && _exitForwardImpulse > 0f)
            {
                _motor.AddExternalHorizontalVelocity(horizontalDir * _exitForwardImpulse);
            }

            EndZipline(false);
        }

        private void EndZipline(bool teleportToEndIfNeeded)
        {
            _isZipping = false;
            _currentZipline = null;
        }

        private void TryBeginZiplineFromInteract()
        {
            if (!_enabled || _isZipping || _motor == null)
                return;

            // Strategy: check all SimpleZiplinePair objects in scene and
            // ask them whether we are inside their trigger in ManualInteract mode.
            // You can optimize this later by having a manager or registration.
            var allPairs = FindObjectsOfType<SimpleZiplinePair>();
            foreach (var pair in allPairs)
            {
                if (pair.TryStartZiplineFromInteract(_motor, this))
                {
                    break;
                }
            }
        }

        #endregion
    }
}