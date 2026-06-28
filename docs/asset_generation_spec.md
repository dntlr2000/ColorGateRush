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

Use short lifetime and low particle counts for mobile.

## Audio

Use runtime-generated clips:

- Collect: short high sine tone.
- Gate: rising tone.
- Fail: low buzz/noise-like tone.
- Finish: simple arpeggio.

Implementation target: `AudioClip.Create` with decay envelope. No `.wav`, `.mp3`, or imported audio.

## UI

Generate a Canvas and basic text at runtime:

- Score
- Combo
- Current color
- State/result message
- Optional seed/debug string

Use built-in uGUI for MVP.
