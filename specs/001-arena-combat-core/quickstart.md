# Quickstart: Elden Necklace — Arena Combat Core

**Branch**: `001-arena-combat-core` | **Date**: 2026-06-01

This is the developer-side runbook for working on this feature. It assumes you are on macOS (development host) with the project already cloned and the Unity Hub installed.

---

## 1. Open the project

1. Open Unity Hub.
2. Add `~/Desktop/master/An1/Sem2/Sisteme_Interactive(SI)/project/game/EldenNecklace` as a project.
3. Open it with **Unity 6 LTS 6000.4.0f1** (matches `ProjectSettings/ProjectVersion.txt`).
4. Wait for the package import to settle. The project's `Packages/manifest.json` already declares:
   - `com.unity.ai.navigation` 2.0.11 — NavMesh
   - `com.unity.inputsystem` 1.19.0 — Input System
   - `com.unity.ugui` 2.0.0 — uGUI canvas
   - `com.unity.render-pipelines.universal` 17.4.0 — URP
   - `com.cziberpv.unity-bridge` — Bridge automation

If the Bridge package does not appear, re-import via `Window → Package Manager → +` → "Add package from git URL" → `https://github.com/cziberpv/unity-bridge.git`.

---

## 2. Activate the Unity Bridge

The Bridge listens to file events at `Assets/LLM/Bridge/request.json`. To enable:

1. In Unity, open `Window → Unity Bridge` (the exact menu path is defined by the package).
2. Confirm the Bridge window shows "Listening" or equivalent.
3. Leave Unity running in the background while you work.

You can verify the Bridge is alive from the terminal:

```sh
cd ~/Desktop/master/An1/Sem2/Sisteme_Interactive\(SI\)/project/game/EldenNecklace
powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "scene"}'
```

You should see a JSON dump of the current scene hierarchy.

---

## 3. Implementation workflow (per Constitution)

For each new script or feature, follow this exact sequence (Constitution's Unity Workflow):

1. **Write the C# script** under `Assets/Scripts/<area>/`.
2. **Refresh** to compile:
   ```sh
   powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "refresh"}' -Timeout 120
   ```
3. **Resolve all compile errors** before doing anything else (Constitution I).
4. **Wire scene objects** via `unity-cmd.ps1` `add-component` / `set` commands per `contracts/unity-bridge-commands.md`.
5. **Inspect** to verify:
   ```sh
   powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type":"inspect","path":"Player","lens":"scripts"}'
   ```
6. **Save the scene** before any scene switch or session end:
   ```sh
   powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type":"save-scene"}'
   ```

---

## 4. Build for both platforms

Per Constitution III, both `StandaloneOSX` and `StandaloneWindows64` MUST remain buildable. Build from the Unity Editor:

**macOS build**:
1. `File → Build Profiles`.
2. Select `macOS` → make sure `Apple Silicon` is the architecture.
3. `Build` → choose `Builds/macOS/` as output.

**Windows build**:
1. `File → Build Profiles`.
2. Select `Windows` → architecture `x86_64`.
3. `Build` → choose `Builds/Windows/`.
4. Test the resulting `.exe` on a Windows machine before shipping (running it under Wine is acceptable for a sanity check, but not for evaluation).

Build both targets at least once before any milestone (end of each user story implementation) per Constitution III.

---

## 5. Run the gameplay loop

1. In the Editor, open `Assets/Scenes/Arena.unity`.
2. Press Play.
3. Default controls (per `contracts/input-actions.md`):
   - **WASD** — move (camera-relative)
   - **Mouse** — rotate camera (yaw + pitch)
   - **Left mouse button** — attack
   - **Escape** — pause

Verify the HUD shows health, stamina, wave, and score in the four corners.

---

## 6. Verify the spec's acceptance scenarios

After implementation, run through the spec's success criteria manually:

| Spec SC | How to verify |
|---------|---------------|
| SC-001 | Hand the build to someone unfamiliar with the project; they should be able to play within 60 s. |
| SC-002 | Play through 5 waves without crash or AI lockup. |
| SC-003 | Stop, line up 3 grunts in a row, alert the front one — back two should chase within 3 seconds. |
| SC-004 | Reach wave 4+; count gates used during spawn. Should be ≥ 2. |
| SC-005 | Sneak away from a chasing grunt behind a pillar; it should return to patrol within ~5 s. |
| SC-006 | Damage the knight below the berserk threshold; speed and swing rate should noticeably change. |
| SC-007 | Watch the Editor Console — should be clear of red/yellow during a full run. |
| SC-008 | Run both the macOS and Windows builds end-to-end. |
| SC-009 | Take damage; the HUD slider should drop the same frame. |

---

## 7. Common pitfalls

- **Enemies stuck at gate spawn**: the spawn point sits just inside the arena wall — verify it's on the baked NavMesh by selecting the gate prefab and checking the NavMesh overlay in the Scene view (`AI → NavMesh` window).
- **Animator parameter missing errors at runtime**: every `Animator.StringToHash` call must match a parameter declared in the Animator Controller's Parameters panel. If a hash returns a "Parameter does not exist" warning, add the parameter in the Animator.
- **Bridge times out**: usually means Unity is paused or the Bridge window is closed. Bring Unity to focus, confirm the Bridge window status, then retry.
- **macOS PowerShell missing**: install via `brew install --cask powershell`. The `powershell` command will resolve to `pwsh`.

---

## 8. Where to look when something breaks

| Symptom | Look at |
|---------|---------|
| Enemy not pathing | `EnemyAI.UpdateChase` → `agent.SetDestination` call; check `agent.isOnNavMesh` |
| HUD not updating | `HUDController.Start` subscription — confirm the right delegate is hooked |
| Attack doing no damage | `PlayerCombat.OnAttack` → `Physics.OverlapSphere` radius/layer mask |
| Game won't compile | Run the Bridge `refresh` command from terminal; read the error list |
| Player falls through floor | `CharacterController` collider radius; `NavMesh` bake covers the spawn point |
| Camera clips through wall | `ThirdPersonCamera.LateUpdate` → `SphereCast` collision layer mask |
