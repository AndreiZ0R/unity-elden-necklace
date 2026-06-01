---

description: "Task list for Elden Necklace — Arena Combat Core"
---

# Tasks: Elden Necklace — Arena Combat Core

**Input**: Design documents from `/specs/001-arena-combat-core/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: NOT included — the feature spec does not request tests; the constitution prioritises a shipping product over test infrastructure. Manual playtesting is the verification path per quickstart.md.

**Organization**: Tasks are grouped by user story to enable independent implementation and verification of each story per Constitution II (Additive Stability).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1, US2, US3, US4) — present only on user-story phase tasks
- All file paths assume the Unity project root (the directory containing `Assets/`)
- Bridge-driven scene/prefab tasks run via `unity-cmd.ps1` per `contracts/unity-bridge-commands.md`; after every C# write task, run `{"type":"refresh"}` before moving on (Constitution I)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project skeleton, scene file, and input bindings — nothing gameplay-specific yet.

- [X] T001 Create the script folder layout under `Assets/Scripts/`: `Player/`, `Enemies/`, `AI/`, `AI/BehaviourTree/`, `World/`, `UI/`, `Camera/`, `Game/`. Use Finder or `mkdir -p` (Unity will pick the folders up on next refresh).
- [X] T002 [P] Extend `Assets/InputSystem_Actions.inputactions` so the `Gameplay` action map contains `Move` (WASD Vector2), `Look` (mouse delta Vector2), `Attack` (Left Mouse Button), and `Pause` (Escape), per `contracts/input-actions.md`. Regenerate the C# class so `PlayerInputActions` is available to scripts.
- [X] T003 Create `Assets/Scenes/Arena.unity` via Bridge: send `{"type":"save-scene"}` first to checkpoint, then `{"type":"new-scene"}`, then `{"type":"save-scene"}` to write it to `Assets/Scenes/Arena.unity`. Add `Arena` to `File → Build Settings → Scenes In Build` (or via `ProjectSettings/EditorBuildSettings.asset`).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared runtime infrastructure that every user story consumes — arena geometry, baked NavMesh, the game manager singleton skeleton, the shared perception layer, and the noise event bus.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Build the Arena scene geometry via Bridge in `Assets/Scenes/Arena.unity`: an `Arena` root GameObject containing children `Floor` (24×24m primitive plane), `Walls` (four box walls forming an octagonal-feel boundary), `Pillars` (8 cubes scaled to ~1×4×1m placed in two rings), `Gates` (four empty markers at N/E/S/W positions just inside the walls), `Platform` (1×0.4×1m central raised cube), and `Lighting` (directional light + four point-light torches near gates). Use `{"type":"scratch", ...}` for the batch placement to keep the operation atomic. Then `save-scene`.
- [X] T005 Bake the NavMesh on the Arena scene: in Unity, open `Window → AI → Navigation`, mark `Floor` as `Navigation Static`, mark `Pillars`, `Walls`, and `Platform` as obstacles (or set them to `Navigation Static` carve), then `Bake`. Verify enemies will be able to path around obstacles by checking the green walkable overlay in the Scene view. Save scene.
- [X] T006 [P] Create `Assets/Scripts/AI/NoiseEmitter.cs` — a static class declaring `public enum NoiseType { Footstep, Attack, LandImpact }`, `public static event System.Action<Vector3, float, NoiseType, GameObject> OnNoiseEmitted`, and `public static void EmitNoise(Vector3 worldPos, float radius, NoiseType type, GameObject source)` that invokes the event. No instance state. Bridge `refresh` to confirm compile.
- [X] T007 [P] Create `Assets/Scripts/Game/GameManager.cs` — singleton (`public static GameManager Instance { get; private set; }` in `Awake` with `DontDestroyOnLoad`), expose `public int Score { get; private set; }`, `public int CurrentWave { get; private set; }`, `public bool IsGameOver { get; private set; }`, `public bool IsPaused { get; private set; }`, plus events `OnScoreChanged(int)`, `OnWaveChanged(int)`, `OnGameOver()`, `OnPauseChanged(bool)`. Add `AddScore(int)`, `TriggerGameOver()`, `TogglePause()` methods. Bridge `refresh`.
- [X] T008 [P] Create `Assets/Scripts/AI/PerceptionSystem.cs` — declares `public enum PerceptionState { Unaware, Suspicious, Alert, Combat }`, fields `CurrentState`, `HasTarget` (derived: state ≥ Alert), `LastKnownPosition`, `alertTimeout` (5s), `suspiciousTimeout` (8s); `[RequireComponent]` for `VisionSystem` and `HearingSystem`; `EscalateTo(state)` raises only; de-escalation runs from `Update()` timers; fires `OnStateChanged(old, new)` event. Bridge `refresh`.
- [X] T009 [P] Create `Assets/Scripts/AI/VisionSystem.cs` — forward-facing cone check using `Physics.OverlapSphere` + dot product + `Physics.Raycast` occlusion against a configurable `sightBlockers` layer mask. Fields: `fieldOfViewAngle`, `visionRange`, `peripheralRange` (always-detect within), `checkInterval` (0.1s). Caches the player `Transform` in `Awake` via `FindFirstObjectByType<PlayerController>()`. Exposes `TargetVisible`, `TargetDistance`, `LastKnownPosition`, `LastKnownTransform`. Bridge `refresh`.
- [X] T010 [P] Create `Assets/Scripts/AI/HearingSystem.cs` — subscribes to `NoiseEmitter.OnNoiseEmitted` in `OnEnable`, unsubscribes in `OnDisable`. Fields: `hearingRange`, `combatHearingRange` (larger when already in combat), `minimumNoiseRadius`. Exposes `OnNoiseHeard(Vector3 pos, NoiseType type)` event. Filters out noise sourced from itself. Bridge `refresh`.

**Checkpoint**: Arena scene is built, NavMesh is baked, the four shared runtime classes (NoiseEmitter, GameManager, PerceptionSystem, VisionSystem, HearingSystem) compile. User story implementation can begin.

---

## Phase 3: User Story 1 - Survive a Wave of Melee Enemies (Priority: P1) 🎯 MVP

**Goal**: Deliver the full playable gameplay loop — player can move/look/attack, stamina gates attacks, health drops on enemy hit, dying ends the run, and the basic Grunt enemy (FSM-driven) emerges from the four gates in escalating waves. After this phase the game is already a complete one-archetype arena survival prototype.

**Independent Test**: Launch the Arena scene, kill at least one grunt from wave 1, observe stamina deplete on attack and regenerate, take damage from a grunt, die from accumulated damage, see the game-over panel display the final wave and score.

### Player implementation

- [X] T011 [P] [US1] Create `Assets/Scripts/Player/PlayerHealth.cs` — fields `maxHealth` (100), `CurrentHealth` property; methods `TakeDamage(float)` (clamp to `[0, max]`, fire `OnHealthChanged(current, max)`, fire `OnDeath()` when crossing zero) and `Heal(float)`. Bridge `refresh`.
- [X] T012 [P] [US1] Create `Assets/Scripts/Player/PlayerStamina.cs` — fields `maxStamina` (100), `regenRate` (25/s), `regenDelay` (1.5s), `attackCost` (20); method `Spend(float cost) → bool` (returns false if insufficient, resets regen-delay timer otherwise); per-frame regen after delay; fires `OnStaminaChanged(current, max)`. Bridge `refresh`.
- [X] T013 [P] [US1] Create `Assets/Scripts/Player/PlayerController.cs` — `[RequireComponent(typeof(CharacterController))]`. Reads `Move` and `Look` from the generated `PlayerInputActions` C# class. Moves the `CharacterController` at `moveSpeed = 5` m/s camera-relative (project camera forward/right onto XZ plane). Public `IsBlocking` property returning false (placeholder used by `AnimatorSync` in later stories). Bridge `refresh`.
- [X] T014 [P] [US1] Create `Assets/Scripts/Camera/ThirdPersonCamera.cs` — fields `target` (`Transform`), `offset` (`Vector3(0, 1.8, 0)`), `defaultDistance` (5m), `mouseSensitivity` (2), `minPitch` (-20), `maxPitch` (60), `collisionPadding` (0.3), `collisionMask`. `LateUpdate` reads mouse axes, clamps pitch, performs `Physics.SphereCast` from pivot along camera arm to shorten on wall hit; smooths arm with `Mathf.Lerp` at 10/s. Lock cursor when not paused. Bridge `refresh`.
- [X] T015 [US1] Create `Assets/Scripts/Player/PlayerCombat.cs` — depends on T011, T012. Reads `Attack` from input. On press: if `attackTimer <= 0` AND `stamina.Spend(attackCost)`, set animator trigger `LightAttack`, start `attackTimer = attackCooldown`, perform `Physics.OverlapSphere(transform.position + transform.forward * 1.2f, attackRange = 2f, enemyLayerMask)`, call `EnemyHealth.TakeDamage(25)` on each hit, then `NoiseEmitter.EmitNoise(pos, 8f, NoiseType.Attack, gameObject)`. Bridge `refresh`.
- [X] T016 [US1] Assemble `Assets/Prefabs/Player.prefab` via Bridge: create a humanoid GameObject from a Mixamo character (FBX with humanoid rig), attach `CharacterController` (radius 0.4, height 1.8), `Animator` controller with `Speed` float + `LightAttack` trigger + `Hit` trigger + `Death` trigger parameters, `PlayerHealth`, `PlayerStamina`, `PlayerController`, `PlayerCombat`, and a child `Camera` GameObject with `ThirdPersonCamera` script + `Camera` component + `AudioListener`. Wire `ThirdPersonCamera.target = Player.transform`. Save prefab.

### Enemy & wave implementation (single-archetype Grunt only)

- [X] T017 [P] [US1] Create `Assets/Scripts/World/SpawnGate.cs` — fields `gateIndex` (int 0..3 for N/E/S/W), `spawnPoint` (`Transform`). Trivial MonoBehaviour. Bridge `refresh`.
- [X] T018 [P] [US1] Create `Assets/Scripts/Enemies/EnemyHealth.cs` — fields `maxHealth` (set by archetype companion), `CurrentHealth`. `TakeDamage(float)` fires `OnHealthChanged` and `OnDeath(EnemyHealth)` (passes self) when crossing zero. Bridge `refresh`.
- [X] T019 [US1] Create `Assets/Scripts/Enemies/EnemyAI.cs` — depends on T008 (PerceptionSystem) and T018 (EnemyHealth). Declares `public enum EnemyType { Grunt, Guardian, Knight }` and `public enum AIState { Idle, Patrol, Alert, Chase, Attack, Dead }`. `[RequireComponent]` for `NavMeshAgent`, `PerceptionSystem`. Per-frame `switch (State)` dispatches to private update methods. Subscribes to `perception.OnStateChanged` in `Start` to drive transitions per the table in `data-model.md`. On `Dead`: `agent.isStopped = true; agent.enabled = false; Destroy(gameObject, 3f)`. Includes `TakeDamage(float)` proxy to `EnemyHealth.TakeDamage` plus perception escalation. Bridge `refresh`.
- [X] T020 [P] [US1] Create `Assets/Scripts/Enemies/GruntAI.cs` — companion to `EnemyAI`. `Awake` sets `enemyType = Grunt`, `maxHealth = 40`, `attackDamage = 12`, `attackRange = 1.5`, `agent.speed = 3.5`, `attackCooldown = 1.5`. Bridge `refresh`.
- [X] T021 [US1] Assemble `Assets/Prefabs/Grunt.prefab` via Bridge: humanoid mesh (different Mixamo character or recoloured warrior), `NavMeshAgent` (radius 0.4, height 1.8, stoppingDistance 1.2, obstacleAvoidance Med), `CapsuleCollider`, `Animator` with `Speed` + `Attack` + `Hit` + `Death` params, `VisionSystem` (FOV 120°, range 8m, peripheral 2.5m), `HearingSystem` (range 5m), `PerceptionSystem`, `EnemyHealth`, `EnemyAI`, `GruntAI`. Set the `Enemy` layer on the GameObject. Save prefab.
- [X] T022 [P] [US1] Create `Assets/Scripts/World/WaveManager.cs` — fields `gates` (`SpawnGate[]`), `gruntPrefab` (`GameObject`), `spawnInterval` (0.8s), `restDuration` (5s); state `currentWave` (int), `aliveEnemies` (`List<EnemyHealth>`); methods `BuildSpawnList(int wave)` returning `List<(GameObject, int gateIndex)>` with formula `grunts = 2 + wave * 2` and round-robin gate assignment + Fisher-Yates shuffle; coroutine `StartWave(int wave)` instantiates one enemy per `spawnInterval` at the assigned gate's `spawnPoint`, subscribes to each `EnemyHealth.OnDeath` to remove from `aliveEnemies`; when empty, fires `OnWaveCleared` and starts the rest coroutine. Fires `OnWaveChanged(int)` on increment. Notifies `GameManager.AddScore(currentWave * 100)` on each enemy death. Bridge `refresh`.

### Game state & minimal HUD

- [X] T023 [US1] Extend `Assets/Scripts/Game/GameManager.cs` (from T007) to subscribe to `PlayerHealth.OnDeath` (find via `FindFirstObjectByType<PlayerHealth>()` in `Start`); on death set `IsGameOver = true`, `Time.timeScale = 0`, unlock cursor, fire `OnGameOver()`. Add `RestartGame()` method that resets time scale and reloads the scene. Bridge `refresh`.
- [X] T024 [P] [US1] Create a minimal `Assets/Prefabs/HUDCanvas.prefab` via Bridge — a `Canvas` (Screen Space Overlay) with four child `Text` (or TextMeshProUGUI) elements positioned in the corners showing placeholder values for `HP: 100`, `ST: 100`, `Wave: 1`, `Score: 0`, plus a hidden `GameOver` panel child containing `Final Wave: X` and `Final Score: Y` text fields and a `Restart` button. A throw-away `HUDStub.cs` component on the canvas hooks `PlayerHealth.OnHealthChanged`, `PlayerStamina.OnStaminaChanged`, `GameManager.OnScoreChanged`, `WaveManager.OnWaveChanged`, and `GameManager.OnGameOver` to update the texts and reveal the panel. This stub will be replaced wholesale by a polished `HUDController` in US4. Bridge `refresh`.
- [X] T025 [US1] Wire the Arena scene via Bridge: place `Player.prefab` at the central platform position; create four `SpawnGate` GameObjects as children of the existing `Gates` group, set `gateIndex` 0..3 and assign each `spawnPoint` to a transform just inside the corresponding gate arch on the NavMesh; create a `WaveManager` GameObject, assign `gates` and `gruntPrefab` in the inspector; create a `GameManager` GameObject; instantiate `HUDCanvas.prefab`. Save scene.
- [ ] T026 [US1] Manual smoke test in Editor: press Play, verify (a) player moves with WASD and camera rotates with mouse, (b) at least one grunt spawns from each of two gates during wave 1, (c) attack consumes stamina and damages a grunt, (d) attack is refused when stamina is empty, (e) player taking enough hits dies and the game-over panel appears with non-zero wave + score, (f) restart button reloads the scene cleanly. Document any failures and fix before declaring the checkpoint complete.

**Checkpoint**: User Story 1 is fully functional. The game is shippable as a single-archetype arena survival prototype.

---

## Phase 4: User Story 2 - Face Three Distinct Enemy Archetypes With Different AI (Priority: P1)

**Goal**: Add the curriculum-required Guardian (FSM with patrol waypoints) and Knight (Behaviour Tree with Berserk threshold) archetypes alongside the existing Grunt. After this phase, all three curriculum-mandated AI techniques (FSM, Behaviour Tree, NavMesh, perception) are demonstrated.

**Independent Test**: Reach wave 3 and confirm a Guardian spawns; observe it patrolling between waypoints before engaging. Reach wave 5 and confirm a Knight spawns; damage it past the wound threshold and observe a visibly different behaviour (faster pursuit, faster swings).

### Guardian — patrolling FSM archetype

- [X] T027 [P] [US2] Create `Assets/Scripts/Enemies/GuardianAI.cs` — companion to `EnemyAI`. `Awake` sets `enemyType = Guardian`, `maxHealth = 100`, `attackDamage = 20`, `attackRange = 1.8`, `agent.speed = 2.0`, `attackCooldown = 2.5`. Adds public `Transform[] patrolWaypoints` and `private int currentWaypoint`. Bridge `refresh`.
- [X] T028 [US2] Extend `Assets/Scripts/Enemies/EnemyAI.cs` so `UpdatePatrol()` reads waypoints from a `GuardianAI` companion (via `GetComponent<GuardianAI>()`); if present, paths between them with a 1-second pause at each (using a private timer). Grunt's UpdatePatrol remains a no-op idle. Bridge `refresh`; re-verify Grunt behaviour from US1 is unaffected (Constitution II).
- [X] T029 [US2] Assemble `Assets/Prefabs/Guardian.prefab` via Bridge: heavier-looking humanoid (Mixamo or recoloured warrior with armour material), `NavMeshAgent` (stoppingDistance 1.8, speed 2.0), `CapsuleCollider`, `Animator`, `VisionSystem` (FOV 90°, range 12m), `HearingSystem` (range 7m), `PerceptionSystem`, `EnemyHealth`, `EnemyAI`, `GuardianAI`. Set `Enemy` layer.
- [X] T030 [US2] Place 4 patrol-waypoint empty Transforms in the Arena scene (near each gate) and assign them to a sample Guardian instance's `patrolWaypoints` array. The `WaveManager` will reuse the same waypoint set for every spawned Guardian via inspector reference passed through the prefab.

### Knight — Behaviour Tree archetype

- [X] T031 [P] [US2] Create `Assets/Scripts/AI/BehaviourTree/BTNode.cs` — abstract base class with `public enum Status { Success, Failure, Running }` and `public abstract Status Tick();`. Bridge `refresh`.
- [X] T032 [P] [US2] Create `Assets/Scripts/AI/BehaviourTree/Sequence.cs` — composite that ticks children in order; returns `Failure` on first child failure, `Running` on first running child, `Success` if all succeed. Bridge `refresh`.
- [X] T033 [P] [US2] Create `Assets/Scripts/AI/BehaviourTree/Selector.cs` — composite that ticks children in order; returns `Success` on first success, `Running` on first running child, `Failure` if all fail. Bridge `refresh`.
- [X] T034 [P] [US2] Create `Assets/Scripts/AI/BehaviourTree/Leaf.cs` — two concrete leaf types: `Action(Func<Status>)` and `Condition(Func<bool>)`. Bridge `refresh`.
- [X] T035 [US2] Create `Assets/Scripts/Enemies/KnightBT.cs` — depends on T031-T034 and T008 (PerceptionSystem). `[RequireComponent]` for `NavMeshAgent`, `PerceptionSystem`, `EnemyHealth`. Fields: `berserkThreshold` (0.3), `attackRange` (2.0), `attackDamage` (25), `attackCooldown` (2.0), private `attackTimer`. In `Awake` build the BT root per `data-model.md` Entity 2 → KnightBT: a Selector with [Dead branch, Berserk branch (if HP < threshold), Standard branch (chase + attack)]. `Update()` calls `root.Tick()`. Includes private helpers `Chase()`, `BerserkChase()` (agent.speed *= 1.4), `StandardAttack()`, `BerserkAttack()` (cooldown × 0.55). Bridge `refresh`.
- [X] T036 [US2] Assemble `Assets/Prefabs/Knight.prefab` via Bridge: imposing humanoid (Mixamo armoured character), `NavMeshAgent` (speed 2.5, stoppingDistance 1.8), `CapsuleCollider`, `Animator`, `VisionSystem` (FOV 140°, range 15m), `HearingSystem` (range 10m), `PerceptionSystem`, `EnemyHealth (maxHealth = 150)`, `KnightBT`. Set `Enemy` layer.

### Wave system extension

- [X] T037 [US2] Extend `Assets/Scripts/World/WaveManager.cs` (from T022): add serialized `guardianPrefab` and `knightPrefab` fields. Update `BuildSpawnList(int wave)` to append `guardians = wave >= 3 ? (wave - 2) : 0` Guardian entries and `knights = wave >= 5 ? (wave - 4) : 0` Knight entries; preserve the existing round-robin + Fisher-Yates logic. Assign both new prefabs in the inspector on the Arena scene's `WaveManager`. Verify wave 1-2 still spawn only grunts (Constitution II additive check).
- [ ] T038 [US2] Manual playtest: reach wave 3, confirm at least one Guardian appears and patrols its waypoints before engaging; reach wave 5, confirm a Knight appears; dump damage on the Knight until below 30% HP, confirm visibly faster movement and swing cadence.

**Checkpoint**: User Story 2 complete. All three curriculum-required AI archetypes (FSM, FSM-with-patrol, Behaviour Tree) are demonstrably present.

---

## Phase 5: User Story 3 - Enemies Cooperate as a Group (Priority: P2)

**Goal**: Implement the curriculum-required group tactical cooperation — alert broadcast between nearby enemies and spacing between pursuing enemies. Multi-gate distribution is already in WaveManager (since US1's spawn-list builder uses round-robin gates).

**Independent Test**: Spawn 3 grunts in visual range of each other (e.g. wave 1), approach the closest one; the other two transition to chase within 3 seconds without the player walking up to them individually. Reach a wave with 4+ enemies and confirm enemies emerge from at least 2 of the 4 gates.

- [X] T039 [P] [US3] Create `Assets/Scripts/World/EnemyManager.cs` — singleton (`Instance` set in `Awake`). Method `public void AlertNearby(Vector3 origin, float radius, EnemyAI source)` performs `Physics.OverlapSphere(origin, radius, LayerMask.GetMask("Enemy"))`, iterates, skips the source, and for any neighbour in `Patrol` or `Alert` state calls `ai.TakeDamage(0f)` (per research §11 — zero-damage hit triggers existing perception escalation). Bridge `refresh`.
- [X] T040 [US3] Extend `Assets/Scripts/AI/PerceptionSystem.cs` (from T008) so that when `CurrentState` first escalates to `Combat`, it calls `EnemyManager.Instance?.AlertNearby(transform.position, 10f, GetComponent<EnemyAI>())`. Skip for Knight (uses `KnightBT`, not `EnemyAI`) — guard with a null check on the `EnemyAI` reference. Bridge `refresh`; re-test US1 and US2 to confirm nothing regressed.
- [X] T041 [P] [US3] Create `Assets/Scripts/AI/FlockingBehavior.cs` — Reynolds separation steering (the simplified single-rule version of the curriculum's Stage V Boids). Fields: `neighbourRadius` (4m), `separationRadius` (1.6m), `separationWeight` (1.4), `maxOffsetDistance` (1.0m), `updateInterval` (0.15s), `enemyLayerMask`. `Tick()` runs once per interval using `Physics.OverlapSphereNonAlloc` with a static buffer; returns a separation offset capped at `maxOffsetDistance`. `ApplyTo(Vector3 target) → Vector3` adds the offset and projects via `NavMesh.SamplePosition` to ensure the result is navigable. Bridge `refresh`.
- [X] T042 [US3] Extend `Assets/Scripts/Enemies/EnemyAI.cs` (and mirror in `KnightBT.cs`): in `UpdateChase()` if a `FlockingBehavior` component is present, call `flocking.Tick()` and `agent.SetDestination(flocking.ApplyTo(target))` instead of the raw target. Add `FlockingBehavior` component to the Grunt, Guardian, and Knight prefabs via Bridge.
- [X] T043 [US3] Add the `EnemyManager` GameObject to the Arena scene via Bridge; save scene. Confirm in the inspector that the `Enemy` layer exists (add via `Layers → Edit Layers` if not) and that all three enemy prefabs are assigned to it.
- [ ] T044 [US3] Manual playtest: line up 3 grunts visible from the player's spawn position in a test wave; approach one — verify the others react within 3 seconds (SC-003). Trigger a wave with 4+ enemies; visually count gates used during the spawn-in (SC-004). With 5+ enemies pursuing simultaneously, verify they do not all stack at the same world point (SC enabled by FR-029).

**Checkpoint**: User Story 3 complete. Group cooperation is demonstrably present and SC-003 + SC-004 are satisfied.

---

## Phase 6: User Story 4 - Track Survival Progress on the HUD (Priority: P2)

**Goal**: Replace the stub HUD from US1 with a polished, designed HUD that uses real UI sliders for health/stamina and styled text for wave/score, positioned to not obscure the combat area.

**Independent Test**: Begin a run; verify all four readouts are visible from frame one without blocking the central arena view; take damage / attack / clear a wave / kill enemies and verify each readout updates instantly.

- [X] T045 [P] [US4] Create `Assets/Scripts/UI/HUDController.cs` — serialized fields for `healthSlider` (`UnityEngine.UI.Slider`), `staminaSlider`, `waveText` (`Text`), `scoreText`, `gameOverPanel`, `finalScoreText`, `finalWaveText`, `restartButton`. In `Start()` find `PlayerHealth`, `PlayerStamina`, `WaveManager`, `GameManager` (via `FindFirstObjectByType`) and subscribe each event to the matching update method per `data-model.md` Entity 6. Unsubscribe in `OnDestroy`. Hook the restart button's `onClick` to `GameManager.RestartGame()`. Bridge `refresh`.
- [X] T046 [US4] Rebuild `Assets/Prefabs/HUDCanvas.prefab` via Bridge: a `Canvas` (Screen Space Overlay) with `CanvasScaler` set to Scale With Screen Size (reference 1920×1080). Top-left: vertical pair of `Slider` components (Health red fill, Stamina green fill) anchored top-left with padding. Top-right: `WAVE N` text anchored top-right with `Score: NNN` underneath. Bottom-centre: hidden `GameOverPanel` containing `Final Wave: N`, `Final Score: NNN`, and a `Restart` button. Attach `HUDController` to the Canvas and assign all serialized fields. Save prefab.
- [X] T047 [US4] In the Arena scene, delete the stub HUD instance from US1 (T024/T025) and instantiate the new `HUDCanvas.prefab`. Remove the now-unused `Assets/Scripts/UI/HUDStub.cs` script and its `.cs.meta` to keep the project clean. Bridge `refresh` to confirm no broken references. Save scene.
- [ ] T048 [US4] Manual playtest: take damage and confirm the health slider drops the same frame (SC-009); attack and confirm the stamina slider drops then regenerates after the delay; clear a wave and confirm the wave text increments; kill enemies and confirm the score text increases. Take a screenshot of the in-game view and verify no HUD element overlaps the central 60% of the screen (FR-034).

**Checkpoint**: User Story 4 complete. The HUD is polished and SC-009 is satisfied.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Constitution III cross-platform validation, edge-case hardening per spec, and runtime stability checks. None of these add new features — they harden the existing four stories.

- [X] T049 [P] Implement the pause edge case: extend `GameManager.TogglePause()` to set `Time.timeScale = 0/1`, fire `OnPauseChanged(bool)`, and unlock/lock the cursor. Add a `Pause` input binding handler on the Player or GameManager that calls `TogglePause()`. Add a minimal `Paused` text overlay on `HUDCanvas` that is shown/hidden via `OnPauseChanged`. Bridge `refresh` and test in Editor.
- [X] T050 [P] Verify the spawn-safety edge case from the spec: extend `WaveManager.StartWave` so the spawn position is validated with `NavMesh.SamplePosition` before each `Instantiate`; if invalid, walk inward 0.5m at a time until a valid point is found (max 5 attempts). Re-test wave 5+ to confirm no enemies are stuck off-mesh at gate arches.
- [X] T051 [P] Verify the player-dies-mid-wave edge case: ensure all `EnemyAI.Update()` and `KnightBT.Update()` short-circuit when `GameManager.Instance.IsGameOver` is true. Bridge `refresh`; play through to player death and confirm enemies freeze immediately.
- [ ] T052 Build the macOS standalone: `File → Build Profiles → macOS → Build` to `Builds/macOS/EldenNecklace.app`. Launch the app outside the Editor and confirm the full gameplay loop works through at least wave 3.
- [ ] T053 Build the Windows standalone: `File → Build Profiles → Windows (x86_64) → Build` to `Builds/Windows/EldenNecklace.exe`. Transfer to a Windows machine (or test under a Windows VM) and confirm the full gameplay loop works through at least wave 3 (SC-008).
- [ ] T054 Run the full spec verification checklist in `quickstart.md` section 6: tick off each of SC-001 through SC-009 against the final build. Document any failures and create remediation tasks before declaring the feature done.
- [ ] T055 [P] Profile a wave-5 stress scenario: enable the Unity Profiler, play through wave 5 with all three archetypes simultaneously active; capture FPS over a 30-second window; confirm stable ≥ 60 FPS on the development host. If lower, apply the throttling techniques from research/curriculum (VisionSystem `checkInterval` increase, fewer simultaneous active sensors).
- [ ] T056 [P] Final constitution gate check: re-read `.specify/memory/constitution.md` and confirm against the final state of the project — (I) the project compiles, (II) all four user stories work end-to-end on the same build, (III) both macOS and Windows builds run, (IV) no third-party AI packages were pulled in. Document the result at the bottom of `specs/001-arena-combat-core/quickstart.md` or in a brief release note.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational. P1 MVP.
- **User Story 2 (Phase 4)**: Depends on US1 (extends `EnemyAI`, `WaveManager`, `Player` prefab). P1.
- **User Story 3 (Phase 5)**: Depends on US1 (PerceptionSystem must exist on enemies) and US2 (Knight prefab must exist before its `EnemyAI`-less perception hook is wired). P2.
- **User Story 4 (Phase 6)**: Depends on US1 (consumes all the change events US1 emits). P2.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Internal Dependencies

- **US1**: Player scripts (T011-T015) can be written in parallel; the prefab assembly (T016) needs all of them. EnemyHealth (T018), SpawnGate (T017), EnemyAI base (T019), and GruntAI (T020) can be parallel; Grunt prefab (T021) needs them all. WaveManager (T022) needs SpawnGate + EnemyHealth. Scene wiring (T025) needs every prefab assembled.
- **US2**: BT node classes (T031-T034) are parallel; `KnightBT` (T035) depends on all four. GuardianAI (T027) and Knight BT (T031-T035) can progress in parallel.
- **US3**: `EnemyManager` (T039) and `FlockingBehavior` (T041) are parallel; wiring them in (T040, T042) depends on the corresponding script.
- **US4**: `HUDController` script (T045) and `HUDCanvas` prefab rebuild (T046) can be authored in parallel once the script exists.

### Parallel Opportunities (single-developer scope)

Within the Setup and Foundational phases, T002 (Input Actions) and T003 (scene create) can run while T004-T010 scripts are being authored; the scripts only need to exist before scene wiring begins.

Within US1, the five player scripts (T011-T015) and the enemy base scripts (T017-T020) can all be authored back-to-back as code — the Bridge prefab assembly steps (T016, T021, T025) must run sequentially against the same Unity Editor session.

---

## Suggested MVP Scope

**Minimum viable product = User Story 1 only** (Phases 1 + 2 + 3).

After completing T026 the project is already playable: a third-person warrior fights waves of grunts in the arena, with stamina-gated combat and a game-over flow. The curriculum AI requirements (3 archetypes, BT) are added in subsequent phases without disrupting this baseline (Constitution II).

---

## Format Validation

All tasks above follow the required format:
- ✅ Every task starts with `- [ ]`
- ✅ Every task has a sequential `T###` ID
- ✅ `[P]` marker present where genuine parallelism exists (different files, no incomplete dependencies)
- ✅ `[US1]`/`[US2]`/`[US3]`/`[US4]` story labels present on user-story phase tasks; absent from Setup, Foundational, Polish phases
- ✅ Every task names exact file paths or scene/prefab artefacts
