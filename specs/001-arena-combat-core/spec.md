# Feature Specification: Elden Necklace — Arena Combat Core

**Feature Branch**: `001-arena-combat-core`

**Created**: 2026-06-01

**Status**: Draft

**Input**: User description: "Build a Unity game called Elden Necklace, which is inspired by the Elden Ring game. The player controls a lone warrior trapped in a cursed arena - a Colosseum like dungeon where entities pour through gates. It should have a stamina-based combat, with no ranged weapons, no projectiles, and no open world. Only melee, movement and survival. The scope is having three types of enemies, required by the curriculum (available at ../../doc): NavMesh pathfinding, vision and hearing sensors, Finite State Machine decision-making, Behaviour Trees, and group tactical cooperation. The game will be third person, with wave-based enemy spawning and GUI HUD for health, stamina, wave and score. The player should be movable with WASD and have an attack bound, with the camera being able to move freely with the mouse."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Survive a Wave of Melee Enemies (Priority: P1)

The player loads the game, spawns in the centre of a sealed colosseum-style arena, and is immediately faced with a small wave of melee enemies pouring through stone gates at the cardinal points. The player uses WASD to move, the mouse to rotate the camera, and the attack input to swing their weapon. They must defeat every enemy in the wave before the next one begins. Health and stamina are visible on a HUD; running out of stamina prevents further attacks until it regenerates; running out of health ends the run.

**Why this priority**: This is the entire minimum viable game. Without it, no other curriculum-mandated AI technique has a meaningful context to be demonstrated in. If only this story shipped, the project would already be a playable arena survival prototype showcasing third-person combat, stamina pressure, wave structure, and the core HUD.

**Independent Test**: Launch the game; verify the warrior spawns in the arena, can move and rotate the camera, can swing the weapon to kill at least one enemy from a wave, observe HP/Stamina/Wave/Score updating on the HUD, and see the run end with a game-over screen when HP reaches zero.

**Acceptance Scenarios**:

1. **Given** the player has just launched the game, **When** the first wave begins, **Then** enemies spawn through one or more arena gates and begin moving toward the player.
2. **Given** the player has full stamina and an enemy is within melee range, **When** the player presses the attack input, **Then** the warrior performs an attack animation, the enemy takes damage, and the stamina bar decreases.
3. **Given** the player's stamina is empty, **When** the player presses the attack input, **Then** no attack is executed and the HUD makes the empty stamina state visible.
4. **Given** the player defeats every enemy in the current wave, **When** the last enemy dies, **Then** the wave counter on the HUD increments, a short rest period begins, and the next wave starts.
5. **Given** the player's health reaches zero, **When** they take a final hit, **Then** the run ends and a game-over screen is displayed showing the final wave reached and final score.

---

### User Story 2 - Face Three Distinct Enemy Archetypes With Different AI (Priority: P1)

As waves escalate, the player encounters three distinct melee enemy archetypes — a fast lightly-armoured grunt, a slow heavily-armoured patrolling guardian, and a boss-class knight that escalates its behaviour when wounded. Each archetype behaves in a visibly different way: the grunt rushes directly, the guardian patrols then engages, and the knight uses more sophisticated decision-making (deciding when to close, when to swing, and when to escalate into a faster phase below a health threshold). All three use the NavMesh to path around pillars and the central platform, and all three perceive the player through a vision cone and through hearing.

**Why this priority**: This is the entire curriculum-mandated AI scope (NavMesh, vision/hearing sensors, FSM, Behaviour Tree). The course evaluation depends on these three archetypes existing and behaving distinguishably. Without this story, the project would fail to satisfy the educational requirements stated by the user.

**Independent Test**: In an isolated test scene or via wave progression, encounter each of the three archetypes one at a time; observe that the grunt rushes the player, the guardian patrols a route before engaging, and the knight visibly changes its attack pattern when reduced below a defined health threshold. Confirm that all three navigate around pillars rather than through them, that they react when the player enters their vision cone, and that they react when the player makes loud actions outside vision but within hearing range.

**Acceptance Scenarios**:

1. **Given** a grunt is alive and the player is in its vision cone, **When** the player is detected, **Then** the grunt transitions to a chase behaviour and pursues the player along a navigation-mesh path that avoids pillars.
2. **Given** a guardian is on patrol and the player makes a loud action (e.g. sprint or attack) within hearing range but outside vision, **When** the noise is emitted, **Then** the guardian moves to investigate the source rather than ignoring it.
3. **Given** a boss knight is engaged and has more than the wound threshold of health, **When** the player damages it, **Then** the knight reacts and continues its base attack pattern; **when** its health drops below the wound threshold, **then** it visibly escalates (faster pursuit, faster swings, or otherwise observably different behaviour).
4. **Given** an enemy has lost sight of the player and a configured time elapses without further detection, **When** the timeout expires, **Then** the enemy de-escalates back toward its base state (patrol or idle) rather than chasing forever.

