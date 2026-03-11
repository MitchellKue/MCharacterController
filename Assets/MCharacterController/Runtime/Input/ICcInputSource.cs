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
        bool InteractPressed { get; }  // NEW
        bool InteractHeld { get; }  // NEW

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

        //SPRINT
        /// <summary>
        /// True while the sprint button is held.
        /// </summary>
        bool SprintHeld { get; }

        bool DashPressed { get; }

        //JUMP
        /// <summary>
        /// True only on the frame the jump button is pressed.
        /// </summary>
        bool JumpPressed { get; }

        /// <summary>
        /// True while the jump button is held.
        /// </summary>
        bool JumpHeld { get; }
         
        //CROUCH
        /// <summary>
        /// True only on the frame the crouch button is pressed.
        /// </summary>
        bool CrouchPressed { get; }

        /// <summary>
        /// True while the crouch button is held.
        /// </summary>
        bool CrouchHeld { get; }

        // AIM
        /// <summary>
        /// True while the aim/ADS button is held.
        /// </summary>
        bool AimHeld { get; }

        /// <summary>
        /// True only on the frame the aim/ADS button is pressed.
        /// </summary>
        bool AimPressed { get; }

        //VIEW MDOE
        /// <summary>
        /// True only on the frame the view-switch button is pressed.
        /// (Reserved for later FPS/TPS switching.)
        /// </summary>
        bool SwitchViewPressed { get; }
    }
}