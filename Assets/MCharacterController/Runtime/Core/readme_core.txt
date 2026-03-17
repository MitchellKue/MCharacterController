MCharacterController – Core
===========================

Overview
--------
The Core layer is the heart of the MCharacterController. It owns the main character
loop and coordinates movement, abilities, and integration with input and camera.

IMPORTANT: The Abilities system is considered part of Core. The root controller has
a hard, intentional dependency on abilities and calls into them every frame. This
tight coupling is by design for simplicity.

Files
-----

MCharacter_Root_Controller.cs
-----------------------------
Role:
- Top-level orchestrator for a single character instance.

Responsibilities:
- Owns references to:
  - MCharacter_Motor_Controller (movement/motor)
  - MCharacter_Ability_Controller (abilities)
  - Input source (ICcInputSource)
  - Camera rig (CameraRig_Base)
- Initializes the character on startup (order: input, camera, motor, abilities).
- Per-frame:
  - Reads input through ICcInputSource.
  - Feeds look input into the active CameraRig.
  - Converts movement input into a world-space movement vector.
  - Calls into MCharacter_Motor_Controller to perform movement.
  - Calls into MCharacter_Ability_Controller to tick abilities (pre/post motor step).
- Exposes simple locking APIs (movement/camera) that abilities and other systems
  can use to temporarily disable player control.

Notes:
- This is the primary entry point for the abilities system from Core.
- Any system that wants to extend character behavior should plug into or route
  through MCharacter_Root_Controller.

MCharacter_Motor_Controller.cs
------------------------------
Role:
- Low-level movement and physics wrapper for the character.

Responsibilities:
- Manages the actual motion of the character (typically via CharacterController
  or Rigidbody).
- Takes in a desired movement vector (from the root controller) and applies:
  - Grounding checks.
  - Gravity and vertical motion.
  - Slope handling and step offsets.
  - Velocity integration and final position updates.
- Exposes properties and methods commonly used by abilities and higher-level
  gameplay code, for example:
  - Current velocity, grounded state, surface normal, etc.
  - Methods to apply external impulses or overrides.

Notes:
- Does not read input directly; it only responds to commands from
  MCharacter_Root_Controller and/or abilities.
- Consider this the "motor/locomotion" layer of the character.

MCharacter_Ability_Controller.cs
--------------------------------
Role:
- Central manager for all character abilities, tightly coupled to Core.

Responsibilities:
- Owns and updates a collection of abilities that implement ICharacterAbility.
- Provides a single entry point for the Root controller to interact with abilities:
  - Initialization of all abilities.
  - Per-frame ticking of abilities.
  - Post-movement callbacks so abilities can react to the final motor state.
- Coordinates cross-ability concerns, for example:
  - Enforcing priority between abilities.
  - Handling movement/camera locks issued by abilities via the root controller.
  - Resolving conflicts (e.g., sprint + crouch, jump while climbing, etc.).

Notes:
- This controller intentionally lives in the Core layer and is *not* decoupled
  from it. Abilities are treated as a first-class, built-in part of the character
  controller rather than an optional plugin.
- MCharacter_Root_Controller should be considered the "one and only" Core entry
  point into this abilities layer.