---

### User Story 3 - Enemies Cooperate as a Group (Priority: P2)

When one enemy spots or is hit by the player, nearby allies become aware of the threat and converge on the player rather than remaining oblivious. Waves are distributed across multiple gates so the player is pressured from multiple directions simultaneously, and enemies do not stack on a single point — they spread out to maintain group cohesion without overlapping. This curriculum-mandated group tactical cooperation makes encounters feel coordinated rather than as if each enemy is acting in isolation.

**Why this priority**: Group cooperation is an explicit curriculum requirement called out by the user. It also dramatically changes the feel of the game (single-target tunnel vision becomes survival under multi-directional pressure). Importantly, it depends on Stories 1 and 2 being functional, which is why it is P2 rather than P1.

**Independent Test**: Spawn at least three enemies of the same archetype within visual range of each other, alert one of them by approaching it directly, and observe that the others also transition out of their idle/patrol state and begin moving toward the player within a few seconds — without the player having to walk into each one individually. In a separate test, spawn a wave with more than four enemies and verify enemies emerge from at least two distinct gates rather than all from a single gate.

**Acceptance Scenarios**:

1. **Given** three enemies are within an alert radius of each other and one detects the player, **When** the detecting enemy transitions to combat, **Then** the other two transition out of idle/patrol within a configured radius and start pursuing the player.
2. **Given** a wave contains enough enemies to spread across multiple gates, **When** the wave begins, **Then** enemies are distributed across at least two of the four gates rather than all spawning from one.
3. **Given** multiple enemies are pursuing the player simultaneously, **When** they reach attack range, **Then** they do not all stack on the same point — they maintain spacing so that the player can visibly identify multiple distinct threats.

---

### User Story 4 - Track Survival Progress on the HUD (Priority: P2)

While playing, the player can read their current health, current stamina, current wave number, and current score on a heads-up display that does not obscure the combat area. These four readouts update in real time as the player takes damage, attacks, advances waves, and kills enemies. This gives the player the information they need to make survival decisions (do I attack now or wait for stamina to regenerate, am I safe to push or should I retreat) without leaving the gameplay view.

**Why this priority**: The HUD is explicitly called out by the user as required, but the gameplay loop in Story 1 is what makes the HUD meaningful. The HUD itself is a thin presentation layer over state that Story 1 already maintains.

**Independent Test**: Begin a run and verify that all four HUD elements (health, stamina, wave, score) are visible from the moment gameplay starts. Take damage and verify the health readout drops. Attack and verify the stamina readout drops then regenerates. Clear a wave and verify the wave readout increments. Kill enemies and verify the score readout increases.

**Acceptance Scenarios**:

1. **Given** gameplay is active, **When** the player looks at the screen, **Then** health, stamina, wave number, and score are all visible without obscuring the combat area.
2. **Given** the player takes damage, **When** the hit lands, **Then** the on-screen health readout immediately decreases to the new value.
3. **Given** the player performs an attack, **When** the attack completes, **Then** the on-screen stamina readout immediately decreases and begins regenerating after a short delay.
4. **Given** the player clears the final enemy of a wave, **When** the wave transition begins, **Then** the on-screen wave readout increments to the next wave number.
5. **Given** the player kills an enemy, **When** the kill is registered, **Then** the on-screen score readout increases.

---

### Edge Cases

- **Spawn safety**: An enemy that would spawn directly inside the player, a wall, or another enemy must be repositioned to a valid navigable point so it never gets stuck off the navigation mesh.
- **Player dies mid-wave**: If the player's health reaches zero while enemies are still pursuing, all enemies must stop attacking, all damage processing must halt, and the game-over screen must display correctly (no further HP underflow, no posthumous wave advancement).
- **Stamina exhaustion during attack chain**: If the player attempts to attack when stamina is below the per-attack cost, the input must be rejected cleanly — no half-played animation, no damage applied, no negative stamina.
- **Enemies lose sight of the player mid-chase**: An enemy that was actively chasing must eventually de-escalate back to its base state (rather than chasing indefinitely) once a configured timeout elapses without re-detection.
- **Camera collides with arena geometry**: When the player backs into a wall or pillar, the camera must avoid clipping through geometry — it must shorten its arm so the player remains visible.
- **Pause during combat**: Pausing must stop enemy AI, animation, and damage processing; resuming must restore them without enemies snapping or losing track of the player.
- **All four gates spawning simultaneously**: When a high-wave spawn distributes enemies across all four gates, the framerate and AI must remain stable; no enemy may be lost off-mesh due to crowding at a gate.

## Requirements *(mandatory)*

### Functional Requirements

**Arena & Camera**

