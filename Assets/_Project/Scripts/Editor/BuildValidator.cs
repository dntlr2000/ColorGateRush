#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            EnsureFinishUsesHudScoreForStars();
            EnsurePauseAndMenuFlowReferences();
            EnsureSettingsAndTutorialReferences();
            EnsureStageStartHintReferences();
            EnsureVisualReadabilityReferences();
            EnsureVisualPolishReferences();
            EnsureNoAutomaticRestartReferences();
            EnsureNoForbiddenProcessOrPrefsUsage();
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

        [MenuItem("Tools/Color Gate Rush/Validate Visual Polish")]
        // Validates visual polish source hooks without mutating the open scene.
        public static void ValidateVisualPolish()
        {
            EnsureVisualReadabilityReferences();
            EnsureVisualPolishReferences();
            Debug.Log("Color Gate Rush visual polish validation passed.");
        }

        [MenuItem("Tools/Color Gate Rush/Generate Balance Report")]
        // Generates deterministic stage balance summaries in an isolated temporary scene.
        public static void GenerateBalanceReport()
        {
            EnsureRuntimeGenerationSmoke();
        }

        [MenuItem("Tools/Color Gate Rush/Generate Release Readiness Report")]
        // Generates a non-building Android/WebGL readiness report from static settings and validator smoke checks.
        public static void GenerateReleaseReadinessReport()
        {
            StringBuilder report = new StringBuilder();
            int hardFails = 0;
            int warnings = 0;
            report.AppendLine("Color Gate Rush Release Readiness Report");
            report.AppendLine("This report does not run Android/WebGL builds or external processes.");
            report.AppendLine("Unity Version: " + GetProjectVersionSummary());

            AppendValidation(report, "Main scene exists", EnsureSceneAssetExists, ref hardFails);
            AppendValidation(report, "Main scene registered in Build Settings", EnsureBuildSceneRegistered, ref hardFails);
            AppendValidation(report, "Stage progression and star targets", EnsureStageProgressionRules, ref hardFails);
            AppendValidation(report, "Stage 1-30 generation and balance smoke", EnsureRuntimeGenerationSmoke, ref hardFails);
            AppendValidation(report, "Runtime folder has no UnityEditor references", EnsureRuntimeFolderHasNoUnityEditorReferences, ref hardFails);
            AppendValidation(report, "No imported media/font/prefab assets under Assets/_Project", EnsureNoProjectRuntimeAssets, ref hardFails);
            AppendValidation(report, "No automatic restart patterns", EnsureNoAutomaticRestartReferences, ref hardFails);
            AppendValidation(report, "No forbidden process launch or full PlayerPrefs reset usage", EnsureNoForbiddenProcessOrPrefsUsage, ref hardFails);
            AppendValidation(report, "Stage start hint is transient", EnsureStageStartHintReferences, ref hardFails);
            AppendValidation(report, "VisualTheme and HUD readability hooks exist", EnsureVisualPolishReferences, ref hardFails);
            AppendAndroidReadiness(report, ref warnings);
            AppendWebGlReadiness(report, ref warnings);
            report.AppendLine("Manual Checks: Android/WebGL builds, device thermal/performance pass, browser audio unlock, signing, icon, and store metadata are not automated.");
            report.AppendLine("Summary: hardFails=" + hardFails + ", warnings=" + warnings);

            string output = report.ToString();
            if (hardFails > 0)
            {
                Debug.LogError(output);
                throw new InvalidOperationException("Release readiness report found " + hardFails + " hard fail(s).");
            }

            if (warnings > 0)
            {
                Debug.LogWarning(output);
            }
            else
            {
                Debug.Log(output);
            }
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

        // Returns the Unity version line recorded by ProjectVersion.txt.
        private static string GetProjectVersionSummary()
        {
            string projectVersionPath = "ProjectSettings/ProjectVersion.txt";
            if (!File.Exists(projectVersionPath))
            {
                return "missing ProjectVersion.txt";
            }

            using (StringReader reader = new StringReader(File.ReadAllText(projectVersionPath)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("m_EditorVersion:"))
                    {
                        return line.Trim();
                    }
                }
            }

            return "unknown";
        }

        // Appends one hard-fail validator result without stopping the readiness report early.
        private static void AppendValidation(StringBuilder report, string label, Action validation, ref int hardFails)
        {
            try
            {
                validation();
                report.AppendLine("[PASS] " + label);
            }
            catch (Exception exception)
            {
                hardFails++;
                report.AppendLine("[FAIL] " + label + " - " + exception.Message);
            }
        }

        // Appends one warning line and increments the report warning count.
        private static void AppendWarning(StringBuilder report, string label, string detail, ref int warnings)
        {
            warnings++;
            report.AppendLine("[WARN] " + label + " - " + detail);
        }

        // Appends one informational release checklist line.
        private static void AppendInfo(StringBuilder report, string label, string detail)
        {
            report.AppendLine("[INFO] " + label + " - " + detail);
        }

        // Extracts top-level arguments from a method call so validators are resilient to multi-line formatting.
        private static string[] ExtractMethodCallArguments(string source, string callToken)
        {
            int callIndex = source.IndexOf(callToken, StringComparison.Ordinal);
            if (callIndex < 0)
            {
                return new string[0];
            }

            int openParenIndex = source.IndexOf('(', callIndex);
            if (openParenIndex < 0)
            {
                return new string[0];
            }

            List<string> arguments = new List<string>();
            int depth = 0;
            int argumentStart = openParenIndex + 1;
            for (int i = openParenIndex + 1; i < source.Length; i++)
            {
                char character = source[i];
                if (character == '(')
                {
                    depth++;
                }
                else if (character == ')')
                {
                    if (depth == 0)
                    {
                        arguments.Add(source.Substring(argumentStart, i - argumentStart).Trim());
                        return arguments.ToArray();
                    }

                    depth--;
                }
                else if (character == ',' && depth == 0)
                {
                    arguments.Add(source.Substring(argumentStart, i - argumentStart).Trim());
                    argumentStart = i + 1;
                }
            }

            return arguments.ToArray();
        }

        // Returns a bounded source slice starting at a marker for focused static validation.
        private static string ExtractSourceWindow(string source, string marker, int maxLength)
        {
            int startIndex = source.IndexOf(marker, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                return string.Empty;
            }

            int length = Math.Min(maxLength, source.Length - startIndex);
            return source.Substring(startIndex, length);
        }

        // Checks whether a source fragment contains any token from a small allow/deny list.
        private static bool ContainsAnyToken(string source, string[] tokens)
        {
            foreach (string token in tokens)
            {
                if (source.Contains(token))
                {
                    return true;
                }
            }

            return false;
        }

        // Appends Android build-readiness warnings that require manual Unity Editor review.
        private static void AppendAndroidReadiness(StringBuilder report, ref int warnings)
        {
            string settingsPath = "ProjectSettings/ProjectSettings.asset";
            if (!File.Exists(settingsPath))
            {
                AppendWarning(report, "Android settings", "ProjectSettings.asset is missing.", ref warnings);
                return;
            }

            string projectSettings = File.ReadAllText(settingsPath);
            string companyName = ReadProjectSettingValue(projectSettings, "companyName");
            string productName = ReadProjectSettingValue(projectSettings, "productName");
            string applicationIdentifier = ReadProjectSettingValue(projectSettings, "applicationIdentifier");
            string bundleVersion = ReadProjectSettingValue(projectSettings, "bundleVersion");
            string versionCode = ReadProjectSettingValue(projectSettings, "AndroidBundleVersionCode");
            string minSdk = ReadProjectSettingValue(projectSettings, "AndroidMinSdkVersion");
            string targetSdk = ReadProjectSettingValue(projectSettings, "AndroidTargetSdkVersion");
            string orientation = ReadProjectSettingValue(projectSettings, "defaultScreenOrientation");
            string portrait = ReadProjectSettingValue(projectSettings, "allowedAutorotateToPortrait");
            string landscapeRight = ReadProjectSettingValue(projectSettings, "allowedAutorotateToLandscapeRight");
            string landscapeLeft = ReadProjectSettingValue(projectSettings, "allowedAutorotateToLandscapeLeft");
            string scriptingBackend = ReadProjectSettingValue(projectSettings, "scriptingBackend");
            string architectures = ReadProjectSettingValue(projectSettings, "AndroidTargetArchitectures");

            if (string.IsNullOrEmpty(companyName) || companyName == "DefaultCompany")
            {
                AppendWarning(report, "Android company name", "Replace DefaultCompany before release.", ref warnings);
            }

            if (string.IsNullOrEmpty(productName) || productName.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendWarning(report, "Android product name", "Confirm a store-ready product name.", ref warnings);
            }

            if (string.IsNullOrEmpty(applicationIdentifier)
                || applicationIdentifier.IndexOf("UnityTechnologies", StringComparison.OrdinalIgnoreCase) >= 0
                || applicationIdentifier.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendWarning(report, "Android package name", "Set a unique reverse-DNS package id, e.g. com.studio.colorgaterush.", ref warnings);
            }

            if (versionCode == "1")
            {
                AppendWarning(report, "Android version code", "Version code is still 1; bump deliberately for store uploads.", ref warnings);
            }

            if (orientation == "0" || landscapeRight == "1" || landscapeLeft == "1")
            {
                AppendWarning(report, "Android orientation", "This portrait runner should be reviewed for portrait-first orientation and safe area behavior.", ref warnings);
            }

            AppendInfo(report, "Android bundle version", string.IsNullOrEmpty(bundleVersion) ? "missing" : bundleVersion);
            AppendInfo(report, "Android SDK levels", "min=" + minSdk + ", target=" + targetSdk + " (0 usually means automatic/highest installed in Unity).");
            AppendInfo(report, "Android scripting backend", "value=" + scriptingBackend + " (Unity enum; verify IL2CPP for release if required).");
            AppendInfo(report, "Android target architectures", "value=" + architectures + " (verify ARM64 for Google Play).");
            AppendInfo(report, "Android signing", "Keystore/signing credentials must be configured manually outside the repository.");
            AppendInfo(report, "Android build artifact", "Use APK for local testing and AAB for Google Play submission.");
        }

        // Appends WebGL build-readiness notes that require browser testing after manual build.
        private static void AppendWebGlReadiness(StringBuilder report, ref int warnings)
        {
            string settingsPath = "ProjectSettings/ProjectSettings.asset";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string settingsSource = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : string.Empty;
            string runtimeUiSource = File.Exists(runtimeUiPath) ? File.ReadAllText(runtimeUiPath) : string.Empty;

            if (runtimeUiSource.Contains("Application.Quit"))
            {
                AppendWarning(report, "WebGL quit flow", "Application.Quit is not meaningful in WebGL and should be hidden or guarded.", ref warnings);
            }

            if (!runtimeUiSource.Contains("ScrollRect") || !runtimeUiSource.Contains("CanvasScaler"))
            {
                AppendWarning(report, "WebGL responsive UI", "Stage Select and long panels should use CanvasScaler and scrolling controls.", ref warnings);
            }

            AppendInfo(report, "WebGL PlayerPrefs", "Progress uses simple CGR_ PlayerPrefs keys; verify browser persistence after build.");
            AppendInfo(report, "WebGL audio", "Browsers may block AudioContext until first user input; verify collect/gate/fail/finish sounds after clicking Start.");
            AppendInfo(report, "WebGL memory/compression", "Compression, load time, and browser memory are manual Build Profile checks.");
            AppendInfo(report, "WebGL input", "Mouse/touch UI should be tested for lane input conflicts in browser.");
            AppendInfo(report, "WebGL input backend", "activeInputHandler=" + ReadProjectSettingValue(settingsSource, "activeInputHandler"));
        }

        // Reads a single YAML-style ProjectSettings value.
        private static string ReadProjectSettingValue(string source, string key)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            using (StringReader reader = new StringReader(source))
            {
                string line;
                string prefix = key + ":";
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return trimmed.Substring(prefix.Length).Trim();
                    }
                }
            }

            return string.Empty;
        }

        // Fails validation if runtime/editor project code uses prohibited process or PlayerPrefs reset APIs.
        private static void EnsureNoForbiddenProcessOrPrefsUsage()
        {
            string scriptsPath = "Assets/_Project/Scripts";
            if (!Directory.Exists(scriptsPath))
            {
                throw new InvalidOperationException("Missing project scripts folder: " + scriptsPath);
            }

            string deleteAllToken = "PlayerPrefs." + "DeleteAll";
            string processStartToken = "Process." + "Start";
            string processTypeToken = "System.Diagnostics." + "Process";
            string[] scripts = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
            foreach (string script in scripts)
            {
                string source = File.ReadAllText(script);
                if (source.Contains(deleteAllToken) || source.Contains(processStartToken) || source.Contains(processTypeToken))
                {
                    throw new InvalidOperationException("Forbidden build-readiness API usage found in " + script.Replace('\\', '/'));
                }
            }
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

        // Verifies stage config count, default unlock assumptions, and one-star clear unlock rules.
        private static void EnsureStageProgressionRules()
        {
            StageManager stageManager = new StageManager();
            if (stageManager.Stages == null || stageManager.Stages.Length < StageManager.TotalStageCount)
            {
                throw new InvalidOperationException("StageManager must provide at least " + StageManager.TotalStageCount + " stages.");
            }

            if (StageManager.TotalStageCount < 30)
            {
                throw new InvalidOperationException("Color Gate Rush content expansion requires at least 30 stages.");
            }

            if (!stageManager.IsStageUnlocked(1))
            {
                throw new InvalidOperationException("Stage 1 must be unlocked by default.");
            }

            foreach (StageConfig stage in stageManager.Stages)
            {
                if (stage.TwoStarScore <= 0 || stage.ThreeStarScore <= 0 || stage.TwoStarScore >= stage.ThreeStarScore)
                {
                    throw new InvalidOperationException("Invalid star target order for stage " + stage.StageIndex + ".");
                }

                int expectedTwoStar = StageScoreAnalyzer.CalculateTwoStarFromThreeStar(stage.ThreeStarScore);
                if (stage.TwoStarScore != expectedTwoStar)
                {
                    throw new InvalidOperationException("Stage " + stage.StageIndex + " two-star target must be two-thirds of three-star target. Expected " + expectedTwoStar + ".");
                }

                if (stage.EstimatedMaxAchievableScore <= 0)
                {
                    throw new InvalidOperationException("Stage " + stage.StageIndex + " must expose a positive route-aware max score.");
                }

                if (stage.ThreeStarScore > stage.EstimatedMaxAchievableScore)
                {
                    throw new InvalidOperationException("Stage " + stage.StageIndex + " has an impossible three-star target.");
                }

                if (stage.ThemeIndex < 0 || stage.ThemeIndex >= VisualTheme.ThemeVariationCount)
                {
                    throw new InvalidOperationException("Stage " + stage.StageIndex + " has an invalid visual theme index.");
                }

                for (int otherIndex = 0; otherIndex < stageManager.Stages.Length; otherIndex++)
                {
                    StageConfig other = stageManager.Stages[otherIndex];
                    if (other.StageIndex != stage.StageIndex && other.Seed == stage.Seed)
                    {
                        throw new InvalidOperationException("Stages must use unique deterministic seeds. Duplicate seed: " + stage.Seed);
                    }
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

            if (stageManager.WouldUnlockNextStage(firstStage, 0)
                || !stageManager.WouldUnlockNextStage(firstStage, 1)
                || !stageManager.WouldUnlockNextStage(firstStage, 2)
                || !stageManager.WouldUnlockNextStage(firstStage, 3))
            {
                throw new InvalidOperationException("Any cleared stage with at least 1 star should unlock the next stage.");
            }

            EnsureNoLegacyThreeStarUnlockText();
        }

        // Fails validation when old copy still tells players that three stars are required for unlock.
        private static void EnsureNoLegacyThreeStarUnlockText()
        {
            string[] checkedPaths =
            {
                "Assets/_Project/Scripts/Runtime/RuntimeUi.cs",
                "GAME_DESIGN.md",
                "README.md",
                "docs/automation_workflow.md",
                "docs/asset_generation_spec.md",
                "docs/unity_project_structure.md"
            };
            string[] legacyPhrases =
            {
                "별 3개를 달성하면 다음 스테이지가 열립니다",
                "별 3개를 달성해야 다음 스테이지가 열립니다",
                "별 3개를 얻으면 다음 스테이지가 열립니다",
                "Only a 3-star clear unlocks",
                "3-star clears unlock the next stage",
                "only 3-star clears unlock"
            };

            foreach (string path in checkedPaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                string source = File.ReadAllText(path);
                foreach (string phrase in legacyPhrases)
                {
                    if (source.Contains(phrase))
                    {
                        throw new InvalidOperationException("Legacy three-star unlock text remains in " + path + ": " + phrase);
                    }
                }
            }
        }

        // Verifies the stage-start hint is a transient UI toast and not a gameplay transition timer.
        private static void EnsureStageStartHintReferences()
        {
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            if (!File.Exists(runtimeUiPath))
            {
                throw new InvalidOperationException("Missing RuntimeUi script for stage-start hint validation.");
            }

            string source = File.ReadAllText(runtimeUiPath);
            if (!source.Contains("ShowStageStartHint") || !source.Contains("_messageHideAt") || !source.Contains("Time.unscaledTime"))
            {
                throw new InvalidOperationException("RuntimeUi must implement a transient unscaled stage-start hint.");
            }

            if (!source.Contains("_hintText.text = string.Empty") || !source.Contains("ClearMessage();"))
            {
                throw new InvalidOperationException("RuntimeUi must clear persistent center hint text and expose ClearMessage for state transitions.");
            }

            if (source.Contains("StartRun(") || source.Contains("StartStage(") || source.Contains("RestartCurrentRun("))
            {
                throw new InvalidOperationException("RuntimeUi stage-start hint must not trigger gameplay transitions.");
            }
        }

        // Verifies finish completion saves the same score shown in the HUD instead of applying a hidden multiplier.
        private static void EnsureFinishUsesHudScoreForStars()
        {
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            string constantsPath = "Assets/_Project/Scripts/Runtime/GameConstants.cs";
            if (!File.Exists(gameManagerPath) || !File.Exists(constantsPath))
            {
                throw new InvalidOperationException("Missing scripts for finish star score validation.");
            }

            string gameManagerSource = File.ReadAllText(gameManagerPath);
            string constantsSource = File.ReadAllText(constantsPath);
            string[] bannedTokens =
            {
                "FinishMultiplier",
                "_score *=",
                "FloorToInt(_score / 250f)"
            };

            foreach (string token in bannedTokens)
            {
                if (gameManagerSource.Contains(token) || constantsSource.Contains(token))
                {
                    throw new InvalidOperationException("Finish score multiplier must not affect star ratings: " + token);
                }
            }

            if (!gameManagerSource.Contains("SaveStageResult(_currentStage, _score)"))
            {
                throw new InvalidOperationException("Completed stages should save the current HUD score for star rating.");
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

            string[] configureArguments = ExtractMethodCallArguments(gameManagerSource, "_ui.Configure(");
            if (configureArguments.Length < 8)
            {
                throw new InvalidOperationException("Could not parse GameManager.cs _ui.Configure callback list for menu flow validation.");
            }

            string startRoute = configureArguments[0].Trim();
            string stageSelectRoute = configureArguments[1].Trim();
            string stageButtonRoute = configureArguments[7].Trim();
            string[] allowedStageSelectRoutes = { "ShowStageSelect", "EnterStageSelect", "OpenStageSelect" };
            string[] forbiddenDirectStartRoutes = { "StartRun", "StartStage", "StartStageFromSelect", "RestartCurrentRun", "BeginRun", "StartSelectedStage" };
            if (ContainsAnyToken(startRoute, forbiddenDirectStartRoutes))
            {
                throw new InvalidOperationException("Main Menu Start appears to call a direct start route in GameManager.cs _ui.Configure: " + startRoute);
            }

            if (!ContainsAnyToken(startRoute, allowedStageSelectRoutes))
            {
                throw new InvalidOperationException("Main Menu Start appears to call " + startRoute
                    + " instead of a StageSelect route. Check RuntimeUi.CreateMenuPanel and GameManager.ShowStageSelect.");
            }

            if (!ContainsAnyToken(stageSelectRoute, allowedStageSelectRoutes))
            {
                throw new InvalidOperationException("Stage Select menu callback does not route to StageSelect. Found in GameManager.cs _ui.Configure: " + stageSelectRoute);
            }

            if (!ContainsAnyToken(stageButtonRoute, new[] { "StartStageFromSelect", "StartStage" }))
            {
                throw new InvalidOperationException("StageSelect stage button callback should be the only direct stage-start route. Found: " + stageButtonRoute);
            }

            string menuPanelSource = ExtractSourceWindow(runtimeUiSource, "private GameObject CreateMenuPanel", 1800);
            if (!menuPanelSource.Contains("StartButton") || !menuPanelSource.Contains("_onStart?.Invoke()"))
            {
                throw new InvalidOperationException("RuntimeUi.cs Main Menu Start button is not clearly bound to _onStart. Check RuntimeUi.CreateMenuPanel.");
            }

            if (ContainsAnyToken(menuPanelSource, new[] { "_onStageSelected", "_onRestart", "StartRun", "StartStage", "RestartCurrentRun", "BeginRun" }))
            {
                throw new InvalidOperationException("RuntimeUi.cs Main Menu Start panel contains a forbidden direct-start reference.");
            }

            string stageButtonSource = ExtractSourceWindow(runtimeUiSource, "private void RebuildStageButtons", 2200);
            if (!stageButtonSource.Contains("_onStageSelected?.Invoke(stageIndex)") || !stageButtonSource.Contains("button.interactable = unlocked"))
            {
                throw new InvalidOperationException("RuntimeUi.cs StageSelect buttons must call _onStageSelected and keep locked stages non-interactable.");
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
                "CGR_MusicEnabled",
                "CGR_SfxEnabled",
                "CGR_MusicVolume",
                "CGR_SfxVolume",
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
                "ToggleMusic",
                "ToggleSfx",
                "CycleMusicVolume",
                "CycleSfxVolume",
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

            string[] requiredAudioTokens =
            {
                "MusicType",
                "PlayMusic",
                "StopMusic",
                "SetMusicDucked",
                "RefreshSettings",
                "_musicSource",
                "_sfxSource",
                "_sfxClipCache",
                "AudioClip.Create",
                "GameSettings.MusicEnabled",
                "GameSettings.SfxEnabled",
                "GameSettings.MusicVolume",
                "GameSettings.SfxVolume"
            };

            foreach (string token in requiredAudioTokens)
            {
                if (!proceduralAudioSource.Contains(token))
                {
                    throw new InvalidOperationException("ProceduralAudio missing music/SFX setting hook: " + token);
                }
            }

            string[] requiredSettingsUiTokens =
            {
                "MusicToggleButton",
                "MusicVolumeButton",
                "SfxToggleButton",
                "SfxVolumeButton"
            };

            foreach (string token in requiredSettingsUiTokens)
            {
                if (!runtimeUiSource.Contains(token))
                {
                    throw new InvalidOperationException("Settings UI missing split audio control: " + token);
                }
            }
        }

        // Verifies the color/shape visual source of truth, HUD contrast panel, and removal of legacy symbol overlays.
        private static void EnsureVisualReadabilityReferences()
        {
            string constantsPath = "Assets/_Project/Scripts/Runtime/GameConstants.cs";
            string factoryPath = "Assets/_Project/Scripts/Runtime/ProceduralFactory.cs";
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            string generatorPath = "Assets/_Project/Scripts/Runtime/LevelGenerator.cs";
            string runnerPath = "Assets/_Project/Scripts/Runtime/LaneRunnerController.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string[] requiredFiles = { constantsPath, factoryPath, gameManagerPath, generatorPath, runnerPath, runtimeUiPath };
            foreach (string path in requiredFiles)
            {
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Missing script for visual readability validation: " + path);
                }
            }

            string constantsSource = File.ReadAllText(constantsPath);
            string factorySource = File.ReadAllText(factoryPath);
            string gameManagerSource = File.ReadAllText(gameManagerPath);
            string generatorSource = File.ReadAllText(generatorPath);
            string runnerSource = File.ReadAllText(runnerPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string combinedRuntimeSource = constantsSource + factorySource + gameManagerSource + generatorSource + runnerSource + runtimeUiSource;
            string[] bannedLegacyTokens =
            {
                "AttachColorSymbol",
                "ColorSymbol",
                "_symbolText",
                "ColorSymbol_"
            };

            foreach (string token in bannedLegacyTokens)
            {
                if (combinedRuntimeSource.Contains(token))
                {
                    throw new InvalidOperationException("Legacy text symbol overlay reference remains: " + token);
                }
            }

            string[] requiredVisualTokens =
            {
                "ColorShapeType",
                "ColorVisualProfile",
                "GetVisualProfile",
                "ShapeName",
                "CreateShardVisual",
                "CreateColorShape",
                "CreatePlayerAccent",
                "PlayerAccentMaterial"
            };

            foreach (string token in requiredVisualTokens)
            {
                if (!combinedRuntimeSource.Contains(token))
                {
                    throw new InvalidOperationException("Color/shape visual source reference missing: " + token);
                }
            }

            if (!generatorSource.Contains("CreateShardVisual"))
            {
                throw new InvalidOperationException("LevelGenerator should create shards through shape-aware procedural visuals.");
            }

            if (!runnerSource.Contains("CreatePlayerAccent") || !runnerSource.Contains("ApplyPlayerAccentMaterial"))
            {
                throw new InvalidOperationException("Player current color should be expressed with a procedural accent, not text.");
            }

            if (!runtimeUiSource.Contains("HudInfoPanel") || !runtimeUiSource.Contains("AddTextShadow") || !runtimeUiSource.Contains("profile.HudLabel"))
            {
                throw new InvalidOperationException("Runtime HUD should include a contrast panel, text shadow, and color/shape label.");
            }
        }

        // Verifies the visual polish sprint has a theme source and generated world/UI polish hooks.
        private static void EnsureVisualPolishReferences()
        {
            string themePath = "Assets/_Project/Scripts/Runtime/VisualTheme.cs";
            string factoryPath = "Assets/_Project/Scripts/Runtime/ProceduralFactory.cs";
            string generatorPath = "Assets/_Project/Scripts/Runtime/LevelGenerator.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string bootstrapPath = "Assets/_Project/Scripts/Editor/BootstrapColorGateRush.cs";
            string animatorPath = "Assets/_Project/Scripts/Runtime/ShardVisualAnimator.cs";
            string[] requiredFiles = { themePath, factoryPath, generatorPath, runtimeUiPath, bootstrapPath, animatorPath };
            foreach (string path in requiredFiles)
            {
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Missing script for visual polish validation: " + path);
                }
            }

            string themeSource = File.ReadAllText(themePath);
            string factorySource = File.ReadAllText(factoryPath);
            string generatorSource = File.ReadAllText(generatorPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string bootstrapSource = File.ReadAllText(bootstrapPath);
            string animatorSource = File.ReadAllText(animatorPath);
            string combinedSource = themeSource + factorySource + generatorSource + runtimeUiSource + bootstrapSource + animatorSource;
            string[] requiredTokens =
            {
                "VisualThemeProfile",
                "VisualTheme.Current",
                "CameraBackgroundColor",
                "TrackBaseColor",
                "HudPanelColor",
                "BackgroundRoot",
                "TrackVisualRoot",
                "TrackEdgeRail",
                "TrackSideLightStrip",
                "TrackRhythmStripe",
                "ShardGlow",
                "ShardVisualAnimator",
                "ObstacleWarningTop",
                "ObstacleSpike",
                "GateApproachCue",
                "FinishChecker",
                "ApplySceneLighting",
                "RenderSettings.skybox = null",
                "ScreenPanelColor",
                "ButtonColor",
                "ScaledParticleCount",
                "CollectBurst",
                "GateBurst",
                "FinishBurst",
                "FailBurst",
                "Apply Visual Theme"
            };

            foreach (string token in requiredTokens)
            {
                if (!combinedSource.Contains(token))
                {
                    throw new InvalidOperationException("Visual polish hook missing: " + token);
                }
            }

            if (!factorySource.Contains("VisualPrimitive") || !factorySource.Contains("DisableCollider"))
            {
                throw new InvalidOperationException("Decorative geometry must use visual-only primitive helpers.");
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
                    EnsureGeneratedCount<ObstacleBlock>(generatedRoot, stage.StageIndex == 1 ? 0 : 1);
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
                    EnsureGeneratedScoreTargets(generator.LastReport, stage);
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

        // Verifies route-aware score targets are possible and strict enough for near-perfect three-star play.
        private static void EnsureGeneratedScoreTargets(LevelGenerationReport report, StageConfig stage)
        {
            if (report.EstimatedMaxAchievableScore <= 0)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: stage "
                    + stage.StageIndex + " has no route-aware max score.");
            }

            if (!report.ClearRouteExists)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: stage "
                    + stage.StageIndex + " has no clearable route under lane movement constraints.");
            }

            if (stage.ThreeStarScore > report.EstimatedMaxAchievableScore)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: stage "
                    + stage.StageIndex + " has impossible three-star target " + stage.ThreeStarScore
                    + " > " + report.EstimatedMaxAchievableScore + ".");
            }

            if (stage.EstimatedMaxAchievableScore != report.EstimatedMaxAchievableScore)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: stage "
                    + stage.StageIndex + " route-aware max score changed between config build and generation.");
            }

            if (stage.TwoStarScore >= stage.ThreeStarScore)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: stage "
                    + stage.StageIndex + " has two-star target >= three-star target.");
            }

            int expectedTwoStar = StageScoreAnalyzer.CalculateTwoStarFromThreeStar(stage.ThreeStarScore);
            if (stage.TwoStarScore != expectedTwoStar)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: stage "
                    + stage.StageIndex + " two-star target is not two-thirds of three-star target.");
            }

            if (!report.PerfectOrNearPerfectRouteExists)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: stage "
                    + stage.StageIndex + " has no route that can reach the three-star target.");
            }

            float ratio = stage.ThreeStarScore / (float)report.EstimatedMaxAchievableScore;
            float warningThreshold = stage.StageIndex <= 3 ? 0.90f : 0.94f;
            if (ratio < warningThreshold)
            {
                Debug.LogWarning("Stage " + stage.StageIndex + " three-star ratio is low: " + ratio.ToString("P0") + ".");
            }

            if (ratio >= 0.995f)
            {
                Debug.LogWarning("Stage " + stage.StageIndex + " three-star target is extremely strict: " + ratio.ToString("P1") + ".");
            }

            if (report.NaiveMaxScore > 0 && report.GetRouteAwareMaxScoreRatio() < 0.80f)
            {
                Debug.LogWarning("Stage " + stage.StageIndex + " route-aware max is much lower than naive max: "
                    + report.EstimatedMaxAchievableScore + "/" + report.NaiveMaxScore + ".");
            }
        }

        // Builds a compact per-stage balance summary for validator output.
        private static string BuildGenerationSummary(LevelGenerationReport report, StageConfig stage)
        {
            return "Stage " + report.StageIndex
                + " seed=" + stage.Seed
                + " tier=" + stage.DifficultyTier
                + " theme=" + stage.ThemeIndex
                + " rows=" + report.TotalRows
                + " trackLength=" + stage.TrackLength.ToString("0")
                + " speed=" + stage.PlayerForwardSpeed.ToString("0.00")
                + " gates=" + report.GateRows
                + " shardRows=" + report.ShardRows + " (" + report.GetShardRowRatio().ToString("P0") + ")"
                + " emptyRows=" + report.EmptyRows
                + " obstacleRows=" + report.ObstacleRows + " (" + report.GetObstacleRowRatio().ToString("P0") + ")"
                + " totalShards=" + report.TotalShards
                + " totalObstacles=" + report.TotalObstacles
                + " avgShardsPerRow=" + report.GetAverageShardsPerRow().ToString("0.00")
                + " matchingRows=" + report.GetMatchingShardRatio().ToString("P0")
                + " totalMatchingNaive=" + report.TotalMatchingShardsByNaiveCount
                + " multiMatchRows=" + report.RowsWithMultipleMatchingShards
                + " oneShardRows=" + report.RowsWhereOnlyOneShardCanBeCollected
                + " naiveMax=" + report.NaiveMaxScore
                + " routeMax=" + report.EstimatedMaxAchievableScore
                + " naiveMinusRoute=" + report.NaiveMinusRouteAwareMax
                + " routeVsNaive=" + report.GetRouteAwareMaxScoreRatio().ToString("P0")
                + " maxCollectibles=" + report.EstimatedMaxCollectibleCount
                + " targets(★1=finish, ★2=" + stage.TwoStarScore + ", ★3=" + stage.ThreeStarScore + ")"
                + " twoVsThree=" + (stage.TwoStarScore / (float)Mathf.Max(1, stage.ThreeStarScore)).ToString("P0")
                + " threeStarRatio=" + (stage.ThreeStarScore / (float)Mathf.Max(1, report.EstimatedMaxAchievableScore)).ToString("P0")
                + " mistakeAllowance=" + stage.ThreeStarMistakeAllowance
                + " unlock=clear(★>=1)"
                + " clearRoute=" + report.ClearRouteExists
                + " nearPerfectRoute=" + report.PerfectOrNearPerfectRouteExists
                + " rowsWithoutMatching=" + report.RowsWithoutMatchingShard
                + " repairedUnsafeRows=" + report.UnsafeRowsRepaired
                + " prevented(allOff=" + report.AllOffColorRowsPrevented
                + ", allObstacle=" + report.AllObstacleRowsPrevented
                + ", mixed=" + report.MixedUnsafeRowsPrevented + ")"
                + " warnings=" + report.Warnings.Count;
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
            string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets/_Project" });
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
            if (textureGuids.Length > 0 || audioGuids.Length > 0 || modelGuids.Length > 0 || fontGuids.Length > 0 || prefabGuids.Length > 0)
            {
                throw new InvalidOperationException("Imported Texture2D, AudioClip, Model, Font, or Prefab assets found under Assets/_Project. Release assets must be procedural/runtime generated.");
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
            WarnAboutAssetsOutsideProject("t:Font", "font assets");
            WarnAboutAssetsOutsideProject("t:Prefab", "prefab assets");
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
