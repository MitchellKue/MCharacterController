// File: Runtime/Camera/CameraRigBase.cs
// Namespace: Kojiko.MCharacterController.Camera
//
// Summary:
// 1. Defines a common interface for all camera rigs (FPS, TPS, etc.).
// 2. Allows CharacterControllerRoot to talk to any camera rig generically.
// 3. Provides hooks for initialization and look handling.
//
// Dependencies:
// - Kojiko.MCharacterController.Core.CharacterControllerRoot (calls Initialize/HandleLook).
// - Concrete implementations (e.g., FirstPersonCameraRig).

using UnityEngine;

namespace Kojiko.MCharacterController.Camera
{
    /// <summary>
    /// 1. STEP 1: Initialize with a reference to the character root transform.
    /// 2. STEP 2: Handle look input (yaw/pitch) each frame based on provided axes.
    /// 3. STEP 3: Derived classes implement specific camera logic (FPS, TPS).
    /// </summary>
    public abstract class CameraRig_Base : MonoBehaviour
    {
        /// <summary>
        /// Called from CharacterControllerRoot when the rig is set up.
        /// </summary>
        /// <param name="characterRoot">
        /// The main character root transform (usually the GameObject containing CharacterControllerRoot).
        /// </param>
        public abstract void Initialize(Transform characterRoot);

        /// <summary>
        /// Called every frame by CharacterControllerRoot to apply look input.
        /// </summary>
        /// <param name="lookAxis">
        /// Look delta (x = yaw, y = pitch).
        /// </param>
        /// <param name="deltaTime">
        /// Time step for this frame.
        /// </param>
        public abstract void HandleLook(Vector2 lookAxis, float deltaTime);
    }
}