# Release Readiness Checklist

Color Gate Rush is ready for manual Android/WebGL build validation after RC QA. This checklist does not require network access, external assets, generated keystores, or command-line builds.

For tester-session flow, survey questions, and manual scenario coverage, also see `docs/playtest_checklist.md`.

## Editor Menu Order

Run these in the Unity Editor before any manual build:

1. `Tools/Color Gate Rush/Bootstrap Project`
2. Open `Assets/_Project/Scenes/Main.unity`
3. `Tools/Color Gate Rush/Validate Build`
4. `Tools/Color Gate Rush/Validate Runtime Visuals`
5. `Tools/Color Gate Rush/Generate Balance Report`
6. `Tools/Color Gate Rush/Generate Release Readiness Report`
7. `Tools/Color Gate Rush/Apply Visual Theme`
8. Optional before an Endless record retest: `Tools/Color Gate Rush/Reset Endless Records`

Hard failures must be fixed before packaging. Warnings identify manual release decisions such as package name, signing, icon, store metadata, and real device tests.

## Localization Checks

- Settings shows Korean / English language buttons.
- Changing language refreshes Main Menu, Stage Select, Rules, Settings, Pause, Failed/Completed, Stage HUD, Endless HUD, and stage-start hints without restarting the run.
- `CGR_Language` persists across app restart and is not removed by Reset Progress.
- Korean and English dictionaries contain every `LocalizationKey`.
- Format placeholders match across Korean and English entries.
- No Unity Localization package or external font asset is required.
- Settings opens on General and separates Language and Data reset sections.

## Balance Report Checks

- Stage 1-30 all generate deterministically.
- `estimatedMaxAchievableScore > 0`.
- `threeStarScore <= estimatedMaxAchievableScore`.
- `twoStarScore = ceil(threeStarScore * 2 / 3)` using the project score step.
- A row with multiple matching shards counts as at most one collectible in route-aware max score.
- No all-obstacle, all-off-color, or mixed all-unsafe row exists.
- Clear route and near-perfect route both exist.

## Android Pre-Build Checklist

- Product name is final enough for tester builds. Current static check target: `ColorGateRush`.
- Company name is not `DefaultCompany`; change it in Unity Player Settings before external RC distribution.
- Application Identifier is a unique reverse-DNS id, not a Unity template id such as `com.UnityTechnologies.com.unity.template.urpblank`.
- Bundle version and Android version code are intentionally set. Current static check target: version `0.1.0`, Android version code `1`.
- Portrait-only orientation is selected for the vertical runner UI.
- `Validate Runtime Visuals` passes before device packaging.
- Runtime generated components use generic component creation, not `AddComponent(string)` or `GameObject.CreatePrimitive`.
- `Universal Render Pipeline/Lit` is absent from Graphics Settings Always Included Shaders.
- Runtime base materials exist under `Assets/_Project/Resources/ColorGateRush/Materials`.
- The approved title screen image exists at `Assets/_Project/Resources/ColorGateRush/Images/TitleScreen.png`.
- The approved main menu background exists at `Assets/_Project/Resources/ColorGateRush/Images/MainMenuBackground.png`.
- Pink-material prevention uses Resources material asset references, not full URP shader Always Included entries.
- Opaque runtime objects use `CGR_SimpleLit*` material references and still show lighting/shadow depth in device builds.
- Scripting Backend and target architectures are reviewed; Google Play release builds should include ARM64.
- Development Build is off for release-candidate distribution builds.
- Use APK for local device testing.
- Use Android App Bundle (AAB) for Google Play submission.
- Keystore, passwords, and signing credentials are created and stored outside the repository.
- Do not commit keystore files, passwords, or signing notes.
- App icon, splash screen, and store screenshots are reviewed; treat placeholder branding as a warning before public submission.

## Android Manual Build Steps

1. Open File > Build Profiles.
2. Select Android and switch platform if Unity asks.
3. Confirm `Assets/_Project/Scenes/Main.unity` is the first enabled scene.
4. For local testing, build an APK with Development Build only when debugging is needed.
5. For Google Play, build an AAB and sign it with the manually managed keystore.
6. Install on a physical Android device and run the device test checklist below.

## WebGL Pre-Build Checklist

