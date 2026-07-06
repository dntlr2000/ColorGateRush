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
    release_readiness_checklist.md
    unity_project_structure.md
  Assets/
    _Project/
      Scenes/
      Scripts/
        Runtime/
          CameraFollow.cs
          CollectibleShard.cs
          ColorGate.cs
          ColorId.cs
          FinishLine.cs
          FloatingFeedback.cs
          GameConstants.cs
          GameManager.cs
          GameSettings.cs
          GameplayObjects.cs
          LaneRunnerController.cs
          LevelGenerationReport.cs
          LevelGenerator.cs
          LevelRowReport.cs
          ObstacleBlock.cs
          ProceduralAudio.cs
          ProceduralFactory.cs
          RuntimeUi.cs
          ShardVisualAnimator.cs
          StageConfig.cs
          StageManager.cs
          StageResult.cs
          StageScoreAnalyzer.cs
          VisualTheme.cs
        Editor/
          BootstrapColorGateRush.cs
          BuildValidator.cs
```

## Notes

- `Scenes` may be empty until the bootstrap menu runs.
- Editor scripts must stay in an `Editor` folder.
- Checked-in prefab assets are not used; runtime generation is preferred for release candidate builds.
- `VisualTheme.cs` owns the code-defined candy-neon palette for world objects, HUD, and VFX.
- `StageConfig.cs` and `StageManager.cs` generate the 30-stage deterministic campaign and PlayerPrefs-backed progression.
- `StageScoreAnalyzer.cs` derives route-aware max score and 2-star/3-star targets from generated row data, counting at most one chosen lane per row.
- Stage 1 is always available, and each later stage unlocks from a clear with at least 1 star while best-star records never downgrade.
- `ShardVisualAnimator.cs` adds mobile-safe bob/spin polish without changing lane or row generation rules.
- Visual-only background, track rails, stripes, obstacle accents, gate cues, and finish details are generated under `GeneratedLevel` and use disabled colliders.
- `BuildValidator.cs` exposes Validate Build, Generate Balance Report, Generate Release Readiness Report, and Reset Local Progress for release-candidate QA without requiring external assets.
