/*
------------------------------------------------------------
File: FlickValidator.cs
Description:
    Applies mechanical rules and shaping logic to analyzed flick.

Responsibilities:
    1. Cancel downward flicks
    2. Enforce drag thresholds
    3. Enforce forward bias cone
    4. Apply diagonal power reduction (AnimationCurve)
    5. Apply horizontal deadzones
    6. Apply spin threshold
    7. Apply fallback force logic

------------------------------------------------------------
*/

using UnityEngine;
using FlickThrowSystem.Core;

namespace FlickThrowSystem.Analysis
{
    public class FlickValidator
    {
        private readonly FlickSettings settings;

        public FlickValidator(FlickSettings settings)
        {
            this.settings = settings;
        }

        public FlickResult Validate(FlickAnalyzer.AnalyzedFlick flick)
        {
            // ----------------------------------------------------
            // 1. Drag Distance Validation
            // ----------------------------------------------------

            if (flick.DragDistance < settings.minDragDistance ||
                flick.DragDistance > settings.maxDragDistance)
            {
                return Invalid();
            }

            // ----------------------------------------------------
            // 2. Downward Flick Cancel
            // ----------------------------------------------------

            if (flick.ForwardComponent <= 0f)
            {
                return Invalid();
            }

            // ----------------------------------------------------
            // 3. Forward Bias Cone Check
            // ----------------------------------------------------

            Vector2 direction = flick.NormalizedVelocity.normalized;
            float forwardDot = Vector2.Dot(direction, Vector2.up);

            if (forwardDot < settings.minForwardDot)
            {
                return Invalid();
            }

            // ----------------------------------------------------
            // 4. Diagonal Power Reduction (Curve-Based)
            // ----------------------------------------------------

            float diagonalMultiplier =
                settings.diagonalPowerCurve.Evaluate(forwardDot);

            float adjustedForward =
                flick.ForwardComponent * diagonalMultiplier;

            // ----------------------------------------------------
            // 5. Horizontal Deadzone
            // ----------------------------------------------------

            float adjustedHorizontal = 0f;

            if (Mathf.Abs(flick.HorizontalComponent) >
                settings.horizontalDeviationThreshold)
            {
                adjustedHorizontal = flick.HorizontalComponent;
            }

            // ----------------------------------------------------
            // 6. Fallback Force Logic
            // ----------------------------------------------------

            float speedMagnitude = flick.RawVelocity.magnitude;

            if (speedMagnitude < settings.minReleaseSpeed)
            {
                adjustedForward = settings.fallbackForce;
            }

            // ----------------------------------------------------
            // 7. Return Valid Flick
            // ----------------------------------------------------

            Vector2 finalVelocity =
                new Vector2(adjustedHorizontal, adjustedForward);

            return new FlickResult(
                finalVelocity,
                flick.DragDistance,
                flick.Duration,
                flick.StartTime,
                flick.EndTime,
                true
            );
        }

        private FlickResult Invalid()
        {
            return new FlickResult(
                Vector2.zero,
                0f,
                0f,
                0f,   // StartTime
                0f,   // EndTime
                false
            );
        }
    }
}