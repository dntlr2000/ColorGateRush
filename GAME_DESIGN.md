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

1. Run starts automatically.
2. Player moves forward at constant speed.
3. Player changes lane left/right.
4. Same-color shard increases score and combo.
5. Wrong-color shard breaks combo or subtracts score.
6. Gate changes player color.
7. Obstacle causes fail or heavy penalty.
8. Finish line converts score into multiplier.
9. Result is shown for 2 seconds, then next seed starts.

## 4. Controls

- Desktop test: `A/LeftArrow` = left lane, `D/RightArrow` = right lane.
- Mobile: horizontal swipe changes lane.
- Optional alternate: left/right half-screen tap.

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

## 6. Scoring

- Same-color shard: +10 × comboMultiplier.
- Combo increases every successful shard, capped at 10.
- Wrong-color shard: -15 and combo reset.
- Gate: +5 feedback score, changes player color.
- Finish multiplier: `1 + floor(score / 250)`, capped at 10.

## 7. Level Generation Rules

- 3 lanes: x = -2.2, 0, +2.2.
- Track length: 180–220 units.
- Shards every 5–7 z units.
- Gate every 28–36 z units.
- Obstacle every 12–20 z units, never directly after a gate.
- Ensure at least one safe lane per row.
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
