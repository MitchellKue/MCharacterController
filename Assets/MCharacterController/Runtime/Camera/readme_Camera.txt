MCharacterController – Camera
=============================

Overview
--------
The Camera layer defines how the player looks around and how the view follows
the character. It is driven by Core (via MCharacter_Root_Controller) and can be
extended by abilities (e.g., aiming, climbing camera behavior).

Files
-----

CameraRig_Base.cs
-----------------
Role:
- Abstract base class for all camera rigs used by the character.

Responsibilities:
- Defines the common API for character-driven cameras, typically including:
  - Initialization with a target transform (usually the character root or head).
  - Methods for handling look input (yaw/pitch changes).
  - Methods for updating the camera position/rotation each frame.
- Provides shared logic and helpers used by concrete camera rigs:
  - Clamping pitch.
  - Smoothing rotations.
  - Offsets and follow behavior.

Notes:
- MCharacter_Root_Controller stores and calls into an instance of CameraRig_Base.
- Higher-level systems (abilities) can modify camera behavior by interacting with
  the active rig or implementing the camera interfaces below.

CameraRig_FPV.cs
----------------
Role:
- First-person view camera rig implementation.

Responsibilities:
- Implements CameraRig_Base for a first-person camera:
  - Attaches the camera to a head/eyes pivot.
  - Applies yaw and pitch from look input.
  - May handle head bobbing, FOV changes, and weapon/hand alignment.

Notes:
- Typically used as the default camera rig for first-person characters.
- Abilities such as aiming or sprinting may adjust properties like FOV, sensitivity,
  or offsets through this rig.

IAimLookRig.cs
--------------
Role:
- Interface for camera rigs that support an "aim" mode.

Responsibilities:
- Defines methods and/or properties to:
  - Enter and exit aiming state.
  - Adjust aiming-specific offsets, FOV, or sensitivity.
- Allows abilities (e.g., Aim/ADS ability) to manipulate the camera in a
  capability-aware way without depending on a specific rig implementation.

Notes:
- CameraRig_FPV is expected to implement this interface if it supports aiming.

IClimbLookRig.cs
----------------
Role:
- Interface for camera rigs that support a "climb" or special traversal mode.

Responsibilities:
- Defines methods and/or properties to:
  - Adjust camera orientation/offsets during climbing.
  - Handle transitions into and out of climbing state.

Notes:
- Climbing-related abilities can detect and control climb-specific camera behavior
  via this interface rather than hard-coding assumptions about the camera rig.