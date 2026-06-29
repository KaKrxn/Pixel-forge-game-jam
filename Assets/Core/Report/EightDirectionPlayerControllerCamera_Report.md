# Eight Direction Player Controller and Smooth Top-View Camera Report

## 1. Objective

Create a 2D player controller for a top-view / bird-eye-view game where the player can move in eight directions and the camera follows the player smoothly, similar to the polished follow feeling of games like Ori.

The target result is:

- The player moves up, down, left, right, and diagonally.
- Diagonal movement keeps the same speed as cardinal movement.
- Movement is physics-friendly and works with 2D collisions.
- The camera stays centered on the player with smooth damping.
- The setup fits the current Unity project structure and installed packages.

## 2. Current Project Analysis

Project information found in this workspace:

- Unity version: `6000.3.17f1`
- Render setup: URP 2D is available.
- Main scene: `Assets/Scenes/SampleScene.unity`
- Main Camera:
  - Orthographic enabled.
  - Orthographic size is currently `5`.
  - Camera position is currently `(0, 0, -10)`.
- Input setup:
  - `Assets/InputSystem_Actions.inputactions` already exists.
  - The `Player` action map already contains a `Move` action.
  - `Move` is a `Vector2` input.
  - WASD, arrow keys, gamepad left stick, joystick, and XR bindings already exist.
- Installed packages:
  - `com.unity.inputsystem`
  - `com.unity.cinemachine`
  - `com.unity.render-pipelines.universal`
  - 2D packages such as Sprite, Tilemap, SpriteShape, Animation, Aseprite, and PSD Importer.
- Existing gameplay scripts:
  - No `.cs` gameplay scripts were found under the current scan.

Because the Input System and Cinemachine are already installed, the best implementation path is to use them instead of writing custom input polling or a custom camera from scratch.

## 3. Recommended Design

### 3.1 Player Movement Model

Use a `Rigidbody2D` based controller.

Recommended component setup on the Player GameObject:

- `SpriteRenderer`
- `Rigidbody2D`
- `Collider2D`
- `PlayerInput`
- `PlayerController`
- Optional: `Animator`

Recommended `Rigidbody2D` settings:

- Body Type: `Dynamic`
- Gravity Scale: `0`
- Linear Drag: `0`
- Angular Drag: default is fine
- Interpolate: `Interpolate`
- Collision Detection: `Continuous`
- Constraints:
  - Freeze Rotation Z

Why this approach:

- `Rigidbody2D.MovePosition` keeps movement compatible with physics.
- Collision response remains stable.
- Movement belongs in `FixedUpdate`, which matches the physics loop.
- Input can be read through the existing `Player/Move` action.

### 3.2 Eight-Direction Movement

The controller should read a `Vector2` input.

Examples:

- `(0, 1)` means up.
- `(0, -1)` means down.
- `(-1, 0)` means left.
- `(1, 0)` means right.
- `(1, 1)` means up-right.
- `(-1, 1)` means up-left.
- `(1, -1)` means down-right.
- `(-1, -1)` means down-left.

The important rule is to normalize movement when the input magnitude is greater than `1`.

Without normalization, diagonal movement becomes faster because `(1, 1)` has a magnitude of about `1.414`. Normalizing keeps diagonal and straight movement at the same speed.

### 3.3 Facing Direction

For top-view movement, the player usually needs to remember the last non-zero movement direction.

This supports:

- Idle animation facing the last direction.
- Attack direction.
- Interaction direction.
- Future dash, aim, or tool-use direction.

Recommended runtime values:

- `moveInput`
- `lastMoveDirection`
- `isMoving`

### 3.4 Animation Parameters

If an `Animator` is used, the controller should send clear movement parameters.

Recommended Animator parameters:

- `MoveX` as float
- `MoveY` as float
- `LastMoveX` as float
- `LastMoveY` as float
- `Speed` as float
- Optional: `IsMoving` as bool

For an eight-direction sprite set, animation blend trees can use `MoveX` and `MoveY` while moving, and `LastMoveX` / `LastMoveY` while idle.

## 4. Recommended Script Structure

Recommended location:

```text
Assets/Core/Script/Player/PlayerController.cs
```

