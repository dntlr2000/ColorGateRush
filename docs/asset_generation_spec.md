# Procedural Asset Generation Spec

## Hard rule

No external art, audio, font, model, video, or paid/free Asset Store package is required for the MVP.

## Geometry

| Asset | Generation method | Notes |
|---|---|---|
| Player | `GameObject.CreatePrimitive(PrimitiveType.Sphere)` | Rigidbody kinematic, sphere collider |
| Shard | Sphere | Trigger collider, color-coded |
| Gate | Cubes | One transparent trigger per lane, visible arch frame |
| Obstacle | Cube | Trigger collider or blocking wall |
| Track | Cube segments | Long rectangular slabs |
| Finish | Cube trigger + arch | Clear visual endpoint |
| Color symbols | TextMesh | Built-in shape symbols paired with each color |

## Materials

Use code-created materials with shader fallback:

1. `Universal Render Pipeline/Lit`
2. `Standard`
3. Any available fallback shader

Palette:

- Cyan: `#00D7FF`
- Magenta: `#FF3AF2`
- Yellow: `#FFE84A`
- Lime: `#8CFF4A`
- Track: dark navy/charcoal
- Obstacle: red/orange
- Finish: white/gold

Transparent gate material:

- Alpha around 0.35.
- Enable blending when shader supports it.
- Still keep a solid arch/frame so gates are visible.

## Particles

Create runtime ParticleSystem bursts:

- Collect: small colored burst.
- Gate: vertical ring-like burst or short shower.
- Fail: red burst.
- Finish: larger white/gold burst.
- Floating score: pooled runtime `TextMesh` feedback.

Use short lifetime and low particle counts for mobile.

## Audio

Use runtime-generated clips:

- Collect: short high sine tone.
- Gate: rising tone.
- Fail: low buzz/noise-like tone.
- Finish: simple arpeggio.

Implementation target: `AudioClip.Create` with decay envelope. No `.wav`, `.mp3`, or imported audio.
Sound playback is controlled by `CGR_SoundEnabled`.

## UI

Generate a Canvas and basic text at runtime:

- Score
- Combo
- Current color
- Stage number and `★1/★2/★3` targets during play
- Pause button anchored to the screen top-right
- Settings screen for Sound, Camera Shake, Color Assist, and guarded Reset Progress
- Stage 1 first-run tutorial panel
- State/result message
- Optional seed/debug string

Use built-in uGUI for MVP.

Result screens remain open until explicit player input through Retry, Stage Select, Main Menu, or Next Stage buttons. They do not use timer-based or global tap restart.

## Accessibility

Each `ColorId` has one source-of-truth color name and symbol:

- Cyan: `●`
- Magenta: `■`
- Yellow: `◆`
- Lime: `▲`

Color Assist / high contrast mode uses the same symbols and an alternate procedural palette. Camera shake feedback is controlled by `CGR_CameraShake`. No sprite, image, model, or font import is required.

## Stage and fairness data

No external data file is required for stages. Stage configuration is generated in C# and includes:

- stage index and deterministic seed;
- track length and shard row count;
- obstacle, matching shard, off-color shard, safe-empty-lane chances, and gate interval;
- available color count;
- 2-star and 3-star score targets;
- player forward speed and lane movement speed.

Fair generation rules:

- mandatory full-width color gates make the next expected player color predictable;
- shards and obstacles in one row share the exact same row z coordinate with no z jitter;
- expected-color shards are probability-driven and are not required on every row;
- Stage 1-2 target shard-rich, obstacle-light rows so the first sessions emphasize collection;
- Stage 1 should keep obstacles rare while showing shards in most rows;
- every row must leave at least one safe option: empty lane, expected-color shard, or other neutral object;
- rows with three off-color shards are invalid;
- obstacle rows must never block all three lanes;
- mixed off-color shard plus obstacle rows that make all three lanes unsafe are invalid;
- early stages reduce matching-shard overpopulation by favoring safe empty lanes over guaranteed matching shards;
- early unsafe-row repair can replace an obstacle or off-color shard with a matching shard before using an empty lane;
- validator reports shard rows, empty rows, obstacle rows, total shards, total obstacles, average shards per row, obstacle row ratio, and matching shard row ratio;
- validator smoke tests generate all MVP stages without saving imported art/audio/model assets.
