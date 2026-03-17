MCharacterController – Input
============================

Overview
--------
The Input layer abstracts away the underlying input system (Unity Input System,
legacy Input, custom devices, etc.) and exposes a clean, game-focused interface
to Core and Abilities.

Files
-----

ICcInputSource.cs
-----------------
Role:
- Interface that defines what input data the character controller needs.

Responsibilities:
- Exposes high-level input properties and methods, such as:
  - MoveAxis (Vector2) – movement input (e.g., WASD / left stick).
  - LookAxis (Vector2) – look input (e.g., mouse / right stick).
  - Action buttons (jump, crouch, sprint, interact, etc.), typically via:
    - Boolean properties, methods like WasPressed/IsHeld, or equivalent.

Notes:
- MCharacter_Root_Controller depends on an ICcInputSource to drive movement
  and look.
- Abilities may also query the same ICcInputSource (via the root controller)
  to respond to inputs relevant to that ability.

NewInputSystem_Source.cs
------------------------
Role:
- Concrete implementation of ICcInputSource for Unity's "new" Input System.

Responsibilities:
- Maps Unity Input System actions to ICcInputSource properties and methods.
- Handles:
  - Reading action values (e.g., Vector2 for move/look).
  - Translating button presses/releases to the interface expected by Core/Abilities.
- Serves as the default input provider for characters in projects using the
  new Input System.

Notes:
- Can be replaced with other ICcInputSource implementations (e.g., AI input,
  replay/ghost input, custom devices) without changing the Core or Abilities
  code.