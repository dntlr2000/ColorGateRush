# Unity Project Structure

```text
UnityProjectRoot/
  AGENTS.md
  Agent.md
  GAME_DESIGN.md
  README.md
  .codex/
    config.toml
    agents/
      unity_architect.toml
      gameplay_engineer.toml
      procedural_asset_engineer.toml
      qa_build_reviewer.toml
  prompts/
    00_master_build_prompt.md
    01_bootstrap_prompt.md
    02_gameplay_prompt.md
    03_procedural_assets_prompt.md
    04_qa_review_prompt.md
  docs/
    automation_workflow.md
    asset_generation_spec.md
    unity_project_structure.md
  Assets/
    _Project/
      Scenes/
      Scripts/
        Runtime/
          CameraFollow.cs
          ColorId.cs
          GameConstants.cs
          GameManager.cs
          GameplayObjects.cs
          LaneRunnerController.cs
          LevelGenerator.cs
          ProceduralAudio.cs
          ProceduralFactory.cs
          RuntimeUi.cs
        Editor/
          BootstrapColorGateRush.cs
          BuildValidator.cs
```

## Notes

- `Scenes` may be empty until the bootstrap menu runs.
- Editor scripts must stay in an `Editor` folder.
- Generated materials/prefabs are optional; runtime generation is preferred for MVP.
