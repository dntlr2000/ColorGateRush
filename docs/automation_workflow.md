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

## Stage progression QA additions

- The project targets Unity 6 / 6000.x.
- Validate at least 10 `StageConfig` entries.
- Validate Stage 1 is unlocked by default.
- Validate only 3-star clears unlock the next stage.
- Validate Main Menu Start opens Stage Select and does not directly start gameplay.
- Validate Main Menu Settings opens Sound, Camera Shake, Color Assist, and Reset Progress controls.
- Validate gameplay starts only from an unlocked Stage Select button.
- Validate Stage 1 first entry shows the tutorial until the player presses OK.
- Validate Playing can enter Paused through the HUD Pause button or ESC/P.
- Validate Pause Menu supports Resume, Retry, Stage Select, and Main Menu.
- Validate Pause does not save stars or unlock stages.
- Validate Time.timeScale is restored when resuming, retrying, or leaving pause.
- Generate every stage in an isolated temporary scene during validator smoke tests.
- Confirm shards and obstacles in the same row share the exact same z coordinate.
- Confirm matching shards are not forced on every row.
- Confirm Stage 1-2 remain shard-rich and obstacle-light.
- Confirm Stage 1 shard row ratio, obstacle row ratio, average shards per row, and matching shard row ratio are reported by validator.
- Confirm every row has at least one safe option after mandatory gate color changes.
- Confirm no row is all off-color shards, all obstacles, or a mixed all-unsafe off-color/obstacle row.
- Confirm early unsafe-row repair does not only create empty lanes and can convert danger into collectible shards.
- Confirm result and pause screens do not auto-restart/auto-resume and only explicit buttons or keyboard shortcuts change state.
- Confirm PlayerPrefs keys use the `CGR_` prefix.
- Confirm Sound Off prevents procedural SFX playback.
- Confirm Camera Shake Off prevents hit/finish shake.
- Confirm Color Assist uses symbol and high-contrast support without external sprites.

## Validator menu

- `Tools/Color Gate Rush/Validate Project`: full static and generation smoke validation.
- `Tools/Color Gate Rush/Validate Build`: release-oriented alias for the same validation.
- `Tools/Color Gate Rush/Generate Balance Report`: generate Stage 1-10 balance summaries in an isolated scene.
- `Tools/Color Gate Rush/Reset Local Progress`: delete only Color Gate Rush progress keys.
