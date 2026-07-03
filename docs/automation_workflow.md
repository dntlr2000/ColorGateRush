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
- ensure `Assets/_Project` contains no imported texture, audio, model, font, or prefab assets;
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
- Validate at least 30 `StageConfig` entries.
- Validate Stage 1 is unlocked by default.
- Validate any clear with at least 1 star unlocks the next stage.
- Validate failures with 0 stars do not unlock the next stage.
- Validate legacy "3-star required to unlock" player-facing text is absent.
- Validate 3-star targets are possible but strict: `threeStarScore <= estimatedMaxAchievableScore`.
- Validate Stage 1-3 three-star ratios stay near-perfect and Stage 4+ ratios do not drift below the stricter threshold.
- Validate `StageScoreAnalyzer` uses row/lane report data instead of raw total matching-shard count.
- Validate a row with multiple matching shards contributes at most one collectible to route-aware max score.
- Validate balance report shows naive max, route-aware max, their difference, and multi-matching rows for Stage 1-30.
- Validate the HUD shows 3-star remaining score and result screens show 3-star shortfall when needed.
- Validate finish completion does not apply a hidden score multiplier before star rating.
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
- Confirm stage-start guidance appears briefly and disappears without triggering start/retry/resume.
- Confirm persistent center gameplay guide text does not remain during normal play.
- Confirm PlayerPrefs keys use the `CGR_` prefix.
- Confirm Reset Local Progress deletes only `CGR_` keys and never calls `PlayerPrefs.DeleteAll`.
- Confirm Sound Off prevents procedural SFX playback.
- Confirm Camera Shake Off prevents hit/finish shake.
- Confirm Color Assist uses color-specific primitive shapes and high-contrast support without external sprites.
- Confirm shards and the player no longer create black TextMesh symbol overlays.
- Confirm the top-left HUD uses a translucent contrast panel and shadowed text for stage, score, star targets, and current color/shape.
- Confirm background, track, collectibles, and obstacles remain visually distinct on mobile portrait screens.
- Confirm `VisualTheme` is the source of truth for world, HUD, and VFX colors.
- Confirm `Tools/Color Gate Rush/Apply Visual Theme` applies camera background, fog, ambient, and directional light settings without external assets.
- Confirm default Unity skybox appearance is suppressed through code-defined background/fog settings.
- Confirm generated `BackgroundRoot` and `TrackVisualRoot` objects appear under the generated level.
- Confirm track rails, lane separators, edge glow, and rhythm stripes do not add blocking colliders.
- Confirm obstacle warning stripes/spikes, gate cue strips, and finish checker tiles are visual-only.
- Confirm shard glow and bob/spin animation preserve row/lane alignment and trigger collection.
- Confirm ParticleSystem bursts remain short, low-count, and readable on mobile.
- Confirm `Validate Visual Polish` passes before adding new content stages.

## Validator menu

- `Tools/Color Gate Rush/Validate Project`: full static and generation smoke validation.
- `Tools/Color Gate Rush/Validate Build`: release-oriented alias for the same validation.
- `Tools/Color Gate Rush/Validate Visual Polish`: static check for theme, world polish hooks, HUD contrast, and legacy symbol removal.
- `Tools/Color Gate Rush/Generate Balance Report`: generate Stage 1-30 balance summaries in an isolated scene.
- `Tools/Color Gate Rush/Apply Visual Theme`: apply code-defined visual tone to the open scene.
- `Tools/Color Gate Rush/Reset Local Progress`: delete only Color Gate Rush progress keys.

Release-candidate sign-off should run Validate Build, Generate Balance Report, and Reset Local Progress manually in the Unity Editor before packaging.
