# Procedural Asset Generation Spec

## Hard rule

No external art, audio, font, model, prefab, video, or paid/free Asset Store package is required for the MVP.

## Geometry

| Asset | Generation method | Notes |
|---|---|---|
| Player | Procedural mesh object | Rigidbody kinematic, explicit sphere collider |
| Shard | Color-specific primitive shape | Trigger collider, color-coded and silhouette-coded |
| Shard glow | Sphere child | Non-colliding translucent shell |
| Gate | Cubes | Full-width transparent trigger, arch frame, shape marker, cue strips |
| Obstacle | Cube + visual details | Trigger collider plus non-colliding warning stripes/spikes |
| Track | Cube segments | Slabs plus visual-only rails, separators, edge glow, rhythm stripes |
| Background | Cubes | Backdrop panels and side glow panels |
| Finish | Cube trigger + arch/checkers | Clear endpoint with primitive checker strip |
| Player accent | Color-specific primitive shape | Non-colliding current color/shape accent near the player |

## Materials

Use code-created material instances cloned from project-owned base materials through `RuntimeMaterialProvider`:

1. `Assets/_Project/Resources/ColorGateRush/Materials/CGR_UnlitOpaque.mat`
2. `Assets/_Project/Resources/ColorGateRush/Materials/CGR_UnlitTransparent.mat`
3. `Assets/_Project/Resources/ColorGateRush/Materials/CGR_ParticleUnlit.mat`

These material assets reference only limited URP shader variants:

- `Universal Render Pipeline/Unlit` for procedural world geometry.
- `Universal Render Pipeline/Particles/Unlit` for ParticleSystem feedback.

Palette:

- Cyan: `#00D7FF`
- Magenta: `#FF3AF2`
- Yellow: `#FFE84A`
- Lime: `#8CFF4A`
- Track: dark navy/charcoal
- Obstacle: red/orange
- Finish: white/gold

`VisualTheme` is the source of truth for background, fog, track, obstacle, finish, HUD, and VFX colors. High contrast mode returns an alternate code-defined theme without creating ScriptableObject assets. Stage configs cycle through five code-defined theme variations by `themeIndex`.

Required URP shaders are included through Resources material asset references, not by adding full URP shaders to Graphics Settings Always Included Shaders. `Universal Render Pipeline/Lit` must not be added to Always Included Shaders because it can generate too many Android shader variants. Runtime gameplay code should not scatter direct shader lookup calls outside `RuntimeMaterialProvider`; provider fallback lookup exists only for diagnostics when the Resources material assets are missing.

If a later polish pass needs manual shader variant control, use a ShaderVariantCollection containing only the exact variants used by the game. Do not include the entire URP/Lit shader.

Transparent gate material:

- Alpha around 0.35.
- Enable blending when shader supports it.
- Still keep a solid arch/frame so gates are visible.

## Runtime Components

- `AddComponent(string)` is forbidden.
- `GameObject.CreatePrimitive` is forbidden for generated runtime gameplay/visual objects.
- Procedural objects are created with `GameObject`, `MeshFilter`, `MeshRenderer`, explicit material assignment, and generic collider helpers.
- Use `ProceduralFactory.EnsureBoxCollider`, `EnsureSphereCollider`, and `EnsureCapsuleCollider` for collider creation.
- Visual-only geometry must use `VisualPrimitive`, which disables colliders after creation.
- Gameplay triggers must keep explicit trigger colliders.
- Generated objects must have active renderers, non-null meshes, supported materials, and non-zero scale.
- Run `Tools/Color Gate Rush/Validate Runtime Visuals` before Android/PC build smoke tests.

## Particles

Create runtime ParticleSystem bursts:

- Collect: small colored burst plus white sparkle and a tiny ring.
- Gate: vertical ring-like burst plus short color burst and white pulse ring.
- Fail: red/warning shock burst plus compact impact ring.
- Finish: larger gold/white burst plus ring and bright sparkle burst.
- Floating score: pooled runtime `TextMesh` feedback.

Use short lifetime and low particle counts for mobile.

## Lighting and tone

- Camera background color, fog color, ambient light, and directional light come from `VisualTheme`.
- `RenderSettings.skybox = null` removes the default Unity skybox feel.
- `Tools/Color Gate Rush/Apply Visual Theme` can reapply the code-defined scene tone.
- URP Volume/post-processing is optional; the MVP uses safe fallback visual settings to avoid compile risk when URP APIs differ.
- Bloom/vignette/motion blur are not required for readability and mobile performance.