- **FR-001**: The game MUST present a single enclosed arena (approximately 20–24 m square) with four enemy spawn gates at the cardinal directions, internal pillar obstacles for cover, and a central raised area.
- **FR-002**: The game MUST use a third-person camera that follows the player from behind and rotates freely in response to mouse input on both axes (yaw and pitch).
- **FR-003**: The camera MUST avoid clipping through arena geometry by automatically shortening its arm when a wall or pillar would occlude the player.

**Player Controls**

- **FR-004**: The player MUST be able to move the warrior using W (forward), A (strafe left), S (backward), and D (strafe right), with motion direction interpreted relative to the camera's current facing.
- **FR-005**: The player MUST have a melee attack action bound to a dedicated input that triggers an attack animation and applies damage to enemies hit within melee range during the attack's active frames.
- **FR-006**: The player MUST be able to rotate the camera continuously with the mouse during gameplay, independently of movement.

**Stamina-Based Combat**

- **FR-007**: The player MUST have a finite stamina pool that depletes when performing the attack action.
- **FR-008**: The game MUST refuse the attack input when current stamina is below the attack's stamina cost, with no animation played and no damage applied.
- **FR-009**: Stamina MUST regenerate over time after a short delay following the last stamina expenditure.
- **FR-010**: Combat MUST be melee-only — the game MUST NOT include ranged weapons, projectiles, magic bolts, throwable items, or any other non-melee damage source.

**Health & Death**

- **FR-011**: The player MUST have a finite health pool that decreases when struck by enemy attacks within enemy melee range during enemy attack active frames.
- **FR-012**: When player health reaches zero, the game MUST transition to a game-over state that halts enemy AI and displays the final wave reached and final score.

**Wave System**

- **FR-013**: The game MUST spawn enemies in discrete waves. A wave MUST be considered cleared when all enemies belonging to that wave are defeated.
- **FR-014**: Wave difficulty MUST escalate with successive waves: later waves MUST contain more enemies, and from a configurable wave onward MUST introduce tougher archetypes.
- **FR-015**: After a wave is cleared, the game MUST grant a short rest period before automatically starting the next wave.

**Three Enemy Archetypes (Curriculum)**

- **FR-016**: The game MUST include three distinct melee enemy archetypes, with no enemy possessing a ranged attack of any kind.
- **FR-017**: Archetype 1 (fast grunt) MUST use a Finite State Machine for decision-making, transitioning between idle/patrol, alert, chase, attack, and death states.
- **FR-018**: Archetype 2 (heavy patrolling guardian) MUST use a Finite State Machine that includes a patrol behaviour distinct from the grunt's behaviour (e.g. a waypoint route or sentry sweep) before engaging.
- **FR-019**: Archetype 3 (boss knight) MUST use a Behaviour Tree for decision-making, and MUST observably escalate its behaviour (faster attacks, faster pursuit, or otherwise distinguishable) when its health falls below a configurable threshold.

**AI Navigation (Curriculum)**

- **FR-020**: All enemies MUST navigate the arena using a baked navigation mesh; pillars and the central raised area MUST be excluded from the walkable surface so enemies path around them.
- **FR-021**: When an enemy dies, it MUST be removed from the navigation system so it no longer consumes navigation queries.

**AI Perception (Curriculum)**

- **FR-022**: Each enemy MUST carry a vision sensor with a forward-facing cone (limited angle and range) that detects the player only when the player is inside the cone and not occluded by a wall or pillar.
- **FR-023**: Each enemy MUST carry a hearing sensor that reacts to loud player actions (e.g. sprinting, attacking) emitted within a configured radius, even when the player is outside the vision cone.
- **FR-024**: An enemy that detects the player visually MUST transition into an aggressive state and pursue the player.
- **FR-025**: An enemy that hears a noise without seeing the player MUST transition into an investigative state and move toward the noise source.
- **FR-026**: An enemy that loses both vision and hearing contact MUST de-escalate to its base state after a configurable timeout.

**Group Tactical Cooperation (Curriculum)**

- **FR-027**: When one enemy detects the player and enters its combat state, nearby allied enemies within a configurable alert radius MUST also escalate to a pursuing state without each having to detect the player independently.
- **FR-028**: A wave's enemies MUST be distributed across at least two of the four spawn gates whenever the wave contains enough enemies to do so, so the player faces multi-directional pressure.
- **FR-029**: Enemies pursuing the player MUST maintain visible spacing rather than all stacking on a single position, so the player can distinguish multiple distinct threats.

**HUD**

- **FR-030**: The game MUST display the player's current health on a screen-space HUD, updated immediately when health changes.
- **FR-031**: The game MUST display the player's current stamina on the HUD, updated immediately when stamina changes.
- **FR-032**: The game MUST display the current wave number on the HUD, updated when waves transition.
- **FR-033**: The game MUST display the current score on the HUD, updated when an enemy is killed.
- **FR-034**: HUD elements MUST be positioned so they do not obscure the central combat area of the arena view.

