# Game Design — Color Gate Rush

## 1. Concept

**Color Gate Rush**는 30~45초 세션을 목표로 하는 3-lane hyper-casual runner입니다. 플레이어는 자동 전진하는 컬러 볼을 좌우로 움직이며 같은 색 샤드를 모읍니다. 중간중간 컬러 게이트가 플레이어 색을 바꾸므로, 다음 수집 대상도 바뀝니다. 장애물에 부딪히면 실패하거나 점수가 크게 감소하고, 피니시를 통과하면 현재 점수로 별점이 확정됩니다.

## 2. Why this game is Codex-automatable

- **단일 핵심 입력**: 좌우 이동만 필요합니다.
- **단순 충돌 규칙**: collect, gate, obstacle, finish 네 가지 trigger만 있으면 됩니다.
- **절차 생성 친화적**: 트랙, 샤드, 게이트, 장애물 모두 규칙 기반 배치가 가능합니다.
- **외부 다운로드 에셋 불필요**: gameplay는 구, 큐브, 캡슐, 파티클, 머티리얼만으로 구성합니다. 메인 메뉴만 승인된 사용자 제공 배경 이미지를 `Resources`에서 사용합니다.
- **테스트 쉬움**: 모바일 기준 swipe/tap 조작을 중심으로 검증하며, PC 키보드 입력은 개발용 보조 입력으로만 유지합니다.

## 3. Core Loop

1. Player starts on a full-screen title image.
2. Tapping/clicking the title screen opens Main Menu.
3. Main Menu Start opens Stage Select instead of starting a run.
4. Main Menu Endless Mode starts an independent record run.
5. Main Menu Quit requests application exit on Android/PC builds; Editor/WebGL are handled safely.
6. Selecting an unlocked stage starts that stage.
7. Player moves forward during Playing, with higher campaign speed paired with wider row spacing for preserved reaction time.
8. Stage 1 first entry shows a short localized tutorial once.
9. Player can pause during Playing and resume the same run.
10. Same-color shard increases score and combo with floating score feedback.
11. Stage Mode wrong-color shards use the same three-chance failure rule as Endless Mode.
12. Gate changes player color and current target shape.
13. Obstacle causes fail or heavy penalty.
14. Finish line clears finite stages and locks in the current score.
15. Endless Mode has no finish, stars, or unlocks; it keeps getting faster and harder until failure and saves best score/distance.
16. Result is shown until the player explicitly chooses Retry, Stage Select, Main Menu, or Next Stage.
17. Release playtests use the manual checklist; the in-game Playtest Stats screen and `CGR_Stats_` telemetry are not exposed in the release candidate.

## Localization

- Runtime UI supports Korean and English through the lightweight code-based `LocalizationManager`.
- The selected language is stored in `CGR_Language`; Reset Progress does not delete this setting.
- Settings exposes Korean / English buttons and refreshes the currently open UI immediately when the language changes.
- Runtime UI text should be added through `LocalizationKey` before it is displayed. Do not add the Unity Localization package, external fonts, or downloaded localization assets.
- Validator checks both dictionaries for missing keys and mismatched format placeholders.

## 4. Controls

- Mobile primary: horizontal swipe or left/right half-screen tap changes lane.
- Desktop test fallback: left/right keys remain available for editor and PC smoke tests.
- Pause: `ESC` or `P` toggles pause/resume during gameplay.
- Pause shortcuts: `R` retries the current stage, `M` returns to Main Menu.
- Android Back/Escape: gameplay pauses; submenus return to Main Menu.
- UI buttons are ignored by lane-touch input through EventSystem checks.

## 5. Objects

| Object | Shape | Behavior | Asset source |
|---|---|---|---|
| Player | Sphere | Auto-run, lane switch, color state | Primitive Sphere |
| Track | Long Cube segments | Visual ground | Primitive Cube |
| Track polish | Rails, separators, rhythm stripes | Lane readability and forward rhythm | Visual-only Primitive Cubes |
| Shard | Color-specific primitive shape | Score if same color/shape | Primitive Sphere/Cube/Capsule |
| Gate | Transparent Cube trigger + arch | Changes player color | Primitive Cubes |
| Obstacle | Warning block + stripe/spike accents | Fail or penalty | Primitive Cubes |
| Finish | Trigger plane + arch/checker strip | Ends run | Primitive Cubes |
| Particles | ParticleSystem | Collect/gate/fail/finish feedback | Built-in ParticleSystem |
| Audio | Approved user BGM + runtime sine waves | Menu/gameplay BGM, collect/gate/hit/result | Resources AudioClip + `AudioClip.Create` |
| Player accent | Color-specific primitive shape | Shows current target color/shape without text overlay | Primitive Sphere/Cube/Capsule |

## 6. Scoring

- Same-color shard: +10 × comboMultiplier.
- Combo increases every successful shard, capped at 10.
- Wrong-color shard: -15, combo reset, and one strike toward the 3-strike game over.
- Gate: +5 feedback score, changes player color.
- Finish: clears the stage; star rating uses the current score shown in the HUD.
- Combo is reset by obstacles and by wrong-color shard strikes; the third wrong-color shard ends the run.

