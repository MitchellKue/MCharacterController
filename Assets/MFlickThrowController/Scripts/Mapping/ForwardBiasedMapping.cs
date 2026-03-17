/*
------------------------------------------------------------
File: ForwardBiasedMapping.cs
Description:
    Converts flick into:
        - Forward-biased world velocity
        - Yaw deviation
        - Roll spin (local Z)
        - Curve spin (world Y)

Flow:
    1. Apply user sensitivities
    2. Compute yaw angle
    3. Compute world direction
    4. Apply velocity
    5. Apply spin torque
    6. Store debug data for visualization

Notes:
    - Assumes flick has already been validated.
    - Does NOT perform validation.
    - Designed for injection & extensibility.
------------------------------------------------------------
*/

using UnityEngine;
using FlickThrowSystem.Core;
using FlickThrowSystem.UserSettings;

namespace FlickThrowSystem.Mapping
{
    public class ForwardBiasedMapping : IThrowMappingStrategy
    {
        // --------------------------------------------------------
        // Debug Data Container
        // --------------------------------------------------------

        public struct DebugThrowData
        {
            public Vector3 Origin;
            public Vector3 FinalDirection;
            public float Power;
            public float YawAngle;
            public float RollSpin;
            public float CurveSpin;
        }

        public DebugThrowData LastDebugData { get; private set; }

        // --------------------------------------------------------
        // Dependencies
        // --------------------------------------------------------

        private readonly ThrowMappingSettings mappingSettings;
        private readonly FlickUserTuning userTuning;

        // Optional runtime debug drawing
        private readonly bool drawRuntimeDebug;

        // --------------------------------------------------------
        // Constructor
        // --------------------------------------------------------

        public ForwardBiasedMapping(
            ThrowMappingSettings mappingSettings,
            FlickUserTuning userTuning,
            bool drawRuntimeDebug = false)
        {
            this.mappingSettings = mappingSettings;
            this.userTuning = userTuning;
            this.drawRuntimeDebug = drawRuntimeDebug;
        }

        // --------------------------------------------------------
        // Main Mapping Entry
        // --------------------------------------------------------

        public void MapThrow(
            FlickResult flick,
            Transform cameraTransform,
            Rigidbody rb)
        {
            if (!flick.IsValid)
                return;

            // ----------------------------------------------------
            // 1. Apply User Sensitivity Scaling
            // ----------------------------------------------------

            float forward =
                flick.ScreenVelocity.y *
                userTuning.powerSensitivity;

            float horizontal =
                flick.ScreenVelocity.x;

            // ----------------------------------------------------
            // 2. Compute Throw Power
            // ----------------------------------------------------

            float power =
                forward *
                mappingSettings.basePowerMultiplier;

            // ----------------------------------------------------
            // 3. Compute Yaw Deviation
            // ----------------------------------------------------

            float yawAngle =
                horizontal *
                mappingSettings.maxYawAngle *
                userTuning.angleSensitivity;

            Vector3 baseDir = cameraTransform.forward;

            Vector3 finalDir =
                Quaternion.AngleAxis(yawAngle, Vector3.up) *
                baseDir;

            // ----------------------------------------------------
            // 4. Apply Linear Velocity
            // ----------------------------------------------------

            Vector3 velocity = finalDir * power;

            rb.linearVelocity = velocity;

            // ----------------------------------------------------
            // 5. Spin Logic (Roll + Curve)
            // ----------------------------------------------------

            float rollSpin = 0f;
            float curveSpin = 0f;

            if (Mathf.Abs(horizontal) >
                mappingSettings.spinActivationThreshold)
            {
                // Roll Spin (local Z / around forward axis)
                rollSpin =
                    horizontal *
                    mappingSettings.rollSpinMultiplier *
                    userTuning.rollSpinSensitivity;

                rb.AddTorque(
                    cameraTransform.forward * -rollSpin,
                    ForceMode.Impulse);

                // Curve Spin (world Y axis)
                curveSpin =
                    horizontal *
                    mappingSettings.curveSpinMultiplier *
                    userTuning.curveSpinSensitivity;

                rb.AddTorque(
                    Vector3.up * curveSpin,
                    ForceMode.Impulse);
            }

            // ----------------------------------------------------
            // 6. Store Debug Data
            // ----------------------------------------------------

            LastDebugData = new DebugThrowData
            {
                Origin = rb.position,
                FinalDirection = finalDir,
                Power = power,
                YawAngle = yawAngle,
                RollSpin = rollSpin,
                CurveSpin = curveSpin
            };

            // ----------------------------------------------------
            // 7. Optional Runtime Debug Lines
            // ----------------------------------------------------

            if (drawRuntimeDebug)
            {
                Debug.DrawLine(
                    rb.position,
                    rb.position + finalDir * 2f,
                    Color.green,
                    2f);

                Debug.DrawLine(
                    rb.position,
                    rb.position + cameraTransform.forward * 2f,
                    Color.yellow,
                    2f);

                if (rollSpin != 0f)
                {
                    Debug.DrawLine(
                        rb.position,
                        rb.position +
                        cameraTransform.forward * -rollSpin * 0.1f,
                        Color.red,
                        2f);
                }

                if (curveSpin != 0f)
                {
                    Debug.DrawLine(
                        rb.position,
                        rb.position +
                        Vector3.up * curveSpin * 0.1f,
                        Color.magenta,
                        2f);
                }
            }
        }
    }
}