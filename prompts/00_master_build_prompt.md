# Master Codex Prompt — Build Color Gate Rush

You are in a Unity project repository. Build the MVP described in `GAME_DESIGN.md` and obey `AGENTS.md`.

Use subagents deliberately:

1. Spawn `unity_architect` to inspect the repo and propose/implement scene bootstrap and validation structure.
2. Spawn `gameplay_engineer` to implement the runtime game loop, controls, collision rules, scoring, and finish/restart flow.
3. Spawn `procedural_asset_engineer` to implement procedural materials, primitive geometry helpers, particle feedback, and runtime audio.
4. After integration, spawn `qa_build_reviewer` in read-only mode to review compile risks, missing acceptance criteria, and asset-rule violations.

Wait for all subagents at each phase and consolidate their results. Resolve conflicts in the parent thread. Keep changes inside `Assets/_Project` unless project configuration requires otherwise.

Required final result:

- A generated playable scene via `Tools/Color Gate Rush/Bootstrap Project`.
- No external art/audio assets.
- Keyboard and touch/swipe input.
- Generated track, shards, gates, obstacles, finish line.
- Runtime UI and procedural sound.
- Build validator menu item.

Validation checklist before final response:

- Confirm whether Unity compile/play mode was run. If not possible, state that clearly.
- List all changed files.
- Summarize gameplay behavior implemented.
- Report remaining risks or manual verification needed.