## 6.1 Feedback and Accessibility

- Successful collection uses colored particles, a small sparkle layer, procedural SFX, and floating score text.
- Wrong-color shards and obstacles use red/warning fail particles, shock rings, buzz SFX, and clear failure messages.
- Gates display a short color-change message, a small primitive target marker, a pulse burst, and a rising procedural tone.
- Menu and gameplay screens use the approved user-provided BGM clips from `Resources/ColorGateRush/Audio`; completed/failed screens use short procedural stings.
- Every gameplay color has a paired shape: Cyan 구슬, Magenta 큐브, Yellow 캡슐, Lime 다이아.
- Shards, player accent, and the HUD use the same color/shape source of truth; no black TextMesh symbols are placed above shards or the player.
- Settings include Music, SFX, fine Music/SFX volume sliders, Camera Shake, and Color Assist controls saved with `CGR_` PlayerPrefs keys.
- SFX remain procedural for now; any future third-party or replacement audio must be tracked separately before inclusion.

## 6.2 Release Playtest Readiness

- The release candidate removes the in-game Playtest Stats screen and no longer writes new `CGR_Stats_` telemetry.
- Playtesting is driven by `docs/playtest_checklist.md`, Balance Report, Validate Build, Validate Runtime Visuals, and Release Readiness Report.
- Settings is split into General, Language, and Data sections so language/audio options are visually separate from destructive reset actions.
- Reset Stage Progress affects only stage unlock/star/selection progress.
- Reset Endless Records affects only `CGR_Endless...` record keys.
- Language, BGM/SFX, camera shake, and color-assist settings survive progress resets.

## 6.3 Endless Mode MVP

- Entry: Main Menu `Endless Mode`.
- Rules: no finish line, no stars, no stage unlock writes.
- Objective: run until obstacle failure or until three wrong-color shards are collected while building score and distance.
- Difficulty starts around Stage 3-5 pressure and ramps by elapsed time plus distance through forward speed, row spacing, obstacle chance, off-color pressure, and gate interval.
- Forward speed keeps increasing through an Endless-specific speed formula; `Time.timeScale` is used only for pause/resume.
- Row spacing and lane-move sharpness rise with speed so the mode feels faster without becoming instantly unreadable.
- Wrong-color shards are three strikes in both finite stages and Endless Mode: 0/3, 1/3, 2/3 continue; 3/3 ends the run.
- Finite Stage Mode still uses stars, finish, and unlocks; Endless Mode remains independent from stars and unlocks.
- Generation uses rolling rows/chunks ahead of the player and cleans chunks behind the player so objects do not accumulate forever.
- Fair row invariant still applies: no all-obstacle row, no all-off-color row, no mixed all-unsafe row, and at least one safe option.
- Saved records: `CGR_EndlessBestScore`, `CGR_EndlessBestDistance`, `CGR_EndlessBestRows`, `CGR_EndlessAttempts`, `CGR_EndlessTotalRuns`.
- Each Endless run receives a fresh per-run seed while pause/resume keeps the same run sequence.
- Reset Endless Records is separate from Reset Local Progress.

## 7. Level Generation Rules

- 3 lanes: x = -2.2, 0, +2.2.
- Track length: about 185–452 units across the 30-stage campaign after the speed/spacing pass.
- Shards/obstacles are arranged in deterministic decision rows, with row count rising by stage.
- Gate spacing starts wide and tightens toward advanced stages, about 70 down to 36 z units.
- Row spacing rises with speed so expected reaction time stays playable as forward speed increases.
- Obstacle every 12–20 z units, never directly after a gate.
- Shards and obstacles are generated on exact shared row z positions.
- Matching shards are not forced on every row.
- Stage 1-2 are shard-rich and obstacle-light so collection is the main early emotion.
- Stage 1 targets roughly 75-90% shard rows, rare obstacles, and enough empty lanes for recovery.
- Ensure at least one safe option per row.
- Use deterministic `seed` for reproducible QA.

## 8. Visual Style

- Theme: clean candy neon, soft sci-fi, minimal arcade.
- `VisualTheme` is the source of truth for background, fog, track, obstacle, finish, HUD, and VFX colors.
- Stages cycle through five code-defined theme variations for background, track accent, HUD accent, gate, and finish tone.
- Background: softened blue-violet Camera color, fog, and procedural backdrop/side panels instead of the default Unity skybox.
- Track: darker blue/charcoal base with side rails, lane separators, edge glow, side light strips, and rhythm stripes for object contrast.
- Colors: cyan, magenta, yellow, lime.
- Shapes: each collectible color has a distinct primitive silhouette.
- Shards: glossy/emissive material, soft glow shell, subtle bob/spin, and short collect burst.
- Obstacles: danger red blocks with warning-yellow stripes and spike silhouettes.
- Gates: positive transparent color panels, arch frames, target shape marker, and approach/exit cue strips.
- Finish: gold arch, primitive checker strip, and clear burst.
- HUD: theme-driven translucent panels, shadowed text, and accent buttons for readability.
- Post-processing: do not require external assets or URP-specific compile dependencies; camera background, fog, ambient light, and directional light are the safe fallback tone pass.
- Camera: third-person elevated follow, portrait friendly.

