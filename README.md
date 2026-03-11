# MCharacterController

A small, opinionated character controller for Unity’s **kinematic** `CharacterController`.

- Designed for **first-person** and **third-person** style controllers.
- Built around **separation of concerns**:
  - `CharacterMotor` – pure movement and physics-ish behavior.
  - `CharacterControllerRoot` – orchestrates input, motor, camera, and abilities.
  - `CharacterAbilityController` – discovers and ticks abilities.
  - `CameraRigBase` + `CameraRig_FPV` – camera behavior (FPS style).
  - `ICcInputSource` + `NewInputSystem_Source` – input abstraction.
  - `MovementDirectionGizmos` / `CameraDirectionGizmos` – debug‑only visualization.
- Uses **Unity’s New Input System**.
- Target engine: **Unity 6.0.62** (but should work on recent 2022+ with minor changes).
- Currently tuned for internal use, but code is public on GitHub.

Core abilities layer on top of the motor:

- **Sprint**
- **Jump** (with coyote time + jump buffering)
- **Crouch** (FPS-style)
- **Head bob** (FPS-style)
- **Aim Down Sight (ADS)** (camera/FOV/sensitivity/offset only)
- **Dash**
- **Zipline**
- **Climb** (simple teleports + full climb volumes)
- **External forces** (knockback, wind, etc.)

---

## Goals and design philosophy

### 1. Clear responsibilities

Each piece has a single job:

- Movement logic lives in the **motor**, not in input or camera.
- The **root/orchestrator** glues input, camera, motor, and abilities together.
- **Input** is abstracted via an interface so it can be swapped (player, AI, network).
- **Abilities** are small, focused components that plug into a shared ability pipeline.
- **Debug visuals** are kept in dedicated gizmo components.

This makes it easier to:

- Swap input sources (e.g. AI, replay, network).
- Change camera rigs without touching movement code.
- Add/remove abilities without changing the motor or root.
- Upgrade the motor without breaking UI, camera, input, or gameplay systems.

### 2. Kinematic, not rigidbody

The controller is built around Unity’s `CharacterController`:

- Great for classic FPS/TPS, platformers, and “hero controllers”.
- Predictable collisions and step offset behavior.
- Movement is authored via velocities and accelerations, but ultimately applied with `CharacterController.Move`.

### 3. Editor‑friendly debugging

We lean heavily on **Gizmos**:

- **MovementDirectionGizmos**:
  - Ellipse visualizing directional **max speeds** (forward/back/strafe).
  - Line showing **current horizontal velocity** and **acceleration state**.
- **CameraDirectionGizmos**:
  - Line showing camera forward direction.
  - Sphere at the end for quick visual of where the camera is “looking”.
- **SimpleZiplinePair / SimpleClimbPair / ClimbVolume**:
  - Lines, spheres, and cones showing paths, teleports, and detection cones.
- **Ability_Dash / Ability_Climb**:
  - Dash direction/length and climb auto-start cone.

The goal is to let you glance at the Scene view and immediately understand:

- “How fast *can* I go in this direction?”
- “How fast *am* I currently moving?”
- “Am I accelerating or braking?”
- “Where is the camera actually pointing?”
- “Where does this ladder / zipline / dash go?”

---

## Quick start

### 1. Requirements

- **Unity**: 6.0.62 (or equivalent recent Unity version; minor changes might be required for older 2022 LTS).
- **Input**: Unity’s **New Input System** enabled in Project Settings.
- **Actions**: A `PlayerInput` with an action map named `"Player"` containing:

Core movement & abilities:

- `"Move"` – `Vector2`
- `"Look"` – `Vector2`
- `"Jump"` – `Button`
- `"Sprint"` – `Button`
- `"Crouch"` – `Button`
- `"Dash"` – `Button`
- `"AimDownSight"` – `Button`
- `"Interact"` – `Button`
- `"SwitchView"` – `Button` (reserved / optional)

You can rename these; just update the action names in `NewInputSystem_Source`.

### 2. Using the provided prefabs (recommended)

1. Drag the **Player** prefab (or equivalent in your project) into the scene.
2. Ensure the prefab includes (names are the actual class names in `Runtime`):
   - `CharacterController` (Unity built‑in)
   - `MCharacter_Motor`
   - `MCharacter_Controller_Root`
   - `MCharacter_Controller_Ability`
   - `NewInputSystem_Source` (with a `PlayerInput` on the same GameObject)
   - `CameraRig_FPV` on or near the main Camera
   - Optionally, abilities (any subset you want):
     - `Ability_Sprint`
     - `Ability_Jump`
     - `Ability_FPV_Crouch`
     - `Ability_FPV_HeadBob`
     - `Ability_FPV_AimDownSight`
     - `Ability_Dash`
     - `Ability_Zipline`
     - `Ability_Climb`
     - `Ability_ExternalForces`
