/*
------------------------------------------------------------
File: FlickSettings.cs
Description:
    Mechanical rules & thresholds for flick validation.
    These are NOT player-facing sensitivity sliders.

------------------------------------------------------------
*/

using UnityEngine;

namespace FlickThrowSystem.Core
{
    [System.Serializable]
    public class FlickSettings
    {
        [Header("Sampling")]
        public float velocityWindow = 0.08f; // 80ms

        [Header("Drag Rules")]
        public float minDragDistance = 25f;
        public float maxDragDistance = 600f;

        [Header("Velocity Rules")]
        public float minReleaseSpeed = 50f;
        public float fallbackForce = 3f;

        [Header("Directional Rules")]
        [Range(0f, 1f)]
        public float minForwardDot = 0.5f; // forward bias cone

        [Header("Horizontal Deadzones")]
        public float horizontalDeviationThreshold = 0.05f;
        public float spinThreshold = 0.05f;

        [Header("Power Falloff")]
        [Range(0f, 1f)]
        public float diagonalPowerReductionThreshold = 0.7f;

        [Header("Power Falloff Curve")]
        public AnimationCurve diagonalPowerCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
}