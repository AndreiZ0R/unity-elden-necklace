# Contract: Unity Bridge Commands

**Branch**: `001-arena-combat-core` | **Date**: 2026-06-01

The implementation relies on the `com.cziberpv.unity-bridge` package, invoked from the shell via `powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '<json>'`. This document is the contract for which Bridge commands the feature uses and what JSON envelope each accepts. The actual command implementations live inside the Bridge package; this file documents the agreement, not the implementation.

The Bridge speaks via a file protocol: `unity-cmd.ps1` writes the JSON to `Assets/LLM/Bridge/request.json` and polls `Assets/LLM/Bridge/response.md` for the reply. Unity is required to be running with the Bridge editor window active.

---

## Command: `scene`

**Purpose**: Return a hierarchical summary of the currently-open scene (GameObject names, parent relationships, active state).

**Request**:
```json
{ "type": "scene" }
```

**Used by this feature**: Step 1 of every editor session — confirm the current scene state before mutating it. Also called after `save-scene` to verify what was persisted.

---

## Command: `inspect`

**Purpose**: Inspect a specific GameObject by hierarchical path. The `lens` argument selects which subset of data is returned (avoids dumping every component for every inspection).

**Request**:
```json
{ "type": "inspect", "path": "Player", "lens": "scripts" }
```

**Lenses used by this feature**:
- `scripts` — list MonoBehaviour scripts attached and their public fields.
- `components` — list all components on the GameObject.
- `transform` — position, rotation, scale, parent.
- `navmesh` — NavMeshAgent configuration (used when verifying enemy prefabs).

**Used by this feature**: After every `add-component` or `set` operation, to verify the change landed correctly.

---

## Command: `create`

**Purpose**: Create a new GameObject in the current scene. Optionally specify a parent path and a primitive type.

**Request**:
```json
{ "type": "create", "name": "Arena", "primitive": "Empty" }
```

**Used by this feature**: Building the Arena scene's root hierarchy (Arena root, Walls, Pillars, Gates, Lighting groups).

---

## Command: `add-component`

**Purpose**: Attach a component (Unity built-in or custom `MonoBehaviour`) to a GameObject.

**Request**:
```json
{ "type": "add-component", "path": "Player", "component": "PlayerController" }
```

**Used by this feature**: Wire up player and enemy prefabs by attaching the C# scripts written under `Assets/Scripts/`. Built-in components like `CharacterController`, `NavMeshAgent`, `Animator`, `CapsuleCollider` use their short Unity names per CLAUDE.md.

---

## Command: `set`

**Purpose**: Set a serialized property on a component, supporting nested paths.

**Request**:
```json
{
  "type": "set",
  "path": "Player",
  "component": "PlayerHealth",
  "property": "maxHealth",
  "value": 100
}
```

**Used by this feature**: Configure per-archetype tuning (HP, damage, speed) on enemy prefabs. Per CLAUDE.md, serialized Unity properties use the `m_` prefix (`m_LocalPosition`, `m_SizeDelta`).

---

## Command: `save-scene`

**Purpose**: Persist the current scene state to its `.unity` file.

**Request**:
```json
{ "type": "save-scene" }
```

**Used by this feature**: Before any `new-scene` / `open-scene` and at the end of every editor session per Constitution's Unity Workflow.

---

## Command: `new-scene`

**Purpose**: Create a new empty scene (in-memory). Must be followed by `save-scene` with a path to persist.

**Request**:
```json
{ "type": "new-scene" }
```

**Used by this feature**: Creating the initial `Arena.unity` scene if `SampleScene.unity` is kept untouched.

---

## Command: `open-scene`

**Purpose**: Open an existing scene by asset path.

**Request**:
```json
{ "type": "open-scene", "path": "Assets/Scenes/Arena.unity" }
```

**Used by this feature**: Returning to the Arena scene after any side-trip (e.g. opening a Mixamo character preview scene).

---

## Command: `refresh`

**Purpose**: Force Unity to re-import assets and recompile scripts. Returns compile errors as plain text in the response, or an OK marker.

**Request**:
```json
{ "type": "refresh" }
```

**Used by this feature**: After **every** C# script write or edit, per Constitution I. The `-Timeout 120` flag is recommended because compilation can take 30-60 seconds on a cold cache.

**Expected response**:
- Success: `[OK] refresh complete (0 errors)`.
- Failure: a list of compile errors with file paths and line numbers; implementation MUST resolve all of them before proceeding.

---

## Command: `scratch`

**Purpose**: Execute an arbitrary C# editor script in the Bridge's context. Used for complex multi-step setups that would otherwise require many individual commands.

**Request**:
```json
{
  "type": "scratch",
  "code": "// arbitrary editor C# here"
}
```

**Used by this feature**: Bulk arena geometry setup (placing the eight pillars in a single call) and bulk prefab wiring (configuring all four `SpawnGate` references at once on `WaveManager`).

---

## Batch operations

Per CLAUDE.md, multiple independent commands can be combined as a JSON array:

```json
[
  { "type": "create", "name": "Arena" },
  { "type": "create", "name": "Walls", "parent": "Arena" },
  { "type": "create", "name": "Pillars", "parent": "Arena" }
]
```

This is the preferred form whenever 3+ independent operations are issued back-to-back.

---

## Error envelope

Any command may return an error response:

```text
[ERROR] <human-readable message>
[ERROR] <stack trace if available>
```

On `[ERROR]`, the implementation MUST stop, report the error to the user, and not attempt the same command identically. Most errors are recoverable by fixing the JSON arguments or by ensuring Unity is running.
