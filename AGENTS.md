# AGENTS.md — Color Gate Rush

## Mission

Build and maintain **Color Gate Rush**, a fully procedural Unity hyper-casual runner that can be created from an empty Unity 3D project with no manual art/audio imports.

## Repository layout

- `Assets/_Project/Scripts/Runtime/`: runtime gameplay, procedural generation, UI, audio, camera.
- `Assets/_Project/Scripts/Editor/`: editor bootstrap and validation utilities.
- `Assets/_Project/Scenes/`: generated Unity scenes.
- `docs/`: design, automation, asset generation, QA procedures.
- `prompts/`: prompts for Codex parent agent and specialized subagents.
- `.codex/agents/`: project-scoped Codex custom subagents.

## Non-negotiable constraints

- Do not add Asset Store assets, copyrighted media, downloaded models, downloaded textures, or downloaded audio.
- Prefer Unity primitives, procedural materials, built-in ParticleSystem, and runtime-generated AudioClip effects.
- Keep the MVP one-scene and mobile portrait friendly.
- Keep scripts deterministic where practical. Level generation must expose a seed.
- Do not introduce package dependencies unless absolutely necessary. If necessary, explain why and ask before adding.
- Do not place project code outside `Assets/_Project` unless Unity requires it.
- Keep public methods small and obvious. Prefer clear names over clever abstractions.

## Unity implementation assumptions

- Target Unity: Unity 6 / 6000.x 3D project. This repository is pinned to 6000.0.69f1 and does not target Unity 2022.3 package compatibility.
- Render pipeline: Built-in or URP-compatible code. Use shader fallback logic when creating materials.
- Input: keyboard for editor, swipe/touch for mobile.
- UI: use built-in uGUI unless the project already uses TextMeshPro.

## Core files to preserve

- `Assets/_Project/Scripts/Runtime/GameManager.cs`
- `Assets/_Project/Scripts/Runtime/LaneRunnerController.cs`
- `Assets/_Project/Scripts/Runtime/LevelGenerator.cs`
- `Assets/_Project/Scripts/Runtime/ProceduralFactory.cs`
- `Assets/_Project/Scripts/Runtime/RuntimeUi.cs`
- `Assets/_Project/Scripts/Runtime/ProceduralAudio.cs`
- `Assets/_Project/Scripts/Editor/BootstrapColorGateRush.cs`
- `Assets/_Project/Scripts/Editor/BuildValidator.cs`

## Build and validation

When Unity is available, validate in this order:

1. Let Unity compile scripts and check Console errors.
2. Run `Tools/Color Gate Rush/Bootstrap Project`.
3. Open `Assets/_Project/Scenes/Main.unity`.
4. Enter Play Mode and confirm:
   - player auto-runs;
   - left/right input changes lanes;
   - same-color shards score;
   - wrong-color shards penalize;
   - gates change color;
   - obstacles fail or penalize;
   - finish triggers result and restart.
5. Run `Tools/Color Gate Rush/Validate Project`.
6. Report exact files changed and verification performed.

If Unity CLI is configured, prefer a batchmode compile/validation command adapted to the local Unity path, for example:

```bash
Unity -batchmode -quit -projectPath "$PWD" -executeMethod ColorGateRush.EditorTools.BuildValidator.ValidateFromCommandLine
```

## Subagent workflow

For broad work, spawn specialized subagents only when the parent prompt explicitly asks for them. Use parallel subagents for analysis/planning and isolated file work, then merge through the parent agent.

Recommended roles:

- `unity_architect`: project layout, bootstrap scene, editor validation.
- `gameplay_engineer`: runner movement, collision, scoring, game states.
- `procedural_asset_engineer`: materials, primitive prefabs, particles, runtime audio.
- `qa_build_reviewer`: compile risks, acceptance test checklist, edge cases.

## Definition of done

A task is done only when:

- C# compile errors are addressed or clearly reported if Unity could not be run.
- The requested behavior is implemented in the smallest coherent change.
- No external art/audio assets were introduced.
- The final response includes changed files, validation results, and remaining risks.