**Cross-Cutting**

- **FR-035**: The game MUST run on both macOS and Windows desktop builds.
- **FR-036**: The Unity project MUST compile without errors after every change, and each added feature MUST leave previously implemented features functional.

### Key Entities *(include if feature involves data)*

- **Player Warrior**: The single controllable character. Holds current health, current stamina, a position in the arena, a facing direction, and a current action state (idle, moving, attacking, dead).
- **Enemy Agent**: A single enemy instance belonging to one of three archetypes. Holds current health, an AI state (idle/patrol/alert/chase/attack/dead), perception state (unaware/suspicious/alert/combat), and a navigation-mesh agent reference.
- **Enemy Archetype**: A definition of one of the three enemy types — its base stats (health, damage, move speed, attack range), its decision-making model (FSM or Behaviour Tree), its perception parameters (vision cone angle/range, hearing radius), and the wave at which it first appears.
- **Wave**: A configured set of enemies to spawn together. Holds wave number, the list of archetype-count pairs to spawn, and the rest duration before the next wave.
- **Spawn Gate**: One of the four cardinal entry points around the arena boundary. Holds a world position and is the entry point for an enemy assigned to it during round-robin spawn distribution.
- **HUD Readouts**: The four on-screen values exposed to the player — current health, current stamina, current wave number, current score. Updated reactively when the underlying entity values change.
- **Run Score**: The cumulative score for the current run. Increases when enemies are killed; reset when a new run begins.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new player can launch the game, control the warrior with WASD and the mouse, kill at least one enemy, and observe the HUD updating in response — all within the first 60 seconds, with no in-game tutorial or external instruction.
- **SC-002**: A skilled player can survive at least 5 consecutive waves on default difficulty without the game crashing, hanging, or producing visibly broken AI behaviour (enemies stuck off-mesh, enemies idling while the player attacks them, enemies never reaching the player).
- **SC-003**: When at least three enemies of the same archetype are present and one detects the player, the other two enter a pursuit state within 3 seconds of the first detection.
- **SC-004**: A wave containing 4 or more enemies distributes those enemies across at least 2 of the 4 spawn gates 100% of the time.
- **SC-005**: An enemy that loses both vision and hearing contact with the player returns to its base state (patrol or idle) within a configurable timeout — the timeout is honoured 100% of the time.
- **SC-006**: The boss knight observably changes behaviour (visible to the player without requiring debug overlay) when its health crosses the wound threshold, 100% of the time.
- **SC-007**: Across an extended playtest (a complete run from wave 1 to player death or wave 10, whichever comes first), the game produces zero compile errors and zero runtime exceptions visible in the console.
- **SC-008**: A successful standalone build is produced for both macOS and Windows from the same Unity project, and each build launches and runs the gameplay loop end-to-end on its respective platform.
- **SC-009**: HUD readouts (health, stamina, wave, score) reflect their underlying state with no visible lag — any change is observable on screen within the same frame the underlying value changes.

## Assumptions

- The user wants the smallest viable attack scheme — one bound attack input — rather than the full light/heavy/dodge/block combat described in the supplementary curriculum documents. Richer combat (heavy attacks, dodge roll, block) is out of scope for this feature and can be added later as a separate spec without breaking this one.
- The arena layout follows the dungeon colosseum described in the curriculum documents (octagonal stone walls, four cardinal gates, eight pillars, central raised platform), but exact dimensions and prop placement are an implementation detail.
- The three enemy archetypes are interpreted as three melee archetypes — fast grunt (FSM), heavy patrolling guardian (FSM with patrol), boss knight (Behaviour Tree with health-threshold escalation) — matching the original Stage I curriculum design. The Stage III evolution toward a ranged "Shadow Archer" is intentionally not adopted, because the user explicitly forbade ranged weapons and projectiles.
- Power-up drops, weapon-swap pickups, the main menu, the settings/pause menu, and audio polish from the curriculum documents are out of scope for this core feature spec. The HUD is intentionally minimal — just the four readouts the user named.
- Default desktop input (keyboard + mouse) is the only input modality. Gamepad support is out of scope for this feature.
- Visual assets (3D character models, animations, dungeon environment props) will be sourced from Unity Asset Store packs and Mixamo, consistent with the curriculum documents; no custom 3D modelling is required.
- The user's stated priority "code quality is not important, the focus is the finished product" is interpreted as licence to ship a working game over an elegant codebase, in line with the project constitution principle IV (Product-First Development).
- The user's constraint "the game should compile at all times, and each new addition should not break the ones before it" is interpreted as a process requirement that governs how this feature is implemented (incremental, additive commits) — it does not change the spec itself, since the project constitution already enforces it (principles I and II).
