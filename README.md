# Color Gate Rush

Procedural Unity 6 hyper-casual runner built without external art, audio, models, fonts, prefabs, sprites, or Asset Store packages.

## Flow

- MainMenu
- StageSelect
- Rules
- Settings
- Playing
- Tutorial
- Paused
- Failed
- Completed

Main Menu `Start` opens Stage Select. Gameplay starts only after selecting an unlocked stage.
Stage Select lists 30 deterministic stages in a scrollable two-column grid.

## Controls

- Keyboard: `A/D` or `Left/Right Arrow`
- Mobile: horizontal swipe or left/right half-screen tap
- Pause: HUD button, `ESC`, or `P`
- Pause shortcuts: `R` retry, `M` main menu

## Rules

- Collect same color and shape shards to score.
- Wrong-color shards reset combo and subtract score.
- Gates change the player color and target shape.
- Obstacles fail the run.
- Finish grants at least 1 star and preserves the current HUD score.
- 2-star and 3-star score targets are shown in the HUD and use that same current score.
- The HUD also shows the score remaining to reach the 3-star target.
- 2 stars require the rounded-up two-thirds point of the 3-star target.
- 3 stars are tuned as a near-perfect route reward; missing or miscollecting 1-2 key shards can make the cutoff hard to reach.
- Any clear with at least 1 star unlocks the next stage.

## Stage Content

- Stage 1 is unlocked by default.
- Stages 2-30 unlock sequentially from clears with at least 1 star.
- Stage configs are generated in C# with unique seeds, no external data files.
- Difficulty increases through row count, track length, speed, gate frequency, obstacle pressure, and off-color shard pressure.
- Star targets are derived from route-aware estimated max score, including lane choice, combo scoring, penalties, and gate score.
- The route-aware max treats each row as one lane choice, so multiple same-row shards are not counted as all collectible.
- Balance Report shows naive max, route-aware max, and the gap between them for Stage 1-30.

## Procedural Assets

All gameplay assets are generated from Unity primitives, built-in UI/TextMesh, ParticleSystem, procedural materials, and `AudioClip.Create`.

## Visual Polish

- `VisualTheme` centralizes the candy-neon palette for background, track, hazards, finish, HUD, and VFX.
- 30 stages cycle through five procedural theme variations without external textures or skyboxes.
- The default Unity skybox feel is replaced by camera color, fog, ambient light, directional light, and procedural backdrop panels.
- Track readability uses primitive rails, lane separators, edge glow, and rhythm stripes.
- Shards use color-specific primitive silhouettes, glow shells, subtle bob/spin, and short collect bursts.
- Obstacles use warning colors, stripes, and spike-like primitive accents.
- Gates and finish use procedural cue strips, arches, checker tiles, and mobile-safe particle bursts.
- URP Volume is optional; safe fallback visual settings are applied without external assets or package changes.

## Settings

Settings use `CGR_` PlayerPrefs keys:

- `CGR_SoundEnabled`
- `CGR_CameraShake`
- `CGR_HighContrast`
- `CGR_TutorialSeen`

Reset Local Progress deletes only Color Gate Rush progress keys: unlocked stage, selected stage, best stars, and tutorial seen.

## Validator

Unity menu:

- `Tools/Color Gate Rush/Bootstrap Project`
- `Tools/Color Gate Rush/Validate Project`
- `Tools/Color Gate Rush/Validate Build`
- `Tools/Color Gate Rush/Validate Visual Polish`
- `Tools/Color Gate Rush/Generate Balance Report`
- `Tools/Color Gate Rush/Apply Visual Theme`
- `Tools/Color Gate Rush/Reset Local Progress`

Manual QA should verify MainMenu to StageSelect, Stage unlocks, pause/resume, no automatic restart, row fairness, star targets, settings toggles, Stage 1 tutorial, and the visual polish checklist for HUD contrast, track readability, shard/obstacle/gate/finish clarity, and mobile-safe VFX.