## Audio

Use runtime-generated clips:

- Collect: short high sine tone.
- Gate: rising tone.
- Fail: low buzz/noise-like tone.
- Finish: simple arpeggio.
- Menu BGM: gentle short loop generated in code.
- Gameplay BGM: stage-tier loop generated in code with small tempo/pitch variation.
- Completed/Failed stings: short non-looped generated clips.

Implementation target: `AudioClip.Create` with envelopes and simple oscillators. No `.wav`, `.mp3`, `.ogg`, or imported audio.
Music and SFX playback are controlled independently by `CGR_MusicEnabled`, `CGR_SfxEnabled`, `CGR_MusicVolume`, and `CGR_SfxVolume`. The legacy `CGR_SoundEnabled` key is retained only for compatibility.

## UI

Generate a Canvas and basic text at runtime:

- Score
- Combo
- Current color
- Current color and shape label
- Stage number and `★1/★2/★3` targets during play
- Score remaining to the 3-star target during play
- Top-left HUD contrast panel with text shadow
- Theme-driven menu/pause/result panels and accent buttons
- Pause button anchored to the screen top-right
- Settings screen for Music, Music Volume, SFX, SFX Volume, Camera Shake, Color Assist, and guarded Reset Progress
- Stage 1 first-run tutorial panel
- State/result message
- Short stage-start toast that auto-hides after a few seconds and does not change game state
- Scrollable 30-stage selection grid with locked/unlocked/star states
- Optional seed/debug string

Use built-in uGUI for MVP.

Result screens remain open until explicit player input through Retry, Stage Select, Main Menu, or Next Stage buttons. They do not use timer-based or global tap restart. Gameplay does not keep persistent center guide text; detailed rules live in Rules and compact star/score data remains in the HUD.

Release validation treats imported Texture2D, AudioClip, Model, Font, and Prefab assets under `Assets/_Project` as hard failures. All HUD/menu/result UI is generated at runtime.

## Accessibility

Each `ColorId` has one source-of-truth color name and primitive shape:

- Cyan: Sphere / `구슬`
- Magenta: Cube / `큐브`
- Yellow: Capsule / `캡슐`
- Lime: Diamond-like rotated Cube / `다이아`

Color Assist / high contrast mode uses the same shapes and an alternate procedural palette. Shards, gate markers, and the player accent are procedural primitives, while the player body keeps the active material color. Camera shake feedback is controlled by `CGR_CameraShake`. No sprite, image, model, or font import is required.

## Stage and fairness data

No external data file is required for stages. Stage configuration is generated in C# and includes:

- stage index and deterministic seed;
- track length and shard row count;
- obstacle, matching shard, off-color shard, safe-empty-lane chances, and gate interval;
- available color count;
- 2-star and 3-star score targets;
- route-aware estimated max score and best-route collectible count;
- naive count-only max score, rows with multiple matching shards, and naive-vs-route-aware score difference;
- 3-star mistake allowance, difficulty tier, and visual theme index;
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
- validator reports route-aware max score, star targets, three-star ratio, mistake allowance, and theme index for Stage 1-30;
- `Tools/Color Gate Rush/Generate Balance Report` prints naive max, route-aware max, target scores, density, and warning data for Stage 1-30;
- validator smoke tests generate all 30 MVP content stages without saving imported art/audio/model/font/prefab assets;
- visual polish validation checks `VisualTheme`, generated backdrop/track roots, visual-only obstacle/gate/finish details, HUD contrast, and legacy symbol removal.

Star target data:

- 1 star is awarded for reaching the finish.
- Any clear with at least 1 star unlocks the next stage.
- 2-star targets are the rounded-up two-thirds point of the 3-star target.
- 3-star targets are based on a strict percentage of route-aware estimated max score, roughly 93-98% by tier.
- `StageScoreAnalyzer` evaluates row/lane choices, combo scoring, off-color penalties, gate score, and row-spacing-based lane reach.
- One generated row is one decision; multiple matching shards in the same row still count as at most one collectible in route-aware max score.
- Balance report compares naive count-only max score with route-aware max score to catch impossible or overly strict 3-star targets.
