# Playtest Checklist

This checklist is for gameplay and build-readiness playtests with the two approved BGM clips included. Do not add additional audio, images, fonts, models, prefabs, or Asset Store packages for this pass.

## Unity Editor Setup

1. Open `Assets/_Project/Scenes/Main.unity`.
2. Run `Tools/Color Gate Rush/Validate Build`.
3. Run `Tools/Color Gate Rush/Validate Runtime Visuals`.
4. Run `Tools/Color Gate Rush/Generate Balance Report`.
5. Run `Tools/Color Gate Rush/Generate Release Readiness Report`.
6. Optional before Endless retests: run `Tools/Color Gate Rush/Reset Endless Records`.

## Release Candidate Build Settings Review

- Confirm Company Name is no longer `DefaultCompany` before external release.
- Confirm Android Application Identifier is a project-owned reverse-DNS id, not the Unity template id.
- Confirm Product Name is `Color Gate Rush`, and bundle version, Android version code, and portrait orientation are intentional.
- Confirm APK is used for local device tests and AAB is used only for Google Play submission.
- Confirm Development Build is off for submission builds and keystore/signing files stay outside the repository.
- Confirm app icon, splash, and store screenshots are still placeholder or explicitly approved for RC use.
- Confirm `docs/audio_licenses.md` has completed BGM license/source records before any public store submission.
- Confirm `docs/store_listing_draft.md` TODOs are either resolved or intentionally deferred for internal testing only.

## Android APK Smoke Test

1. Build a local APK from Unity Build Profiles.
2. Install on a physical portrait Android device.
3. Confirm the app opens on the title image.
4. Tap the title screen and confirm MainMenu appears.
5. Confirm MainMenu Start opens StageSelect.
6. Confirm MainMenu Endless Mode starts immediately and shows score/distance/best records, wrong-shard chance icons, and Speed x value.
7. Confirm MainMenu Quit exits only from the explicit button in Android/PC builds.
8. Start Stage 1 and confirm the stage-start hint disappears after a few seconds.
9. Confirm swipe and left/right half-screen tap change lanes without triggering UI buttons.
10. Confirm Pause button is easy to hit and does not overlap the HUD or notch area.
11. Confirm Failed/Completed screens never auto-restart.
12. Confirm clearing Stage 1 with at least 1 star unlocks Stage 2.
13. Open Settings and confirm General, Language, and Data sections are separated.
14. Check Logcat for no `Can't add component because 'BoxCollider' doesn't exist!` and no pink procedural materials.

## Language Smoke Test

1. Start in Korean, verify the Title Screen appears first, tap through, then verify Main Menu, Stage Select, Rules, Settings, HUD, Pause, Result, and Endless text.
2. Open Settings, switch to English, and confirm the currently open Settings screen updates immediately.
3. Revisit the Title Screen flow, Main Menu, Stage Select, Rules, Stage HUD, Endless HUD, Pause, Failed, and Completed screens in English.
4. Restart the app and confirm English persists through `CGR_Language`.
5. Switch back to Korean and confirm the same screens return to Korean without resetting progress.
6. Confirm Reset Progress does not reset the selected language.

## PC Build Smoke Test

1. Start from the title image, tap/click into MainMenu, and go to StageSelect.
2. Start Stage 1 and verify player, track, shards, obstacles, gates, finish, VFX, and HUD render.
3. Test `A/D`, arrow keys, `ESC/P`, `R`, and `M`.
4. Clear and fail at least one stage, then verify result buttons.
5. Restart the build and confirm stage progress, language, and audio/display settings persist locally.
6. Start Endless Mode, fail intentionally by obstacle and by three wrong-color shards, and confirm best score/distance persist after restarting the build.

## Stage Sampling

