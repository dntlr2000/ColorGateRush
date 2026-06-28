# Prompt — Bootstrap Existing Empty Unity Project

Implement only the project bootstrap layer for Color Gate Rush.

Read `AGENTS.md`, `GAME_DESIGN.md`, and `docs/automation_workflow.md` first. Then:

- Create `Assets/_Project` folders if missing.
- Create or update `BootstrapColorGateRush.cs` under `Assets/_Project/Scripts/Editor`.
- Add a menu item `Tools/Color Gate Rush/Bootstrap Project`.
- The menu item must create/open `Assets/_Project/Scenes/Main.unity`.
- The scene must include GameManager, LevelGenerator, ProceduralAudio, RuntimeUi, Camera, CameraFollow, and Directional Light.
- Set mobile portrait-friendly camera defaults.
- Add `BuildValidator.cs` with a menu item `Tools/Color Gate Rush/Validate Project`.

Do not implement full gameplay in this task. Return changed files and validation steps.
