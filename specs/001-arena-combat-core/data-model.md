# Phase 1 Data Model: Elden Necklace — Arena Combat Core

**Branch**: `001-arena-combat-core` | **Date**: 2026-06-01

This document maps the spec's seven key entities to concrete Unity runtime data shapes (C# fields, events, and component composition). Data lives in-memory only — there is no persistence layer.

---

## Entity 1: Player Warrior

**Composed of**: a single `Player.prefab` carrying a `CharacterController`, an `Animator` (humanoid), and the following scripts:

### `PlayerHealth.cs`
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `maxHealth` | `float` | 100 | Inspector-tunable |
| `CurrentHealth` | `float` (property) | `maxHealth` | Read-only outside the script |
| `IsDead` | `bool` (property) | `false` | Computed from `CurrentHealth <= 0` |

**Events**:
- `event System.Action<float, float> OnHealthChanged` — fires on damage and on heal with `(current, max)`.
- `event System.Action OnDeath` — fires once when `CurrentHealth` first crosses zero.

**Validation**: `TakeDamage(amount)` clamps `CurrentHealth` to `[0, maxHealth]`. Negative damage is ignored.

### `PlayerStamina.cs`
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `maxStamina` | `float` | 100 | Inspector-tunable |
| `CurrentStamina` | `float` (property) | `maxStamina` | |
| `regenRate` | `float` | 25 | Stamina per second |
| `regenDelay` | `float` | 1.5 | Seconds after last spend before regen starts |
| `attackCost` | `float` | 20 | Stamina cost of one attack |

**Events**:
- `event System.Action<float, float> OnStaminaChanged` — fires on every change with `(current, max)`.

**State transitions**:
- `Spend(cost)` → `CurrentStamina -= cost`; resets regen-delay timer. Returns `bool` (false if insufficient).
- After `regenDelay` seconds with no spend, stamina regenerates linearly at `regenRate` per second until clamped at `maxStamina`.

### `PlayerController.cs`
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `moveSpeed` | `float` | 5 | m/s |
| `mouseSensitivity` | `float` | 2 | Forwarded to camera |
| `cc` | `CharacterController` | (required) | `[RequireComponent]` |

Reads `Move` (Vector2) and `Look` (Vector2) from the Input Actions asset. Move direction is camera-relative.

### `PlayerCombat.cs`
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `attackRange` | `float` | 2 | Radius of hitbox sphere |
| `attackDamage` | `float` | 25 | HP dealt per hit |
| `attackCooldown` | `float` | 1.0 | Seconds between swings |
| `attackTimer` | `float` (private) | 0 | Counts down |

Reads `Attack` input. On press: if `attackTimer <= 0` AND `stamina.Spend(attackCost)`, fires animator trigger, runs an Animation Event-timed `Physics.OverlapSphere` against `EnemyHealth` colliders, and emits a `NoiseEvent`.

---

## Entity 2: Enemy Agent

**Composed of**: one of three prefabs (`GruntEnemy`, `GuardianEnemy`, `KnightEnemy`) — each with `NavMeshAgent`, `Animator`, capsule `Collider`, and the following scripts:

### `EnemyHealth.cs` (shared by all 3 archetypes)
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `maxHealth` | `float` | varies by archetype | Set by archetype script in `Awake` |
| `CurrentHealth` | `float` (property) | `maxHealth` | |
| `IsDead` | `bool` (property) | `false` | |

**Events**:
- `event System.Action<EnemyHealth> OnDeath` — fires once; used by `GameManager` for score and by `WaveManager` for wave-clear detection.

### `EnemyAI.cs` (shared FSM for Grunt + Guardian)
| Field | Type | Notes |
|-------|------|-------|
| `enemyType` | `EnemyType` enum | `Grunt` or `Guardian` |
| `State` | `AIState` (property) | Public read-only |
| `agent` | `NavMeshAgent` | `[RequireComponent]` |
| `perception` | `PerceptionSystem` | `[RequireComponent]` |
| `attackRange` | `float` | Set by type |
| `attackDamage` | `float` | Set by type |
| `attackCooldown` | `float` | Set by type |
| `attackTimer` | `float` (private) | |

**Enums**:
```csharp
public enum EnemyType { Grunt, Guardian, Knight }
public enum AIState  { Idle, Patrol, Alert, Chase, Attack, Dead }
```

**State transitions** (driven by `PerceptionSystem` events + per-frame distance checks):

| From → To | Trigger |
|-----------|---------|
| `Idle` / `Patrol` → `Alert` | `PerceptionState` rises to `Suspicious` |
| `Alert` → `Chase` | `PerceptionState` reaches `Alert` or `Combat` |
| `Chase` → `Attack` | Distance to player ≤ `attackRange` |
| `Attack` → `Chase` | Distance to player > `attackRange × 1.4` (hysteresis) |
| `Chase` / `Alert` → `Patrol` | `PerceptionState` returns to `Unaware` (after timeout) |
| Any → `Dead` | `EnemyHealth.CurrentHealth ≤ 0` |

