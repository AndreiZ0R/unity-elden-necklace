# Phase 0 Research: Elden Necklace — Arena Combat Core

**Branch**: `001-arena-combat-core` | **Date**: 2026-06-01

All open technical choices are resolved here so the data model and contracts can be written without ambiguity. Each decision follows the format **Decision / Rationale / Alternatives**.

---

## 1. Behaviour Tree implementation for the boss knight

**Decision**: Hand-roll a minimal Behaviour Tree in C# under `Assets/Scripts/AI/BehaviourTree/`. Three node types only — `Sequence`, `Selector`, and `Leaf` (action or condition lambda) — with a `Status` enum (`Success`, `Failure`, `Running`). The knight's root tree composes these to express: "if HP below threshold → Berserk branch (faster strafe + faster swing); else → standard Chase → Attack branch."

**Rationale**:
- Product-First (Constitution IV) discourages pulling in a full graphical BT package when the entire knight tree fits in roughly 50 lines.
- Hand-rolled BT keeps the project's dependency surface unchanged — no new package risks breaking the existing Unity 6 LTS build.
- The curriculum docs (Stage III) describe the knight's BT in pseudocode that maps 1:1 to this structure.
- Cross-platform (Constitution III): no third-party native plugins introduced.

**Alternatives considered**:
- *NodeCanvas* (Asset Store): full-featured visual BT/FSM editor. Rejected — pulls in ~50 MB of editor tooling for one enemy.
- *Behavior Designer* (Asset Store): commercial, paid. Rejected — out of scope and would violate Product-First.
- *Unity Behavior* (preview package): too new for Unity 6 LTS, API stability not guaranteed.

---

## 2. Finite State Machine pattern for grunt + guardian

**Decision**: Single `EnemyAI.cs` script with a `State` enum (`Idle`, `Patrol`, `Alert`, `Chase`, `Attack`, `Dead`) and a per-frame `switch` in `Update()` that dispatches to private `UpdateIdle()`, `UpdatePatrol()`, etc. methods. State transitions go through a private `TransitionTo(State)` that fires the appropriate `Enter`/`Exit` logic via switch.

**Rationale**:
- Smallest amount of code that satisfies the curriculum's FSM requirement (FR-017, FR-018).
- No `IState` interface or polymorphic state classes — Product-First says three similar lines beats premature abstraction.
- Both grunt and guardian use the same `EnemyAI.cs` base; archetype-specific tuning (`speed`, `attackRange`, `patrolWaypoints`) lives on small `GruntAI.cs` / `GuardianAI.cs` companion scripts that set fields on `EnemyAI` in `Awake`.

**Alternatives considered**:
- *State pattern with one class per state*: textbook OO design, but the curriculum's reference architecture (Stage III) already shows a switch-based FSM works fine for this scope. Avoided for line-count and file-count reasons.
- *Animator-driven FSM*: Unity's Animator can drive logic state machines, but state behaviours mixed with animation state cause well-known coupling problems. The Animator is used for animation only.

---

## 3. NavMesh authoring strategy

**Decision**: Bake the NavMesh at edit time once the arena geometry is final, using Unity's built-in `AI Navigation` package (already installed as `com.unity.ai.navigation` 2.0.11). Pillars and the central platform are marked as `NavMeshObstacle` with carve disabled (they don't move). Walls are marked `Navigation Static` so they bake out of the walkable area. Enemies receive a `NavMeshAgent` per archetype with per-archetype `speed` and `stoppingDistance`.

**Rationale**:
- Arena is fixed and small (~24 × 24 m). Edit-time bake is fastest at runtime and simplest to author.
- The `AI Navigation` 2.0.11 package supports runtime baking too, but that's overhead the spec doesn't need.
- Spec FR-020 only requires that enemies path around pillars — edit-time bake achieves this.
- On enemy death we set `agent.enabled = false` then `Destroy(gameObject, 3f)` to release the navmesh slot (per spec FR-021).

**Alternatives considered**:
- *Runtime NavMesh bake*: needed only for procedural levels. Out of scope.
- *Custom A\** grid: pointless when Unity's NavMesh exists and is documented in the curriculum.