3. Press Play:
   - Move with WASD / left stick.
   - Look around with mouse / right stick.
   - Sprint, jump, crouch, dash, aim, interact, etc., depending on which abilities are enabled.

> For most users, using the prefab is enough. The components are modular if you need to re‑wire manually.

### 3. Wiring manually (advanced / custom rigs)

Minimal setup:

1. **GameObject: Character Root**
   - Add `CharacterController`.
   - Add `MCharacter_Motor`.
   - Add `MCharacter_Controller_Root`.
   - Add `MCharacter_Controller_Ability`.

2. **Input**
   - Add `PlayerInput` with your Input Actions asset.
   - Add `NewInputSystem_Source` on the same GameObject as `PlayerInput`.
   - In `MCharacter_Controller_Root`, assign `_inputSourceBehaviour` to the `NewInputSystem_Source` component.

3. **Camera rig**
   - Create a camera with:
     - `CameraRig_FPV` (implements `CameraRig_Base`, `IAimLookRig`, `IClimbLookRig`).
   - In `MCharacter_Controller_Root`, assign `_cameraRig` to that camera rig.
   - On `CameraRig_FPV`:
     - `_yawRoot` (optional) – usually the character root.
     - `_pitchRoot` – usually a pivot under the camera or the camera transform itself.

4. **Abilities**
   - On the same GameObject as `MCharacter_Controller_Root`, add any combination of:
     - `Ability_Sprint`
     - `Ability_Jump`
     - `Ability_FPV_Crouch`
     - `Ability_FPV_HeadBob`
     - `Ability_FPV_AimDownSight`
     - `Ability_Dash`
     - `Ability_Zipline`
     - `Ability_Climb`
     - `Ability_ExternalForces`
   - In `MCharacter_Controller_Ability`:
     - Either leave `_abilityBehaviours` empty to auto-discover these components,
       or explicitly list them.

5. **Environment helpers (optional)**
   - Add **SimpleClimbPair** + **SimpleClimbTriggerRelay** for very simple “ladder” teleports.
   - Add **SimpleZiplinePair** + **SimpleZiplineTriggerRelay** for basic ziplines.

6. **Debug gizmos (optional but recommended)**
   - On the character root:
     - Add `MovementDirectionGizmos`.
       - Assign `_motor` or leave it empty to auto‑grab `MCharacter_Motor`.
   - On the character root or a debug object:
     - Add `CameraDirectionGizmos`.
       - Assign `_cameraTransform` to your main camera or its pivot.
       - Optionally set `_originTransform` to the character to show camera direction from character position.

Make sure **Scene view Gizmos** are enabled to see the visualizations.

---

## Components overview

This section is aimed at new team members reading the code for the first time.

---

### 1. `MCharacter_Motor` (Core movement)

**File:** `Runtime/Core/MCharacter_Motor.cs`  
**Namespace:** `Kojiko.MCharacterController.Core`

**Responsibility:**  
Owns all movement math and calls into Unity’s `CharacterController`.

**Key ideas:**

- Takes a **desired world‑space horizontal move direction** each frame (from `MCharacter_Controller_Root` + abilities).
- Decides how fast to move based on:
  - `_forwardSpeed`
  - `_backwardSpeed`
  - `_strafeSpeed`
- Applies separate **acceleration** and **deceleration** values:
  - Faster when speeding up / turning (`_acceleration`).
  - Potentially harder brake (`_deceleration`).
  - Scaled down in air by `_airAccelerationMultiplier`.
- Handles **gravity** and a smaller **grounded gravity**:
  - `_gravity` pulls you down when not grounded.
  - `_groundedGravity` keeps you “stuck” to ground when grounded.
- Accepts **external velocity contributions** from abilities:
  - `AddExternalHorizontalVelocity(Vector3)`
  - `AddExternalVerticalVelocity(float)`
  - Cleared every frame at the end of `Step`.

**API surface most other systems care about:**

