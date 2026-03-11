// File: Runtime/Camera/IClimbLookRig.cs
// Namespace: Kojiko.MCharacterController.Camera
//
// Summary:
// - Optional extension interface for CameraRigBase.
// - Lets abilities constrain yaw/pitch while climbing.

using UnityEngine;

namespace Kojiko.MCharacterController.Camera
{
    /// <summary>
    /// Optional extension for camera rigs that support climb-style yaw/pitch constraints.
    /// </summary>
    public interface IClimbLookRig
    {
        /// <summary>
        /// Whether the rig is currently applying climb constraints.
        /// </summary>
        bool ClimbConstraintsActive { get; }

        /// <summary>
        /// Enable/disable climb constraints and configure them.
        /// </summary>
        /// <param name="enabled">True to enable constraints; false to disable.</param>
        /// <param name="surfaceForward">The forward direction of the climb surface (what the player should generally face).</param>
        /// <param name="maxYawFromSurface">Max allowed yaw deviation from surface forward (degrees).</param>
        /// <param name="minPitch">Minimum pitch (degrees).</param>
        /// <param name="maxPitch">Maximum pitch (degrees).</param>
        void SetClimbConstraints(
            bool enabled,
            Vector3 surfaceForward,
            float maxYawFromSurface,
            float minPitch,
            float maxPitch);
    }
}