---

## 4. Third-person camera

**Decision**: Hand-roll a `ThirdPersonCamera.cs` script with a single transform pivot at `player.position + (0, 1.8, 0)`. `Input.GetAxis("Mouse X"/"Mouse Y")` drives yaw/pitch; pitch is clamped to `[-20°, +60°]`. A `Physics.SphereCast` from the pivot along the camera arm (`Vector3.back` rotated by yaw/pitch) shortens the arm when geometry intersects, satisfying spec FR-003.

**Rationale**:
- Curriculum Stage IV (E4 doc) already implements this exact pattern; the design has evaluator-approved precedent.
- No Cinemachine dependency — Cinemachine works well but adds an extra package and several config assets per camera. Product-First.
- Cursor lock/unlock toggles on pause via `Cursor.lockState`.

**Alternatives considered**:
- *Cinemachine FreeLook*: more features (3 rings, blended shoulder heights, automatic damping) but overkill for a single fixed-distance orbit camera.
- *Camera anchored to player root with no SphereCast*: fails the wall-clipping edge case from the spec.

---

## 5. Input handling

**Decision**: Use the existing `InputSystem_Actions.inputactions` asset (already in `Assets/`) and extend it with bindings for `Move` (WASD vector), `Look` (mouse delta), `Attack` (Left Mouse Button), and `Pause` (Escape). `PlayerController.cs` reads `Move` and `Look` via the generated `PlayerInputActions` C# class; `PlayerCombat.cs` reads `Attack`.

**Rationale**:
- Input System 1.19.0 is already installed; the asset already exists. Using legacy `Input.GetKey` would mean ignoring infrastructure already in the repo.
- Input Action Maps are remappable, satisfying any future settings menu work without spec changes.
- Cross-platform (Constitution III): Input System abstracts Mac vs Windows input differences.

**Alternatives considered**:
- *Legacy Input Manager*: simpler but the new Input System is the project's existing convention.

---

## 6. Character assets — player + enemies

**Decision**: Use **Mixamo** for the humanoid character mesh and animation clips (idle, walk, run, attack, hit, death). Download three distinct rigged characters: warrior (player), skeleton or armoured grunt, dark knight; reuse the warrior rig with a different material/colour for the patrolling guardian to keep asset count down. Mixamo provides Adobe-licensed free-use FBX files compatible with Unity's Humanoid rig.

**Rationale**:
- Curriculum docs (Stage I) explicitly identify Mixamo as the chosen animation source.
- Mixamo's Humanoid retargeting works out-of-the-box with Unity's Humanoid Animator.
- Free, no licensing complexity.

**Alternatives considered**:
- *Unity Asset Store character packs* (e.g., Synty POLYGON): nice models but most are non-humanoid-rigged or come with their own non-Mixamo animation sets.
- *Custom modelling*: out of scope; not a 3D-art project.

---

## 7. Environment / arena assets

**Decision**: Use a free **Unity Asset Store** dungeon pack — specifically `Fantasy Dungeon` or `POLYGON Dungeon` (curriculum-named alternatives) — for stone walls, pillars, arched gates, and floor tiles. If neither is suitable, fall back to Unity primitives (`Cube`, `Cylinder`) styled with stone-textured materials. Torch emitters use point lights + a basic flame `ParticleSystem`.

**Rationale**:
- The Stage IV evaluation passed with this approach.
- Asset Store packs are free, dark-fantasy-themed, and require no custom modelling.
- Primitives are a safe Product-First fallback if asset import friction appears.

**Alternatives considered**:
- *Pure primitives only*: faster to author but visually flat; the curriculum already endorses a dungeon pack.
- *ProBuilder manual mesh*: doable but slower than dropping a pack in.

---

## 8. Unity Bridge as authoring channel

