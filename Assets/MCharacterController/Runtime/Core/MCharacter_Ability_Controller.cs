// File: Runtime/Core/CharacterAbilityController.cs
// Namespace: Kojiko.MCharacterController.Core
//
// Summary:
// 1. Collects and manages ICharacterAbility components for a character.
// 2. Initializes all abilities with references to motor, controller root, input, and camera rig.
// 3. Calls abilities before and after CharacterMotor.Step() each frame.
//
// Dependencies:
// - Kojiko.MCharacterController.Abilities.ICharacterAbility
// - Kojiko.MCharacterController.Core.CharacterMotor
// - Kojiko.MCharacterController.Core.CharacterControllerRoot
// - Kojiko.MCharacterController.Input.ICcInputSource
// - Kojiko.MCharacterController.Camera.CameraRigBase
//
// Usage:
// - Add this component to the same GameObject as CharacterControllerRoot.
// - Either:
//     * Explicitly list abilities in _abilityBehaviours, or
//     * Leave the list empty and let it auto-discover ICharacterAbility MonoBehaviours on this GameObject.
// - From CharacterControllerRoot.Awake(), call Initialize(...) once references are resolved.
// - From CharacterControllerRoot.Update():
//     * Call TickAbilities(deltaTime, ref moveDirection) BEFORE motor.Step().
//     * Call PostStepAbilities(deltaTime) AFTER motor.Step().

using UnityEngine;
using System.Collections.Generic;
using Kojiko.MCharacterController.Abilities;
using Kojiko.MCharacterController.Input;
using Kojiko.MCharacterController.Camera;

namespace Kojiko.MCharacterController.Core
{
    [DisallowMultipleComponent]
    public class MCharacter_Ability_Controller : MonoBehaviour
    {
        [Header("Ability Source")]

        [Tooltip(
            "Optional explicit list of behaviours that implement ICharacterAbility.\n" +
            "If empty, all components on this GameObject that implement ICharacterAbility\n" +
            "will be auto-discovered at initialization.")]
        [SerializeField]
        private List<MonoBehaviour> _abilityBehaviours = new();

        // Internal list of abilities, derived from _abilityBehaviours.
        private readonly List<ICharacterAbility> _abilities = new();

        private bool _initialized;
        private MCharacter_Root_Controller _characterRootController;

        /// <summary>
        /// Initializes all abilities associated with this controller.
        /// Should be called once from CharacterControllerRoot.Awake(), after
        /// motor, input, and camera rig references are known.
        /// </summary>
        /// <param name="motor">The CharacterMotor controlling movement.</param>
        /// <param name="characterRootController">The main CharacterControllerRoot.</param>
        /// <param name="input">The ICcInputSource for this character.</param>
        /// <param name="cameraRig">The active CameraRigBase (may be null).</param>
        public void Initialize(
            MCharacter_Motor_Controller motor,
            MCharacter_Root_Controller characterRootController,
            ICcInputSource input,
            CameraRig_Base cameraRig)
        {
            _abilities.Clear();

            // set the root controller that must be on the player root
            if (characterRootController != null)
            {
                _characterRootController = characterRootController;
            }

            // if no motor controller or root controller exists, early out and print log
            if (motor == null || characterRootController == null)
            {
                UnityEngine.Debug.LogWarning("[CharacterAbilityController] Missing motor or controllerRoot; abilities will not be initialized.", this);
                _initialized = false;
                return;
            }

            // If no explicit list is provided, auto-discover from this GameObject (abilities should be placed on the root game object to correctly function).
            if (_abilityBehaviours == null || _abilityBehaviours.Count == 0)
            {
                // new list of abilities
                _abilityBehaviours = new List<MonoBehaviour>();

                GetComponents(_abilityBehaviours);
            }

            // cycle through each found component
            foreach (var mb in _abilityBehaviours)
            {
                // if we got none, early out
                if (mb == null)
                    continue;

                // if we found a valid ability
                if (mb is ICharacterAbility ability)
                {
                    // print log
                    UnityEngine.Debug.Log($"[CharacterAbilityController] Found ability: {mb.GetType().Name}", mb);

                    // add ability to list
                    _abilities.Add(ability);
                    // initialize the ability
                    ability.Initialize(motor, characterRootController, input, cameraRig);
                }
                else
                {
                    // log warning, component found doesnt implement ICharacterAbility
                    UnityEngine.Debug.LogWarning(
                        $"[CharacterAbilityController] Behaviour '{mb.name}' on '{name}' " +
                        "is listed but does not implement ICharacterAbility.",
                        mb);
                }
            }

            // complete initialization
            _initialized = true;
        }

        /// <summary>
        /// Called from CharacterControllerRoot.Update() BEFORE CharacterMotor.Step().
        /// Allows all abilities to modify the desired movement vector and update state.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        /// <param name="desiredMoveWorld">
        /// Desired horizontal move vector in world space (Y should be 0).
        /// This is passed by ref so abilities can modify it.
        /// </param>
        public void TickAbilities(float deltaTime, ref Vector3 desiredMoveWorld)
        {
            //var exclusive = _characterRootController.MovementLockedByAbility;

            if (!_initialized || deltaTime <= 0f)
                return;

        //    if (exclusive != null)
        {
            // Only the locking ability controls movement this frame
        //    exclusive.Tick(deltaTime, ref desiredMoveWorld);
        //    return;
        }

        // Normal case: tick all abilities
        foreach (var ability in _abilities)
        {
            ability.Tick(deltaTime, ref desiredMoveWorld);
        }
        }

        /// <summary>
        /// Called from CharacterControllerRoot.Update() AFTER CharacterMotor.Step().
        /// Allows abilities to react to the final motor state (grounded, velocity, etc.).
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        public void PostStepAbilities(float deltaTime)
        {
            if (!_initialized || deltaTime <= 0f)
                return;

            for (int i = 0; i < _abilities.Count; i++)
            {
                var ability = _abilities[i];
                if (ability == null)
                    continue;

                ability.PostStep(deltaTime);
            }
        }
    }
}