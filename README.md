# MCharacterController

A small, opinionated character controller for Unity’s **kinematic** `CharacterController`.

- Designed for **first-person** and **third-person** style controllers.
- Built around **separation of concerns**:
  - `CharacterMotor` – pure movement and physics-ish behavior.
  - `CharacterControllerRoot` – orchestrates input, motor, and camera.
  - `CameraRigBase` + `FirstPersonCameraRig` – camera behavior.
  - `ICcInputSource` + `NewInputSystemSource` – input abstraction.
  - `MovementDirectionGizmos` / `CameraDirectionGizmos` – debug‑only visualization.
- Uses **Unity’s New Input System**.
- Target engine: **Unity 6.0.62** (but should work on recent 2022+ with minor changes).
- Currently tuned for internal use, but code is public on GitHub.

Planned features: **sprint, jump, crouch, slide**, and related abilities layered on top of the existing motor.

---

## Goals and design philosophy

### 1. Clear responsibilities

Each piece has a single job:

- Movement logic lives in the **motor**, not in input or camera.
- The **root/orchestrator** glues input, camera, and motor together.
- **Input** is abstracted via an interface so it can be swapped (player, AI, network).
- **Debug visuals** are kept in dedicated gizmo components.

This makes it easier to:

- Swap input sources (e.g. AI, replay, network).
- Change camera rigs without touching movement code.
- Upgrade the motor without breaking UI, camera, or input.

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

The goal is to let you glance at the Scene view and immediately understand:

- “How fast *can* I go in this direction?”
- “How fast *am* I currently moving?”
- “Am I accelerating or braking?”
- “Where is the camera actually pointing?”

---

## Quick start

### 1. Requirements

- **Unity**: 6.0.62 (or equivalent recent Unity version; minor changes might be required for older 2022 LTS).
- **Input**: Unity’s **New Input System** enabled in Project Settings.
- **Actions**: A `PlayerInput` with an action map named `"Player"` containing:
  - `"Move"` – `Vector2`
  - `"Look"` – `Vector2`
  - `"Jump"` – `Button` (future use)
  - `"Sprint"` – `Button` (future use)
  - `"SwitchView"` – `Button` (future use)

### 2. Using the provided prefabs (recommended)

1. Drag the **Player** prefab (or equivalent in your project) into the scene.
2. Ensure the prefab includes:
   - `CharacterController`
   - `CharacterMotor`
   - `CharacterControllerRoot`
   - `NewInputSystemSource` (with a `PlayerInput` on the same GameObject)
   - `FirstPersonCameraRig` (either on the main Camera or a pivot object)
3. Press Play:
   - Move with WASD / left stick (depending on your bindings).
   - Look around with mouse / right stick.

> For most users, using the prefab is enough. The components are modular if you need to re‑wire manually.

### 3. Wiring manually (advanced / custom rigs)

Minimal setup:

1. **GameObject: Character Root**
   - Add `CharacterController`.
   - Add `CharacterMotor`.
   - Add `CharacterControllerRoot`.

2. **Input**
   - Add `PlayerInput` with your Input Actions asset.
   - Add `NewInputSystemSource` on the same GameObject as `PlayerInput`.
   - In `CharacterControllerRoot`, assign `_inputSourceBehaviour` to the `NewInputSystemSource` component.

3. **Camera rig**
   - Create a camera with:
     - `CameraRigBase` implementation, e.g. `FirstPersonCameraRig`.
   - In `CharacterControllerRoot`, assign `_cameraRig` to that camera rig.
   - On `FirstPersonCameraRig`:
     - `_yawRoot` (optional) – usually the character root.
     - `_pitchRoot` – usually a pivot under the camera or the camera transform itself.

4. **Debug gizmos (optional but recommended)**
   - On the character root:
     - Add `MovementDirectionGizmos`.
       - Assign `_motor` or leave it empty to auto‑grab `CharacterMotor`.
   - On the character root or a debug object:
     - Add `CameraDirectionGizmos`.
       - Assign `_cameraTransform` to your main camera or its pivot.
       - Optionally set `_originTransform` to the character to show camera direction from character position.

Make sure **Scene view Gizmos** are enabled to see the visualizations.

---

## Components overview

