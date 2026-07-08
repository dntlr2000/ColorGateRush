# Release Readiness Checklist

Color Gate Rush is ready for manual Android/WebGL build validation after RC QA. This checklist does not require network access, external assets, generated keystores, or command-line builds.

## Editor Menu Order

Run these in the Unity Editor before any manual build:

1. `Tools/Color Gate Rush/Bootstrap Project`
2. Open `Assets/_Project/Scenes/Main.unity`
3. `Tools/Color Gate Rush/Validate Build`
4. `Tools/Color Gate Rush/Validate Runtime Visuals`
5. `Tools/Color Gate Rush/Generate Balance Report`
6. `Tools/Color Gate Rush/Generate Release Readiness Report`
7. `Tools/Color Gate Rush/Apply Visual Theme`

Hard failures must be fixed before packaging. Warnings identify manual release decisions such as package name, signing, icon, store metadata, and real device tests.

## Balance Report Checks

- Stage 1-30 all generate deterministically.
- `estimatedMaxAchievableScore > 0`.
- `threeStarScore <= estimatedMaxAchievableScore`.
- `twoStarScore = ceil(threeStarScore * 2 / 3)` using the project score step.
- A row with multiple matching shards counts as at most one collectible in route-aware max score.
- No all-obstacle, all-off-color, or mixed all-unsafe row exists.
- Clear route and near-perfect route both exist.

## Android Pre-Build Checklist

- Product name is final enough for tester builds.
- Company name is not `DefaultCompany`.
- Application Identifier is a unique reverse-DNS id, not a Unity template id.
- Bundle version and Android version code are intentionally set.
- Portrait-only orientation is selected for the vertical runner UI.
- `Validate Runtime Visuals` passes before device packaging.
- Runtime generated components use generic component creation, not `AddComponent(string)` or `GameObject.CreatePrimitive`.
- `Universal Render Pipeline/Lit` is absent from Graphics Settings Always Included Shaders.
- Runtime base materials exist under `Assets/_Project/Resources/ColorGateRush/Materials`.
- Pink-material prevention uses Resources material asset references, not full URP shader Always Included entries.
- Opaque runtime objects use the `CGR_SimpleLitOpaque` material reference and still show lighting/shadow depth in device builds.
- Scripting Backend and target architectures are reviewed; Google Play release builds should include ARM64.
- Development Build is off for release-candidate distribution builds.
- Use APK for local device testing.
- Use Android App Bundle (AAB) for Google Play submission.
- Keystore, passwords, and signing credentials are created and stored outside the repository.
- Do not commit keystore files, passwords, or signing notes.

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
- No Quit-only flow is required.
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

- MainMenu Start opens StageSelect.
- Stage 1 starts and shows a short stage-start hint that disappears.
- Left/right touch or swipe changes lanes without triggering UI buttons.
- Pause button, ESC/P, Resume, Retry, StageSelect, and MainMenu flows work.
- Failed and Completed screens do not auto-restart.
- Stage clear with at least 1 star unlocks the next stage.
- Best stars never downgrade.
- StageSelect shows 30 stages with locked/unlocked/star states.
- HUD remains readable on a small portrait screen.
- Pause button and top-left HUD do not overlap the safe area.
- Collect, gate, hit, and finish VFX stay readable and short.
- Menu/gameplay BGM does not overlap across Start, Pause, Retry, Failed, Completed, StageSelect, and MainMenu transitions.
- Music Off, SFX Off, Music Volume, and SFX Volume settings work independently.
- Android Logcat does not contain `Can't add component because 'BoxCollider' doesn't exist!`.
- Android generated materials are not pink.
- PC builds show player, track, shards, obstacles, gates, finish, VFX, and HUD after starting Stage 1.
- Development Build runtime visual self-check reports healthy renderer, mesh, material, collider, and camera counts.
- No obvious frame drops, overheating, or memory growth appears during several retries.

## Android Pink Material / Component Log Checks

- If pink procedural materials appear, rerun `Validate Runtime Visuals`.
- Confirm `ProjectSettings/GraphicsSettings.asset` does not include the URP/Lit shader GUID.
- Confirm `Assets/_Project/Resources/ColorGateRush/Materials` contains `CGR_SimpleLitOpaque`, `CGR_UnlitTransparent`, and `CGR_ParticleUnlit`.
- Confirm opaque objects cast/receive shadows while transparent gate/background/VFX objects remain shadow-free.
- Confirm procedural objects are created through `ProceduralFactory.Primitive` and generic collider helpers.
- Confirm no source file contains `AddComponent("...")` or `GameObject.CreatePrimitive`.
- If manual variant management becomes necessary, use a ShaderVariantCollection with only the exact variants used by Color Gate Rush. Do not add full URP/Lit to Always Included Shaders.

## PC Build Visibility Checks

- Build a Development Build for the first repro pass.
- Start Stage 1 from Stage Select.
- Check the log line `Color Gate Rush runtime visual self-check`.
- Investigate immediately if renderer count is low, invalid material count is nonzero, missing mesh count is nonzero, or the camera culling/far clip summary looks wrong.

## Save/Progress Test Scenario

1. Reset Local Progress from the Color Gate Rush menu.
2. Confirm only Stage 1 is unlocked.
3. Clear Stage 1 with 1 star and confirm Stage 2 unlocks.
4. Replay Stage 1 with a lower star result and confirm best stars do not downgrade.
5. Restart the app or refresh the WebGL page and confirm progress persists.

## Audio And Visual Quality Status

Implemented procedural audio/visual quality pass:

- Menu and gameplay procedural BGM loops are generated with `AudioClip.Create`.
- Gameplay music varies slightly by stage tier.
- Completed and Failed use short procedural stings.
- Music/SFX toggles and volume steps use `CGR_` PlayerPrefs keys.
- Collect, gate, fail, and finish VFX use short mobile-safe burst/ring layers.
- Track polish includes side rails, lane separators, edge glow, side light strips, and rhythm stripes.

External music files still require license review. Under the current automation policy, use procedural BGM or directly produced/licensed audio only.

Follow-up quality work after device testing:

- Tune BGM loudness on real Android/WebGL targets.
- Profile VFX particle counts on low-end devices.
- Consider store-ready app icons and screenshots generated from original procedural art.

## Content Expansion Backlog

- Tune stages with Balance Report warnings.
- Add more stage tiers only after Android/WebGL smoke tests pass.
- Add PlayMode/EditMode tests for scoring, unlock, and row invariants.
