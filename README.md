# Color Gate Rush

Procedural Unity 6 hyper-casual runner built without downloaded art, models, fonts, prefabs, sprites, or Asset Store packages. The project currently allowlists two user-provided BGM clips plus user-provided title and main menu images under `Resources`.

## Flow

- TitleScreen
- MainMenu
- StageSelect
- Rules
- Settings
- Endless Playing
- Playing
- Tutorial
- Paused
- Failed
- Completed

The app opens on a full-screen title image. Tap/click the title screen to enter Main Menu.
Main Menu `Start` opens Stage Select. Gameplay starts only after selecting an unlocked stage.
Main Menu `Endless Mode` starts a finish-free record run that is independent from stage stars and unlocks.
Main Menu `게임 종료` calls `Application.Quit()` in Android/PC builds, logs safely in the Editor, and shows a WebGL tab-close notice.
Stage Select lists 30 deterministic stages in a scrollable two-column grid.
Settings is organized into mobile-friendly General, Language, and Data sections so audio/display options, language selection, and reset actions stay clearly separated with padded content width and grouped volume sliders.
General action buttons use a procedural Unity UI style made from dark translucent panels, thin cyan/violet accents, and readable text shadows; Settings controls, tabs, sliders, and scroll UI intentionally keep their separate flat settings style.
The earlier cyan button PNG is retired and is not referenced by runtime UI; it is not required for builds.

## Localization

- Korean and English runtime UI are supported without the Unity Localization package.
- Settings has Korean / English selection buttons.
- The selected language is saved in `CGR_Language` and survives app restart.
- Changing language refreshes the currently open UI immediately.
- New player-facing runtime text should be added as a `LocalizationKey` with Korean and English entries before use.

## Controls

- Mobile: horizontal swipe or left/right half-screen tap
- PC/editor keyboard input remains available for development smoke tests.
- Pause: HUD button, `ESC`, or `P`
- Pause shortcuts: `R` retry, `M` main menu
- Android Back/Escape pauses gameplay and returns submenus to Main Menu.

## Rules

- Collect same color and shape shards to score.
- Wrong-color shards reset combo, subtract score, and count toward the shared 3-strike game over in Stage and Endless Mode.
- Gates change the player color and target shape.
- Obstacles fail the run.
- Finish grants at least 1 star and preserves the current HUD score.
- 2-star and 3-star score targets are shown in the HUD and use that same current score.
- The HUD also shows the score remaining to reach the 3-star target.
- Combo is shown as a compact bottom-right `xN` badge instead of a center toast.
- Current color/shape changes are read from the top-left HUD chip and label, not from center popups.
- 2 stars require the rounded-up two-thirds point of the 3-star target.
- 3 stars are tuned as a near-perfect route reward; missing or miscollecting 1-2 key shards can make the cutoff hard to reach.
- Any clear with at least 1 star unlocks the next stage.
- Endless Mode has no finish, star targets, or unlock writes; it gets faster over time, uses a fresh random seed per run, and failure shows score, distance, best score, best distance, wrong shard chance icons, and failure reason.
- In both Stage and Endless Mode, collecting the third wrong-color shard ends the run. Stage Mode still clears/unlocks by reaching the finish.

## Stage Content

- Stage 1 is unlocked by default.
- Stages 2-30 unlock sequentially from clears with at least 1 star.
- Stage configs are generated in C# with unique seeds, no external data files.
- Difficulty increases through row count, track length, speed, gate frequency, obstacle pressure, and off-color shard pressure.
- The speed/spacing pass raises forward speed while widening row spacing and gate spacing so reaction time remains fair.
- Star targets are derived from route-aware estimated max score, including lane choice, combo scoring, penalties, and gate score.
- The route-aware max treats each row as one lane choice, so multiple same-row shards are not counted as all collectible.
- Balance Report shows naive max, route-aware max, and the gap between them for Stage 1-30.

## Procedural Assets

