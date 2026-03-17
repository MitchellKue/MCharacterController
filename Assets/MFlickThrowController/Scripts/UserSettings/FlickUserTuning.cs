/*
------------------------------------------------------------
File: FlickUserTuning.cs
Description:
    Player-facing sensitivity modifiers.
    These affect feel, not mechanical validation.

------------------------------------------------------------
*/

using UnityEngine;

namespace FlickThrowSystem.UserSettings
{
    [System.Serializable]
    public class FlickUserTuning
    {
        [Header("Power Sensitivity")]
        [Range(0.1f, 3f)]
        public float powerSensitivity = 1f;

        [Header("Angle Sensitivity")]
        [Range(0.1f, 3f)]
        public float angleSensitivity = 1f;

        [Header("Spin Sensitivity")]
        [Range(0.1f, 3f)]
        public float rollSpinSensitivity = 1f;

        [Range(0.1f, 3f)]
        public float curveSpinSensitivity = 1f;
    }
}