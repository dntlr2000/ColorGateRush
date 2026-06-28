# Automation Workflow

## Goal

Make a Unity hyper-casual game that Codex can build from an empty 3D project with minimal human intervention.

## Phase 0 — Repository preparation

- Copy this scaffold into the Unity project root.
- Keep `AGENTS.md`, `.codex`, `prompts`, and `docs` at the repository root.
- Keep all Unity runtime/editor scripts under `Assets/_Project`.

## Phase 1 — Parent planning

Use `prompts/00_master_build_prompt.md`. The parent agent should:

- read `AGENTS.md` and `GAME_DESIGN.md`;
- identify existing Unity project state;
- spawn specialized subagents only for bounded tasks;
- merge and validate changes.

## Phase 2 — Parallel subagent work

Suggested split:

- `unity_architect`: bootstrap scene, folders, validation.
- `gameplay_engineer`: movement, game state, scoring, triggers.
- `procedural_asset_engineer`: generated materials, primitive helpers, VFX, SFX.

Avoid uncoordinated edits to the same file. If conflicts are likely, ask subagents for plans first, then let the parent apply integrated changes.

## Phase 3 — Integration

The parent agent should:

- ensure namespaces match;
- ensure editor scripts are under an `Editor` folder;
- ensure runtime scripts do not use `UnityEditor`;
- ensure generated scene references are created by bootstrap or runtime discovery;
- run compile/build validation when Unity is available.

## Phase 4 — QA review

Spawn `qa_build_reviewer` read-only after implementation. Ask for concrete blockers, not broad opinions.

## Phase 5 — Final report

The final Codex response should include:

- changed files;
- implemented game behaviors;
- validation commands or manual checks run;
- Unity version or inability to verify;
- remaining risks.

## Optional automation improvements

- Add PlayMode tests for scoring and lane movement.
- Add Editor tests for level generation invariants.
- Add a `BuildAndroid` editor method if Android SDK is installed.
- Add generated app icon textures through `Texture2D` if publishing is needed.
