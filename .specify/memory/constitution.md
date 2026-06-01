<!--
SYNC IMPACT REPORT
==================
Version change: template (unversioned) → 1.0.0
Bump type: MINOR — first concrete version; all principles newly defined.

Modified principles:
  [PRINCIPLE_1_NAME] → I. Continuous Compilation
  [PRINCIPLE_2_NAME] → II. Additive Stability
  [PRINCIPLE_3_NAME] → III. Cross-Platform Compatibility
  [PRINCIPLE_4_NAME] → IV. Product-First Development
  [PRINCIPLE_5_NAME] → (removed — four principles sufficient for this project)

Added sections:
  Core Principles (4 principles)
  Unity Workflow
  Development Process
  Governance

Removed sections:
  [SECTION_2_NAME] / [SECTION_3_NAME] placeholders replaced with concrete sections

Templates requiring updates:
  ✅ .specify/templates/plan-template.md — Constitution Check gate uses dynamic ref; no edits needed
  ✅ .specify/templates/spec-template.md — generic structure; compatible with all four principles
  ✅ .specify/templates/tasks-template.md — task phases compatible; no Unity-specific changes required

Deferred TODOs:
  RATIFICATION_DATE set to 2026-06-01 (today; first creation).
-->

# EldenNecklace Constitution

## Core Principles

### I. Continuous Compilation

The project MUST compile without errors at all times. No commit, branch, or intermediate
state may leave the Unity project in a state that fails to compile. Compilation failures
block all other work and MUST be resolved before any new feature work resumes.

**Non-negotiable rules:**
- Every C# script added or modified MUST compile successfully before the work is considered done.
- Missing references, broken assembly definitions, or unresolved namespaces MUST be fixed immediately.
- After any `refresh` or asset import, zero compiler errors is the acceptance gate.

### II. Additive Stability

Each new feature or change MUST leave previously working functionality intact. New additions
are built on top of — never instead of — existing working game systems.

**Non-negotiable rules:**
- A feature is only "done" when the existing build still runs correctly after the addition.
- Changes that alter shared systems (input, camera, player state) MUST be verified against
  all already-implemented features before being committed.
- Regressions introduced by a change MUST be fixed as part of that same change — they MUST
  NOT be deferred.

### III. Cross-Platform Compatibility

The game MUST run on both macOS and Windows. Platform-specific code paths are permitted
only when both platforms are explicitly handled.

**Non-negotiable rules:**
- File paths MUST use `Path.Combine` or Unity's asset path APIs — no hardcoded separators.
- No Windows-only or macOS-only Unity packages or native plugins unless a cross-platform
  alternative is unavailable and both platforms are tested.
- Build targets for `StandaloneOSX` and `StandaloneWindows64` MUST remain configured and
  buildable throughout the project.

### IV. Product-First Development

The goal is a finished, playable game. Code elegance, architecture purity, and refactoring
are secondary to delivering working game features. Technical debt is acceptable as long as
Principles I and II are satisfied.

**Non-negotiable rules:**
- Working over perfect: a solution that ships and runs is better than an elegant one that
  doesn't exist yet.
- Do not spend time on code quality improvements unless a specific defect is being fixed.
- YAGNI (You Aren't Gonna Need It): implement exactly what the current feature requires —
  no speculative abstractions.

## Unity Workflow

All feature work MUST follow this Unity-aware sequence to prevent broken states:

1. Write or modify C# scripts in the appropriate `Assets/` subdirectory.
2. Run `refresh` (via `unity-cmd.ps1`) to trigger compilation and surface errors immediately.
3. Resolve all compiler errors before proceeding.
4. Use `scene`, `inspect`, `create`, `add-component`, and `set` commands to wire up scene
   objects and components.
5. Run `save-scene` before switching scenes or ending a work session.
6. Verify the existing scene(s) still function after any shared-system change (Principle II).

**Batch operations**: Group independent `unity-cmd.ps1` calls into JSON arrays to reduce
round-trips. Use `scratch` for complex multi-step scene setup.

## Development Process

- **Feature scope**: Each feature targets one user story at a time, in priority order.
- **Definition of done**: A feature is done when it compiles (Principle I), does not break
  prior features (Principle II), and runs correctly on the target platforms (Principle III).
- **No gold-plating**: Stop implementing when the acceptance criteria are met. Do not add
  polish, animations, or "nice to have" behaviour beyond the agreed scope.
- **Incremental commits**: Commit after each task or logical unit of work so that regressions
  can be bisected quickly.

## Governance

This constitution supersedes all other development practices and style guides for the
EldenNecklace project. Amendments require:

1. A clear description of the change and the rationale.
2. A version bump following semantic versioning (MAJOR / MINOR / PATCH as defined below).
3. Update of `LAST_AMENDED_DATE` to the amendment date.
4. A re-run of `/speckit-constitution` to propagate changes to dependent templates.

**Versioning policy:**
- MAJOR: A principle is removed, redefined incompatibly, or a hard constraint is relaxed.
- MINOR: A new principle or section is added, or existing guidance is materially expanded.
- PATCH: Clarifications, wording fixes, or non-semantic refinements.

**Compliance**: Every implementation plan (plan.md) MUST include a Constitution Check gate
that verifies all four principles before Phase 0 research begins and again after Phase 1
design. Violations MUST be documented in the Complexity Tracking table with justification.

**Version**: 1.0.0 | **Ratified**: 2026-06-01 | **Last Amended**: 2026-06-01