- Main scene is the only game startup scene.
- PlayerPrefs progress uses simple `CGR_` keys.
- Quit button shows a browser-tab notice; WebGL should not rely on closing the page from code.
- Canvas Scaler, ScrollRect, and large button targets are checked in browser-sized windows.
- Browser AudioContext may block sound until the first user gesture; test after clicking Start.
- Compression, memory, and loading time are reviewed after the build.
- No external WebGL template, JavaScript plugin, or downloaded asset is required.

## WebGL Manual Build Steps

1. Open File > Build Profiles.
2. Select WebGL and switch platform if Unity asks.
3. Confirm Main scene is enabled first.
4. Build with Unity's built-in WebGL template/settings.
5. Serve the build from a local or internal test host.
6. Test keyboard, mouse, touch, persistence, audio unlock, and browser resizing.

## Device Test Checklist

- App starts on the title image, and tap/click opens MainMenu.
- MainMenu Start opens StageSelect.
- Stage 1 starts and shows a short stage-start hint that disappears.
- Left/right touch or swipe changes lanes without triggering UI buttons.
- Pause button, ESC/P, Resume, Retry, StageSelect, and MainMenu flows work.
- Failed and Completed screens do not auto-restart.
- Stage clear with at least 1 star unlocks the next stage.
- Best stars never downgrade.
- MainMenu Endless Mode starts a record run with no finish line, no star messaging, and no stage unlock changes.
- Repeated Endless starts/retries feel different because each run receives a fresh per-run seed.
- Endless HUD shows score, distance, best score, best distance, wrong-shard chance icons, current color/shape, and speed multiplier.
- Endless speed rises from elapsed time/distance without using `Time.timeScale`; row spacing and lane movement remain readable as speed increases.
- Stage and Endless wrong-color shard count reaches 1/3 and 2/3 without ending the run, then 3/3 triggers failure with the wrong-shard limit reason.
- Stage Mode route-aware scoring tracks wrong-shard count state and treats the third wrong shard as a failed route.
- Endless failure result can Retry or return to MainMenu/StageSelect and saves best score/distance with failure reason.
- MainMenu Quit exits Android/PC builds only from the explicit button; Editor/WebGL show safe feedback.
- Playtest Stats buttons, panels, and new `CGR_Stats_` writes are absent from the release candidate.
- Settings Data separates Reset Stage Progress from Reset Endless Records and both use confirmation panels.
- Settings tabs, buttons, sliders, and the Main Menu bottom action stay inside a padded mobile content width.
- Settings General volume labels stay above slim sliders without overlapping the reduced white handles.
- Reset Endless Records clears only `CGR_Endless...` counters and does not reset unlocks, best stars, tutorial, settings, or language.
- StageSelect shows 30 stages with locked/unlocked/star states.
- HUD remains readable on a small portrait screen.
- Pause button and top-left HUD do not overlap the safe area.
- Combo appears as a bottom-right `xN` badge and never as a center toast.
- Gate color/shape changes update the top-left current chip/label without opening a center toast.
- Collect, gate, hit, and finish VFX stay readable and short.
- Menu BGM (`ColorgateRush_Menu.mp3`) and gameplay BGM (`ColorgateRush_Ingame.mp3`) do not overlap across Start, Pause, Retry, Failed, Completed, StageSelect, Endless, and MainMenu transitions.
- Music Off, SFX Off, Music Volume slider, and SFX Volume slider settings work independently.
- Android Logcat does not contain `Can't add component because 'BoxCollider' doesn't exist!`.
- Android generated materials are not pink.
- PC builds show player, track, shards, obstacles, gates, finish, VFX, and HUD after starting Stage 1.
- Development Build runtime visual self-check reports healthy renderer, mesh, material, collider, and camera counts.
- No obvious frame drops, overheating, or memory growth appears during several retries.
- No obvious object buildup occurs during a several-minute Endless run while speed, obstacle pressure, off-color pressure, and gate frequency rise gradually.

## Android Pink Material / Component Log Checks

