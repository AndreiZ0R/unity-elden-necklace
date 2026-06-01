## Unity Editor Operations

Use `unity-cmd.ps1` to interact with Unity Editor:

### Basic commands
powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "scene"}'
powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "inspect", "path": "Player", "lens": "scripts"}'
powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "refresh"}' -Timeout 120

### Workflow
1. `scene` to understand current state
2. `create` / `add-component` / `set` to build
3. `inspect` with lenses to verify
4. `save-scene` to persist
5. Write C# scripts, then `refresh` to compile and check errors

### Tips
- Use batch commands (JSON array) to group independent operations
- Use `scratch` for complex multi-step setup instead of many JSON commands
- Always `save-scene` before `new-scene` or `open-scene`
- For serialized Unity properties, use `m_` prefix: `m_LocalPosition`, `m_SizeDelta`
- Component names are short: `Image`, not `UnityEngine.UI.Image`

<!-- SPECKIT START -->
**Active feature**: `001-arena-combat-core` (branch: `001-arena-combat-core`).

Read these before doing implementation work on this feature:
- Constitution: `.specify/memory/constitution.md`
- Spec: `specs/001-arena-combat-core/spec.md`
- Plan: `specs/001-arena-combat-core/plan.md`
- Research decisions: `specs/001-arena-combat-core/research.md`
- Data model: `specs/001-arena-combat-core/data-model.md`
- Quickstart: `specs/001-arena-combat-core/quickstart.md`
- Bridge command contract: `specs/001-arena-combat-core/contracts/unity-bridge-commands.md`
- Input action contract: `specs/001-arena-combat-core/contracts/input-actions.md`

**Tech stack at a glance**: Unity 6 LTS 6000.4.0f1, C#, AI Navigation 2.0.11, Input System 1.19.0, URP 17.4.0, uGUI 2.0.0, `com.cziberpv.unity-bridge`. Mixamo for character animations, Unity Asset Store free dungeon pack for environment.
<!-- SPECKIT END -->
