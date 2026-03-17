// File: Runtime/Abilities/ICharacterAbility.cs
// Namespace: Kojiko.MCharacterController.Abilities
//
// Summary:
// 1. Defines the contract for character abilities (sprint, jump, crouch, slide, etc.).
// 2. Abilities can read input, motor, and camera state via Initialize().
// 3. Abilities can modify the desired movement vector before CharacterMotor.Step().
// 4. Abilities can react after CharacterMotor.Step() (e.g., landing, timers, VFX).
//
// Used by:
// - CharacterAbilityController
// - Concrete abilities such as SprintAbility, JumpAbility, CrouchAbility, SlideAbility.

using UnityEngine;
using Kojiko.MCharacterController.Core;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Abilities
{
    /// <summary>
    /// Base interface for all character abilities.
    /// 
    /// Lifecycle:
    /// - Initialize(...) is called once when the CharacterAbilityController is set up.
    /// - Tick(...) is called every frame BEFORE CharacterMotor.Step(), allowing abilities
    ///   to adjust the desired world-space movement vector or internal state.
    /// - PostStep(...) is called every frame AFTER CharacterMotor.Step(), allowing abilities
    ///   to react to the final velocity, grounded state, etc.
    /// 
    /// Typical usage:
    /// - Sprint: scale desiredMoveWorld when sprint input is active.
    /// - Jump: on JumpPressed, call CharacterMotor.SetVerticalVelocity(jumpVelocity).
    /// - Crouch: adjust CharacterController height/center and reduce movement speed.
    /// - Slide: inject slide velocity and manage slide duration/friction.
    /// </summary>
    public interface ICharacterAbility
    {
        /// <summary>
        /// Called once when the ability is registered with the CharacterAbilityController.
        /// Provides references to core systems the ability may need.
        /// </summary>
        /// <param name="motor">The CharacterMotor controlling movement.</param>
        /// <param name="controllerRoot">The main CharacterControllerRoot orchestrating this character.</param>
        /// <param name="input">The input source implementing ICcInputSource.</param>
        /// <param name="cameraRig">The active camera rig, if any (may be null).</param>
        void Initialize(
            MCharacter_Motor_Controller motor,
            MCharacter_Root_Controller controllerRoot,
            ICcInputSource input,
            CameraRig_Base cameraRig);

        /// <summary>
        /// Called every frame BEFORE CharacterMotor.Step().
        /// 
        /// Abilities can:
        /// - Read input and current motor state.
        /// - Modify the desiredMoveWorld vector (e.g., scale for sprint, redirect for slide).
        /// - Update internal timers and state.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        /// <param name="desiredMoveWorld">
        /// Desired horizontal move vector in world space (Y should be 0).
        /// This is passed by ref so abilities can modify it.
        /// </param>
        void Tick(float deltaTime, ref Vector3 desiredMoveWorld);

        /// <summary>
        /// Called every frame AFTER CharacterMotor.Step().
        /// 
        /// Abilities can:
        /// - Inspect the final motor velocity and grounded state.
        /// - Detect landing events.
        /// - Trigger VFX/SFX or update secondary systems.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        void PostStep(float deltaTime);
    }
}