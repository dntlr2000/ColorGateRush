# Playtest Checklist

This checklist is for gameplay and build-readiness playtests with the two approved BGM clips included. Do not add additional audio, images, fonts, models, prefabs, or Asset Store packages for this pass.

## Unity Editor Setup

1. Open `Assets/_Project/Scenes/Main.unity`.
2. Run `Tools/Color Gate Rush/Validate Build`.
3. Run `Tools/Color Gate Rush/Validate Runtime Visuals`.
4. Run `Tools/Color Gate Rush/Generate Balance Report`.
5. Run `Tools/Color Gate Rush/Generate Release Readiness Report`.
6. Optional clean telemetry: run `Tools/Color Gate Rush/Reset Playtest Stats`.

## Android APK Smoke Test

1. Build a local APK from Unity Build Profiles.
2. Install on a physical portrait Android device.
3. Confirm MainMenu Start opens StageSelect.
4. Confirm MainMenu Endless Mode starts immediately and shows score/distance/best records, wrong-shard chance icons, and Speed x value.
5. Confirm MainMenu Quit exits only from the explicit button in Android/PC builds.
6. Start Stage 1 and confirm the stage-start hint disappears after a few seconds.
7. Confirm swipe and left/right half-screen tap change lanes without triggering UI buttons.
8. Confirm Pause button is easy to hit and does not overlap the HUD or notch area.
9. Confirm Failed/Completed screens never auto-restart.
10. Confirm clearing Stage 1 with at least 1 star unlocks Stage 2.
11. Open Playtest Stats and confirm Attempts/Clears/Fails/Best Score/Last Score update.
12. Check Logcat for no `Can't add component because 'BoxCollider' doesn't exist!` and no pink procedural materials.

## PC Build Smoke Test

1. Start from MainMenu and go to StageSelect.
2. Start Stage 1 and verify player, track, shards, obstacles, gates, finish, VFX, and HUD render.
3. Test `A/D`, arrow keys, `ESC/P`, `R`, and `M`.
4. Clear and fail at least one stage, then verify result buttons.
5. Restart the build and confirm progress and Playtest Stats persist locally.
6. Start Endless Mode, fail intentionally by obstacle and by three wrong-color shards, and confirm best score/distance persist after restarting the build.

## Stage Sampling

- Stage 1: first 30 seconds, tutorial, collection clarity, obstacle rarity.
- Stage 2-5: early unlock rhythm and 1-star progression.
- Stage 10: mid-campaign speed and obstacle pressure.
- Stage 20: advanced gate frequency and route readability.
- Stage 30: end-campaign strict 3-star target and visual clarity.
- Endless Mode: run for at least 2 minutes and check speed ramp, row spacing readability, rolling generation, pause, retry, MainMenu return, and no object buildup.
- Wrong-shard rule: in both Stage and Endless Mode, collect two wrong-color shards and confirm the run continues; collect a third and confirm game over with the wrong-shard limit reason.
- HUD regression: confirm both Stage and Endless HUDs show three chance icons and update them immediately after each wrong-color shard.

## Save, Unlock, And Stats

- Reset Local Progress affects stage unlocks, best stars, selected stage, and tutorial seen.
- Reset Playtest Stats affects only `CGR_Stats_` counters.
- Failing awards 0 stars and does not unlock the next stage.
- Clearing awards at least 1 star and unlocks the next stage.
- Best stars and best score do not downgrade.
- Pause to MainMenu/StageSelect or pause retry records a quit, not a fail.
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
- Verify the Music slider can be dragged smoothly from 0% to 100% and that Music Off stops only BGM.
- Verify the SFX slider can be dragged smoothly from 0% to 100% and that SFX Off stops only one-shot sounds.
- Do not add more BGM/SFX before this playtest pass finishes.
- After gameplay feedback, consider a focused Audio Quality Sprint for device mix tuning, optional SFX replacement, and license/manifest tracking for any additional audio files.