On `Dead`: `agent.isStopped = true; agent.enabled = false;` then `Destroy(gameObject, 3f)`.

### `GruntAI.cs` (companion; configures EnemyAI in `Awake`)
Sets `maxHealth = 40`, `attackDamage = 12`, `attackRange = 1.5`, `agent.speed = 3.5`, `attackCooldown = 1.5`.

### `GuardianAI.cs` (companion)
Sets `maxHealth = 100`, `attackDamage = 20`, `attackRange = 1.8`, `agent.speed = 2.0`, `attackCooldown = 2.5`. Holds a `Transform[] patrolWaypoints` array and a `currentWaypoint` index. In the `Patrol` state branch of `EnemyAI`, the guardian path-moves between waypoints with a 1-second pause at each.

### `KnightBT.cs` (replaces `EnemyAI` on the Knight prefab)
| Field | Type | Notes |
|-------|------|-------|
| `health` | `EnemyHealth` | |
| `agent` | `NavMeshAgent` | |
| `perception` | `PerceptionSystem` | |
| `berserkThreshold` | `float` | 0.3 (30%) |
| `attackRange` | `float` | 2.0 |
| `attackDamage` | `float` | 25 |
| `attackCooldown` | `float` | 2.0 |
| `root` | `BTNode` (private) | Built in `Awake` |

Holds the BT root. Each `Update()` calls `root.Tick(blackboard)`. The blackboard is `this` — leaves access knight state directly.

**BT root structure** (built in `Awake`):
```
Selector (root)
├── Sequence (Dead)
│   ├── Condition: health.IsDead
│   └── Action: StopAndDie
├── Sequence (Berserk)
│   ├── Condition: health.CurrentHealth / health.maxHealth < berserkThreshold
│   ├── Condition: perception.HasTarget
│   └── Selector
│       ├── Sequence: InAttackRange → BerserkAttack (cooldown × 0.55)
│       └── Action: ChaseFast (agent.speed × 1.4)
└── Sequence (Standard)
    ├── Condition: perception.HasTarget
    └── Selector
        ├── Sequence: InAttackRange → StandardAttack
        └── Action: Chase
```

If `perception.HasTarget` is false, the BT falls through to a default `Patrol` leaf (one waypoint loop).

---

## Entity 3: Enemy Archetype

This entity is **not** a runtime object — it is the static configuration baked into the three companion scripts (`GruntAI.cs`, `GuardianAI.cs`, `KnightBT.cs`) and the perception parameters configured on each prefab's `VisionSystem` / `HearingSystem` components.

| Archetype | Decision Model | Vision FOV / Range | Hearing Range | First Wave | HP / DMG / Speed |
|-----------|---------------|--------------------|---------------|------------|-------------------|
| Grunt | FSM | 120° / 8 m | 5 m | 1 | 40 / 12 / 3.5 |
| Guardian | FSM + Patrol | 90° / 12 m | 7 m | 3 | 100 / 20 / 2.0 |
| Knight | Behaviour Tree | 140° / 15 m | 10 m | 5 | 150 / 25 / 2.5 (×1.4 in Berserk) |

These values live as inspector-tunable fields on each prefab — no `ScriptableObject` archetype asset is created (per research §10).

---

## Entity 4: Wave

**Implemented in**: `WaveManager.cs` (no separate type).

| Field | Type | Notes |
|-------|------|-------|
| `currentWave` | `int` | Wave number, 1-indexed |
| `aliveEnemies` | `List<EnemyHealth>` | Tracked so wave-clear can fire |
| `restDuration` | `float` | 5 seconds between waves |
| `spawnInterval` | `float` | 0.8 seconds between individual enemy spawns |
| `gates` | `SpawnGate[]` | The four cardinal gate references |

**Methods**:
- `BuildSpawnList(int wave)` → `List<(GameObject prefab, int gateIndex)>` — applies the per-wave count formulas and round-robin gate distribution from research §10.
- `StartWave(int wave)` — coroutine: builds the spawn list, Fisher-Yates shuffles it, instantiates one enemy every `spawnInterval`.
- `OnEnemyDied(EnemyHealth e)` — removes `e` from `aliveEnemies`; when empty, fires `OnWaveCleared` and starts the rest coroutine.

**Events**:
- `event System.Action<int> OnWaveChanged` — fires when `currentWave` increments.
- `event System.Action OnWaveCleared` — fires after the last enemy of a wave dies.

**Wave count formula** (from research §10):
```
grunts    = 2 + wave * 2          // 4, 6, 8, ...
guardians = wave >= 3 ? (wave - 2) : 0
knights   = wave >= 5 ? (wave - 4) : 0
```

---

## Entity 5: Spawn Gate

**Implemented in**: `SpawnGate.cs` — a thin `MonoBehaviour` carrying just a world transform.

| Field | Type | Notes |
|-------|------|-------|
| `gateIndex` | `int` | 0..3 → N, E, S, W |
| `spawnPoint` | `Transform` | Where enemies spawn (one step inside the gate arch) |

