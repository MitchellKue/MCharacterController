/*
------------------------------------------------------------
File: ThrowMappingSettings.cs
Description:
    Designer-level mapping controls for converting flick
    into world velocity & spin.

------------------------------------------------------------
*/

using UnityEngine;

namespace FlickThrowSystem.Mapping
{
    [System.Serializable]
    public class ThrowMappingSettings
    {
        [Header("Throw Direction")]
        public float maxYawAngle = 25f;

        [Header("Power Scaling")]
        public float basePowerMultiplier = 15f;

        [Header("Spin")]
        public float rollSpinMultiplier = 10f;
        public float curveSpinMultiplier = 8f;

        public float spinActivationThreshold = 0.1f;
    }
}