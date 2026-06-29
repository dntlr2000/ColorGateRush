# Game Design — Color Gate Rush

## 1. Concept

**Color Gate Rush**는 30~45초 세션을 목표로 하는 3-lane hyper-casual runner입니다. 플레이어는 자동 전진하는 컬러 볼을 좌우로 움직이며 같은 색 샤드를 모읍니다. 중간중간 컬러 게이트가 플레이어 색을 바꾸므로, 다음 수집 대상도 바뀝니다. 장애물에 부딪히면 실패하거나 점수가 크게 감소하고, 피니시를 통과하면 점수 배수가 적용됩니다.

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
9. Gate changes player color and current target symbol.
10. Obstacle causes fail or heavy penalty.
11. Finish line converts score into multiplier.
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
| Shard | Small Sphere | Score if same color | Primitive Sphere |
| Gate | Transparent Cube trigger + arch | Changes player color | Primitive Cubes |
| Obstacle | Cube/Wall | Fail or penalty | Primitive Cube |
| Finish | Trigger plane + arch | Ends run | Primitive Cubes |
| Particles | ParticleSystem | Collect/gate/fail/finish feedback | Built-in ParticleSystem |
| Audio | Runtime sine waves | Collect/gate/hit/finish | `AudioClip.Create` |
| Symbols | TextMesh | Color-assist shape symbols | Built-in TextMesh |

## 6. Scoring

- Same-color shard: +10 × comboMultiplier.
- Combo increases every successful shard, capped at 10.
- Wrong-color shard: -15 and combo reset.
- Gate: +5 feedback score, changes player color.
- Finish multiplier: `1 + floor(score / 250)`, capped at 10.
- Combo is reset by wrong-color shards and obstacles.

## 6.1 Feedback and Accessibility

- Successful collection uses particles, procedural SFX, and floating score text.
- Wrong-color shards and obstacles use red fail particles, buzz SFX, and clear HUD messages.
- Gates display a short color-change message and target symbol.
- Every gameplay color has a paired symbol: Cyan ●, Magenta ■, Yellow ◆, Lime ▲.
- Settings include Sound, Camera Shake, and Color Assist toggles saved with `CGR_` PlayerPrefs keys.

## 7. Level Generation Rules

- 3 lanes: x = -2.2, 0, +2.2.
- Track length: 180–220 units.
- Shards every 5–7 z units.
- Gate every 28–36 z units.
- Obstacle every 12–20 z units, never directly after a gate.
- Shards and obstacles are generated on exact shared row z positions.
- Matching shards are not forced on every row.
- Stage 1-2 are shard-rich and obstacle-light so collection is the main early emotion.
- Stage 1 targets roughly 75-90% shard rows, rare obstacles, and enough empty lanes for recovery.
- Ensure at least one safe option per row.
- Use deterministic `seed` for reproducible QA.

## 8. Visual Style

- Theme: neon candy, clean mobile ad prototype.
- Background: flat gradient-like sky color through Camera background and fog.
- Track: dark blue/charcoal base with lane strips.
- Colors: cyan, magenta, yellow, lime.
- Shapes: rounded impression through spheres and scaled cubes.
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
- MVP includes 10 deterministic stages generated from C# `StageConfig` data.
- Stage 1 is unlocked by default.
- Stage 1 teaches collection first: obstacles are rare, shard density is high, and gates are spaced out.
- Stage 2-3 introduce slightly more off-color shards and obstacles without a difficulty spike.
- Stage 4+ increases gates, speed, length, and obstacle density gradually.
- Clearing a stage awards at least 1 star.
- Score targets award 2 or 3 stars.
- The playing HUD shows `★1: 피니시`, plus the current stage's 2-star and 3-star score targets before the finish.
- Only a 3-star clear unlocks the next stage.
- Pausing does not calculate stars, save progress, or unlock stages.
- Automatic restart is disabled; transitions require explicit buttons or keyboard shortcuts.
- Settings can reset only Color Gate Rush progress keys.
- Saved progress uses PlayerPrefs keys:
  - `CGR_UnlockedStage`
  - `CGR_StageStars_{stageIndex}`
  - `CGR_SelectedStage`
  - `CGR_TutorialSeen`
  - `CGR_SoundEnabled`
  - `CGR_CameraShake`
  - `CGR_HighContrast`

## 11. Fair Generation Rules

- Shards and obstacles are placed in three-lane rows with one shared row z coordinate.
- Matching shards are probability-driven and are not required on every row.
- Shard density is still targeted so early rows do not feel empty or punishment-led.
- Full-width mandatory gates update the expected player color for later shard rows.
- A row containing only three off-color shards is invalid.
- Obstacle generation must never block all lanes in one row.
- Mixed rows where off-color shards and obstacles make all three lanes unsafe are invalid.
- Empty lanes and expected-color shards count as safe options.
- Unsafe row repair favors collectible matching shards on Stage 1-2 before falling back to empty lanes.
- Level generation produces a report that validator smoke tests can inspect.