`WaveManager.gates` holds the four registered `SpawnGate` instances, populated in the inspector via drag-and-drop.

---

## Entity 6: HUD Readouts

**Implemented in**: `HUDController.cs` (one component on `HUDCanvas.prefab`).

| Serialized Field | Type | Bound to |
|------------------|------|----------|
| `healthSlider` | `UnityEngine.UI.Slider` | `PlayerHealth.OnHealthChanged` |
| `staminaSlider` | `UnityEngine.UI.Slider` | `PlayerStamina.OnStaminaChanged` |
| `waveText` | `TMPro.TextMeshProUGUI` or `Text` | `WaveManager.OnWaveChanged` |
| `scoreText` | `TMPro.TextMeshProUGUI` or `Text` | `GameManager.OnScoreChanged` |
| `gameOverPanel` | `GameObject` | Shown via `GameManager.OnGameOver` |
| `finalScoreText` | `Text` | Inside `gameOverPanel` |
| `finalWaveText` | `Text` | Inside `gameOverPanel` |

Subscription happens in `Start()`; unsubscription in `OnDestroy()` to avoid dangling listeners. All updates are event-driven (no polling) per research §11.

---

## Entity 7: Run Score

**Implemented in**: `GameManager.cs`.

| Field | Type | Notes |
|-------|------|-------|
| `Score` | `int` (property) | Public read-only |
| `IsGameOver` | `bool` (property) | |
| `CurrentWave` | `int` (property) | Mirrors `WaveManager.currentWave` |

**Events**:
- `event System.Action<int> OnScoreChanged` — fires when score updates.
- `event System.Action OnGameOver` — fires when player dies.

**Score formula**: `Score += currentWave * 100` per enemy killed. Subscribed to `EnemyHealth.OnDeath` for each spawned enemy via `WaveManager`.

---

## Cross-cutting: Perception System

Not a spec entity, but a shared runtime data structure consumed by every enemy:

### `PerceptionSystem.cs`
| Field | Type | Notes |
|-------|------|-------|
| `CurrentState` | `PerceptionState` (property) | |
| `HasTarget` | `bool` (property) | True when state ≥ `Alert` |
| `LastKnownPosition` | `Vector3` (property) | Updated when target is visible |
| `alertTimeout` | `float` | 5 seconds — Combat → Alert |
| `suspiciousTimeout` | `float` | 8 seconds — Suspicious → Unaware |
| `vision` | `VisionSystem` | `[RequireComponent]` |
| `hearing` | `HearingSystem` | `[RequireComponent]` |

```csharp
public enum PerceptionState { Unaware, Suspicious, Alert, Combat }
```

`EscalateTo(state)` raises CurrentState if new state is higher; never lowers. De-escalation happens via timeouts in `Update()`.

**Events**:
- `event System.Action<PerceptionState, PerceptionState> OnStateChanged` — `(old, new)`.

---

## Cross-cutting: NoiseEmitter event bus

### `NoiseEmitter.cs` (static global event)
```csharp
public enum NoiseType { Footstep, Attack, LandImpact }

public static event System.Action<Vector3, float, NoiseType, GameObject>
    OnNoiseEmitted;

public static void EmitNoise(Vector3 worldPos, float radius,
                             NoiseType type, GameObject source)
    => OnNoiseEmitted?.Invoke(worldPos, radius, type, source);
```

No instance state. Listeners (`HearingSystem`) subscribe in `OnEnable`, unsubscribe in `OnDisable`.

---

## Lifecycle summary (per gameplay run)

```
Scene loads
  └─ GameManager.Awake          (singleton init, score = 0)
  └─ WaveManager.Start          (coroutine: wait 2s, then StartWave(1))
  └─ Player.Awake               (PlayerHealth/Stamina init at max)
  └─ HUDController.Start        (subscribe to all change events)

Wave 1 begins
  └─ WaveManager.StartWave(1)   (instantiate enemies one per spawnInterval)
       └─ each Enemy.Awake      (PerceptionSystem initial state = Unaware)

Player attacks
  └─ PlayerCombat.OnAttack      (Stamina.Spend; OverlapSphere; NoiseEmitter.EmitNoise)
  └─ EnemyHealth.TakeDamage     (HP decrease; if ≤0 → OnDeath)

Enemy dies
  └─ EnemyHealth.OnDeath fires
       └─ GameManager: Score += currentWave * 100; OnScoreChanged
       └─ WaveManager: aliveEnemies.Remove; if empty → OnWaveCleared
       └─ EnemyAI/KnightBT: agent.enabled = false; Destroy(go, 3f)

Wave clear
  └─ WaveManager begins restDuration coroutine
  └─ After rest: StartWave(++currentWave); OnWaveChanged fires

Player dies
  └─ PlayerHealth.OnDeath fires
       └─ GameManager.IsGameOver = true; OnGameOver fires
       └─ HUDController: gameOverPanel.SetActive(true); show finals
       └─ All enemies: AI Update halts via IsGameOver check
```