This section is aimed at new team members reading the code for the first time.

### 1. `CharacterMotor` (Core movement)

**File:** `Runtime/Core/CharacterMotor.cs`  
**Namespace:** `Kojiko.MCharacterController.Core`

**Responsibility:**  
Owns all movement math and calls into Unity’s `CharacterController`.

**Key ideas:**

- Takes a **desired world‑space horizontal move direction** each frame (from `CharacterControllerRoot`).
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

**API surface most other systems care about:**

- Read‑only properties:
  - `IsGrounded` – boolean from `CharacterController.isGrounded`.
  - `Velocity` – full 3D velocity.
  - `HorizontalVelocity` – XZ velocity only.
  - `CurrentSpeed` – horizontal speed magnitude.
  - `CurrentAcceleration` – scalar acceleration (m/s²) from frame‑to‑frame speed change.
  - `ForwardSpeed`, `BackwardSpeed`, `StrafeSpeed`, `MaxGroundSpeed`.

- Main methods:
  - `Step(Vector3 desiredMoveWorld, float deltaTime)` – called once per frame by `CharacterControllerRoot`.
  - `SetVerticalVelocity(float newVerticalVelocity)` – entry point for future abilities (jump, fall, knock‑up).

**Why this separation?**

- Higher‑level code doesn’t need to know about gravity or acceleration logic.
- Tests / future simulations can run the motor with arbitrary input vectors.
- Abilities like sprint, slide, and jump can adjust:
  - The input vector
  - The speed caps
  - Or call `SetVerticalVelocity` without editing core movement math.

---

### 2. `CharacterControllerRoot` (Orchestrator)

**File:** `Runtime/Core/CharacterControllerRoot.cs`  
**Namespace:** `Kojiko.MCharacterController.Core`

**Responsibility:**  
Glue between **input**, **motor**, and **camera rig**.

**What it does each frame (`Update`)**:

1. Reads:
   - `MoveAxis` (Vector2) from an `ICcInputSource`
   - `LookAxis` (Vector2) from the same source
2. Sends `LookAxis` to `CameraRigBase.HandleLook`.
3. Converts `MoveAxis` into a **world‑space** move direction using its own transform orientation.
4. Calls `_motor.Step(moveDirection, Time.deltaTime)`.

**Why this exists:**

- To keep `CharacterMotor` free from input and camera dependencies.
- To ensure the same motor can be used with:
  - Player input,
  - AI steering,
  - Networked remote controllers.

**Key fields:**

- `_motor` – movement implementation (`CharacterMotor`).
- `_inputSourceBehaviour` – any `MonoBehaviour` that implements `ICcInputSource`.
- `_cameraRig` – a `CameraRigBase` implementation (current: `FirstPersonCameraRig`).

---

### 3. `ICcInputSource` and `NewInputSystemSource` (Input abstraction)

#### `ICcInputSource`

**File:** `Runtime/Input/ICcInputSource.cs`  
**Namespace:** `Kojiko.MCharacterController.Input`

**Responsibility:**  
Defines a **minimal input contract** for the character controller system:

- `MoveAxis` – 2D move input (x = strafe, y = forward/back).
- `LookAxis` – 2D look delta (x = yaw, y = pitch).
- `JumpPressed`, `JumpHeld`, `SprintHeld`, `SwitchViewPressed` – future ability hooks.

Any system (player, AI, network) can implement this interface and plug into `CharacterControllerRoot` without changing movement or camera code.

#### `NewInputSystemSource`

**File:** `Runtime/Input/NewInputSystemSource.cs`  
**Namespace:** `Kojiko.MCharacterController.Input`

**Responsibility:**  
Concrete implementation of `ICcInputSource` that wraps **Unity’s New Input System** via `PlayerInput`.

**How it works:**

- In `Awake`:
  - Finds `PlayerInput` on the same GameObject.
  - Looks up the configured action map (default `"Player"`).
  - Caches references to actions:
    - Move
    - Look
    - Jump
    - Sprint
    - SwitchView
- In `OnEnable` / `OnDisable`:
  - Enables/disables the cached actions.
- In `Update`:
  - Reads `Move` and `Look` as `Vector2`.
  - Computes button states (pressed this frame vs held).

**Why this abstraction:**

