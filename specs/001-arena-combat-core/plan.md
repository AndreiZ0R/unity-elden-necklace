# Implementation Plan: Elden Necklace — Arena Combat Core

**Branch**: `001-arena-combat-core` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-arena-combat-core/spec.md`

## Summary

Build a single-arena third-person melee survival game in Unity 6 LTS. A warrior controlled with WASD + mouse fights waves of three distinct melee enemy archetypes (FSM grunt, FSM patrolling guardian, Behaviour Tree boss knight) in a colosseum-style dungeon with four cardinal spawn gates. Enemies navigate via baked NavMesh, perceive the player via vision cones and a shared hearing event bus, cooperate as a group through alert broadcasts and round-robin gate spawning, and the HUD shows live health, stamina, wave and score. The technical approach is hand-rolled C# `MonoBehaviour` scripts driven through Unity's Input System and uGUI, with all AI logic written from scratch (no third-party BT/FSM packages) to keep dependencies minimal and satisfy the constitution's Product-First principle. Authoring and verification happen through the already-installed Unity Bridge (`com.cziberpv.unity-bridge`) using the `unity-cmd.ps1` PowerShell wrapper for scene mutation, component inspection, and compile checks.

## Technical Context

**Language/Version**: C# 9 (Unity 6 LTS scripting)

**Primary Dependencies**:
- Unity 6 LTS 6000.4.0f1 (already pinned in `ProjectSettings/ProjectVersion.txt`)
- `com.unity.ai.navigation` 2.0.11 — runtime NavMesh + NavMeshAgent (already installed)
- `com.unity.inputsystem` 1.19.0 — keyboard/mouse via Input Actions (already installed)
- `com.unity.ugui` 2.0.0 — Canvas + Slider + Text for HUD (already installed)
- `com.unity.render-pipelines.universal` 17.4.0 — URP for shadows/lighting (already installed)
- `com.cziberpv.unity-bridge` — editor automation bridge consumed by `unity-cmd.ps1` (already installed)

**Storage**: N/A — gameplay state lives in-memory only; no save/load in scope. Wave configuration is hardcoded constants (Product-First: no ScriptableObject system until proven necessary).

**Testing**: Manual playtesting in the Unity Editor and standalone builds. No automated test framework is in scope — the constitution prioritises a shippable product over test infrastructure. Verification happens by running the scene, by `unity-cmd.ps1` `refresh` for compile checks, and by reading scene/component state via the Bridge.

**Target Platform**: Desktop standalone, `StandaloneOSX` (Apple Silicon) and `StandaloneWindows64`. Editor target is macOS (development host); both build targets must remain green throughout the project per Constitution III.

**Project Type**: Single Unity project (no separate backend/frontend). Source root is `Assets/`.

**Performance Goals**: Stable 60 FPS in the Editor on the MacBook M1 Pro development host with up to 20 active enemy agents on screen. AI perception is throttled (vision check every 0.1 s, object scan every 0.08 s) — sufficient for the planned wave sizes.

**Constraints**:
- Project MUST compile after every change (Constitution I).
- Each addition MUST leave previously implemented features functional (Constitution II).
- No platform-specific APIs without a cross-platform alternative (Constitution III).
- Combat is melee-only — no `Rigidbody`-projectile code, no `Raycast`-as-bullet code, no magic bolt prefabs (spec FR-010).
- All file paths in C# code use `Path.Combine` or Unity asset-path APIs.

**Scale/Scope**: Single Unity scene (`Arena.unity`), one player prefab, three enemy prefabs, one HUD canvas prefab. C# scripts grouped under `Assets/Scripts/` — estimated ~15-20 scripts total. No multi-scene transitions, no networking, no save system.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution defines four principles. The plan is evaluated against each below.

| # | Principle | Plan compliance |
|---|-----------|-----------------|
| I | Continuous Compilation | Every script will be added incrementally and `unity-cmd.ps1 refresh` will be called after each script to surface compiler errors immediately. Compile failures block all other work. **PASS.** |
| II | Additive Stability | Tasks will be ordered so each user story can be implemented and verified independently before the next is started. After any shared-system change (player input, camera, perception event bus), all previously-implemented features will be re-verified by running the scene. **PASS.** |
| III | Cross-Platform Compatibility | No platform-specific Unity APIs are planned (no `WindowsRuntime`, no AppleScript). File paths use `Path.Combine`. Both `StandaloneOSX` and `StandaloneWindows64` build targets will be kept buildable. The Unity Bridge is editor-only and is not part of the shipped runtime. **PASS.** |
| IV | Product-First Development | Hand-rolled FSM/BT (no NodeCanvas, no Behavior Designer); hardcoded wave config (no ScriptableObject system unless needed); no abstract base classes beyond what the curriculum's `IState` interface requires; no logging framework, no DI container. Comments are kept minimal (only where the WHY is non-obvious). **PASS.** |

**Gate result**: All four principles pass. No violations to track in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-arena-combat-core/
├── plan.md                  # This file (/speckit-plan command output)
├── spec.md                  # Feature specification (/speckit-specify output)
├── research.md              # Phase 0 output (decisions on Mixamo, Asset Store pack, BT impl, etc.)
├── data-model.md            # Phase 1 output (Player, Enemy, Wave, Gate, HUD entities)
├── quickstart.md            # Phase 1 output (Unity Bridge usage + build/run instructions)
├── contracts/
│   ├── unity-bridge-commands.md  # The Bridge commands this feature relies on
│   └── input-actions.md          # The Input System action map exposed to the player
├── checklists/
│   └── requirements.md      # Spec quality checklist (from /speckit-specify)
└── tasks.md                 # Phase 2 output (/speckit-tasks command — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Assets/
├── Scripts/
│   ├── Player/
│   │   ├── PlayerController.cs       # WASD locomotion + mouse-look input forwarding
│   │   ├── PlayerCombat.cs           # Attack input → animator trigger → hitbox window
│   │   ├── PlayerHealth.cs           # HP pool, damage reception, OnDeath event
│   │   └── PlayerStamina.cs          # Stamina pool, depletion, delayed regen
│   ├── Enemies/
│   │   ├── EnemyAI.cs                # FSM driver shared by Grunt + Guardian (state enum + switch)
│   │   ├── EnemyHealth.cs            # HP pool, damage reception, OnDeath event
│   │   ├── GruntAI.cs                # Type-specific tuning + chase-rush behaviour
│   │   ├── GuardianAI.cs             # Waypoint patrol loop + slow engage
│   │   └── KnightBT.cs               # Behaviour Tree root for boss knight (with Berserk node)
│   ├── AI/
│   │   ├── VisionSystem.cs           # FOV cone + occlusion raycast
│   │   ├── HearingSystem.cs          # Subscribes to NoiseEmitter static event
│   │   ├── NoiseEmitter.cs           # Global noise event bus (static Action)
│   │   ├── PerceptionSystem.cs       # Aggregates Vision + Hearing → PerceptionState
│   │   └── BehaviourTree/
│   │       ├── BTNode.cs             # Base node, Status enum
│   │       ├── Sequence.cs           # Sequence composite
│   │       ├── Selector.cs           # Selector composite
│   │       └── Leaf.cs               # Action / Condition leaves
│   ├── World/
│   │   ├── WaveManager.cs            # Builds spawn list, round-robin gates, Fisher-Yates shuffle
│   │   ├── SpawnGate.cs              # World-position marker for one of the 4 cardinal gates
│   │   └── EnemyManager.cs           # Singleton; AlertNearby broadcast
│   ├── UI/
│   │   └── HUDController.cs          # Subscribes to health/stamina/wave/score change events
│   ├── Camera/
│   │   └── ThirdPersonCamera.cs      # Orbit + mouse-look + SphereCast wall avoidance
│   └── Game/
│       └── GameManager.cs            # Singleton; score, wave transitions, game-over state
├── Scenes/
│   └── Arena.unity                   # Single gameplay scene (created via Bridge in Phase 2)
├── Prefabs/
│   ├── Player.prefab
│   ├── GruntEnemy.prefab
│   ├── GuardianEnemy.prefab
│   ├── KnightEnemy.prefab
│   └── HUDCanvas.prefab
├── Animations/                       # Mixamo-sourced clips, retargeted to humanoid rig
├── Models/                           # Asset Store dungeon pack + character models
├── Materials/
├── Settings/                         # URP volume profiles
├── InputSystem_Actions.inputactions  # Already exists; will be extended for combat bindings
└── LLM/Bridge/                       # Existing Unity Bridge folder (request.json / response.md)

ProjectSettings/                       # Unity-managed; build target configuration lives here
Packages/                              # Already configured (see Technical Context)
```

**Structure Decision**: Single Unity project. All gameplay code lives under `Assets/Scripts/` organised by feature area (Player, Enemies, AI, World, UI, Camera, Game). Behaviour Tree nodes are placed under `Assets/Scripts/AI/BehaviourTree/` to keep the Knight's BT reusable if a second BT-driven enemy is ever added. Mixamo character animations and Asset Store environment props go under `Assets/Animations/` and `Assets/Models/` respectively. No separate `backend/` or `frontend/` folders — this is a desktop game, not a web app or service.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. This section is intentionally empty.
