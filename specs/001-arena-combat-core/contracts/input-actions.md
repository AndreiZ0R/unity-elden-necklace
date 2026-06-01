# Contract: Input Actions

**Branch**: `001-arena-combat-core` | **Date**: 2026-06-01

This document is the contract for the Input System action map exposed by the game to the player. The implementation extends the existing `Assets/InputSystem_Actions.inputactions` asset; consumers (player scripts) bind via the auto-generated `PlayerInputActions` C# class.

---

## Action Map: `Gameplay`

| Action | Type | Default Binding | Read By | Notes |
|--------|------|-----------------|---------|-------|
| `Move` | `Value` (Vector2) | WASD composite | `PlayerController.cs` | Interpreted as camera-relative on the XZ plane |
| `Look` | `Value` (Vector2) | Mouse Delta | `ThirdPersonCamera.cs` | Drives yaw (X) and pitch (Y) |
| `Attack` | `Button` | Left Mouse Button | `PlayerCombat.cs` | Single melee swing; respects stamina cost |
| `Pause` | `Button` | Escape | `GameManager.cs` | Toggles `Time.timeScale` between 0 and 1 |

---

## Action Map: `UI`

| Action | Type | Default Binding | Read By | Notes |
|--------|------|-----------------|---------|-------|
| `Submit` | `Button` | Enter / Left Mouse Button | `HUDController.cs` (game-over restart) | Standard UI submit |
| `Cancel` | `Button` | Escape | `HUDController.cs` (pause menu close) | |

---

## Action map activation

- `Gameplay` is enabled while `Time.timeScale > 0` and `!GameManager.IsGameOver`.
- `UI` is enabled when paused or when the game-over panel is visible.

Switching is done via `PlayerInput.SwitchCurrentActionMap("Gameplay" | "UI")` on the player GameObject.

---

## Cursor behaviour contract

| Game State | `Cursor.lockState` | `Cursor.visible` |
|-----------|---------------------|------------------|
| Playing | `Locked` | `false` |
| Paused | `None` | `true` |
| Game Over | `None` | `true` |

Implementation lives in `GameManager.cs` and is toggled on every `OnPauseChanged` / `OnGameOver` event.

---

## Out of scope for this feature

- Heavy attack, block, dodge — explicitly excluded by the spec assumption that only one bound attack input is implemented in this iteration.
- Gamepad bindings — desktop keyboard + mouse only per spec.
- Key rebinding UI — no settings menu in scope for this feature.
- Interact / pickup key — no power-ups or weapon-swap in scope.

If any of the above is added later, it gets its own spec and its own update to this contract.
