// File: Runtime/Feedback/MCharacter_StanceFeedback.cs
// Namespace: Kojiko.MCharacterController.Feedback
//
// Summary:
// 1. Listens to MCharacter_Stance_Controller.OnStanceChanged.
// 2. Plays stance-specific SFX (optional).
// 3. Drives Animator parameters for stance (optional).
// 4. Exposes simple UnityEvents for designers to hook into (optional).
//
// Notes:
// - Keep this component dumb: no stance logic, only reacts to events.
// - Safe if references are missing: it will early-out and just not do feedback.
//

using UnityEngine;
using UnityEngine.Events;
using Kojiko.MCharacterController.Core;

namespace Kojiko.MCharacterController.Feedback
{
    /// <summary>
    /// Handles visual/audio feedback when the character stance changes.
    /// Intended to sit on the same GameObject as the stance controller or a
    /// nearby "character root" object.
    ///
    /// Responsibilities:
    /// 1. Subscribe to MCharacter_Stance_Controller.OnStanceChanged.
    /// 2. Optionally:
    ///    - Drive an Animator (e.g., stance int / bools / triggers).
    ///    - Play stance-specific SFX AudioClips.
    ///    - Fire UnityEvents so designers can hook arbitrary feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public class MCharacter_StanceFeedback : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Serialized configuration
        // --------------------------------------------------------------------

        [Header("References")]

        [Tooltip("Stance controller that owns the stance state and fires OnStanceChanged.")]
        [SerializeField] private MCharacter_Stance_Controller _stanceController;

        [Tooltip("Optional Animator to drive stance parameters (3rd person, arms, etc.).")]
        [SerializeField] private Animator _animator;

        [Tooltip("Optional AudioSource used to play stance change SFX.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Animator Settings")]

        [Tooltip("If true, updates these Animator parameters when stance changes.")]
        [SerializeField] private bool _useAnimator = false;

        [Tooltip("Animator int parameter name to set to the current stance enum value (optional).")]
        [SerializeField] private string _animParam_StanceInt = "Stance";

        [Tooltip("Animator bool parameter name for 'IsCrouching' (optional).")]
        [SerializeField] private string _animParam_IsCrouching = "IsCrouching";

        [Tooltip("Animator bool parameter name for 'IsProne' (optional).")]
        [SerializeField] private string _animParam_IsProne = "IsProne";

        [Header("Audio Settings")]

        [Tooltip("If true, plays SFX when stance changes.")]
        [SerializeField] private bool _useAudio = false;

        [Tooltip("AudioClip played when entering Standing stance.")]
        [SerializeField] private AudioClip _sfx_Standing;

        [Tooltip("AudioClip played when entering Crouching stance.")]
        [SerializeField] private AudioClip _sfx_Crouching;

        [Tooltip("AudioClip played when entering Prone stance.")]
        [SerializeField] private AudioClip _sfx_Prone;

        [Header("Unity Events")]

        [Tooltip("Invoked whenever stance changes (oldStance -> newStance).")]
        [SerializeField] private UnityEvent<CharacterStance, CharacterStance> _onStanceChanged;

        [Tooltip("Invoked when entering Standing stance.")]
        [SerializeField] private UnityEvent _onEnterStanding;

        [Tooltip("Invoked when entering Crouching stance.")]
        [SerializeField] private UnityEvent _onEnterCrouching;

        [Tooltip("Invoked when entering Prone stance.")]
        [SerializeField] private UnityEvent _onEnterProne;

        // --------------------------------------------------------------------
        // Unity lifecycle
        // --------------------------------------------------------------------

        private void Awake()
        {
            // 1. Try to auto-find stance controller if not assigned.
            if (_stanceController == null)
            {
                _stanceController = GetComponent<MCharacter_Stance_Controller>();
            }

            // 2. Warn (once) if we still don't have a stance controller.
            if (_stanceController == null)
            {
                UnityEngine.Debug.LogWarning("[MCharacter_StanceFeedback] Missing MCharacter_Stance_Controller reference. Feedback will be disabled.", this);
            }

            // 3. Optional: auto-assign AudioSource if not set.
            if (_useAudio && _audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (_stanceController != null)
            {
                _stanceController.OnStanceChanged += HandleStanceChanged;
            }
        }

        private void OnDisable()
        {
            if (_stanceController != null)
            {
                _stanceController.OnStanceChanged -= HandleStanceChanged;
            }
        }

        // --------------------------------------------------------------------
        // Event handler
        // --------------------------------------------------------------------

        /// <summary>
        /// Called by the stance controller whenever the stance changes.
        /// </summary>
        private void HandleStanceChanged(CharacterStance oldStance, CharacterStance newStance)
        {
            // 1. UnityEvents for designers / external hooks.
            _onStanceChanged?.Invoke(oldStance, newStance);

            switch (newStance)
            {
                case CharacterStance.Standing:
                    _onEnterStanding?.Invoke();
                    break;

                case CharacterStance.Crouching:
                    _onEnterCrouching?.Invoke();
                    break;

                case CharacterStance.Prone:
                    _onEnterProne?.Invoke();
                    break;
            }

            // 2. Animator feedback.
            if (_useAnimator && _animator != null)
            {
                ApplyAnimatorStance(newStance);
            }

            // 3. Audio feedback.
            if (_useAudio && _audioSource != null)
            {
                PlayStanceSfx(newStance);
            }
        }

        // --------------------------------------------------------------------
        // Animator helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Updates Animator parameters to reflect the current stance.
        ///
        /// Example usage:
        /// - "Stance" int: 0 = Standing, 10 = Crouching, 20 = Prone (matches enum values).
        /// - "IsCrouching" bool.
        /// - "IsProne" bool.
        ///
        /// This lets you keep Animator setup flexible without hard-coding it here.
        /// </summary>
        private void ApplyAnimatorStance(CharacterStance newStance)
        {
            if (_animator == null)
                return;

            // 1. Int stance param.
            if (!string.IsNullOrEmpty(_animParam_StanceInt))
            {
                _animator.SetInteger(_animParam_StanceInt, (int)newStance);
            }

            // 2. Bool params.
            if (!string.IsNullOrEmpty(_animParam_IsCrouching))
            {
                bool isCrouching = newStance == CharacterStance.Crouching;
                _animator.SetBool(_animParam_IsCrouching, isCrouching);
            }

            if (!string.IsNullOrEmpty(_animParam_IsProne))
            {
                bool isProne = newStance == CharacterStance.Prone;
                _animator.SetBool(_animParam_IsProne, isProne);
            }

            // If you ever want triggers like "OnCrouch" / "OnStand", you could
            // add them here and fire only when transitioning into that stance.
        }

        // --------------------------------------------------------------------
        // Audio helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Plays a stance-specific AudioClip on the configured AudioSource.
        /// </summary>
        private void PlayStanceSfx(CharacterStance newStance)
        {
            if (_audioSource == null)
                return;

            AudioClip clip = null;

            switch (newStance)
            {
                case CharacterStance.Standing:
                    clip = _sfx_Standing;
                    break;

                case CharacterStance.Crouching:
                    clip = _sfx_Crouching;
                    break;

                case CharacterStance.Prone:
                    clip = _sfx_Prone;
                    break;
            }

            if (clip == null)
                return;

            // Use PlayOneShot so we don't stomp looping movement audio, etc.
            _audioSource.PlayOneShot(clip);
        }
    }
}