// File: Runtime/Camera/IAimLookRig.cs
// Namespace: Kojiko.MCharacterController.Camera
//
// Summary:
// - Optional extension interface for CameraRigBase.
// - Lets abilities (e.g., aiming) drive FOV, camera offset, and look sensitivity multiplier.

using UnityEngine;

namespace Kojiko.MCharacterController.Camera
{
    /// <summary>
    /// Optional extension for camera rigs that support ADS-style control.
    /// </summary>
    public interface IAimLookRig
    {
        /// <summary>
        /// Base (hip-fire) FOV, used as the "non-aiming" reference FOV.
        /// </summary>
        float BaseFOV { get; set; }

        /// <summary>
        /// Set the current FOV directly (in degrees).
        /// </summary>
        void SetFOV(float fov);

        /// <summary>
        /// Set a local camera offset that should be applied when aiming.
        /// How this offset is used is up to the rig implementation.
        /// </summary>
        void SetAimOffset(Vector3 offset);

        /// <summary>
        /// Set a multiplier for look sensitivity when aiming.
        /// 1.0 = base sensitivity, <1 = slower, >1 = faster.
        /// </summary>
        void SetAimSensitivityMultiplier(float multiplier);
    }
}