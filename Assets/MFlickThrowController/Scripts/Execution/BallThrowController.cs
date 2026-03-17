/*
------------------------------------------------------------
File: BallThrowController.cs
Description:
    High-level orchestrator for flick-based throwing.

Responsibilities:
    1. Manage sampling lifecycle
    2. Invoke analyzer
    3. Invoke validator
    4. Invoke mapping strategy
    5. Lock camera during flick

This class contains NO throw math.
All logic lives in injected modules.

------------------------------------------------------------
*/

using UnityEngine;
using FlickThrowSystem.Core;
using FlickThrowSystem.Sampling;
using FlickThrowSystem.Analysis;
using FlickThrowSystem.Mapping;
using FlickThrowSystem.Input;
using UnityEngine.InputSystem;

namespace FlickThrowSystem.Execution
{
    public class BallThrowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody ballRigidbody;
        [SerializeField] private Transform cameraTransform;

        [Header("Settings")]
        [SerializeField] private FlickSettings flickSettings;
        [SerializeField] private ThrowMappingSettings mappingSettings;
        [SerializeField] private UserSettings.FlickUserTuning userTuning;

        private PointerSampler sampler;
        private FlickAnalyzer analyzer;
        private FlickValidator validator;
        public FlickResult LastFlickResult { get; private set; }
        private ForwardBiasedMapping mappingStrategy; 
        private IPointerInputSource inputSource;

        private bool isSampling;

        // --------------------------------------------------------
        // 1. Initialization
        // --------------------------------------------------------

        private void Awake()
        {
            ApplyInputType(currentInputType);

            sampler = new PointerSampler();
            analyzer = new FlickAnalyzer();
            validator = new FlickValidator(flickSettings);

            mappingStrategy =
            new ForwardBiasedMapping(
                mappingSettings,
                userTuning,
                true); // enable runtime debug lines
        }

        // --------------------------------------------------------
        // 2. Update Loop
        // --------------------------------------------------------

        private void Update()
        {
            HandleInput();

            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                ToggleInputSource();
            }
        }

        // --------------------------------------------------------
        // 3. Input Handling (Temporary Mouse Implementation)
        // --------------------------------------------------------
        private void HandleInput()
        {
            if (inputSource == null)
                return;

            if (inputSource.IsPressedThisFrame())
            {
                StartFlick(inputSource.GetPosition());
                Debug.Log("clicked");
            }

            if (inputSource.IsHeld() && isSampling)
            {
                sampler.AddSample(
                    inputSource.GetPosition(),
                    Time.time);
                Debug.Log("held (sampeling)");
            }

            if (inputSource.IsReleasedThisFrame() && isSampling)
            {
                EndFlick();
                Debug.Log("released");
            }
        }


        // --------------------------------------------------------
        // 4. Flick Lifecycle
        // --------------------------------------------------------

        private void StartFlick(Vector2 position)
        {
            isSampling = true;

            sampler.StartSampling(position, Time.time);

            LockCamera(true);
        }

        private void EndFlick()
        {
            isSampling = false;

            var resultData =
                sampler.CalculateWindowedVelocity(
                    flickSettings.velocityWindow);

            var analyzed =
                analyzer.Analyze(
                    resultData.velocity,
                    resultData.dragDistance,
                    resultData.duration);

            LastFlickResult =
                validator.Validate(analyzed);

            // Apply throw if valid
            if (LastFlickResult.IsValid)
            {
                mappingStrategy.MapThrow(
                    LastFlickResult,
                    cameraTransform,
                    ballRigidbody);
            }

            LockCamera(false);
        }

        // --------------------------------------------------------
        // 5. Camera Lock Hook
        // --------------------------------------------------------

        private void LockCamera(bool locked)
        {
            // Hook into thr camera controller here
            // Example:
            // cameraController.SetInputEnabled(!locked);
        }

        // input
        public enum InputType
        {
            Mouse,
            Gamepad
        }

        [SerializeField]
        private InputType currentInputType = InputType.Mouse;

        private void ApplyInputType(InputType type)
        {
            // Prevent swapping during active flick
            if (isSampling)
            {
                Debug.LogWarning("Cannot switch input while flick is active.");
                return;
            }

            currentInputType = type;

            switch (currentInputType)
            {
                case InputType.Mouse:
                    inputSource = new MousePointerInputSource();
                    Debug.Log("Switched to Mouse Input");
                    break;

                case InputType.Gamepad:
                    inputSource = new GamepadStickInputSource();
                    Debug.Log("Switched to Gamepad Input");
                    break;
            }
        }

        // Input Switching API
        public void SetInputMouse()
        {
            ApplyInputType(InputType.Mouse);
        }

        public void SetInputGamepad()
        {
            ApplyInputType(InputType.Gamepad);
        }

        public void ToggleInputSource()
        {
            if (currentInputType == InputType.Mouse)
                ApplyInputType(InputType.Gamepad);
            else
                ApplyInputType(InputType.Mouse);
        }

        //mapping
        public ForwardBiasedMapping GetMappingStrategy()
        {
            return mappingStrategy;
        }
    }
}