# Color Gate Rush

Procedural Unity 6 hyper-casual runner built without external art, audio, models, sprites, or Asset Store packages.

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

## Controls

- Keyboard: `A/D` or `Left/Right Arrow`
- Mobile: horizontal swipe or left/right half-screen tap
- Pause: HUD button, `ESC`, or `P`
- Pause shortcuts: `R` retry, `M` main menu

## Rules

- Collect same color and shape shards to score.
- Wrong-color shards reset combo and subtract score.
- Gates change the player color and target symbol.
- Obstacles fail the run.
- Finish grants at least 1 star.
- 2-star and 3-star score targets are shown in the HUD.
- Only a 3-star clear unlocks the next stage.

## Procedural Assets

All gameplay assets are generated from Unity primitives, built-in UI/TextMesh, ParticleSystem, procedural materials, and `AudioClip.Create`.

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
- `Tools/Color Gate Rush/Generate Balance Report`
- `Tools/Color Gate Rush/Reset Local Progress`

Manual QA should verify MainMenu to StageSelect, Stage unlocks, pause/resume, no automatic restart, row fairness, star targets, settings toggles, and Stage 1 tutorial.