- Stage 1-5: first-session flow, tutorial, collection clarity, obstacle rarity, early unlock rhythm, and 1-star progression.
- Stage 10: mid-campaign speed and obstacle pressure.
- Stage 20: advanced gate frequency and route readability.
- Stage 30: end-campaign strict 3-star target and visual clarity.
- Endless 30 seconds: confirm the early speed ramp is readable and wrong-shard chances are clear.
- Endless retry variation: start Endless three times and confirm the early rows do not feel like a fixed seed replay.
- Endless 60 seconds: confirm obstacle/off-color pressure and gate frequency feel higher without unfair rows.
- Endless 90 seconds: confirm high-speed row spacing remains playable and no object buildup is visible.
- Endless 2+ minutes: check rolling generation, pause, retry, MainMenu return, and no obvious memory/performance growth.
- Wrong-shard rule: in both Stage and Endless Mode, collect two wrong-color shards and confirm the run continues; collect a third and confirm game over with the wrong-shard limit reason.
- HUD regression: confirm both Stage and Endless HUDs show three chance icons and update them immediately after each wrong-color shard.
- Combo HUD regression: confirm combo appears only as the bottom-right `xN` badge and never as a center toast.
- Gate HUD regression: confirm color/shape changes update the top-left chip/label and never open a center toast.

## Save, Unlock, And Data

- Reset Local Progress affects stage unlocks, best stars, selected stage, and tutorial seen.
- Settings Data separates Reset Stage Progress from Reset Endless Records and both show confirmation panels.
- Reset Stage Progress does not reset language, BGM/SFX volume, camera shake, color assist, or Endless records.
- Failing awards 0 stars and does not unlock the next stage.
- Clearing awards at least 1 star and unlocks the next stage.
- Best stars and best score do not downgrade.
- Endless records use `CGR_Endless...` keys and do not affect Stage unlocks or stars.
- Reset Endless Records affects only Endless best score/distance/rows/attempts/runs.

## Tester Questions

- Did you understand the goal in the first 30 seconds?
- Did lane movement feel responsive on your device?
- Were shards, gates, obstacles, and finish clearly distinguishable?
- Did the 3-star target feel like a satisfying challenge?
- Did failure reasons feel understandable?
- Did the next-stage unlock flow feel clear after a 1-star clear?
- Was any UI text too small, crowded, or hidden by the device safe area?
- Did any VFX feel distracting or tiring?
- Did you want to continue after Stage 3?
- Did Endless Mode feel readable as speed, off-color pressure, obstacle pressure, and gate frequency increased?
- Were the wrong-shard chance icons understandable before the third mistake ended the run?
- Did the Quit button placement feel safe and intentional?

## Audio Playtest Notes

- Menu BGM uses `ColorgateRush_Menu.mp3`; Stage and Endless gameplay use `ColorgateRush_Ingame.mp3`.
- Move MainMenu -> StageSelect -> Stage -> Pause -> Resume -> Failed -> Retry -> Completed -> MainMenu and confirm BGM does not overlap or double.
- Move MainMenu -> Endless -> Pause -> Retry -> MainMenu and confirm only one gameplay/menu BGM source is audible.
- Verify the Music slider can be dragged smoothly from 0% to 100% and that Music Off stops only BGM.
- Verify the SFX slider can be dragged smoothly from 0% to 100% and that SFX Off stops only one-shot sounds.
- Confirm every user-facing button click plays one short `ui_click` SFX when SFX is enabled, and no click repeats while dragging sliders or scrollbars.
- Do not add more BGM/SFX before this playtest pass finishes.
- After gameplay feedback, consider a focused Audio Quality Sprint for device mix tuning, optional SFX replacement, and license/manifest tracking for any additional audio files.

## Store Submission Prep Smoke

- Run `Generate Release Readiness Report` and review all warnings, even when there are no hard failures.
- Confirm Android target API level against the current Google Play requirement in Unity Build Profiles.
- Confirm icons and splash branding are no longer placeholders before public release.
- Confirm final screenshots are captured from the actual release candidate build.
- Confirm signing/keystore material is stored outside the repository.