**Decision**: All scene mutation, prefab inspection, and compile-check operations during implementation are performed via `unity-cmd.ps1` (the project's existing `com.cziberpv.unity-bridge` integration). The bridge supports `scene`, `inspect`, `create`, `add-component`, `set`, `save-scene`, `new-scene`, `open-scene`, `refresh`, and `scratch` operations. The CLAUDE.md project file documents the exact JSON envelopes.

**Rationale**:
- The bridge is already installed and configured (`Assets/LLM/Bridge/` exists, `unity-cmd.ps1` is in the project root).
- PowerShell is available on macOS (confirmed by CLAUDE.md usage examples).
- Provides synchronous compile feedback via `refresh` — directly enforces Constitution I.

**Alternatives considered**:
- *Manual Editor operations*: slower and unrepeatable. Avoided.
- *A separate MCP server for Unity*: no such tool appears in the available toolchain; the Unity Bridge IS the integration.

---

## 9. Noise event bus implementation

**Decision**: A single `public static event System.Action<Vector3, float, NoiseType, GameObject> OnNoiseEmitted` declared on `NoiseEmitter.cs`. The player's `FootstepController` and `PlayerCombat` fire `EmitNoise(pos, radius, type)`; each enemy's `HearingSystem` subscribes in `OnEnable` and unsubscribes in `OnDisable`. Distance filter happens in the handler.

**Rationale**:
- The curriculum's Stage III evaluator explicitly approved this pattern.
- Replaces O(n²) per-pair distance checks with O(n) per emission.
- Trivial to implement, no infrastructure needed.

**Alternatives considered**:
- *Unity's `SendMessage`*: reflection-based, slow. Avoided.
- *MessagePipe / Zenject signal bus*: full DI framework. Massive overkill.

---

## 10. Wave configuration

**Decision**: Hardcoded constants inside `WaveManager.cs` — a simple `BuildSpawnList(int wave)` function that returns `(prefab, gateIndex)` pairs using the linear formulas from Stage III (`grunts = 2 + wave * 2`, knights from wave 5 onward, guardians from wave 3 onward). Difficulty multiplier is a single `float` constant for now.

**Rationale**:
- Product-First (Constitution IV): no ScriptableObject schema, no `WaveConfig.asset` per wave, no JSON. Just code that's easy to read and tweak.
- If a designer ever wants to tweak wave shape later, refactoring to ScriptableObjects is a small isolated change.

**Alternatives considered**:
- *ScriptableObject per wave*: clean but premature. YAGNI.
- *External JSON wave file*: complicates the build pipeline; no current need.

---

## 11. Group cooperation — alert broadcast

**Decision**: A singleton `EnemyManager.cs` exposes `AlertNearby(Vector3 origin, float radius, EnemyAI source)` which performs one `Physics.OverlapSphere` filtered by the `Enemy` layer mask and calls `ai.TakeDamage(0f)` on each neighbour (zero-damage hit triggers the existing perception-escalation branch — pattern endorsed by Stage III evaluator). The detecting enemy invokes this when its `PerceptionSystem` first escalates to `Combat`.

**Rationale**:
- Reuses the existing damage path's perception-escalation side effect; no new alert-specific code path needed.
- One `OverlapSphere` per detection event; O(1) cost amortised.

**Alternatives considered**:
- *Dedicated `AlertFromNearby(Vector3 pos)` method on EnemyAI*: cleaner separation of concerns. Stage III Evaluator 2 actually recommended this. Deferred — the zero-damage trick is faster to implement and acceptable per Product-First; can be refactored later.

---

## 12. Build target configuration

**Decision**: Keep `StandaloneOSX` (Apple Silicon) as the development build target while in the Editor on macOS. Configure `StandaloneWindows64` as a secondary build target so it is exercised at least once before the project closes. Both build settings live in `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ProjectSettings.asset`; the `Arena` scene must be added to `Scenes In Build` once it exists.

**Rationale**:
- Constitution III requires both targets to be buildable throughout the project.
- The CI/local-build cadence is "before each phase ends" rather than after every commit, to avoid build-time spam.

**Alternatives considered**:
- *Build both targets after every commit*: too slow during heavy iteration. Deferred to milestone checkpoints.

---

## Open questions / deferred decisions

None. All NEEDS CLARIFICATION items raised by the plan template are resolved above.

If any decision proves wrong during implementation, the constitution amendment procedure applies — but for now the plan is internally consistent and aligned with the spec.
