# Game Design — Color Gate Rush

## 1. Concept

**Color Gate Rush**는 30~45초 세션을 목표로 하는 3-lane hyper-casual runner입니다. 플레이어는 자동 전진하는 컬러 볼을 좌우로 움직이며 같은 색 샤드를 모읍니다. 중간중간 컬러 게이트가 플레이어 색을 바꾸므로, 다음 수집 대상도 바뀝니다. 장애물에 부딪히면 실패하거나 점수가 크게 감소하고, 피니시를 통과하면 현재 점수로 별점이 확정됩니다.

## 2. Why this game is Codex-automatable

- **단일 핵심 입력**: 좌우 이동만 필요합니다.
- **단순 충돌 규칙**: collect, gate, obstacle, finish 네 가지 trigger만 있으면 됩니다.
- **절차 생성 친화적**: 트랙, 샤드, 게이트, 장애물 모두 규칙 기반 배치가 가능합니다.
- **외부 에셋 불필요**: 구, 큐브, 캡슐, 파티클, 머티리얼만으로 충분합니다.
- **테스트 쉬움**: 에디터에서 WASD/Arrow, 모바일에서 swipe/tap으로 검증 가능합니다.

## 3. Core Loop

1. Player starts from Main Menu.
2. Main Menu Start opens Stage Select instead of starting a run.
3. Selecting an unlocked stage starts that stage.
4. Player moves forward at constant speed during Playing.
5. Stage 1 first entry shows a short Korean tutorial once.
6. Player can pause during Playing and resume the same run.
7. Same-color shard increases score and combo with floating score feedback.
8. Wrong-color shard breaks combo or subtracts score.
9. Gate changes player color and current target shape.
10. Obstacle causes fail or heavy penalty.
11. Finish line clears the stage and locks in the current score.
12. Result is shown until the player explicitly chooses Retry, Stage Select, Main Menu, or Next Stage.

## 4. Controls

- Desktop test: `A/LeftArrow` = left lane, `D/RightArrow` = right lane.
- Pause: `ESC` or `P` toggles pause/resume during gameplay.
- Pause shortcuts: `R` retries the current stage, `M` returns to Main Menu.
- Mobile: horizontal swipe changes lane.
- Optional alternate: left/right half-screen tap.
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
| Audio | Runtime sine waves and short loops | Menu/gameplay BGM, collect/gate/hit/result | `AudioClip.Create` |
| Player accent | Color-specific primitive shape | Shows current target color/shape without text overlay | Primitive Sphere/Cube/Capsule |

## 6. Scoring

- Same-color shard: +10 × comboMultiplier.
- Combo increases every successful shard, capped at 10.
- Wrong-color shard: -15 and combo reset.
- Gate: +5 feedback score, changes player color.
- Finish: clears the stage; star rating uses the current score shown in the HUD.
- Combo is reset by wrong-color shards and obstacles.

## 6.1 Feedback and Accessibility

- Successful collection uses colored particles, a small sparkle layer, procedural SFX, and floating score text.
- Wrong-color shards and obstacles use red/warning fail particles, shock rings, buzz SFX, and clear HUD messages.
- Gates display a short color-change message, a small primitive target marker, a pulse burst, and a rising procedural tone.
- Menu and gameplay screens use separate lightweight procedural BGM loops; completed/failed screens use short stings instead of external audio files.
- Every gameplay color has a paired shape: Cyan 구슬, Magenta 큐브, Yellow 캡슐, Lime 다이아.
- Shards, player accent, and the HUD use the same color/shape source of truth; no black TextMesh symbols are placed above shards or the player.
- Settings include Music, SFX, volume steps, Camera Shake, and Color Assist controls saved with `CGR_` PlayerPrefs keys.

## 7. Level Generation Rules

- 3 lanes: x = -2.2, 0, +2.2.
- Track length: about 160–370 units across the 30-stage campaign.
- Shards/obstacles are arranged in deterministic decision rows, with row count rising by stage.
- Gate spacing starts wide and tightens toward advanced stages, about 62 down to 30 z units.
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
- `StageScoreAnalyzer` estimates a route-aware maximum score from generated row/lane data, combo, wrong-color penalties, gate score, and row-spacing-based lane movement.
- A row is one decision; even if multiple matching shards appear in that row, the analyzer counts at most one lane choice and at most one shard pickup.
- Balance reports compare naive matching-shard score against route-aware max score so impossible 3-star targets are caught.
- 2-star targets are always the rounded-up two-thirds point of the 3-star target.
- 3-star targets are roughly 93-98% of the route-aware maximum score, clamped below the estimated maximum.
- Stage 1-3 allow about two mistakes for 3 stars; later stages allow only one or nearly none in practice.
- The playing HUD shows `★1: 피니시`, 2-star and 3-star targets, and the remaining score needed for 3 stars before the finish.
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
- Unsafe row repair favors collectible matching shards on Stage 1-2 before falling back to empty lanes.
- Level generation produces a report that validator smoke tests can inspect.

## 12. Build Readiness

- Validate Build, Generate Balance Report, and Generate Release Readiness Report must pass before packaging.
- Stage 1-30 should be manually sampled for unlock, score, pause, retry, and result-screen regressions.
- No external art/audio/model/font/prefab assets are allowed under `Assets/_Project`.
- Android and WebGL builds are manual Unity Editor tasks; automation maintains only static checks, validators, and checklists.
- APK is for local Android testing, AAB is for Google Play submission, and keystore/signing files stay outside the repository.
- WebGL testing must include browser persistence, resize behavior, first-input audio unlock, and keyboard/mouse/touch checks.