- Read‑only properties:
  - `IsGrounded`
  - `Velocity`
  - `HorizontalVelocity`
  - `CurrentSpeed`
  - `CurrentAcceleration`
  - `ForwardSpeed`, `BackwardSpeed`, `StrafeSpeed`, `MaxGroundSpeed`
  - `SpeedMultiplier` (settable; used by Sprint/Crouch/etc.)

- Main methods:
  - `Step(Vector3 desiredMoveWorld, float deltaTime)` – called once per frame by `MCharacter_Controller_Root`.
  - `SetVerticalVelocity(float newVerticalVelocity)` – used by jump, climb, zipline, etc.
  - `TeleportToPoint(...)` – used by climb teleports, ziplines, and any hard reposition.
  - `AddExternalHorizontalVelocity(...)`, `AddExternalVerticalVelocity(...)` – used by dash, climb, external forces, zipline, etc.
  - `ClearExternalVelocities()` – usually called internally; used manually in a few abilities for “hard reset”.

**Why this separation?**

- Higher‑level code doesn’t need to know about gravity or acceleration logic.
- Tests / future simulations can run the motor with arbitrary input vectors.
- Abilities like sprint, slide, jump, dash, climb, and ziplines can adjust:
  - The input vector
  - The speed caps
  - Or call `SetVerticalVelocity` / add external velocities without editing core movement math.

---

### 2. `MCharacter_Controller_Root` (Orchestrator)

**File:** `Runtime/Core/MCharacter_Controller_Root.cs`  
**Namespace:** `Kojiko.MCharacterController.Core`

**Responsibility:**  
Glue between **input**, **motor**, **camera rig**, and **abilities**.

**What it does each frame (`Update`)**:

1. Reads:
   - `MoveAxis` (Vector2) from `ICcInputSource`
   - `LookAxis` (Vector2) from the same source
2. Sends `LookAxis` to `CameraRig_Base.HandleLook`.
3. Converts `MoveAxis` into a **world‑space** move direction using its own transform orientation.
4. Calls `_abilityController?.TickAbilities(dt, ref moveDirection)` so abilities can:
   - Modify `moveDirection`,
   - Add external forces via the motor,
   - Handle input and internal state.
5. Calls `_motor.Step(moveDirection, dt)`.
6. Calls `_abilityController?.PostStepAbilities(dt)` so abilities can react to the final motor state.

**Key fields:**

- `_motor` – movement implementation (`MCharacter_Motor`).
- `_inputSourceBehaviour` – any `MonoBehaviour` that implements `ICcInputSource`.
- `_cameraRig` – a `CameraRig_Base` implementation (current: `CameraRig_FPV`).
- `_abilityController` – `MCharacter_Controller_Ability`.

---

### 3. `MCharacter_Controller_Ability` and `ICharacterAbility` (Ability pipeline)

#### `ICharacterAbility`

**File:** `Runtime/Abilities/ICharacterAbility.cs`  
**Namespace:** `Kojiko.MCharacterController.Abilities`

Defines the contract for abilities:

- `Initialize(MCharacter_Motor, MCharacter_Controller_Root, ICcInputSource, CameraRig_Base)`
- `Tick(float deltaTime, ref Vector3 desiredMoveWorld)` – called **before** motor step.
- `PostStep(float deltaTime)` – called **after** motor step.

Typical usage:

- Sprint scales `SpeedMultiplier`.
- Jump sets vertical velocity.
- Crouch adjusts movement speed and character height.
- Dash overrides or augments `desiredMoveWorld`.
- Climb/Zipline/ExternalForces push via external velocities and may zero `desiredMoveWorld`.

#### `MCharacter_Controller_Ability`

**File:** `Runtime/Core/MCharacter_Controller_Ability.cs`  
**Namespace:** `Kojiko.MCharacterController.Core`

**Responsibility:**  
Finds and manages all `ICharacterAbility` components for a character.

- On `Initialize(...)`:
  - Either uses an explicit list `_abilityBehaviours`, or
  - Auto-discovers all `MonoBehaviour`s on the same GameObject that implement `ICharacterAbility`.
  - Calls `Initialize(...)` on each.
- On `TickAbilities(...)`:
  - Calls `Tick(...)` on all abilities in order.
- On `PostStepAbilities(...)`:
  - Calls `PostStep(...)` on all abilities.

This keeps each ability small and focused while letting them share input/motor/camera.

---

### 4. Input: `ICcInputSource` and `NewInputSystem_Source`

#### `ICcInputSource`

**File:** `Runtime/Input/ICcInputSource.cs`  
**Namespace:** `Kojiko.MCharacterController.Input`