Gameplay visual assets are generated from Unity primitives, built-in UI/TextMesh, ParticleSystem, and procedural materials. The main menu uses the approved user-provided background image at `Assets/_Project/Resources/ColorGateRush/Images/MainMenuBackground.png`; runtime gameplay visuals still use procedural geometry. Audio uses two approved user-provided BGM clips for menu/gameplay music, with procedural `AudioClip.Create` fallbacks, SFX, and result stings.

Runtime mesh objects are created with explicit `GameObject` + `MeshFilter` + `MeshRenderer` + generic collider helpers. `GameObject.CreatePrimitive` and `AddComponent(string)` are not used, because Android player builds can fail built-in primitive collider creation with messages such as `Can't add component because 'BoxCollider' doesn't exist!`.

## Visual Polish

- `VisualTheme` centralizes the candy-neon palette for background, track, hazards, finish, HUD, and VFX.
- `RuntimeMaterialProvider` centralizes URP-compatible shader/material creation for generated objects.
- Runtime base materials live under `Assets/_Project/Resources/ColorGateRush/Materials`, so Android/WebGL/PC builds include the limited shader variants actually used by generated objects.
- Opaque generated meshes use `Universal Render Pipeline/Simple Lit` through small Resources material presets such as `CGR_SimpleLitPlayer`, `CGR_SimpleLitShard`, `CGR_SimpleLitTrack`, `CGR_SimpleLitObstacle`, and `CGR_SimpleLitFinish` to keep lighting and shadows without pulling in full URP/Lit variants.
- The player body uses a player-specific `CGR_SimpleLitPlayer` provider path with shadow casting/receiving enabled. The player color accent remains on `CGR_UnlitTransparent`, so the decorative cue stays build-safe and shadow-free.
- Transparent panels and ParticleSystem feedback stay on limited `URP/Unlit` materials for build safety.
- `Universal Render Pipeline/Lit` is not placed in Graphics Settings Always Included Shaders because its variant count can break Android builds.
- 30 stages cycle through five procedural theme variations without external textures or skyboxes.
- The default Unity skybox feel is replaced by camera color, fog, ambient light, directional light, and subtle side/horizon accents rather than large backdrop wall panels.
- Track readability uses primitive rails, lane separators, surface sheen strips, edge glow, side light strips, and rhythm stripes.
- Shards use color-specific primitive silhouettes, glow shells, subtle bob/spin, and short collect bursts.
- Obstacles use warning colors, stripes, and spike-like primitive accents.
- Gates and finish use procedural cue strips, arches, checker tiles, and mobile-safe particle bursts.
- URP Volume is optional; safe fallback visual settings are applied without external assets or package changes.

## Audio

- Menu BGM is loaded from `Assets/_Project/Resources/ColorGateRush/Audio/ColorgateRush_Menu.mp3`.
- Gameplay and Endless BGM are loaded from `Assets/_Project/Resources/ColorGateRush/Audio/ColorgateRush_Ingame.mp3`.
- `ProceduralAudio` loads the approved BGM clips through `Resources.Load<AudioClip>` and falls back to runtime `AudioClip.Create` loops if the clips are missing.
- Completed and Failed states stop the loop and play short procedural stings.
- Pause/tutorial temporarily duck music volume and all state changes restore normal volume.
- Repeated tone/buzz SFX clips are cached after creation to avoid allocating a new clip for every collect or hit.
- User-facing buttons play a short procedural `ui_click` SFX through `ProceduralAudio`; Settings sliders and scroll UI do not play repeated click sounds while dragged.
- Settings buttons and tabs use static visual transitions so their selected state is clear without hover/pressed color flashes.

## Settings

Settings use `CGR_` PlayerPrefs keys:

- `CGR_SoundEnabled`
- `CGR_MusicEnabled`
- `CGR_SfxEnabled`
- `CGR_MusicVolume`
- `CGR_SfxVolume`
- `CGR_CameraShake`
- `CGR_HighContrast`
- `CGR_TutorialSeen`

