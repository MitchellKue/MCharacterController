/*
------------------------------------------------------------
File: FlickResult.cs
Description:
    Output from FlickAnalyzer after successful validation.

Flow:
    1. Analyzer computes velocity & drag info
    2. Validator approves flick
    3. FlickResult passed to mapping layer
------------------------------------------------------------
*/

using UnityEngine;

namespace FlickThrowSystem.Core
{
    public struct FlickResult
    {
        public bool IsValid;

        public float StartTime;
        public float EndTime;
        public float Duration;

        public float TotalDistance;

        public Vector2 ScreenVelocity;

        public FlickResult(
            Vector2 screenVelocity,
            float totalDistance,
            float duration,
            float startTime,
            float endTime,
            bool isValid)
        {
            ScreenVelocity = screenVelocity;
            TotalDistance = totalDistance;
            Duration = duration;
            StartTime = startTime;
            EndTime = endTime;
            IsValid = isValid;
        }
    }
}