Defines a minimal, extensible input contract:

- Movement & look:
  - `Vector2 MoveAxis`
  - `Vector2 LookAxis`
- Buttons (pressed vs held where relevant):
  - `bool JumpPressed`, `JumpHeld`
  - `bool SprintHeld`
  - `bool CrouchPressed`, `CrouchHeld`
  - `bool DashPressed`
  - `bool AimHeld`, `AimPressed`
  - `bool InteractPressed`, `InteractHeld`
  - `bool SwitchViewPressed`

Any system (player, AI, network replay) can implement this and plug into the controller.

#### `NewInputSystem_Source`

**File:** `Runtime/Input/NewInputSystemSource.cs`  
**Namespace:** `Kojiko.MCharacterController.Input`

Concrete implementation using Unity’s **New Input System**:

- Wraps `PlayerInput` and an action map (default `"Player"`).
- Maps the following actions (configurable via serialized strings):
  - `"Move"`
  - `"Look"`
  - `"Jump"`
  - `"Sprint"`
  - `"Crouch"`
  - `"Dash"`
  - `"AimDownSight"`
  - `"Interact"`
  - `"SwitchView"`
- In `Update`, fills the `ICcInputSource` properties based on:
  - `ReadValue<Vector2>()` for axes.
  - `WasPressedThisFrame()` / `IsPressed()` for buttons.

---

### 5. Camera: `CameraRig_Base`, `CameraRig_FPV`, `IAimLookRig`, `IClimbLookRig`

#### `CameraRig_Base`

**File:** `Runtime/Camera/CameraRig_Base.cs`  
**Namespace:** `Kojiko.MCharacterController.Camera`

Abstract base for all camera rigs:

- `Initialize(Transform characterRoot)`
- `HandleLook(Vector2 lookAxis, float deltaTime)`

Used by `MCharacter_Controller_Root` without caring about the concrete camera implementation.

#### `CameraRig_FPV` (First-person view rig)

**File:** `Runtime/Camera/CameraRig_FPV.cs`  
**Namespace:** `Kojiko.MCharacterController.Camera`

Implements:

- `CameraRig_Base`
- `IAimLookRig` – for ADS FOV/offset/sensitivity control.
- `IClimbLookRig` – for climb-time yaw/pitch constraints.

**Responsibilities:**

- **Yaw** on `_yawRoot` (usually character root).
- **Pitch** on `_pitchRoot` (camera pivot).
- Caches:
  - `_currentPitch` (normalized \([-180, 180)\))
  - `_currentYaw`
- Supports:
  - Configurable `_sensitivityX`, `_sensitivityY`.
  - `_minPitch`, `_maxPitch` for default camera limits.
  - FOV handling and additive aim offsets.
  - Climb constraints clamping yaw around a surface forward and overriding pitch range.

##### `IAimLookRig`

**File:** `Runtime/Camera/IAimLookRig.cs`  

Allows abilities (e.g., `Ability_FPV_AimDownSight`) to:

- Read/write `BaseFOV`.
- `SetFOV(float)`
- `SetAimOffset(Vector3)`
- `SetAimSensitivityMultiplier(float)`

##### `IClimbLookRig`

**File:** `Runtime/Camera/IClimbLookRig.cs`  

Allows climb abilities to:

- Query `ClimbConstraintsActive`.
- `SetClimbConstraints(bool enabled, Vector3 surfaceForward, float maxYawFromSurface, float minPitch, float maxPitch)`.

---

### 6. Core abilities

#### `Ability_Sprint`

**File:** `Runtime/Abilities/Ability_Sprint.cs`  
**Namespace:** `Kojiko.MCharacterController.Abilities`

- Uses `SprintHeld` from input.
- Optionally requires:
  - Sufficient move magnitude.
  - Mostly-forward movement (`_restrictToForward`, `_maxForwardAngle`).
- When active:
  - Sets `_motor.SpeedMultiplier = _sprintSpeedMultiplier`.
- Exposes `IsSprinting` for other systems (e.g., head bob, VFX).

#### `Ability_Jump`

**File:** `Runtime/Abilities/Ability_Jump.cs`  

- Simple jump with:
  - Ground requirement (optional).
  - **Coyote time** (`_coyoteTime`).
  - **Jump buffer** (`_jumpBufferTime`).
- Uses `JumpPressed` and `JumpHeld` from input (held currently not used for variable height).
- When triggering a jump:
  - Calls `_motor.SetVerticalVelocity(_jumpSpeed)`.