- Keeps Unity’s input system code and handling **out** of the motor, camera, and root.
- Allows swapping `NewInputSystemSource` for:
  - AI controllers,
  - Network playback,
  - Recorded input replays,
  - Custom input mappings.

---

### 4. `CameraRigBase` and `FirstPersonCameraRig` (Camera system)

#### `CameraRigBase`

**File:** `Runtime/Camera/CameraRigBase.cs`  
**Namespace:** `Kojiko.MCharacterController.Camera`

**Responsibility:**  
Defines the **minimal interface** any camera rig must implement:

- `Initialize(Transform characterRoot)` – for wiring to the character.
- `HandleLook(Vector2 lookAxis, float deltaTime)` – per‑frame look updates.

This is what `CharacterControllerRoot` talks to.

#### `FirstPersonCameraRig`

**File:** `Runtime/Camera/FirstPersonCameraRig.cs`  
**Namespace:** `Kojiko.MCharacterController.Camera`

**Responsibility:**  
Simple FPS camera:

- **Yaw** (horizontal rotation) on a `_yawRoot` (usually the character root).
- **Pitch** (vertical rotation) on a `_pitchRoot` (pivot / camera transform).

**How it works:**

- `Initialize`:
  - Defaults `_yawRoot` to the given `characterRoot` if not set.
  - Defaults `_pitchRoot` to its own transform if not set.
  - Reads the current local X rotation to initialize `_currentPitch` (normalized to \([-180, 180)\)).
- `HandleLook`:
  - Scales look input by `_sensitivityX` / `_sensitivityY`.
  - Rotates `_yawRoot` around world up for yaw.
  - Accumulates and clamps `_currentPitch` between `_minPitch` and `_maxPitch`, then applies it to `_pitchRoot.localEulerAngles.x`.

**Why split yaw/pitch:**

- Keeps the character’s body rotation separate from camera tilt.
- Avoids issues with rolling the character.
- Makes it easier to add 3rd person rigs or aim offsets later.

---

### 5. `MovementDirectionGizmos` (Movement debug visualization)

**File:** `Runtime/Debug/Visualization/MovementDirectionGizmos.cs`  
**Namespace:** (currently global; consider `Kojiko.MCharacterController.Debug` or `.Debug.Visualization` for consistency)

**Responsibility:**  
Draws **movement‑related gizmos** in the Scene view:

- An **ellipse** on the XZ plane around the character representing max speeds:
  - Forward radius  = `CharacterMotor.ForwardSpeed`
  - Backward radius = `CharacterMotor.BackwardSpeed`
  - Side radius     = `CharacterMotor.StrafeSpeed`
- A **line** showing the current horizontal velocity:
  - Origin at the character’s feet (from `CharacterController.bounds`).
  - Length & direction = `CharacterMotor.HorizontalVelocity`.
  - Optional color change based on `CurrentAcceleration`:
    - Green = accelerating.
    - Red   = decelerating.
    - Yellow = neutral / near zero accel.

**How to read it:**

- The ellipse is the **max possible speed envelope** in each direction.
- The line is your **current speed vector**.
- If the line touches the edge of the ellipse: you’re at (or near) max speed in that direction.
- If it’s shorter: you’re still accelerating or are limited (e.g., in air, decelerating, etc.).

---

### 6. `CameraDirectionGizmos` (Camera debug visualization)

**File:** `Runtime/Debug/Visualization/CameraDirectionGizmos.cs`  
**Namespace:** `Kojiko.MCharacterController.Debug`

**Responsibility:**  
Show where the camera (or camera rig) is pointing:

- Draws a line from an **origin** (camera or character).
- In the **camera’s forward direction**.
- With a wire sphere at the endpoint for easy visual pickup.

**Fields:**

- `_cameraTransform` – the camera or camera rig pivot.
- `_originTransform` – optional; if set, line starts there; otherwise, starts at camera.
- `_lineLength` – how long the line should be.
- `_endpointSphereRadius` – radius of the wire sphere at the end.
- `_lineColor` – color for line and sphere.

**Use cases:**

- Debugging alignment of movement direction vs camera direction.
- Ensuring look input is wired to the correct transform.
- Helpful when experimenting with alternative camera rigs.

---