## 9. MVP Scope

### Must have

- One generated playable scene.
- Player auto-run and lane switch.
- Color match collection.
- Color gate.
- Obstacle and failure/restart.
- Finish and result overlay.
- Procedural materials, VFX, SFX.

### Should have

- Progressive speed increase.
- Combo feedback.
- Seed text in debug overlay.
- Editor bootstrap menu.
- Build validator menu.

### Not in MVP

- External ad SDK.
- Real analytics SDK.
- Account/login.
- Remote config.
- Asset Store packages.

## 10. Stage Progression

- Target Unity: Unity 6 / 6000.x.
- MVP content now includes 30 deterministic stages generated from C# `StageConfig` data.
- Stage 1 is unlocked by default.
- Stage 1 teaches collection first: obstacles are rare, shard density is high, and gates are spaced out.
- Stage 2-3 introduce slightly more off-color shards and obstacles without a difficulty spike.
- Stage 4-10 increase gates, speed, length, and obstacle density gradually.
- Stage 11-20 are the middle tier with longer tracks, faster speed, more off-color pressure, and shorter gate intervals.
- Stage 21-30 are advanced stages with the shortest gate spacing and strictest 3-star margins.
- Clearing a stage awards at least 1 star.
- Score targets award 2 or 3 stars.
- `StageScoreAnalyzer` estimates a route-aware maximum score from generated row/lane data, combo, gate score, row-spacing-based lane movement, and wrong-shard count state; routes fail only when the third wrong shard is reached.
- A row is one decision; even if multiple matching shards appear in that row, the analyzer counts at most one lane choice and at most one shard pickup.
- Balance reports compare naive matching-shard score against route-aware max score so impossible 3-star targets are caught.
- 2-star targets are always the rounded-up two-thirds point of the 3-star target.
- 3-star targets are roughly 93-98% of the route-aware maximum score, clamped below the estimated maximum.
- Stage 1-3 allow about two mistakes for 3 stars; later stages allow only one or nearly none in practice.
- The playing HUD shows `★1: 피니시`, 2-star and 3-star targets, the remaining score needed for 3 stars, and wrong-shard chance icons before the finish.
- Any clear with at least 1 star unlocks the next stage.
- 3 stars remain a near-perfect-play challenge target, not the unlock gate.
- Pausing does not calculate stars, save progress, or unlock stages.
- Automatic restart is disabled; transitions require explicit buttons or keyboard shortcuts.
- Settings can reset only Color Gate Rush progress keys.
- Saved progress uses PlayerPrefs keys:
  - `CGR_UnlockedStage`
  - `CGR_StageStars_{stageIndex}`
  - `CGR_SelectedStage`
  - `CGR_TutorialSeen`
  - `CGR_SoundEnabled` (legacy compatibility)
  - `CGR_MusicEnabled`
  - `CGR_SfxEnabled`
  - `CGR_MusicVolume`
  - `CGR_SfxVolume`
  - `CGR_CameraShake`
  - `CGR_HighContrast`

## 11. Fair Generation Rules

- Shards and obstacles are placed in three-lane rows with one shared row z coordinate.
- Each row is one lane choice for scoring analysis; route-aware max never assumes multiple same-row shards can all be collected.
- Matching shards are probability-driven and are not required on every row.
- Shard density is still targeted so early rows do not feel empty or punishment-led.
- Full-width mandatory gates update the expected player color for later shard rows.
- A row containing only three off-color shards is invalid.
- Obstacle generation must never block all lanes in one row.
- Mixed rows where off-color shards and obstacles make all three lanes unsafe are invalid.
- Empty lanes and expected-color shards count as safe options.
- Off-color shards are unsafe/mistake choices because the third wrong shard fails the run.
- Unsafe row repair favors collectible matching shards on Stage 1-2 before falling back to empty lanes.
- Level generation produces a report that validator smoke tests can inspect.

## 12. Build Readiness

- Validate Build, Generate Balance Report, and Generate Release Readiness Report must pass before packaging.
- Stage 1-30 should be manually sampled for unlock, score, pause, retry, and result-screen regressions.
- No unapproved external art/audio/model/font/prefab assets are allowed under `Assets/_Project`; the allowlist contains the approved BGM clips, `Resources/ColorGateRush/Images/TitleScreen.png`, and `Resources/ColorGateRush/Images/MainMenuBackground.png`.
- Android and WebGL builds are manual Unity Editor tasks; automation maintains only static checks, validators, and checklists.
- APK is for local Android testing, AAB is for Google Play submission, and keystore/signing files stay outside the repository.
- WebGL testing must include browser persistence, resize behavior, first-input audio unlock, and keyboard/mouse/touch checks.