- Tracks `IsJumping` for debug or animation.

#### `Ability_FPV_Crouch`

**File:** `Runtime/Abilities/Ability_FPV_Crouch.cs`  

FPS-style crouch:

- Smoothly lerps:
  - `CharacterController.height` and center.
  - Camera local Y position.
  - Optional body visual scale and offset.
- Supports:
  - Toggle vs hold input.
  - Optional air-crouch.
  - Movement speed multiplier while crouched.
  - Ceiling check to block uncrouching when obstructed.
- Uses `CrouchPressed` / `CrouchHeld` from input.
- Scales `desiredMoveWorld` by `_crouchSpeedMultiplier` when crouched.
- Exposes `IsCrouched`.

#### `Ability_FPV_HeadBob`

**File:** `Runtime/Abilities/Ability_FPV_HeadBob.cs`  

- Applies a sinusoidal head bob to a camera transform based on:
  - `MCharacter_Motor.CurrentSpeed`
  - `IsGrounded`
  - Sprint + crouch states (auto-detected via `Ability_Sprint` and `Ability_FPV_Crouch`).
- Chooses amplitudes/frequencies for:
  - Walking
  - Sprinting
  - Crouched
- Lerp-based enable/disable based on speed, so it eases in/out.
- Writes to camera local position relative to a cached base.

#### `Ability_FPV_AimDownSight`

**File:** `Runtime/Abilities/Ability_FPV_AimDownSight.cs`  

Camera-only ADS handling:

- Uses `AimHeld` from input (hold-to-aim; toggle prepared for future).
- Drives:
  - **FOV** via `IAimLookRig.SetFOV`.
  - **Look sensitivity multiplier** via `SetAimSensitivityMultiplier`.
  - **Camera local offset** via `SetAimOffset`.
- Blends smoothly between hip and ADS using `SmoothDamp` style curves.
- Exposes:
  - `IsAiming`
  - `AimBlend` (0 = fully hip, 1 = fully ADS).
- Configurable per-weapon or script:
  - `SetFOVSettings(hipFOV, adsFOV)`
  - `SetSensitivitySettings(hipMult, adsMult)`
  - `SetADSOffset(offset)`

#### `Ability_Dash`

**File:** `Runtime/Abilities/Ability_Dash.cs`  

Fast directional dash:

- Uses `DashPressed` and `MoveAxis` from input.
- Dash direction is **camera-relative**:
  - Uses `CameraRig_FPV` (or main camera) forward/right.
  - Can restrict to cardinal directions or allow diagonals.
- Per-direction distances:
  - `_forwardDistance`
  - `_backwardDistance`
  - `_strafeDistance`
- Dash is **duration-based** and near-instant:
  - `_dashDuration` controls dash time.
  - Internally computes a per-frame velocity such that total dash distance is covered over the duration.
- Can:
  - Allow ground-dash and/or air-dash.
  - Cancel vertical velocity on start.
  - Override or add to normal movement while dashing.
- Cooldown via `_cooldown`.
- Draws debug gizmos:
  - Active dash path.
  - Preview dash path (while on cooldown or idle with input).

#### `Ability_Zipline`

**File:** `Runtime/Abilities/Ability_Zipline.cs`  

Zipline support, works with `SimpleZiplinePair` volumes:

- `SimpleZiplinePair` has:
  - Two endpoints (bottom/top).
  - Two triggers (bottom/top) with `SimpleZiplineTriggerRelay`.
  - Configurable start mode:
    - `AutoOnEnter` (start immediately).
    - `ManualInteract` (needs Interact input).
- `Ability_Zipline`:
  - Validates presence and enabled state.
  - Attaches the character to the zipline based on a **zipline anchor** transform and world offset.
  - Moves along the line at `_ziplineSpeed`.
  - Can apply optional `_ziplineGravity` while gliding.
  - Exits:
    - Automatically at the end of the line.
    - Early when pressing `Jump` (if `_allowJumpExit`), with a forward impulse (`_exitForwardImpulse`).
- Uses motor `TeleportToPoint` each frame to keep the anchor exactly on the line.

#### `Ability_Climb`

**File:** `Runtime/Abilities/Ability_Climb.cs`  

General climb ability with support for:

- **Modes** (`ClimbMode`):
  - `Disabled`
  - `SimpleTeleport` – for `SimpleClimbPair` ladder-style teleports.
  - `FullClimb` – for proper `ClimbVolume` movement.