Reset Local Progress deletes only Color Gate Rush progress keys: unlocked stage, selected stage, best stars, and tutorial seen. Music/SFX and visual settings are preserved.

## Release Playtesting

The release candidate no longer exposes an in-game Playtest Stats screen and no longer writes new `CGR_Stats_` telemetry. Playtests use `docs/playtest_checklist.md` plus Validate Build, Validate Runtime Visuals, Balance Report, and Release Readiness Report.

Reset Stage Progress and Reset Endless Records are separate Data-section actions. Neither reset removes language, BGM/SFX, camera shake, or color-assist settings.

## Endless Records

Endless records use `CGR_` PlayerPrefs keys and are independent from Stage Mode:

- `CGR_EndlessBestScore`
- `CGR_EndlessBestDistance`
- `CGR_EndlessBestRows`
- `CGR_EndlessAttempts`
- `CGR_EndlessTotalRuns`
- `CGR_EndlessWrongShardLimitFails`

`Reset Endless Records` deletes only those Endless keys. Reset Local Progress does not clear Endless records.

## Validator

Unity menu:

- `Tools/Color Gate Rush/Bootstrap Project`
- `Tools/Color Gate Rush/Validate Project`
- `Tools/Color Gate Rush/Validate Build`
- `Tools/Color Gate Rush/Validate Visual Polish`
- `Tools/Color Gate Rush/Validate Runtime Visuals`
- `Tools/Color Gate Rush/Generate Balance Report`
- `Tools/Color Gate Rush/Generate Release Readiness Report`
- `Tools/Color Gate Rush/Apply Visual Theme`
- `Tools/Color Gate Rush/Reset Local Progress`
- `Tools/Color Gate Rush/Reset Endless Records`

Manual QA should verify MainMenu to StageSelect, Endless entry, Quit behavior, Settings General/Language/Data sections, Stage unlocks, pause/resume, no automatic restart, row fairness, star targets, Endless random seed feel and record reset, Music/SFX toggles and sliders, Stage 1 tutorial, and the visual polish checklist for HUD contrast, bottom-right combo badge, track readability, shard/obstacle/gate/finish clarity, mobile-safe VFX, Android pink-material absence, and PC renderer visibility.

Stage and Endless QA should verify the HUD shows three wrong-shard chance icons, the first two wrong-color shards continue the run, and the third opens the Failed/result screen with the wrong-shard limit reason. Endless QA should also verify speed/difficulty rises without changing `Time.timeScale` and row spacing remains readable as speed increases.

Only the two approved user-provided BGM files under `Assets/_Project/Resources/ColorGateRush/Audio` and the approved main menu background image under `Assets/_Project/Resources/ColorGateRush/Images` are allowed as imported media. SFX remain procedural.

If Android shows pink materials, run `Validate Runtime Visuals`, confirm `Universal Render Pipeline/Lit` is absent from Always Included Shaders, and confirm the Resources material assets exist under `Assets/_Project/Resources/ColorGateRush/Materials`. If the validator reports a null or unsupported material, use the object path, renderer type, slot, material name, and shader name in the error to find the exact generated object. If the player looks flat, confirm the player body uses `CGR_SimpleLitPlayer` and reports `shadowCastingMode=On` and `receiveShadows=True`; the player accent can remain Unlit. If a PC build hides generated objects, use a Development Build and check the runtime visual self-check log for renderer, mesh, material, collider, camera culling mask, and far clip counts.

For Android/WebGL packaging preparation, follow `docs/release_readiness_checklist.md`. It covers Validate Build, Balance Report, Release Readiness Report, APK/AAB usage, keystore safety, WebGL browser checks, and device smoke tests.

For playtest preparation, follow `docs/playtest_checklist.md`. Additional SFX replacement and broader audio licensing workflows remain deferred until after gameplay difficulty, UI, save/unlock, and device stability feedback is collected.