Recommended script:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D body;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    public Vector2 MoveInput => moveInput;
    public Vector2 LastMoveDirection => lastMoveDirection;
    public bool IsMoving => moveInput.sqrMagnitude > 0.001f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateFacingDirection();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Vector2 movement = moveInput * moveSpeed * Time.fixedDeltaTime;
        body.MovePosition(body.position + movement);
    }

    private void ReadMovementInput()
    {
        if (moveAction == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = moveAction.action.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    private void UpdateFacingDirection()
    {
        if (moveInput.sqrMagnitude > 0.001f)
        {
            lastMoveDirection = moveInput.normalized;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
        animator.SetBool("IsMoving", IsMoving);
    }
}
```

## 5. Camera Follow Design

### 5.1 Recommended Camera System

Use Cinemachine because it is already installed in the project.

Recommended scene setup:

- Keep the existing `Main Camera`.
- Add a Cinemachine camera object.
- Assign the Player transform to the Cinemachine camera `Follow` target.
- Keep the camera orthographic.
- Use damping to smooth camera movement.

This avoids writing a custom camera script and gives better editor tuning.

### 5.2 Cinemachine Setup Steps

1. Open the target gameplay scene.
2. Create or select the Player GameObject.
3. Add a `Rigidbody2D`, collider, `PlayerInput`, and `PlayerController`.
4. Assign the existing `InputSystem_Actions.inputactions` asset to `PlayerInput`.
5. Set the default action map to `Player`.
6. Assign the `Player/Move` action to the `moveAction` field on `PlayerController`.
7. Create a Cinemachine Camera:
   - In Unity 6 / Cinemachine 3, create a Cinemachine Camera object from the GameObject menu.
   - Name it `Player Follow Camera`.
8. Assign the Player transform to the Cinemachine camera `Follow` field.
9. Set Lens / Orthographic Size to match the desired view. Start with `5`.
10. Configure position damping:
    - X Damping: `0.2` to `0.5`
    - Y Damping: `0.2` to `0.5`
    - Z Damping: `0`
11. Keep the physical Main Camera at a normal 2D position such as `(0, 0, -10)`.
12. Press Play and tune damping until the camera follows smoothly without feeling delayed.

### 5.3 Optional Custom Camera Fallback

If Cinemachine is not used, a simple fallback camera follow script can be created.

Recommended location:

```text
Assets/Core/Script/Camera/SmoothCameraFollow2D.cs
```

Example:

```csharp
using UnityEngine;

public sealed class SmoothCameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
```

Cinemachine is still the preferred option because it supports damping, follow offsets, camera bounds, confiners, and future camera effects with less custom code.

## 6. Detailed Implementation Plan

### Phase 1: Create the Player Object

1. Create a new GameObject named `Player`.
2. Add a visible placeholder sprite.
3. Set the Player position to `(0, 0, 0)`.
4. Add `Rigidbody2D`.
5. Set Gravity Scale to `0`.
6. Freeze Rotation Z.
7. Add the correct `Collider2D`.
   - Use `CapsuleCollider2D` for a character.
   - Use `BoxCollider2D` for a square placeholder.

### Phase 2: Add Input

1. Add `PlayerInput` to the Player.
2. Assign `Assets/InputSystem_Actions.inputactions`.
3. Set Behavior to one of these:
   - `Invoke Unity Events`, if the project prefers event-based input.
   - `Send Messages`, if using message methods.
   - Manual `InputActionReference`, recommended for the proposed script because it is explicit and simple.
4. Assign `Player/Move` to the `PlayerController.moveAction` field.

### Phase 3: Add the Controller Script

1. Create `Assets/Core/Script/Player/PlayerController.cs`.
2. Paste the recommended script.
3. Attach it to the Player.
4. Set `Move Speed` to a starting value between `4` and `6`.
5. Enter Play Mode.
6. Test movement with:
   - W, A, S, D
   - Arrow keys
   - Gamepad left stick if available

### Phase 4: Validate Movement

Movement should pass these checks:

- Pressing only `W` moves up.
- Pressing only `S` moves down.
- Pressing only `A` moves left.
- Pressing only `D` moves right.
- Pressing `W + D` moves diagonally up-right.
- Pressing `W + A` moves diagonally up-left.
- Pressing `S + D` moves diagonally down-right.
- Pressing `S + A` moves diagonally down-left.
- Diagonal movement does not look faster than straight movement.
- Player does not rotate when colliding.

### Phase 5: Add Smooth Camera Follow

1. Keep the existing `Main Camera`.
2. Create a Cinemachine camera named `Player Follow Camera`.
3. Assign the Player as the follow target.
4. Set Orthographic Size to `5`.
5. Tune damping around `0.3`.
6. Press Play and move the player in all directions.
7. Adjust damping:
   - Lower damping feels more direct.
   - Higher damping feels smoother but more delayed.

### Phase 6: Optional Camera Bounds

When the map exists, add a Cinemachine Confiner 2D.

Recommended approach:

1. Create a `PolygonCollider2D` or `CompositeCollider2D` around the playable map area.
2. Add Cinemachine Confiner 2D to the Cinemachine camera.
3. Assign the map bounds collider.
4. Enable cache/bake if required by the Cinemachine version.
5. Test near all edges of the map.

This prevents the camera from showing outside the level.

## 7. Recommended Folder Layout

```text
Assets/
  Core/
    Script/
      Player/
        PlayerController.cs
      Camera/
        SmoothCameraFollow2D.cs
    Prefab/
      Player.prefab
    Scene/
      GameplayScene.unity
    Report/
      EightDirectionPlayerControllerCamera_Report.md
```

## 8. Testing Checklist

Use this checklist after implementation:

- Player moves in all eight directions.
- Diagonal movement speed is normalized.
- Movement is smooth and does not jitter.
- Player collision works against walls or obstacle colliders.
- Player does not rotate after hitting colliders.
- Camera follows the Player.
- Camera movement is smooth but not too delayed.
- Camera remains orthographic.
- Player remains visible near map edges.
- Input works with keyboard.
- Input works with gamepad if available.
- No console errors appear in Play Mode.

## 9. Risks and Notes

- If `PlayerInput` and `InputActionReference` both enable the same action, input normally still works, but the project should use one clear input pattern long-term.
- If movement jitters, confirm that movement happens in `FixedUpdate` and camera follow happens after movement.
- If collision feels sticky, check collider shape and Rigidbody2D settings.
- If the player appears to move behind tiles or props, configure SpriteRenderer sorting layers and order.
- If diagonal animation looks wrong, use an eight-direction blend tree or snap animation direction to one of eight sectors.
- For pixel art, consider Pixel Perfect Camera settings later, especially if the game uses crisp low-resolution sprites.

## 10. Final Recommendation

The best implementation for this project is:

- Use the existing Input System `Player/Move` action.
- Build `PlayerController.cs` with normalized `Vector2` movement.
- Move the player through `Rigidbody2D.MovePosition` in `FixedUpdate`.
- Store `lastMoveDirection` for idle facing, attacks, and future interaction systems.
- Use Cinemachine for the smooth player-follow camera.
- Add a Cinemachine Confiner 2D after the playable map exists.

This gives a clean foundation for a top-view 2D game and leaves room for future systems such as combat, dash, interaction, animation blend trees, camera bounds, and map transitions.
