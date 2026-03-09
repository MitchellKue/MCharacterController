// File: Runtime/Input/ICcInputSource.cs
// Namespace: Kojiko.MCharacterController.Input
//
// Summary:
// 1. Defines the input contract used by the MCharacterController system.
// 2. Decouples character logic (movement, camera) from Unity's input APIs.
// 3. Allows plugging different input sources (New Input System, AI, network).
//
// Dependencies:
// - Implementations will depend on Unity's Input System (e.g. PlayerInput).
// - Used directly by Kojiko.MCharacterController.Core.CharacterControllerRoot.

using UnityEngine;

namespace Kojiko.MCharacterController.Input
{
    /// <summary>
    /// 1. STEP 1: Expose movement and look axes used by the character controller.
    /// 2. STEP 2: Expose buttons for actions (jump, sprint, view switch) for future abilities.
    /// 3. STEP 3: Allow any system (player, AI, network) to implement this for modular input handling.
    /// </summary>
    public interface ICcInputSource
    {
        /// <summary>
        /// Horizontal (x) and vertical (y) movement input.
        /// Convention: x = strafe (A/D), y = forward/back (W/S).
        /// </summary>
        Vector2 MoveAxis { get; }

        /// <summary>
        /// Horizontal (x) and vertical (y) look delta.
        /// Convention: x = yaw, y = pitch.
        /// </summary>
        Vector2 LookAxis { get; }

        /// <summary>
        /// True only on the frame the jump button is pressed.
        /// (Reserved for later ability implementation.)
        /// </summary>
        bool JumpPressed { get; }

        /// <summary>
        /// True while the jump button is held.
        /// (Reserved for later ability implementation.)
        /// </summary>
        bool JumpHeld { get; }

        /// <summary>
        /// True while the sprint button is held.
        /// (Reserved for later ability implementation.)
        /// </summary>
        bool SprintHeld { get; }

        /// <summary>
        /// True only on the frame the view-switch button is pressed.
        /// (Reserved for later FPS/TPS switching.)
        /// </summary>
        bool SwitchViewPressed { get; }
    }
}