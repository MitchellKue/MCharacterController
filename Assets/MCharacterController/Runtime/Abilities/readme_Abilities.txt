MCharacterController – Abilities
================================

Overview
--------
The Abilities layer defines the interface used by all character abilities.
Abilities are considered a core part of the framework and are tightly integrated
with the Core layer via MCharacter_Ability_Controller and MCharacter_Root_Controller.

Files
-----

ICharacterAbility.cs
--------------------
Role:
- Common interface that all character abilities must implement.

Typical Responsibilities (per ability):
- Initialization:
  - Called once by MCharacter_Ability_Controller when the character is created.
  - Allows the ability to cache references to:
    - MCharacter_Root_Controller
    - MCharacter_Motor_Controller
    - Camera rig, input source, etc., as needed.
- Per-frame updates:
  - Called from MCharacter_Ability_Controller each frame/tick.
  - Reads input (via the root controller’s input source) and motor state.
  - Modifies motor parameters, movement direction, or character state.
- Post-step updates:
  - Optionally called after the motor has completed its move step.
  - Allows the ability to react to final velocity, grounded state, collisions, etc.

Notes:
- Abilities may:
  - Issue movement or camera locks through MCharacter_Root_Controller.
  - Adjust speed multipliers, jump forces, or other motor properties.
  - Interact with the camera through camera interfaces (e.g., IAimLookRig, IClimbLookRig).
- Examples of concrete abilities (not defined here but expected in projects):
  - Jump, Crouch, Sprint, Slide, Climb, Aim, Dash, etc.