- Simple teleport:
  - `AllowSimpleClimbVolumes` returns `true` when in `SimpleTeleport` mode.
  - `SimpleClimbPair` + `SimpleClimbTriggerRelay` handle delayed teleports (with a small stay duration).
- Full climb:
  - Uses `ClimbVolume` (not shown here but part of the environment system).
  - Detects climb surfaces in front (raycast) and within radius (overlap).
  - Auto-start when walking into a climb surface (`_autoStartOnForwardMove`).
  - Manual start via `Interact` if configured.
  - While climbing:
    - Zeroes vertical velocity and drives vertical movement from `MoveAxis.y`.
    - Snaps horizontally to the climb line.
    - Faces the climb surface.
    - Disables normal horizontal movement input.
  - Exits:
    - `JumpPressed`
    - Leaving volume bounds (top/bottom thresholds).
  - Integrates with `IClimbLookRig` (CameraRig_FPV) to clamp yaw/pitch around the surface.

#### `Ability_ExternalForces`

**File:** `Runtime/Abilities/Ability_ExternalForces.cs`  

Shared external-forces system:

- Provides methods for other systems/gameplay code:
  - `AddImpulse(Vector3 impulseWorld, bool includeVertical = true)` – 1-frame velocity injection.
  - `AddForce(Vector3 forceWorld, float duration)` – time-based contributions.
  - `ClearAllForces()`.
- Internally:
  - Accumulates one-frame impulses and timed forces.
  - Optional exponential damping over time (`_useDamping`, `_dampingRate`).
  - Optional clamping of horizontal/vertical external speed.
- On `Tick`:
  - Resolves total contributions and routes them through:
    - `_motor.AddExternalHorizontalVelocity(...)`
    - `_motor.AddExternalVerticalVelocity(...)`

---

### 7. Environment helpers

#### `SimpleClimbPair` + `SimpleClimbTriggerRelay`

**Files:**

- `Runtime/Environment/SimpleClimbPair.cs`
- `Runtime/Environment/SimpleClimbTriggerRelay.cs`

Teleport-style “ladder”:

- Two colliders (top/bottom) marked as triggers.
- Two teleport points (top/bottom).
- When you enter a trigger and stay for `_climbDelaySeconds`, the motor is teleported to the opposite point, preserving facing.
- Only active if the character has `Ability_Climb` and `AllowSimpleClimbVolumes` is `true`.
- Trigger events are forwarded via `SimpleClimbTriggerRelay`.

#### `SimpleZiplinePair` + `SimpleZiplineTriggerRelay`

**Files:**

- `Runtime/Environment/SimpleZiplinePair.cs`
- `Runtime/Environment/SimpleZiplineTriggerRelay.cs`

Simple zipline volumes:

- Two triggers (bottom/top).
- Two endpoints (bottom/top).
- `ZiplineStartMode`:
  - `AutoOnEnter` – enter trigger to auto-start zipline.
  - `ManualInteract` – must press `Interact` while in trigger.
- Works in tandem with `Ability_Zipline` on the character:
  - Validates player tag (optional).
  - Locates `MCharacter_Motor` + `Ability_Zipline` on the entering object.
  - Calls `BeginZipline(...)` with correct direction (bottom→top or top→bottom).

---

### 8. Debug / visualization components

#### `MovementDirectionGizmos`

(See original README section; unchanged conceptually; uses `MCharacter_Motor` state.)

#### `CameraDirectionGizmos`

(See original README section; unchanged conceptually.)

Most environment and ability components also draw useful gizmos:

- `SimpleClimbPair` – lines between teleport points, arrows indicating directions.
- `SimpleZiplinePair` – zipline path and arrow.
- `Ability_Climb` – forward detection cone.
- `Ability_Dash` – path and endpoint of active or preview dash.

---

## Extending and customizing

- Implement your own `ICcInputSource` for AI or network-controlled characters.
- Implement a new `CameraRig_Base` (TPS camera, orbit camera, etc.) and plug it into `MCharacter_Controller_Root`.
- Add new abilities by implementing `ICharacterAbility`:
  - Use `Initialize` to grab motor/input/camera.
  - Use `Tick` to adjust `desiredMoveWorld`, call motor methods, or manipulate camera rigs.
  - Use `PostStep` for reactions (land detection, SFX/VFX, cooldowns).
- Compose feature sets per character by enabling/disabling specific ability components.