- If pink procedural materials appear, rerun `Validate Runtime Visuals`.
- Confirm `ProjectSettings/GraphicsSettings.asset` does not include the URP/Lit shader GUID.
- Confirm `Assets/_Project/Resources/ColorGateRush/Materials` contains `CGR_SimpleLitPlayer`, `CGR_SimpleLitOpaque`, `CGR_SimpleLitShard`, `CGR_SimpleLitTrack`, `CGR_SimpleLitObstacle`, `CGR_SimpleLitFinish`, `CGR_UnlitOpaque`, `CGR_UnlitTransparent`, and `CGR_ParticleUnlit`.
- If `Validate Runtime Visuals` reports a null or unsupported material, use the reported generated object path, renderer type, material slot, material name, and shader name to identify the exact renderer. Player body renderers should route through `RuntimeMaterialProvider` player body methods; player accent renderers may remain Unlit.
- If the player looks flat, confirm the player body material is `CGR_SimpleLitPlayer`-based, the shader name is not Unlit, `shadowCastingMode` is On, and `receiveShadows` is true.
- Confirm opaque objects cast/receive shadows while transparent gate/background/VFX objects remain shadow-free.
- Confirm procedural objects are created through `ProceduralFactory.Primitive` and generic collider helpers.
- Confirm no source file contains `AddComponent("...")` or `GameObject.CreatePrimitive`.
- If manual variant management becomes necessary, use a ShaderVariantCollection with only the exact variants used by Color Gate Rush. Do not add full URP/Lit to Always Included Shaders.

## PC Build Visibility Checks

- Build a Development Build for the first repro pass.
- Start Stage 1 from Stage Select.
- Check the log line `Color Gate Rush runtime visual self-check`.
- Investigate immediately if renderer count is low, invalid material count is nonzero, missing mesh count is nonzero, or the camera culling/far clip summary looks wrong.

## Audio Asset Policy

- Only these user-provided BGM files are approved imported audio:
  - `Assets/_Project/Resources/ColorGateRush/Audio/ColorgateRush_Menu.mp3`
  - `Assets/_Project/Resources/ColorGateRush/Audio/ColorgateRush_Ingame.mp3`
- Do not add additional BGM/SFX files without an explicit asset/license review.
- Current SFX remain procedural.
- Large background/platform polish is deferred to launch or post-launch visual polish; current validation focuses on build compatibility, speed/spacing feel, Quit, and Endless MVP.
- Imported visual media remains limited to the approved title screen and main menu background images; gameplay objects still use procedural geometry/materials.

## Save/Progress Test Scenario

1. Reset Local Progress from the Color Gate Rush menu.
2. Confirm only Stage 1 is unlocked.
3. Clear Stage 1 with 1 star and confirm Stage 2 unlocks.
4. Replay Stage 1 with a lower star result and confirm best stars do not downgrade.
5. Restart the app or refresh the WebGL page and confirm progress persists.

## Settings Data Reset Scenario

1. Open Settings and confirm General, Language, and Data sections are visually separated.
2. Change language and BGM/SFX settings, then use Reset Stage Progress from the Data section.
3. Confirm stage unlocks/stars reset while language, BGM/SFX, camera shake, color assist, and Endless records remain unchanged.
4. Create an Endless record, then use Reset Endless Records from the Data section.
5. Confirm only Endless best score/distance/rows/attempts/runs reset and Stage progress remains unchanged.

## Audio And Visual Quality Status

Implemented audio/visual quality pass:

- Menu and gameplay BGM are loaded from the approved Resources MP3 clips.
- Procedural BGM loops remain as fallback if the approved clips are missing.
- Completed and Failed use short procedural stings.
- Music/SFX toggles and draggable volume sliders use `CGR_` PlayerPrefs keys.
- Collect, gate, fail, and finish VFX use short mobile-safe burst/ring layers.
- Track polish includes side rails, lane separators, edge glow, side light strips, and rhythm stripes.

Additional external music files still require license review. Under the current automation policy, use only the approved BGM files, procedural audio, or directly produced/licensed audio that has been explicitly added to the allowlist.

Playtest readiness status:

- Do not add more BGM/SFX before the next playtest pass.
- Keep current BGM Resources loading and procedural SFX structure stable.
- Prioritize stage difficulty, UI readability, touch feel, save/unlock consistency, and Android/PC build stability feedback.

Follow-up quality work after device testing:

- Tune BGM loudness on real Android/WebGL targets.
- Profile VFX particle counts on low-end devices.
- Consider store-ready app icons and screenshots generated from original procedural art.

## Content Expansion Backlog

- Tune stages with Balance Report warnings.
- Add more stage tiers only after Android/WebGL smoke tests pass.
- Add PlayMode/EditMode tests for scoring, unlock, and row invariants.
