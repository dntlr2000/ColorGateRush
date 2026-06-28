# Prompt — Procedural Assets, VFX, SFX

Implement procedural visual and audio feedback for Color Gate Rush.

Required outputs:

- Material factory with cyan, magenta, yellow, lime, track, obstacle, transparent gate, and finish materials.
- Primitive creation helpers for track, shard, gate, obstacle, finish, and player.
- Particle bursts for collect, gate, fail, and finish.
- Runtime procedural audio using `AudioClip.Create`, not imported audio files.
- Mobile-readable scale, contrast, and camera framing.

Do not download or import assets. Keep generated assets either runtime-only or created by editor script under `Assets/_Project/Generated` if persistence is needed.
