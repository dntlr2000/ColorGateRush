#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorGateRush.EditorTools
{
    public static class BuildValidator
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Tools/Color Gate Rush/Validate Project")]
        // Validates that the generated MVP scene and project rules are ready for play testing.
        public static void Validate()
        {
            EnsureUnitySixProject();
            EnsureSceneAssetExists();
            EnsureBuildSceneRegistered();
            EnsureTypeExists<GameManager>();
            EnsureTypeExists<LevelGenerator>();
            EnsureTypeExists<LaneRunnerController>();
            EnsureTypeExists<RuntimeUi>();
            EnsureTypeExists<ProceduralAudio>();
            EnsureInputConfiguration();
            EnsureStageProgressionRules();
            EnsurePauseAndMenuFlowReferences();
            EnsureSettingsAndTutorialReferences();
            EnsureNoAutomaticRestartReferences();
            EnsureRuntimeFolderHasNoUnityEditorReferences();
            EnsureNoMonoBehaviourFileNameMismatch();
            EnsureSceneContents();
            EnsureNoProjectRuntimeAssets();
            WarnAboutTemplateRemainders();
            WarnAboutTemplateAssets();

            Debug.Log("Color Gate Rush validation passed. Open Main.unity and enter Play Mode for runtime acceptance checks.");
        }

        [MenuItem("Tools/Color Gate Rush/Validate Build")]
        // Provides a release-oriented menu alias for the project validator.
        public static void ValidateBuild()
        {
            Validate();
        }

        [MenuItem("Tools/Color Gate Rush/Generate Balance Report")]
        // Generates deterministic stage balance summaries in an isolated temporary scene.
        public static void GenerateBalanceReport()
        {
            EnsureRuntimeGenerationSmoke();
        }

        [MenuItem("Tools/Color Gate Rush/Reset Local Progress")]
        // Clears only CGR-prefixed local progress keys.
        public static void ResetLocalProgress()
        {
            GameSettings.ResetLocalProgress();
            Debug.Log("Color Gate Rush local CGR_ progress keys reset.");
        }

        // Provides a stable entry point for Unity batchmode validation.
        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        // Confirms that the project is intentionally pinned to Unity 6 / 6000.x.
        private static void EnsureUnitySixProject()
        {
            string projectVersionPath = "ProjectSettings/ProjectVersion.txt";
            if (!File.Exists(projectVersionPath))
            {
                throw new InvalidOperationException("Missing ProjectVersion.txt; Unity version policy cannot be validated.");
            }

            string projectVersion = File.ReadAllText(projectVersionPath);
            if (!projectVersion.Contains("m_EditorVersion: 6000."))
            {
                throw new InvalidOperationException("Color Gate Rush MVP is configured for Unity 6 / 6000.x. Current ProjectVersion.txt does not match.");
            }
        }

        // Fails validation when the bootstrap scene has not been generated yet.
        private static void EnsureSceneAssetExists()
        {
            if (!File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Missing scene. Run Tools/Color Gate Rush/Bootstrap Project first: " + ScenePath);
            }
        }

        // Fails validation when Main.unity is not registered as an enabled build scene.
        private static void EnsureBuildSceneRegistered()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && scene.path == ScenePath)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Main scene is not registered in Build Settings: " + ScenePath);
        }

        // Ensures required compiled types exist after Unity script compilation.
        private static void EnsureTypeExists<T>()
        {
            Type type = typeof(T);
            if (type == null)
            {
                throw new InvalidOperationException("Missing required type: " + typeof(T).Name);
            }
        }

        // Checks that the project input backend is compatible with the runner input code.
        private static void EnsureInputConfiguration()
        {
            string projectSettingsPath = "ProjectSettings/ProjectSettings.asset";
            string manifestPath = "Packages/manifest.json";
            if (!File.Exists(projectSettingsPath))
            {
                Debug.LogWarning("ProjectSettings.asset was not found; input backend could not be checked.");
                return;
            }

            string projectSettings = File.ReadAllText(projectSettingsPath);
            bool newInputOnly = projectSettings.Contains("activeInputHandler: 1");
            bool bothInputBackends = projectSettings.Contains("activeInputHandler: 2");
            if (newInputOnly || bothInputBackends)
            {
                bool hasInputSystemPackage = File.Exists(manifestPath) && File.ReadAllText(manifestPath).Contains("\"com.unity.inputsystem\"");
                if (!hasInputSystemPackage)
                {
                    throw new InvalidOperationException("Project uses the new Input System but com.unity.inputsystem is missing from Packages/manifest.json.");
                }
            }
        }

        // Verifies stage config count, default unlock assumptions, and three-star-only unlock rules.
        private static void EnsureStageProgressionRules()
        {
            StageManager stageManager = new StageManager();
            if (stageManager.Stages == null || stageManager.Stages.Length < StageManager.TotalStageCount)
            {
                throw new InvalidOperationException("StageManager must provide at least " + StageManager.TotalStageCount + " stages.");
            }

            if (!stageManager.IsStageUnlocked(1))
            {
                throw new InvalidOperationException("Stage 1 must be unlocked by default.");
            }

            foreach (StageConfig stage in stageManager.Stages)
            {
                if (stage.TwoStarScore <= 0 || stage.ThreeStarScore <= 0 || stage.TwoStarScore > stage.ThreeStarScore)
                {
                    throw new InvalidOperationException("Invalid star target order for stage " + stage.StageIndex + ".");
                }
            }

            StageConfig firstStage = stageManager.GetStageConfig(1);
            if (stageManager.CalculateStars(firstStage, 0, cleared: false) != 0)
            {
                throw new InvalidOperationException("Failed stages must award 0 stars.");
            }

            if (stageManager.CalculateStars(firstStage, 0, cleared: true) != 1)
            {
                throw new InvalidOperationException("Cleared stages below target score must award 1 star.");
            }

            if (stageManager.CalculateStars(firstStage, firstStage.TwoStarScore, cleared: true) != 2)
            {
                throw new InvalidOperationException("Two-star target does not award 2 stars.");
            }

            if (stageManager.CalculateStars(firstStage, firstStage.ThreeStarScore, cleared: true) != 3)
            {
                throw new InvalidOperationException("Three-star target does not award 3 stars.");
            }

            if (stageManager.WouldUnlockNextStage(firstStage, 1) || stageManager.WouldUnlockNextStage(firstStage, 2) || !stageManager.WouldUnlockNextStage(firstStage, 3))
            {
                throw new InvalidOperationException("Only 3 stars should unlock the next stage.");
            }
        }

        // Verifies the main-menu and pause-flow source hooks exist and avoid direct start from Start button.
        private static void EnsurePauseAndMenuFlowReferences()
        {
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string laneRunnerPath = "Assets/_Project/Scripts/Runtime/LaneRunnerController.cs";
            if (!File.Exists(gameManagerPath) || !File.Exists(runtimeUiPath) || !File.Exists(laneRunnerPath))
            {
                throw new InvalidOperationException("Missing scripts for menu and pause flow validation.");
            }

            string gameManagerSource = File.ReadAllText(gameManagerPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string laneRunnerSource = File.ReadAllText(laneRunnerPath);
            if (!gameManagerSource.Contains("Paused"))
            {
                throw new InvalidOperationException("GameManager is missing a Paused game state.");
            }

            if (gameManagerSource.Contains("BeginRunFromMenu") || gameManagerSource.Contains("_ui.Configure(BeginRunFromMenu"))
            {
                throw new InvalidOperationException("Main Menu Start still appears to start gameplay directly.");
            }

            if (!gameManagerSource.Contains("_ui.Configure(ShowStageSelect, ShowStageSelect"))
            {
                throw new InvalidOperationException("Main Menu Start should route to Stage Select.");
            }

            string[] requiredGameManagerHooks =
            {
                "PauseGame",
                "ResumeGame",
                "HandlePausedInput",
                "Time.timeScale = 0f",
                "RestoreTimeScale"
            };

            foreach (string hook in requiredGameManagerHooks)
            {
                if (!gameManagerSource.Contains(hook))
                {
                    throw new InvalidOperationException("Pause flow hook missing from GameManager.cs: " + hook);
                }
            }

            string[] requiredRuntimeUiHooks =
            {
                "ShowPauseMenu",
                "PauseButton",
                "ResumeButton",
                "PauseRetryButton",
                "PauseStageSelectButton",
                "PauseMainMenuButton"
            };

            foreach (string hook in requiredRuntimeUiHooks)
            {
                if (!runtimeUiSource.Contains(hook))
                {
                    throw new InvalidOperationException("Pause UI hook missing from RuntimeUi.cs: " + hook);
                }
            }

            if (!laneRunnerSource.Contains("!_manager.IsRunning"))
            {
                throw new InvalidOperationException("LaneRunnerController should move only while GameManager is running.");
            }

            if (!gameManagerSource.Contains("if (!IsRunning"))
            {
                throw new InvalidOperationException("GameManager trigger handlers should guard gameplay processing with IsRunning.");
            }
        }

        // Verifies settings, color assist, and first-run tutorial hooks use CGR-prefixed keys.
        private static void EnsureSettingsAndTutorialReferences()
        {
            string settingsPath = "Assets/_Project/Scripts/Runtime/GameSettings.cs";
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string proceduralAudioPath = "Assets/_Project/Scripts/Runtime/ProceduralAudio.cs";
            if (!File.Exists(settingsPath) || !File.Exists(gameManagerPath) || !File.Exists(runtimeUiPath) || !File.Exists(proceduralAudioPath))
            {
                throw new InvalidOperationException("Missing scripts for settings/tutorial validation.");
            }

            string settingsSource = File.ReadAllText(settingsPath);
            string gameManagerSource = File.ReadAllText(gameManagerPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string proceduralAudioSource = File.ReadAllText(proceduralAudioPath);
            string[] requiredKeys =
            {
                "CGR_TutorialSeen",
                "CGR_SoundEnabled",
                "CGR_CameraShake",
                "CGR_HighContrast"
            };

            foreach (string key in requiredKeys)
            {
                if (!settingsSource.Contains(key))
                {
                    throw new InvalidOperationException("Missing CGR-prefixed setting key: " + key);
                }
            }

            string[] requiredHooks =
            {
                "ShowSettings",
                "ShowTutorialIfNeeded",
                "DismissTutorial",
                "ToggleSound",
                "ToggleCameraShake",
                "ToggleColorAssist",
                "ResetLocalProgress"
            };

            foreach (string hook in requiredHooks)
            {
                if (!gameManagerSource.Contains(hook) && !runtimeUiSource.Contains(hook))
                {
                    throw new InvalidOperationException("Missing settings/tutorial hook: " + hook);
                }
            }

            if (!proceduralAudioSource.Contains("GameSettings.SoundEnabled"))
            {
                throw new InvalidOperationException("ProceduralAudio should respect the sound enabled setting.");
            }
        }

        // Fails validation when delayed restart or result-screen global touch retry code remains.
        private static void EnsureNoAutomaticRestartReferences()
        {
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            if (!File.Exists(gameManagerPath))
            {
                throw new InvalidOperationException("Missing GameManager script for restart-flow validation.");
            }

            string source = File.ReadAllText(gameManagerPath);
            string[] bannedTokens =
            {
                "restartAutomatically",
                "autoRestart",
                "restartDelay",
                "RestartAfterDelay",
                "RestartNextSeed",
                "Invoke(nameof(Restart",
                "Invoke(\"Restart",
                "StartCoroutine(Restart"
            };

            foreach (string token in bannedTokens)
            {
                if (source.Contains(token))
                {
                    throw new InvalidOperationException("Automatic restart reference remains in GameManager.cs: " + token);
                }
            }

            if (source.Contains("Touchscreen.current") || source.Contains("Input.touchCount"))
            {
                throw new InvalidOperationException("Result-screen global touch restart is still present in GameManager.cs.");
            }
        }

        // Opens or reuses Main.unity and checks for required scene components.
        private static void EnsureSceneContents()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                EnsureSceneComponent<GameManager>(scene);
                EnsureSceneComponent<LevelGenerator>(scene);
                EnsureSceneComponent<RuntimeUi>(scene);
                EnsureSceneComponent<ProceduralAudio>(scene);
                EnsureSceneComponent<CameraFollow>(scene);
                EnsureSceneComponent<AudioListener>(scene);
                EnsureRuntimeGenerationSmoke();
            }
            finally
            {
                if (openedForValidation && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        // Fails validation when a required component is absent from the generated scene.
        private static void EnsureSceneComponent<T>(Scene scene) where T : Component
        {
            if (FindSceneComponent<T>(scene) != null)
            {
                return;
            }

            throw new InvalidOperationException("Main scene is missing required component: " + typeof(T).Name);
        }

        // Finds the first matching component under the supplied scene roots.
        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        // Generates every stage in an isolated scene and verifies core gameplay object categories exist.
        private static void EnsureRuntimeGenerationSmoke()
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(tempScene);

            GameObject systems = new GameObject("ValidatorSmokeSystems");
            LevelGenerator generator = systems.AddComponent<LevelGenerator>();
            StageManager stageManager = new StageManager();

            try
            {
                foreach (StageConfig stage in stageManager.Stages)
                {
                    LaneRunnerController runner = generator.ClearAndGenerate(null, stage, configureScene: false);
                    if (runner == null)
                    {
                        throw new InvalidOperationException("Runtime generation smoke failed: runner was not created for stage " + stage.StageIndex + ".");
                    }

                    EnsureRunnerPhysics(runner);
                    Transform generatedRoot = systems.transform.Find("GeneratedLevel");
                    if (generatedRoot == null)
                    {
                        throw new InvalidOperationException("Runtime generation smoke failed: GeneratedLevel root was not created for stage " + stage.StageIndex + ".");
                    }

                    EnsureGeneratedCount<CollectibleShard>(generatedRoot, 1);
                    EnsureGeneratedCount<ColorGate>(generatedRoot, 1);
                    EnsureGeneratedCount<ObstacleBlock>(generatedRoot, 1);
                    EnsureGeneratedCount<FinishLine>(generatedRoot, 1);
                    EnsureTriggerColliders<CollectibleShard>(generatedRoot);
                    EnsureTriggerColliders<ColorGate>(generatedRoot);
                    EnsureTriggerColliders<ObstacleBlock>(generatedRoot);
                    EnsureTriggerColliders<FinishLine>(generatedRoot);
                    if (generator.LastReport == null || !generator.LastReport.IsValid)
                    {
                        throw new InvalidOperationException("Runtime generation smoke failed fairness validation for stage " + stage.StageIndex + ".");
                    }

                    EnsureGeneratedRowsAreFair(generator.LastReport);
                    WarnAboutBalanceIfNeeded(generator.LastReport);
                    Debug.Log(BuildGenerationSummary(generator.LastReport, stage));

                    generator.ClearGeneratedLevel();
                }
            }
            finally
            {
                if (systems != null)
                {
                    ClearSmokeGeneratedLevel(systems.transform);
                    UnityEngine.Object.DestroyImmediate(systems);
                }

                if (previousActiveScene.IsValid())
                {
                    EditorSceneManager.SetActiveScene(previousActiveScene);
                }

                if (tempScene.IsValid() && tempScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(tempScene, true);
                }
            }
        }

        // Verifies generated decision rows are aligned and always leave at least one safe option.
        private static void EnsureGeneratedRowsAreFair(LevelGenerationReport report)
        {
            if (report.TotalRows <= 0)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: no decision rows were reported for stage " + report.StageIndex + ".");
            }

            foreach (LevelRowReport row in report.Rows)
            {
                if (!row.IsZAligned)
                {
                    throw new InvalidOperationException("Runtime generation smoke failed: row z mismatch at stage "
                        + report.StageIndex + " row " + row.RowIndex + ".");
                }

                UnsafeRowReason reason = row.GetUnsafeReason();
                if (reason != UnsafeRowReason.None)
                {
                    throw new InvalidOperationException("Runtime generation smoke failed: unsafe row "
                        + reason + " at stage " + report.StageIndex + " row " + row.RowIndex + ".");
                }
            }
        }

        // Builds a compact per-stage balance summary for validator output.
        private static string BuildGenerationSummary(LevelGenerationReport report, StageConfig stage)
        {
            return "Stage " + report.StageIndex
                + " rows=" + report.TotalRows
                + " shardRows=" + report.ShardRows + " (" + report.GetShardRowRatio().ToString("P0") + ")"
                + " emptyRows=" + report.EmptyRows
                + " obstacleRows=" + report.ObstacleRows + " (" + report.GetObstacleRowRatio().ToString("P0") + ")"
                + " totalShards=" + report.TotalShards
                + " totalObstacles=" + report.TotalObstacles
                + " avgShardsPerRow=" + report.GetAverageShardsPerRow().ToString("0.00")
                + " matchingRows=" + report.GetMatchingShardRatio().ToString("P0")
                + " estimatedMaxScore=" + report.MaxPossibleCorrectShardScore
                + " targets(★1=finish, ★2=" + stage.TwoStarScore + ", ★3=" + stage.ThreeStarScore + ")"
                + " rowsWithoutMatching=" + report.RowsWithoutMatchingShard
                + " repairedUnsafeRows=" + report.UnsafeRowsRepaired
                + " prevented(allOff=" + report.AllOffColorRowsPrevented
                + ", allObstacle=" + report.AllObstacleRowsPrevented
                + ", mixed=" + report.MixedUnsafeRowsPrevented + ")";
        }

        // Emits warnings for Stage 1 balance drift without failing valid fair generation.
        private static void WarnAboutBalanceIfNeeded(LevelGenerationReport report)
        {
            if (report.StageIndex != 1)
            {
                return;
            }

            if (report.GetShardRowRatio() < 0.70f)
            {
                Debug.LogWarning("Stage 1 shard row ratio is low: " + report.GetShardRowRatio().ToString("P0") + ".");
            }

            if (report.GetObstacleRowRatio() > 0.10f)
            {
                Debug.LogWarning("Stage 1 obstacle row ratio is high: " + report.GetObstacleRowRatio().ToString("P0") + ".");
            }

            if (report.GetAverageShardsPerRow() < 1.10f)
            {
                Debug.LogWarning("Stage 1 average shards per row is low: " + report.GetAverageShardsPerRow().ToString("0.00") + ".");
            }

            if (report.GetMatchingShardRatio() > 0.70f)
            {
                Debug.LogWarning("Stage 1 matching shard row ratio is high: " + report.GetMatchingShardRatio().ToString("P0") + ".");
            }

            if (report.GetMatchingShardRatio() < 0.30f)
            {
                Debug.LogWarning("Stage 1 matching shard row ratio is low: " + report.GetMatchingShardRatio().ToString("P0") + ".");
            }
        }

        // Verifies the generated runner has the kinematic physics setup expected by trigger gameplay.
        private static void EnsureRunnerPhysics(LaneRunnerController runner)
        {
            Rigidbody rb = runner.GetComponent<Rigidbody>();
            Collider collider = runner.GetComponent<Collider>();
            if (rb == null || !rb.isKinematic || rb.useGravity)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: runner Rigidbody is not kinematic/no-gravity.");
            }

            if (collider == null || collider.isTrigger)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: runner collider is missing or incorrectly marked as trigger.");
            }
        }

        // Verifies at least the requested count of generated gameplay components exists.
        private static void EnsureGeneratedCount<T>(Transform generatedRoot, int minimumCount) where T : Component
        {
            int count = generatedRoot.GetComponentsInChildren<T>(true).Length;
            if (count < minimumCount)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: expected " + typeof(T).Name + " count >= " + minimumCount + ", found " + count + ".");
            }
        }

        // Verifies generated trigger gameplay objects have trigger colliders.
        private static void EnsureTriggerColliders<T>(Transform generatedRoot) where T : Component
        {
            T[] components = generatedRoot.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                Collider collider = component.GetComponent<Collider>();
                if (collider == null || !collider.isTrigger)
                {
                    throw new InvalidOperationException("Runtime generation smoke failed: " + typeof(T).Name + " is missing a trigger collider.");
                }
            }
        }

        // Removes temporary generated level objects created by edit-mode smoke validation.
        private static void ClearSmokeGeneratedLevel(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name.StartsWith("GeneratedLevel"))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        // Fails validation if imported texture or audio assets are placed under the game project folder.
        private static void EnsureNoProjectRuntimeAssets()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project" });
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project" });
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project" });
            if (textureGuids.Length > 0 || audioGuids.Length > 0 || modelGuids.Length > 0)
            {
                throw new InvalidOperationException("Imported Texture2D, AudioClip, or Model assets found under Assets/_Project. MVP assets must be procedural.");
            }
        }

        // Fails validation if runtime scripts reference UnityEditor APIs.
        private static void EnsureRuntimeFolderHasNoUnityEditorReferences()
        {
            string runtimePath = "Assets/_Project/Scripts/Runtime";
            if (!Directory.Exists(runtimePath))
            {
                throw new InvalidOperationException("Missing runtime script folder: " + runtimePath);
            }

            string[] scripts = Directory.GetFiles(runtimePath, "*.cs", SearchOption.AllDirectories);
            foreach (string script in scripts)
            {
                if (File.ReadAllText(script).Contains("UnityEditor"))
                {
                    throw new InvalidOperationException("Runtime script references UnityEditor: " + script.Replace('\\', '/'));
                }
            }
        }

        // Fails validation when a public MonoBehaviour script class does not match its file name.
        private static void EnsureNoMonoBehaviourFileNameMismatch()
        {
            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project/Scripts/Runtime" });
            foreach (string guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                Type scriptClass = script != null ? script.GetClass() : null;
                if (scriptClass == null || !typeof(MonoBehaviour).IsAssignableFrom(scriptClass) || !scriptClass.IsPublic)
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName != scriptClass.Name)
                {
                    throw new InvalidOperationException("Public MonoBehaviour file/class mismatch: " + path + " contains " + scriptClass.Name);
                }
            }
        }

        // Warns when known Unity template leftovers are still present in the project.
        private static void WarnAboutTemplateRemainders()
        {
            WarnIfPathExists("Assets/TutorialInfo");
            WarnIfPathExists("Assets/Readme.asset");
            WarnIfPathExists("Assets/Scenes/SampleScene.unity");
        }

        // Logs a non-fatal warning for a known template path that should not be part of the MVP.
        private static void WarnIfPathExists(string path)
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                Debug.LogWarning("Unity template remainder still exists and should be removed from the MVP: " + path);
            }
        }

        // Warns about Unity template assets that are outside the generated game folder and not used by the MVP.
        private static void WarnAboutTemplateAssets()
        {
            WarnAboutAssetsOutsideProject("t:Texture2D", "texture assets");
            WarnAboutAssetsOutsideProject("t:AudioClip", "audio assets");
            WarnAboutAssetsOutsideProject("t:Model", "model assets");
            WarnAboutAssetsOutsideProject("t:MonoScript", "scripts");
        }

        // Logs non-fatal warnings for assets outside Assets/_Project so they can be cleaned separately.
        private static void WarnAboutAssetsOutsideProject(string filter, string label)
        {
            string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });
            int outsideCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/_Project/") && !path.StartsWith("Assets/Settings/"))
                {
                    outsideCount++;
                    if (outsideCount <= 5)
                    {
                        Debug.LogWarning("Non-MVP " + label + " outside Assets/_Project: " + path);
                    }
                }
            }

            if (outsideCount > 5)
            {
                Debug.LogWarning("Additional non-MVP " + label + " outside Assets/_Project: " + (outsideCount - 5));
            }
        }
    }
}
#endif
