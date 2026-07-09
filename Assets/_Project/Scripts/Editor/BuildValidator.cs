#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorGateRush.EditorTools
{
    public static class BuildValidator
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string MenuBgmAssetPath = "Assets/_Project/Resources/ColorGateRush/Audio/ColorgateRush_Menu.mp3";
        private const string GameplayBgmAssetPath = "Assets/_Project/Resources/ColorGateRush/Audio/ColorgateRush_Ingame.mp3";
        private const string TitleScreenAssetPath = "Assets/_Project/Resources/ColorGateRush/Images/TitleScreen.png";
        private const string MainMenuBackgroundAssetPath = "Assets/_Project/Resources/ColorGateRush/Images/MainMenuBackground.png";
        private const string RetiredPrimaryButtonAssetPath = "Assets/_Project/Resources/ColorGateRush/UI/PrimaryButton.png";
        private const string AppIconAssetPath = "Assets/_Project/Art/AppIcon/color_gate_rush_icon_1024.png";
        private const string SplashTitleArtAssetPath = "Assets/_Project/Art/Splash/color_gate_rush_splash_1080x1920.png";
        private const string AudioLicensesDocPath = "docs/audio_licenses.md";
        private const string StoreListingDraftPath = "docs/store_listing_draft.md";
        private const string ExpectedProductName = "Color Gate Rush";
        private const string ExpectedCompanyName = "Nappa Studio";
        private const string ExpectedAndroidPackageName = "com.nappa.colorgaterush";
        private const string ExpectedBundleVersion = "0.9.0";
        private const string ExpectedAndroidVersionCode = "1";

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
            EnsurePortraitOrientation();
            EnsureStageProgressionRules();
            EnsureFinishUsesHudScoreForStars();
            EnsurePauseAndMenuFlowReferences();
            EnsureSettingsAndTutorialReferences();
            EnsureLocalizationReferences();
            EnsureReleaseUiCleanupReferences();
            EnsureEndlessModeReferences();
            EnsureStageWrongShardFailureReferences();
            EnsureStageStartHintReferences();
            EnsureVisualReadabilityReferences();
            EnsureMainMenuBackgroundReferences();
            EnsureVisualPolishReferences();
            EnsureRuntimeComponentCreationSafety();
            EnsureRuntimeMaterialProviderSafety();
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
            EnsureRuntimeComponentCreationSafety();
            EnsureRuntimeMaterialProviderSafety();
            Debug.Log("Color Gate Rush visual polish validation passed.");
        }

        [MenuItem("Tools/Color Gate Rush/Validate Runtime Visuals")]
        // Validates runtime component, material, shader, and generated renderer safety without running a player build.
        public static void ValidateRuntimeVisuals()
        {
            EnsureRuntimeComponentCreationSafety();
            EnsureRuntimeMaterialProviderSafety();
            EnsureRuntimeGenerationSmoke();
            Debug.Log("Color Gate Rush runtime visual validation passed.");
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
            AppendValidation(report, "Only approved imported BGM and release UI image assets under Assets/_Project", EnsureNoProjectRuntimeAssets, ref hardFails);
            AppendValidation(report, "No automatic restart patterns", EnsureNoAutomaticRestartReferences, ref hardFails);
            AppendValidation(report, "No forbidden process launch or full PlayerPrefs reset usage", EnsureNoForbiddenProcessOrPrefsUsage, ref hardFails);
            AppendValidation(report, "Korean/English runtime localization hooks", EnsureLocalizationReferences, ref hardFails);
            AppendValidation(report, "Release UI cleanup and Settings sections", EnsureReleaseUiCleanupReferences, ref hardFails);
            AppendValidation(report, "Endless Mode MVP hooks", EnsureEndlessModeReferences, ref hardFails);
            AppendValidation(report, "Shared wrong-shard three-strike rule", EnsureStageWrongShardFailureReferences, ref hardFails);
            AppendValidation(report, "Stage start hint is transient", EnsureStageStartHintReferences, ref hardFails);
            AppendValidation(report, "VisualTheme and HUD readability hooks exist", EnsureVisualPolishReferences, ref hardFails);
            AppendValidation(report, "Tap-to-start title screen and MainMenu background are configured", EnsureMainMenuBackgroundReferences, ref hardFails);
            AppendValidation(report, "Runtime component/material visual safety", EnsureRuntimeVisualsForReport, ref hardFails);
            AppendRuntimeMaterialReferenceReport(report);
            AppendAndroidReadiness(report, ref warnings);
            AppendFinalReleasePreparationReadiness(report, ref warnings);
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

        [MenuItem("Tools/Color Gate Rush/Reset Endless Records")]
        // Clears only CGR_Endless local record keys.
        public static void ResetEndlessRecords()
        {
            EndlessRecords.Reset();
            Debug.Log("Color Gate Rush local CGR_Endless record keys reset.");
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
            string applicationIdentifier = ReadNestedProjectSettingValue(projectSettings, "applicationIdentifier", "Android");
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
            string customKeystore = ReadProjectSettingValue(projectSettings, "androidUseCustomKeystore");
            string keystoreName = ReadProjectSettingValue(projectSettings, "AndroidKeystoreName");
            string keyAlias = ReadProjectSettingValue(projectSettings, "AndroidKeyaliasName");

            if (string.IsNullOrEmpty(companyName) || companyName == "DefaultCompany")
            {
                AppendWarning(report, "Android company name", "Replace DefaultCompany before release.", ref warnings);
            }
            else if (companyName != ExpectedCompanyName)
            {
                AppendWarning(report, "Android company name", "Expected " + ExpectedCompanyName + " but found " + companyName + ".", ref warnings);
            }

            if (string.IsNullOrEmpty(productName) || productName.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendWarning(report, "Android product name", "Confirm a store-ready product name.", ref warnings);
            }
            else if (productName != ExpectedProductName)
            {
                AppendWarning(report, "Android product name", "Expected " + ExpectedProductName + " but found " + productName + ".", ref warnings);
            }

            if (string.IsNullOrEmpty(applicationIdentifier)
                || applicationIdentifier.IndexOf("UnityTechnologies", StringComparison.OrdinalIgnoreCase) >= 0
                || applicationIdentifier.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendWarning(report, "Android package name", "Set a unique reverse-DNS package id, e.g. com.studio.colorgaterush.", ref warnings);
            }
            else if (applicationIdentifier != ExpectedAndroidPackageName)
            {
                AppendWarning(report, "Android package name", "Expected " + ExpectedAndroidPackageName + " but found " + applicationIdentifier + ".", ref warnings);
            }

            if (string.IsNullOrEmpty(bundleVersion) || string.IsNullOrEmpty(versionCode))
            {
                AppendWarning(report, "Android version information", "Bundle version and Android version code must be set before packaging.", ref warnings);
            }
            else
            {
                if (bundleVersion != ExpectedBundleVersion)
                {
                    AppendWarning(report, "Android bundle version", "Expected " + ExpectedBundleVersion + " but found " + bundleVersion + ".", ref warnings);
                }

                if (versionCode != ExpectedAndroidVersionCode)
                {
                    AppendWarning(report, "Android version code", "Expected " + ExpectedAndroidVersionCode + " for the first internal test upload, but found " + versionCode + ".", ref warnings);
                }
            }

            if (targetSdk == "0")
            {
                AppendWarning(report, "Android target API", "Target API is automatic/default; verify the installed Android SDK target meets the current Google Play requirement before AAB upload.", ref warnings);
            }

            if (orientation == "0" || landscapeRight == "1" || landscapeLeft == "1")
            {
                AppendWarning(report, "Android orientation", "This portrait runner should be reviewed for portrait-first orientation and safe area behavior.", ref warnings);
            }

            AppendInfo(report, "Android product name", string.IsNullOrEmpty(productName) ? "missing" : productName);
            AppendInfo(report, "Android package id", string.IsNullOrEmpty(applicationIdentifier) ? "missing" : applicationIdentifier);
            AppendInfo(report, "Android bundle version", string.IsNullOrEmpty(bundleVersion) ? "missing" : bundleVersion);
            AppendInfo(report, "Android version code", string.IsNullOrEmpty(versionCode) ? "missing" : versionCode);
            AppendInfo(report, "Android SDK levels", "min=" + minSdk + ", target=" + targetSdk + " (0 usually means automatic/highest installed in Unity).");
            AppendInfo(report, "Android scripting backend", "value=" + scriptingBackend + " (Unity enum; verify IL2CPP for release if required).");
            AppendInfo(report, "Android target architectures", "value=" + architectures + " (verify ARM64 for Google Play).");
            AppendAndroidSigningReadiness(report, customKeystore, keystoreName, keyAlias, ref warnings);
            AppendInfo(report, "Android build artifact", "Use APK for local testing and signed AAB for Google Play internal testing/submission.");
        }

        // Reports Android signing configuration without exposing or requiring keystore passwords in source control.
        private static void AppendAndroidSigningReadiness(StringBuilder report, string customKeystore, string keystoreName, string keyAlias, ref int warnings)
        {
            bool hasCustomKeystore = customKeystore == "1";
            bool hasKeystorePath = !string.IsNullOrWhiteSpace(keystoreName);
            bool hasAlias = !string.IsNullOrWhiteSpace(keyAlias);
            if (!hasCustomKeystore || !hasKeystorePath || !hasAlias)
            {
                AppendWarning(report, "Android signing", "Custom keystore, upload key alias, and private passwords must be configured manually before AAB upload.", ref warnings);
                return;
            }

            if (IsPathInsideProject(keystoreName))
            {
                AppendWarning(report, "Android signing", "Keystore appears to be inside the Unity project. Move it outside the repository and keep passwords private.", ref warnings);
                return;
            }

            AppendInfo(report, "Android signing", "Custom keystore is configured outside the repository with alias `" + keyAlias + "`. Passwords are not inspected and must remain private.");
        }

        // Returns true when an absolute or project-relative path resolves inside the current Unity project folder.
        private static bool IsPathInsideProject(string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                return false;
            }

            try
            {
                string projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string fullPath = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return fullPath.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fullPath, projectRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Appends final release-preparation warnings for store metadata, icon/splash, screenshots, and BGM license records.
        private static void AppendFinalReleasePreparationReadiness(StringBuilder report, ref int warnings)
        {
            string settingsPath = "ProjectSettings/ProjectSettings.asset";
            string projectSettings = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : string.Empty;

            if (string.IsNullOrEmpty(projectSettings))
            {
                AppendWarning(report, "Release Player Settings", "ProjectSettings.asset is missing; icon, splash, and release metadata could not be inspected.", ref warnings);
                return;
            }

            string appIconGuid = ReadUnityMetaGuid(AppIconAssetPath + ".meta");
            int appIconReferenceCount = CountSubstring(projectSettings, appIconGuid);
            if (!File.Exists(AppIconAssetPath) || string.IsNullOrEmpty(appIconGuid) || appIconReferenceCount <= 0)
            {
                AppendWarning(report, "App icon", "Android app icon is missing or not referenced by ProjectSettings: " + AppIconAssetPath, ref warnings);
            }
            else if (appIconReferenceCount < 6)
            {
                AppendWarning(report, "App icon", "App icon asset is referenced, but fewer than the expected Android icon slots are populated. Verify Android Player Settings manually.", ref warnings);
            }
            else
            {
                AppendInfo(report, "App icon", AppIconAssetPath + " is referenced by Android icon slots (" + appIconReferenceCount + " ProjectSettings references).");
            }

            if (File.Exists(TitleScreenAssetPath))
            {
                AppendInfo(report, "Title screen", "Tap-to-start title screen uses " + TitleScreenAssetPath + " and routes to MainMenu by explicit tap/click.");
            }
            else
            {
                AppendWarning(report, "Title screen", "Tap-to-start title image is missing: " + TitleScreenAssetPath, ref warnings);
            }

            if (File.Exists(SplashTitleArtAssetPath))
            {
                AppendInfo(report, "Custom splash screen", "Not used by design. Archived splash/title artwork may remain in the project, but no separate splash scene, timer, or transition is required.");
            }

            AppendBgmLicenseReadiness(report, ref warnings);
            AppendStoreListingReadiness(report, ref warnings);
            AppendWarning(report, "Store screenshots", "Capture final screenshots from the actual RC build: Title Screen, Main Menu, Stage gameplay, Endless gameplay, Stage Select, and Result screens.", ref warnings);
            AppendWarning(report, "Google Play data safety", "Complete the Play Console data safety form manually; current project has no analytics/ad/network SDKs, but store answers must be user-verified.", ref warnings);
            AppendInfo(report, "Unity splash screen", "Official Unity splash/logo settings remain a manual pre-submission review item.");
        }

        // Reports whether the approved BGM clips have a release license record.
        private static void AppendBgmLicenseReadiness(StringBuilder report, ref int warnings)
        {
            if (!File.Exists(AudioLicensesDocPath))
            {
                AppendWarning(report, "BGM license record", "Missing " + AudioLicensesDocPath + "; record source, creator, license, download date, and attribution before submission.", ref warnings);
                return;
            }

            string audioLicenseSource = File.ReadAllText(AudioLicensesDocPath);
            bool mentionsMenuClip = audioLicenseSource.Contains(Path.GetFileName(MenuBgmAssetPath));
            bool mentionsGameplayClip = audioLicenseSource.Contains(Path.GetFileName(GameplayBgmAssetPath));
            if (!mentionsMenuClip || !mentionsGameplayClip)
            {
                AppendWarning(report, "BGM license record", "The audio license document must list both approved BGM files.", ref warnings);
                return;
            }

            if (audioLicenseSource.IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0
                || audioLicenseSource.IndexOf("Unknown", StringComparison.OrdinalIgnoreCase) >= 0
                || audioLicenseSource.IndexOf("사용자 확인 필요", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendWarning(report, "BGM license record", "BGM source/license fields still need user confirmation before store submission.", ref warnings);
            }
            else
            {
                AppendInfo(report, "BGM license record", AudioLicensesDocPath + " contains records for both approved BGM files.");
            }
        }

        // Reports whether the store listing draft exists and still contains release TODOs.
        private static void AppendStoreListingReadiness(StringBuilder report, ref int warnings)
        {
            if (!File.Exists(StoreListingDraftPath))
            {
                AppendWarning(report, "Store listing draft", "Missing " + StoreListingDraftPath + "; prepare name, descriptions, screenshots, privacy notes, and TODOs.", ref warnings);
                return;
            }

            string storeListingSource = File.ReadAllText(StoreListingDraftPath);
            if (storeListingSource.IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppendWarning(report, "Store listing draft", "Store listing draft exists but still contains TODO items that must be resolved before public submission.", ref warnings);
            }
            else
            {
                AppendInfo(report, "Store listing draft", StoreListingDraftPath + " exists with no TODO markers.");
            }
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

        // Appends material/shader inclusion details used by Android/WebGL/PC build readiness checks.
        private static void AppendRuntimeMaterialReferenceReport(StringBuilder report)
        {
            string materialFolder = "Assets/_Project/Resources/ColorGateRush/Materials";
            string graphicsPath = "ProjectSettings/GraphicsSettings.asset";
            string providerPath = "Assets/_Project/Scripts/Runtime/RuntimeMaterialProvider.cs";
            string[] materialPaths = Directory.Exists(materialFolder)
                ? Directory.GetFiles(materialFolder, "*.mat", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();

            AppendInfo(report, "Runtime material Resources folder", materialFolder + " (" + materialPaths.Length + " .mat assets)");
            foreach (string materialPath in materialPaths)
            {
                AppendInfo(report, "Runtime material asset", materialPath.Replace('\\', '/'));
            }

            if (File.Exists(graphicsPath))
            {
                string graphicsSource = File.ReadAllText(graphicsPath);
                AppendInfo(report, "Always Included URP/Lit", graphicsSource.Contains("933532a4fcc9baf4fa0491de14d08ed7") ? "present - hard fail" : "absent");
                AppendInfo(report, "Always Included URP/Unlit", graphicsSource.Contains("650dd9526735d5b46b79224bc6e94025") ? "present" : "absent");
                AppendInfo(report, "Always Included URP/Particles Unlit", graphicsSource.Contains("0406db5a14f94604a8c57ccfbc9f3b46") ? "present" : "absent");
            }

            if (File.Exists(providerPath))
            {
                List<string> shaderFindLocations = new List<string>();
                CollectSourceLocations(providerPath, "Shader." + "Find", shaderFindLocations);
                AppendInfo(report, "Runtime Shader.Find locations", shaderFindLocations.Count == 0 ? "none" : string.Join(", ", shaderFindLocations.ToArray()));
            }
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

        // Reads a nested YAML-style ProjectSettings value such as applicationIdentifier.Android.
        private static string ReadNestedProjectSettingValue(string source, string parentKey, string childKey)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            using (StringReader reader = new StringReader(source))
            {
                string line;
                bool insideParent = false;
                string parentPrefix = parentKey + ":";
                string childPrefix = childKey + ":";
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (!insideParent)
                    {
                        if (trimmed == parentPrefix)
                        {
                            insideParent = true;
                        }

                        continue;
                    }

                    if (!line.StartsWith(" ", StringComparison.Ordinal))
                    {
                        return string.Empty;
                    }

                    if (trimmed.StartsWith(childPrefix, StringComparison.Ordinal))
                    {
                        return trimmed.Substring(childPrefix.Length).Trim();
                    }
                }
            }

            return string.Empty;
        }

        // Reads the guid line from a Unity .meta file used by static ProjectSettings reference checks.
        private static string ReadUnityMetaGuid(string metaPath)
        {
            if (!File.Exists(metaPath))
            {
                return string.Empty;
            }

            using (StringReader reader = new StringReader(File.ReadAllText(metaPath)))
            {
                string line;
                const string prefix = "guid:";
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

        // Counts non-overlapping occurrences of a token in a static settings source.
        private static int CountSubstring(string source, string token)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token))
            {
                return 0;
            }

            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }

        // Fails validation if runtime/editor project code uses prohibited process or PlayerPrefs reset APIs.
        private static void EnsureNoForbiddenProcessOrPrefsUsage()
        {
            string scriptsPath = "Assets/_Project/Scripts";
            if (!Directory.Exists(scriptsPath))
            {
                throw new InvalidOperationException("Missing project scripts folder: " + scriptsPath);
            }

            string[] scripts = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
            List<string> findings = new List<string>();
            foreach (string script in scripts)
            {
                string source = File.ReadAllText(script);
                CollectForbiddenBuildApiUsage(script, source, findings);
            }

            if (findings.Count > 0)
            {
                throw new InvalidOperationException("Forbidden build-readiness API usage found:\n" + string.Join("\n", findings.ToArray()));
            }
        }

        // Collects real forbidden API usages while ignoring validator diagnostics, comments, and string literals.
        private static void CollectForbiddenBuildApiUsage(string scriptPath, string source, List<string> findings)
        {
            string normalizedPath = scriptPath.Replace('\\', '/');
            string[] lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inBlockComment = false;
            Regex deleteAllPattern = new Regex(@"\bPlayerPrefs\s*\.\s*DeleteAll\s*\(", RegexOptions.Compiled);
            Regex processStartPattern = new Regex(@"\bProcess\s*\.\s*Start\s*\(", RegexOptions.Compiled);
            Regex processTypePattern = new Regex(@"\bSystem\s*\.\s*Diagnostics\s*\.\s*Process\b", RegexOptions.Compiled);
            Regex newProcessPattern = new Regex(@"\bnew\s+Process\s*\(", RegexOptions.Compiled);
            for (int i = 0; i < lines.Length; i++)
            {
                string codeLine = StripCommentsAndStringLiterals(lines[i], ref inBlockComment);
                string pattern = null;
                if (deleteAllPattern.IsMatch(codeLine))
                {
                    pattern = "PlayerPrefs.DeleteAll()";
                }
                else if (processStartPattern.IsMatch(codeLine))
                {
                    pattern = "Process.Start()";
                }
                else if (processTypePattern.IsMatch(codeLine))
                {
                    pattern = "System.Diagnostics.Process";
                }
                else if (newProcessPattern.IsMatch(codeLine))
                {
                    pattern = "new Process()";
                }

                if (!string.IsNullOrEmpty(pattern))
                {
                    findings.Add(normalizedPath + ":" + (i + 1) + " " + pattern + " -> " + lines[i].Trim());
                }
            }
        }

        // Removes comments and string/char literal contents so static API scans only inspect executable code.
        private static string StripCommentsAndStringLiterals(string line, ref bool inBlockComment)
        {
            StringBuilder builder = new StringBuilder(line.Length);
            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];
                char next = i + 1 < line.Length ? line[i + 1] : '\0';
                if (inBlockComment)
                {
                    if (current == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    builder.Append(' ');
                    continue;
                }

                if (current == '/' && next == '/')
                {
                    break;
                }

                if (current == '/' && next == '*')
                {
                    inBlockComment = true;
                    builder.Append(' ');
                    i++;
                    continue;
                }

                if (current == '"' || current == '\'')
                {
                    bool verbatimString = current == '"' && i > 0 && line[i - 1] == '@';
                    char quote = current;
                    builder.Append(' ');
                    i = ConsumeLiteral(line, i + 1, quote, verbatimString);
                    continue;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        // Advances through one string or character literal and returns the closing quote index.
        private static int ConsumeLiteral(string line, int startIndex, char quote, bool verbatimString)
        {
            for (int i = startIndex; i < line.Length; i++)
            {
                char current = line[i];
                if (verbatimString && quote == '"' && current == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                if (!verbatimString && current == '\\')
                {
                    i++;
                    continue;
                }

                if (current == quote)
                {
                    return i;
                }
            }

            return line.Length - 1;
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

        // Fails validation when Android is allowed to rotate into untested landscape layouts.
        private static void EnsurePortraitOrientation()
        {
            string projectSettingsPath = "ProjectSettings/ProjectSettings.asset";
            if (!File.Exists(projectSettingsPath))
            {
                throw new InvalidOperationException("ProjectSettings.asset is missing; orientation cannot be validated.");
            }

            string projectSettings = File.ReadAllText(projectSettingsPath);
            string orientation = ReadProjectSettingValue(projectSettings, "defaultScreenOrientation");
            string portrait = ReadProjectSettingValue(projectSettings, "allowedAutorotateToPortrait");
            string landscapeRight = ReadProjectSettingValue(projectSettings, "allowedAutorotateToLandscapeRight");
            string landscapeLeft = ReadProjectSettingValue(projectSettings, "allowedAutorotateToLandscapeLeft");
            if (orientation != "1" || portrait != "1" || landscapeRight == "1" || landscapeLeft == "1")
            {
                throw new InvalidOperationException("Color Gate Rush is a portrait runner. Set defaultScreenOrientation=Portrait and disable landscape autorotation.");
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

            for (int i = 1; i < stageManager.Stages.Length; i++)
            {
                StageConfig previous = stageManager.Stages[i - 1];
                StageConfig current = stageManager.Stages[i];
                if (current.PlayerForwardSpeed - previous.PlayerForwardSpeed > 0.35f)
                {
                    Debug.LogWarning("Stage speed jump may feel abrupt between Stage "
                        + previous.StageIndex + " and Stage " + current.StageIndex + ".");
                }

                if (current.ObstacleChance - previous.ObstacleChance > 0.035f)
                {
                    Debug.LogWarning("Obstacle chance jump may feel abrupt between Stage "
                        + previous.StageIndex + " and Stage " + current.StageIndex + ".");
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

            if (!source.Contains("HudToastPanel")
                || !source.Contains("_messageToastPanel.SetActive(!string.IsNullOrEmpty(message))")
                || !source.Contains("_messageToastPanel.SetActive(false)"))
            {
                throw new InvalidOperationException("RuntimeUi must keep center guidance in a small transient toast panel that hides on timeout/state changes.");
            }

            if (source.Contains("StartRun(") || source.Contains("StartStage(") || source.Contains("RestartCurrentRun("))
            {
                throw new InvalidOperationException("RuntimeUi stage-start hint must not trigger gameplay transitions.");
            }

            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            if (File.Exists(gameManagerPath))
            {
                string gameManagerSource = File.ReadAllText(gameManagerPath);
                if (gameManagerSource.Contains("ShowMessage(\"콤보")
                    || gameManagerSource.Contains("ShowMessage(\"색상 변경")
                    || gameManagerSource.Contains("ShowMessage(\"색상/도형"))
                {
                    throw new InvalidOperationException("Combo and color-change feedback must not use the central toast channel.");
                }
            }
        }

        // Verifies release UI no longer exposes local playtest stats and Settings is split into clear sections.
        private static void EnsureReleaseUiCleanupReferences()
        {
            string statsPath = "Assets/_Project/Scripts/Runtime/PlaytestStats.cs";
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string localizationPath = "Assets/_Project/Scripts/Runtime/LocalizationManager.cs";
            string localizationKeyPath = "Assets/_Project/Scripts/Runtime/LocalizationKey.cs";
            foreach (string path in new[] { statsPath, gameManagerPath, runtimeUiPath, localizationPath, localizationKeyPath })
            {
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Missing script for release UI cleanup validation: " + path);
                }
            }

            string statsSource = File.ReadAllText(statsPath);
            string gameManagerSource = File.ReadAllText(gameManagerPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string localizationSource = File.ReadAllText(localizationPath);
            string localizationKeySource = File.ReadAllText(localizationKeyPath);
            string combinedReleaseSource = statsSource + gameManagerSource + runtimeUiSource + localizationSource + localizationKeySource;
            string[] removedPlaytestTokens =
            {
                "CGR_Stats_",
                "PlaytestStatsButton",
                "PlaytestStatsPanel",
                "PlaytestStatsScrollView",
                "ResetPlaytestStatsButton",
                "StatsResetConfirmPanel",
                "BeginPlaytestAttempt",
                "RecordRunCompleted",
                "RecordRunFailed",
                "RecordRunQuitIfOpen",
                "ShowPlaytestStats",
                "ResetPlaytestStats",
                "RecordStageStarted",
                "RecordCompleted",
                "RecordFailed",
                "RecordQuit",
                "PlaytestExitReason"
            };

            foreach (string token in removedPlaytestTokens)
            {
                if (combinedReleaseSource.Contains(token))
                {
                    throw new InvalidOperationException("Release build must not expose or record Playtest Stats token: " + token);
                }
            }

            string[] requiredSettingsTokens =
            {
                "SettingsTab",
                "SettingsTab.General",
                "SettingsTab.Language",
                "SettingsTab.Data",
                "CreateSettingsTabButton",
                "_settingsGeneralSection",
                "_settingsLanguageSection",
                "_settingsDataSection",
                "SettingsContentWidth",
                "SettingsBottomActionY",
                "ConfigureSettingsControlButton",
                "CreateSettingsOptionGroup",
                "MusicVolumeGroup",
                "SfxVolumeGroup",
                "SettingsSliderHeight",
                "SettingsSliderTrackHeight",
                "SettingsSliderHandleWidth",
                "SettingsSliderHandleHeight",
                "StageProgressReset",
                "ResetEndlessRecordsButton"
            };

            foreach (string token in requiredSettingsTokens)
            {
                if (!runtimeUiSource.Contains(token) && !localizationKeySource.Contains(token))
                {
                    throw new InvalidOperationException("Settings must keep General/Language/Data release sections: " + token);
                }
            }

            string[] requiredButtonStyleTokens =
            {
                "ApplyPrimaryButtonStyle",
                "ApplyGeneratedButtonStyle",
                "PrimaryButtonBackgroundColor",
                "PrimaryButtonBorderColor",
                "usePrimaryStyle: false",
                "AttachButtonClickSfx",
                "ConfigureStaticSelectableTransition"
            };

            foreach (string token in requiredButtonStyleTokens)
            {
                if (!runtimeUiSource.Contains(token))
                {
                    throw new InvalidOperationException("Runtime UI must keep procedural primary button style with Settings exceptions: " + token);
                }
            }

            if (!localizationSource.Contains("스와이프") || !localizationSource.Contains("Swipe left or right"))
            {
                throw new InvalidOperationException("Rules must describe mobile swipe/tap movement.");
            }

            string[] forbiddenUserFacingControlTokens = { "A/D", "Arrow", "방향키", "키보드" };
            foreach (string token in forbiddenUserFacingControlTokens)
            {
                if (localizationSource.Contains(token))
                {
                    throw new InvalidOperationException("Runtime user-facing Rules must be mobile-first and avoid keyboard wording: " + token);
                }
            }
        }

        // Verifies Endless Mode is present, independent from stage stars/unlocks, and uses CGR-prefixed records.
        private static void EnsureEndlessModeReferences()
        {
            string configPath = "Assets/_Project/Scripts/Runtime/EndlessRunConfig.cs";
            string recordsPath = "Assets/_Project/Scripts/Runtime/EndlessRecords.cs";
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            string levelGeneratorPath = "Assets/_Project/Scripts/Runtime/LevelGenerator.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string laneRunnerPath = "Assets/_Project/Scripts/Runtime/LaneRunnerController.cs";
            string[] requiredFiles = { configPath, recordsPath, gameManagerPath, levelGeneratorPath, runtimeUiPath, laneRunnerPath };
            foreach (string path in requiredFiles)
            {
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Missing script for Endless Mode validation: " + path);
                }
            }

            string configSource = File.ReadAllText(configPath);
            string recordsSource = File.ReadAllText(recordsPath);
            string gameManagerSource = File.ReadAllText(gameManagerPath);
            string levelGeneratorSource = File.ReadAllText(levelGeneratorPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string laneRunnerSource = File.ReadAllText(laneRunnerPath);
            string[] requiredTokens =
            {
                "GameMode",
                "Endless",
                "StartEndlessRun",
                "EndEndlessRun",
                "BeginEndless",
                "UpdateEndlessGeneration",
                "CleanupEndlessItems",
                "EndlessModeButton",
                "QuitButton",
                "ShowEndlessHud",
                "ShowEndlessResult",
                "CGR_EndlessBestScore",
                "CGR_EndlessBestDistance",
                "SetForwardSpeedRange",
                "SetLaneMoveSharpness",
                "SpeedGrowthPerSecond",
                "ForwardSpeed",
                "SpeedMultiplier",
                "WrongShardLimit",
                "RegisterWrongShard",
                "EndlessFailReason",
                "FormatMistakeIcons",
                "WithSeed",
                "CreateEndlessRunSeed",
                "_activeEndlessConfig",
                "_endlessRandomState"
            };
            string combinedSource = configSource + recordsSource + gameManagerSource + levelGeneratorSource + runtimeUiSource + laneRunnerSource;
            foreach (string token in requiredTokens)
            {
                if (!combinedSource.Contains(token))
                {
                    throw new InvalidOperationException("Endless Mode hook missing: " + token);
                }
            }

            string beginEndlessSource = ExtractSourceWindow(levelGeneratorSource, "public LaneRunnerController BeginEndless", 1800);
            if (beginEndlessSource.Contains("CreateFinish"))
            {
                throw new InvalidOperationException("Endless generation must not create a finish line.");
            }

            if (!beginEndlessSource.Contains("_endlessRandomState = Random.state"))
            {
                throw new InvalidOperationException("Endless generation must preserve a per-run Random state instead of sharing unrelated runtime random calls.");
            }

            string endEndlessSource = ExtractSourceWindow(gameManagerSource, "private void EndEndlessRun", 900);
            if (endEndlessSource.Contains("SaveStageResult") || endEndlessSource.Contains("CreateFailedResult") || endEndlessSource.Contains("WouldUnlockNextStage"))
            {
                throw new InvalidOperationException("Endless result flow must not write stage stars or unlock progress.");
            }

            string updateEndlessSource = ExtractSourceWindow(gameManagerSource, "private void UpdateEndlessRun", 1200);
            if (!updateEndlessSource.Contains("_endlessElapsedTime += Time.deltaTime")
                || !updateEndlessSource.Contains("ForwardSpeed(_endlessElapsedTime, _endlessDistance)")
                || !updateEndlessSource.Contains("UpdateEndlessGeneration(_endlessDistance, _activeEndlessConfig, _endlessElapsedTime)"))
            {
                throw new InvalidOperationException("Endless difficulty must grow from elapsed time and distance without using Time.timeScale.");
            }

            if (updateEndlessSource.Contains("Time.timeScale"))
            {
                throw new InvalidOperationException("Endless difficulty must not use Time.timeScale as a speed ramp.");
            }

            string wrongShardSource = ExtractSourceWindow(gameManagerSource, "private bool RegisterWrongShard", 1400);
            if (!wrongShardSource.Contains("_wrongShardCount")
                || !wrongShardSource.Contains(">= limit")
                || !gameManagerSource.Contains("FailEndlessRun(EndlessFailReason.WrongShardLimit)"))
            {
                throw new InvalidOperationException("Endless wrong-shard limit must count to three and then fail the Endless run.");
            }

            string recordsResetSource = ExtractSourceWindow(recordsSource, "public static void Reset", 900);
            if (!recordsResetSource.Contains("DeleteKey(BestScoreKey)") || recordsResetSource.Contains("DeleteAll"))
            {
                throw new InvalidOperationException("Endless reset must delete only explicit CGR_Endless keys.");
            }

            if (!gameManagerSource.Contains("Application.Quit()") || !gameManagerSource.Contains("UNITY_WEBGL") || !gameManagerSource.Contains("Quit requested from Main Menu."))
            {
                throw new InvalidOperationException("Main Menu Quit flow must safely handle build, WebGL, and Editor targets.");
            }

            if (!configSource.Contains("DefaultWrongShardLimit = GameConstants.MaxWrongShardCount"))
            {
                throw new InvalidOperationException("Endless wrong shard limit should use the shared GameConstants.MaxWrongShardCount value.");
            }

            string startEndlessSource = ExtractSourceWindow(gameManagerSource, "private void StartEndlessRun", 1800);
            if (!startEndlessSource.Contains("_activeEndlessConfig = _baseEndlessConfig.WithSeed(CreateEndlessRunSeed())")
                || !startEndlessSource.Contains("seed = _activeEndlessConfig.Seed")
                || !startEndlessSource.Contains("BeginEndless(this, _activeEndlessConfig)"))
            {
                throw new InvalidOperationException("Endless Mode must generate a fresh per-run seed while preserving one seed sequence during the run.");
            }

            if (!configSource.Contains("EndlessRunConfig WithSeed(int seed)"))
            {
                throw new InvalidOperationException("EndlessRunConfig must expose a WithSeed helper for per-run randomization.");
            }
        }

        // Verifies Stage and Endless modes share the three-strike wrong-shard rule without changing stage unlocks.
        private static void EnsureStageWrongShardFailureReferences()
        {
            string gameManagerPath = "Assets/_Project/Scripts/Runtime/GameManager.cs";
            string analyzerPath = "Assets/_Project/Scripts/Runtime/StageScoreAnalyzer.cs";
            string rowReportPath = "Assets/_Project/Scripts/Runtime/LevelRowReport.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string localizationPath = "Assets/_Project/Scripts/Runtime/LocalizationManager.cs";
            string constantsPath = "Assets/_Project/Scripts/Runtime/GameConstants.cs";
            foreach (string path in new[] { gameManagerPath, analyzerPath, rowReportPath, runtimeUiPath, localizationPath, constantsPath })
            {
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Missing script for wrong-shard failure validation: " + path);
                }
            }

            string gameManagerSource = File.ReadAllText(gameManagerPath);
            string analyzerSource = File.ReadAllText(analyzerPath);
            string rowReportSource = File.ReadAllText(rowReportPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string localizationSource = File.ReadAllText(localizationPath);
            string constantsSource = File.ReadAllText(constantsPath);
            string collectSource = ExtractSourceWindow(gameManagerSource, "public void HandleCollect", 2600);
            if (!constantsSource.Contains("MaxWrongShardCount = 3"))
            {
                throw new InvalidOperationException("Shared wrong-shard limit must remain configured as 3.");
            }

            if (!collectSource.Contains("RegisterWrongShard()")
                || !collectSource.Contains("FailStageRun(StageFailReason.WrongShardLimit")
                || !collectSource.Contains("FailEndlessRun(EndlessFailReason.WrongShardLimit)"))
            {
                throw new InvalidOperationException("Wrong shard handling must increment the shared counter and fail Stage/Endless only when the limit is reached.");
            }

            if (collectSource.Contains("else\n                {\n                    Destroy(shard.gameObject);\n                    FailStageRun(StageFailReason.WrongShard")
                || collectSource.Contains("StageFailReason.WrongShard,"))
            {
                throw new InvalidOperationException("Stage Mode must no longer fail immediately on the first wrong shard.");
            }

            string failStageSource = ExtractSourceWindow(gameManagerSource, "private void FailStageRun", 1800);
            if (!failStageSource.Contains("CreateFailedResult(_currentStage, _score, failReason)")
                || !failStageSource.Contains("StageFailReason.WrongShardLimit"))
            {
                throw new InvalidOperationException("Stage failure flow must show the wrong-shard limit reason without writing stars or unlocks.");
            }

            if (!analyzerSource.Contains("wrongShardCount")
                || !analyzerSource.Contains("GeneratedLaneContent.OffColorShard")
                || !analyzerSource.Contains("GameConstants.WrongColorShardPenalty")
                || analyzerSource.Contains("content == GeneratedLaneContent.Obstacle || content == GeneratedLaneContent.OffColorShard"))
            {
                throw new InvalidOperationException("StageScoreAnalyzer must model wrong-shard count state and fail only routes that reach the shared limit.");
            }

            if (!rowReportSource.Contains("content == GeneratedLaneContent.Empty || content == GeneratedLaneContent.MatchingShard"))
            {
                throw new InvalidOperationException("LevelRowReport safe-lane counting must keep off-color shards unsafe.");
            }

            if (!runtimeUiSource.Contains("StageFailReasonText")
                || !runtimeUiSource.Contains("WrongShardLimitReason")
                || !runtimeUiSource.Contains("FormatMistakeIcons")
                || !localizationSource.Contains("다른 색 샤드를 3번 먹었습니다")
                || !localizationSource.Contains("Picking 3 wrong shards fails the run"))
            {
                throw new InvalidOperationException("Runtime UI must explain and display the shared wrong-shard three-strike rule.");
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

        // Verifies the title, main-menu, and pause-flow source hooks exist and avoid direct start from Start button.
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
            if (configureArguments.Length < 9)
            {
                throw new InvalidOperationException("Could not parse GameManager.cs _ui.Configure callback list for menu flow validation.");
            }

            string titleRoute = configureArguments[0].Trim();
            string startRoute = configureArguments[1].Trim();
            string stageSelectRoute = configureArguments[2].Trim();
            string stageButtonRoute = configureArguments[8].Trim();
            string[] allowedTitleRoutes = { "ReturnToMainMenu", "ShowMainMenu", "EnterMainMenu" };
            string[] allowedStageSelectRoutes = { "ShowStageSelect", "EnterStageSelect", "OpenStageSelect" };
            string[] forbiddenDirectStartRoutes = { "StartRun", "StartStage", "StartStageFromSelect", "RestartCurrentRun", "BeginRun", "StartSelectedStage" };
            if (!ContainsAnyToken(titleRoute, allowedTitleRoutes))
            {
                throw new InvalidOperationException("Title screen tap/click should route to MainMenu. Found in GameManager.cs _ui.Configure: " + titleRoute);
            }

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

            string titlePanelSource = ExtractSourceWindow(runtimeUiSource, "private GameObject CreateTitlePanel", 1400);
            if (!gameManagerSource.Contains("private void Start()")
                || !gameManagerSource.Contains("ShowTitleScreen();")
                || !gameManagerSource.Contains("private void ShowTitleScreen()")
                || !gameManagerSource.Contains("GameState.Title")
                || !runtimeUiSource.Contains("TitleScreenResourcePath")
                || !runtimeUiSource.Contains("ShowTitleScreen")
                || !titlePanelSource.Contains("TitleScreenPanel")
                || !titlePanelSource.Contains("_onTitleContinue?.Invoke()"))
            {
                throw new InvalidOperationException("App launch should show the approved tap-to-start title screen, then route to MainMenu without a splash timer.");
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
                "SetMusicVolume",
                "SetSfxVolume",
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
                "Resources.Load<AudioClip>",
                "ColorgateRush_Menu",
                "ColorgateRush_Ingame",
                "PlayUiClick",
                "PlayUiClickGlobal",
                "ui_click",
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
                "MusicVolumeLabel",
                "MusicVolumeSlider",
                "SfxToggleButton",
                "SfxVolumeLabel",
                "SfxVolumeSlider",
                "LanguageLabel",
                "KoreanLanguageButton",
                "EnglishLanguageButton"
            };

            foreach (string token in requiredSettingsUiTokens)
            {
                if (!runtimeUiSource.Contains(token))
                {
                    throw new InvalidOperationException("Settings UI missing split audio control: " + token);
                }
            }

            if (!File.Exists(MenuBgmAssetPath) || !File.Exists(GameplayBgmAssetPath))
            {
                throw new InvalidOperationException("Approved BGM assets are missing from Resources/ColorGateRush/Audio.");
            }
        }

        // Verifies the lightweight code-based localization system covers both supported languages.
        private static void EnsureLocalizationReferences()
        {
            string languagePath = "Assets/_Project/Scripts/Runtime/Language.cs";
            string keyPath = "Assets/_Project/Scripts/Runtime/LocalizationKey.cs";
            string managerPath = "Assets/_Project/Scripts/Runtime/LocalizationManager.cs";
            string localizedTextPath = "Assets/_Project/Scripts/Runtime/LocalizedText.cs";
            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            string settingsPath = "Assets/_Project/Scripts/Runtime/GameSettings.cs";
            string manifestPath = "Packages/manifest.json";
            string[] requiredFiles = { languagePath, keyPath, managerPath, localizedTextPath, runtimeUiPath, settingsPath, manifestPath };
            foreach (string path in requiredFiles)
            {
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Missing localization validation input: " + path);
                }
            }

            string managerSource = File.ReadAllText(managerPath);
            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            string localizedTextSource = File.ReadAllText(localizedTextPath);
            string settingsSource = File.ReadAllText(settingsPath);
            string manifestSource = File.ReadAllText(manifestPath);
            string[] requiredTokens =
            {
                "CGR_Language",
                "Language.Korean",
                "Language.English",
                "OnLanguageChanged",
                "SetLanguage(Language",
                "LocalizationManager.T",
                "CreateLocalizedText",
                "CreateLocalizedButton",
                "KoreanLanguageButton",
                "EnglishLanguageButton",
                "RefreshLanguageButtons",
                "LocalizedText"
            };

            string combinedSource = managerSource + runtimeUiSource + localizedTextSource;
            foreach (string token in requiredTokens)
            {
                if (!combinedSource.Contains(token))
                {
                    throw new InvalidOperationException("Localization hook missing: " + token);
                }
            }

            if (manifestSource.Contains("com.unity.localization") || combinedSource.Contains("UnityEngine.Localization"))
            {
                throw new InvalidOperationException("Unity Localization package must not be used for this lightweight runtime localization pass.");
            }

            if (settingsSource.Contains("DeleteKey(LocalizationManager.LanguageKey")
                || settingsSource.Contains("DeleteKey(\"CGR_Language\"")
                || settingsSource.Contains("PlayerPrefs.DeleteAll"))
            {
                throw new InvalidOperationException("Reset Progress must not delete CGR_Language or call PlayerPrefs.DeleteAll.");
            }

            Array keys = Enum.GetValues(typeof(LocalizationKey));
            foreach (LocalizationKey key in keys)
            {
                if (!LocalizationManager.HasAllTranslations(key))
                {
                    throw new InvalidOperationException("Missing Korean or English localization entry for key: " + key);
                }

                int koreanPlaceholders = LocalizationManager.PlaceholderCount(Language.Korean, key);
                int englishPlaceholders = LocalizationManager.PlaceholderCount(Language.English, key);
                if (koreanPlaceholders != englishPlaceholders)
                {
                    throw new InvalidOperationException("Localization placeholder mismatch for key " + key + ": Korean="
                        + koreanPlaceholders + ", English=" + englishPlaceholders);
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

            bool hasHudContrastPanel = runtimeUiSource.Contains("HudInfoPanel") && runtimeUiSource.Contains("HudPanelColor");
            bool hasHudTextShadow = runtimeUiSource.Contains("AddTextShadow");
            bool hasCurrentColorShapeLabel = runtimeUiSource.Contains("HudCurrentText")
                && runtimeUiSource.Contains("LocalizationKey.Current")
                && (runtimeUiSource.Contains("profile.HudLabel")
                    || (runtimeUiSource.Contains("LocalizationManager.ColorName(profile.ColorId)")
                        && runtimeUiSource.Contains("LocalizationManager.ShapeName(profile.ShapeType)"))
                    || (runtimeUiSource.Contains("profile.ColorName") && runtimeUiSource.Contains("profile.ShapeName")));
            if (!hasHudContrastPanel)
            {
                throw new InvalidOperationException("Runtime HUD should include HudInfoPanel with HudPanelColor contrast backing.");
            }

            if (!hasHudTextShadow)
            {
                throw new InvalidOperationException("Runtime HUD should apply AddTextShadow to readable HUD text.");
            }

            if (!hasCurrentColorShapeLabel)
            {
                throw new InvalidOperationException("Runtime HUD should include a localized current color/shape label.");
            }

            string[] requiredHudTokens =
            {
                "HudTopAccent",
                "HudProgressFill",
                "HudMistakeIconsText",
                "HudCurrentColorChip",
                "HudCurrentShapeGlyph",
                "HudCurrentText",
                "ComboBadgePanel",
                "ComboBadgeText",
                "FormatMistakeIcons",
                "SetComboBadge",
                "SetCurrentVisual",
                "SetHudProgress"
            };
            foreach (string token in requiredHudTokens)
            {
                if (!runtimeUiSource.Contains(token))
                {
                    throw new InvalidOperationException("Runtime HUD polish hook missing: " + token);
                }
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
            string materialProviderPath = "Assets/_Project/Scripts/Runtime/RuntimeMaterialProvider.cs";
            string[] requiredFiles = { themePath, factoryPath, generatorPath, runtimeUiPath, bootstrapPath, animatorPath, materialProviderPath };
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
            string materialProviderSource = File.ReadAllText(materialProviderPath);
            string combinedSource = themeSource + factorySource + generatorSource + runtimeUiSource + bootstrapSource + animatorSource + materialProviderSource;
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
                "RuntimeMaterialProvider",
                "CreateParticle",
                "ParticleMaterial",
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

        // Verifies project scripts never use string/reflection based Unity component creation.
        private static void EnsureRuntimeComponentCreationSafety()
        {
            string scriptsPath = "Assets/_Project/Scripts";
            if (!Directory.Exists(scriptsPath))
            {
                throw new InvalidOperationException("Missing project scripts folder: " + scriptsPath);
            }

            string[] scripts = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
            foreach (string script in scripts)
            {
                string source = File.ReadAllText(script);
                string normalizedPath = script.Replace('\\', '/');
                if (TryFindUnsafeComponentCreation(source, normalizedPath, out string finding))
                {
                    throw new InvalidOperationException(finding);
                }
            }
        }

        // Finds unsafe string/reflection component creation without flagging validator diagnostic text.
        private static bool TryFindUnsafeComponentCreation(string source, string normalizedPath, out string finding)
        {
            finding = string.Empty;
            string[] lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            Regex addComponentCall = new Regex(@"\bAddComponent\s*\(\s*([^)]+)\)", RegexOptions.Compiled);
            Regex reflectionComponentLookup = new Regex(
                @"\b(?:Type|[A-Za-z_][A-Za-z0-9_]*)\.GetType\s*\(\s*@?""[^""]*(?:BoxCollider|SphereCollider|CapsuleCollider|MeshCollider|MeshRenderer|MeshFilter|Rigidbody|AudioSource|ParticleSystem|Canvas|EventSystem|Camera|Light)[^""]*""",
                RegexOptions.Compiled);

            for (int i = 0; i < lines.Length; i++)
            {
                string codeLine = RemoveLineComment(lines[i]);
                foreach (Match match in addComponentCall.Matches(codeLine))
                {
                    string argument = match.Groups[1].Value.Trim();
                    if (argument.StartsWith("typeof(", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string pattern = argument.StartsWith("\"", StringComparison.Ordinal) || argument.StartsWith("@\"", StringComparison.Ordinal)
                        ? "AddComponent string literal"
                        : "AddComponent non-type argument";
                    finding = BuildUnsafeComponentCreationMessage(normalizedPath, i + 1, pattern, lines[i]);
                    return true;
                }

                Match reflectionMatch = reflectionComponentLookup.Match(codeLine);
                if (reflectionMatch.Success)
                {
                    finding = BuildUnsafeComponentCreationMessage(normalizedPath, i + 1, "Reflection component type lookup", lines[i]);
                    return true;
                }
            }

            return false;
        }

        // Removes simple line comments so documentation examples do not trip code-pattern checks.
        private static string RemoveLineComment(string line)
        {
            int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            return commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
        }

        // Builds an actionable validator message for unsafe component creation findings.
        private static string BuildUnsafeComponentCreationMessage(string path, int line, string pattern, string snippet)
        {
            return "Unsafe string/reflection component usage found:\n"
                + path + ":" + line + "\n"
                + snippet.Trim() + "\n"
                + "Pattern: " + pattern + "\n"
                + "Use AddComponent<T>() or AddComponent(typeof(T)) instead.";
        }

        // Verifies runtime procedural materials use Resources material references instead of variant-heavy Always Included shaders.
        private static void EnsureRuntimeMaterialProviderSafety()
        {
            string providerPath = "Assets/_Project/Scripts/Runtime/RuntimeMaterialProvider.cs";
            string factoryPath = "Assets/_Project/Scripts/Runtime/ProceduralFactory.cs";
            string graphicsPath = "ProjectSettings/GraphicsSettings.asset";
            if (!File.Exists(providerPath) || !File.Exists(factoryPath) || !File.Exists(graphicsPath))
            {
                throw new InvalidOperationException("Missing runtime material validation input files.");
            }

            string providerSource = File.ReadAllText(providerPath);
            string factorySource = File.ReadAllText(factoryPath);
            string runtimePath = "Assets/_Project/Scripts/Runtime";
            string shaderFindToken = "Shader." + "Find";
            List<string> runtimeShaderFindLocations = new List<string>();
            foreach (string script in Directory.GetFiles(runtimePath, "*.cs", SearchOption.AllDirectories))
            {
                if (script.Replace('\\', '/') == providerPath)
                {
                    continue;
                }

                if (File.ReadAllText(script).Contains(shaderFindToken))
                {
                    throw new InvalidOperationException("Runtime Shader.Find must be centralized in RuntimeMaterialProvider: " + script.Replace('\\', '/'));
                }
            }

            CollectSourceLocations(providerPath, shaderFindToken, runtimeShaderFindLocations);

            string[] providerRequiredTokens =
            {
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Particles/Unlit",
                "Resources.Load<Material>",
                "Resources.GetBuiltinResource<Material>",
                "CGR_SimpleLitOpaque",
                "CGR_SimpleLitShard",
                "CGR_SimpleLitTrack",
                "CGR_SimpleLitObstacle",
                "CGR_SimpleLitFinish",
                "CGR_SimpleLitPlayer",
                "CGR_UnlitOpaque",
                "CGR_UnlitTransparent",
                "CGR_ParticleUnlit",
                "CreatePlayerBody",
                "CreatePlayerAccent",
                "IsMaterialUsable"
            };

            foreach (string token in providerRequiredTokens)
            {
                if (!providerSource.Contains(token))
                {
                    throw new InvalidOperationException("RuntimeMaterialProvider missing required shader/material safety token: " + token);
                }
            }

            if (providerSource.Contains("Universal Render Pipeline/Lit"))
            {
                throw new InvalidOperationException("RuntimeMaterialProvider must not use Universal Render Pipeline/Lit for procedural runtime materials.");
            }

            if (providerSource.Contains("return null;"))
            {
                throw new InvalidOperationException("RuntimeMaterialProvider must not return null materials; fail fast or use a supported fallback.");
            }

            string[] forbiddenFactoryTokens =
            {
                "GameObject.CreatePrimitive",
                "Hidden/InternalErrorShader",
                "Shader.Find",
                "\"Standard\"",
                "\"Diffuse\""
            };

            foreach (string token in forbiddenFactoryTokens)
            {
                if (factorySource.Contains(token))
                {
                    throw new InvalidOperationException("ProceduralFactory contains unsafe runtime visual token: " + token);
                }
            }

            string[] factoryRequiredTokens =
            {
                "EnsureMeshFilter",
                "EnsureMeshRenderer",
                "EnsureBoxCollider",
                "EnsureSphereCollider",
                "EnsureCapsuleCollider",
                "GetPrimitiveMesh",
                "ValidateGeneratedVisual",
                "ParticleMaterial",
                "ApplyParticleMaterial"
            };

            foreach (string token in factoryRequiredTokens)
            {
                if (!factorySource.Contains(token))
                {
                    throw new InvalidOperationException("ProceduralFactory missing runtime build visual safety hook: " + token);
                }
            }

            string graphicsSource = File.ReadAllText(graphicsPath);
            EnsureAlwaysIncludedShaderPolicy(graphicsSource);
            EnsureRuntimeMaterialAssets();
            Debug.Log("Color Gate Rush runtime Shader.Find usage is limited to RuntimeMaterialProvider fallback paths: "
                + string.Join(", ", runtimeShaderFindLocations.ToArray()));
        }

        // Fails if variant-heavy URP shaders are placed in Always Included Shaders.
        private static void EnsureAlwaysIncludedShaderPolicy(string graphicsSource)
        {
            string litShaderGuid = "933532a4fcc9baf4fa0491de14d08ed7";
            if (graphicsSource.Contains(litShaderGuid))
            {
                throw new InvalidOperationException("URP/Lit must not be in Always Included Shaders because it causes excessive shader variants on Android.");
            }

            string[] warningShaderGuids =
            {
                "8d2bb70cbf9db8d4da26e15b26e74248",
                "650dd9526735d5b46b79224bc6e94025",
                "0406db5a14f94604a8c57ccfbc9f3b46"
            };

            foreach (string guid in warningShaderGuids)
            {
                if (!graphicsSource.Contains(guid))
                {
                    continue;
                }

                Debug.LogWarning("URP shader guid remains in Always Included Shaders. Prefer Resources material references unless a tiny, intentional variant set is required: " + guid);
            }
        }

        // Verifies Resources base material assets exist and reference limited URP shaders.
        private static void EnsureRuntimeMaterialAssets()
        {
            string materialFolder = "Assets/_Project/Resources/ColorGateRush/Materials";
            if (!Directory.Exists(materialFolder))
            {
                throw new InvalidOperationException("Runtime Resources material folder is missing: " + materialFolder);
            }

            string unlitGuid = "650dd9526735d5b46b79224bc6e94025";
            string particleUnlitGuid = "0406db5a14f94604a8c57ccfbc9f3b46";
            string simpleLitGuid = "8d2bb70cbf9db8d4da26e15b26e74248";
            string litGuid = "933532a4fcc9baf4fa0491de14d08ed7";
            Dictionary<string, string> requiredMaterials = new Dictionary<string, string>
            {
                { "CGR_SimpleLitOpaque.mat", simpleLitGuid },
                { "CGR_SimpleLitShard.mat", simpleLitGuid },
                { "CGR_SimpleLitTrack.mat", simpleLitGuid },
                { "CGR_SimpleLitObstacle.mat", simpleLitGuid },
                { "CGR_SimpleLitFinish.mat", simpleLitGuid },
                { "CGR_SimpleLitPlayer.mat", simpleLitGuid },
                { "CGR_UnlitOpaque.mat", unlitGuid },
                { "CGR_UnlitTransparent.mat", unlitGuid },
                { "CGR_ParticleUnlit.mat", particleUnlitGuid }
            };

            foreach (KeyValuePair<string, string> required in requiredMaterials)
            {
                string path = Path.Combine(materialFolder, required.Key);
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Missing runtime Resources material asset: " + path.Replace('\\', '/'));
                }

                string source = File.ReadAllText(path);
                if (source.Contains(litGuid))
                {
                    throw new InvalidOperationException("Runtime material must not reference URP/Lit: " + path.Replace('\\', '/'));
                }

                if (!source.Contains(required.Value))
                {
                    throw new InvalidOperationException("Runtime material references the wrong shader guid: " + path.Replace('\\', '/'));
                }
            }
        }

        // Records source locations for report/debug logs without broadening allowed Shader.Find usage.
        private static void CollectSourceLocations(string path, string token, List<string> results)
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(token))
                {
                    results.Add(path + ":" + (i + 1));
                }
            }
        }

        // Runs runtime visual safety checks for the release readiness report.
        private static void EnsureRuntimeVisualsForReport()
        {
            EnsureRuntimeComponentCreationSafety();
            EnsureRuntimeMaterialProviderSafety();
            EnsureRuntimeGenerationSmoke();
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
                    LaneRunnerController runner = generator.ClearAndGenerate(null, stage, configureScene: true);
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
                    EnsureGeneratedVisualHealth(generatedRoot, stage);
                    EnsureCameraCanRenderGeneratedObjects(runner, stage);
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

                EndlessRunConfig endlessConfig = EndlessRunConfig.CreateDefault();
                LaneRunnerController endlessRunner = generator.BeginEndless(null, endlessConfig, configureScene: true);
                if (endlessRunner == null)
                {
                    throw new InvalidOperationException("Runtime generation smoke failed: Endless runner was not created.");
                }

                generator.UpdateEndlessGeneration(620f, endlessConfig, 90f);
                Transform endlessRoot = systems.transform.Find("GeneratedLevel");
                if (endlessRoot == null)
                {
                    throw new InvalidOperationException("Runtime generation smoke failed: Endless GeneratedLevel root was not created.");
                }

                StageConfig visualReferenceStage = stageManager.GetStageConfig(5);
                EnsureRunnerPhysics(endlessRunner);
                EnsureGeneratedCount<CollectibleShard>(endlessRoot, 1);
                EnsureGeneratedCount<ColorGate>(endlessRoot, 1);
                EnsureGeneratedCount<ObstacleBlock>(endlessRoot, 1);
                EnsureGeneratedAbsent<FinishLine>(endlessRoot);
                EnsureTriggerColliders<CollectibleShard>(endlessRoot);
                EnsureTriggerColliders<ColorGate>(endlessRoot);
                EnsureTriggerColliders<ObstacleBlock>(endlessRoot);
                EnsureGeneratedVisualHealth(endlessRoot, visualReferenceStage);
                EnsureCameraCanRenderGeneratedObjects(endlessRunner, visualReferenceStage);
                Debug.Log(BuildEndlessSummary(endlessConfig));
                generator.ClearGeneratedLevel();
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

            if (stage.StageIndex == 1 && ratio > 0.97f)
            {
                Debug.LogWarning("Stage 1 three-star ratio may be too strict for first-session playtests: " + ratio.ToString("P1") + ".");
            }

            if ((stage.StageIndex == 10 || stage.StageIndex == 20 || stage.StageIndex == 30) && ratio >= 0.990f)
            {
                Debug.LogWarning("Stage " + stage.StageIndex + " three-star target is almost perfect-only: " + ratio.ToString("P1") + ".");
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

            if (report.TotalRows > 0 && report.RowsWithMultipleMatchingShards > Mathf.CeilToInt(report.TotalRows * 0.20f))
            {
                Debug.LogWarning("Stage " + stage.StageIndex + " has many rows with multiple matching shards; playtesters may overestimate collectible score: "
                    + report.RowsWithMultipleMatchingShards + "/" + report.TotalRows + ".");
            }
        }

        // Builds a compact per-stage balance summary for validator output.
        private static string BuildGenerationSummary(LevelGenerationReport report, StageConfig stage)
        {
            float rowSpacing = Mathf.Max(20f, stage.TrackLength - 34f) / Mathf.Max(1, stage.ShardRowCount);
            float reactionTime = rowSpacing / Mathf.Max(0.1f, stage.PlayerForwardSpeed);
            return "Stage " + report.StageIndex
                + " seed=" + stage.Seed
                + " tier=" + stage.DifficultyTier
                + " theme=" + stage.ThemeIndex
                + " rows=" + report.TotalRows
                + " trackLength=" + stage.TrackLength.ToString("0")
                + " speed=" + stage.PlayerForwardSpeed.ToString("0.00")
                + " rowSpacing=" + rowSpacing.ToString("0.00")
                + " reaction~" + reactionTime.ToString("0.00") + "s"
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
                + " wrongShardLimit=" + GameConstants.MaxWrongShardCount
                + " offColorRoute=mistakeState"
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

        // Builds a compact Endless Mode readiness summary separate from finite stage balance.
        private static string BuildEndlessSummary(EndlessRunConfig config)
        {
            return "Endless Mode"
                + " startSpeed=" + config.StartForwardSpeed.ToString("0.00")
                + " speedGrowth/s=" + config.SpeedGrowthPerSecond.ToString("0.000")
                + " speed@90s~" + config.ForwardSpeed(90f, 900f).ToString("0.00")
                + " rowSpacing=" + config.RowSpacingStart.ToString("0.00") + "-" + config.RowSpacingEnd.ToString("0.00")
                + " reaction~" + (config.RowSpacingStart / Mathf.Max(0.1f, config.StartForwardSpeed)).ToString("0.00") + "s"
                + " gateInterval=" + config.GateIntervalStart.ToString("0") + "-" + config.GateIntervalEnd.ToString("0")
                + " obstacleChance=" + config.ObstacleChanceStart.ToString("P1") + "-" + config.ObstacleChanceMax.ToString("P1")
                + " offColorChance=" + config.OffColorShardChanceStart.ToString("P1") + "-" + config.OffColorShardChanceEnd.ToString("P1")
                + " wrongShardLimit=" + config.WrongShardLimit
                + " generateAhead=" + config.GenerateAheadDistance.ToString("0")
                + " cleanupDistance=" + config.CleanupDistance.ToString("0")
                + " unlock=none stars=none finish=none";
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

        // Verifies a component type is absent when a mode intentionally omits that gameplay object.
        private static void EnsureGeneratedAbsent<T>(Transform generatedRoot) where T : Component
        {
            int count = generatedRoot.GetComponentsInChildren<T>(true).Length;
            if (count > 0)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: expected no " + typeof(T).Name + " objects, found " + count + ".");
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

        // Verifies generated runtime objects have visible renderers, meshes, and supported materials.
        private static void EnsureGeneratedVisualHealth(Transform generatedRoot, StageConfig stage)
        {
            MeshRenderer[] renderers = generatedRoot.GetComponentsInChildren<MeshRenderer>(true);
            MeshFilter[] meshFilters = generatedRoot.GetComponentsInChildren<MeshFilter>(true);
            if (renderers.Length < 20)
            {
                throw new InvalidOperationException("Runtime visual smoke failed: stage " + stage.StageIndex + " generated too few renderers: " + renderers.Length);
            }

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    throw new InvalidOperationException("Runtime visual smoke failed: disabled or missing renderer in stage " + stage.StageIndex + ".");
                }

                EnsureRendererMaterialsAreUsable(generatedRoot, renderer, stage.StageIndex);
                EnsurePlayerBodyRendererIsLit(generatedRoot, renderer, stage.StageIndex);
            }

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    string name = meshFilter != null ? meshFilter.name : "unknown";
                    string objectPath = meshFilter != null ? BuildGeneratedObjectPath(generatedRoot, meshFilter.transform) : "unknown";
                    throw new InvalidOperationException("Runtime visual smoke failed: missing mesh on " + objectPath + " (MeshFilter name=" + name + ") in stage " + stage.StageIndex + ".");
                }
            }
        }

        // Checks every material slot on a generated renderer and reports the exact object path when one is invalid.
        private static void EnsureRendererMaterialsAreUsable(Transform generatedRoot, Renderer renderer, int stageIndex)
        {
            if (renderer == null)
            {
                throw new InvalidOperationException("Runtime visual smoke failed: missing renderer in stage " + stageIndex + ".");
            }

            Material[] materials = renderer.sharedMaterials;
            string objectPath = BuildGeneratedObjectPath(generatedRoot, renderer.transform);
            if (materials == null || materials.Length == 0)
            {
                throw new InvalidOperationException("Runtime visual smoke failed: no materials assigned on " + objectPath + " (" + renderer.GetType().Name + ") in stage " + stageIndex + ".");
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    throw new InvalidOperationException("Runtime visual smoke failed: null material on " + objectPath + " (" + renderer.GetType().Name + "), slot " + i + ", stage " + stageIndex + ".");
                }

                Shader shader = material.shader;
                if (shader == null)
                {
                    throw new InvalidOperationException("Runtime visual smoke failed: null shader on " + objectPath + " (" + renderer.GetType().Name + "), slot " + i + ", material=" + material.name + ", stage " + stageIndex + ".");
                }

                if (!shader.isSupported)
                {
                    throw new InvalidOperationException("Runtime visual smoke failed: unsupported shader on " + objectPath + " (" + renderer.GetType().Name + "), slot " + i + ", material=" + material.name + ", shader=" + shader.name + ", stage " + stageIndex + ".");
                }
            }
        }

        // Ensures the runner body keeps a lit opaque material and shadow settings after build-compatibility changes.
        private static void EnsurePlayerBodyRendererIsLit(Transform generatedRoot, MeshRenderer renderer, int stageIndex)
        {
            if (renderer == null || renderer.GetComponent<LaneRunnerController>() == null)
            {
                return;
            }

            string objectPath = BuildGeneratedObjectPath(generatedRoot, renderer.transform);
            Material material = renderer.sharedMaterial;
            string materialName = material != null ? material.name : "null";
            string shaderName = material != null && material.shader != null ? material.shader.name : "null";
            if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.On || !renderer.receiveShadows)
            {
                throw new InvalidOperationException(
                    "Runtime visual smoke failed: Player body shadow regression on " + objectPath
                    + ", material=" + materialName
                    + ", shader=" + shaderName
                    + ", shadowCastingMode=" + renderer.shadowCastingMode
                    + ", receiveShadows=" + renderer.receiveShadows
                    + ", stage " + stageIndex + ".");
            }

            if (material == null || material.shader == null)
            {
                return;
            }

            if (material.renderQueue >= 3000 || shaderName.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException(
                    "Runtime visual smoke failed: Player body must use an opaque lit-capable material on " + objectPath
                    + ", material=" + materialName
                    + ", shader=" + shaderName
                    + ", renderQueue=" + material.renderQueue
                    + ", stage " + stageIndex + ".");
            }
        }

        // Builds a stable path for generated smoke-test objects without depending on scene asset paths.
        private static string BuildGeneratedObjectPath(Transform root, Transform target)
        {
            if (target == null)
            {
                return "unknown";
            }

            Stack<string> names = new Stack<string>();
            Transform current = target;
            while (current != null)
            {
                names.Push(current.name);
                if (root != null && current == root)
                {
                    break;
                }

                current = current.parent;
            }

            return string.Join("/", names.ToArray());
        }

        // Verifies the generated camera setup can render the default-layer procedural level.
        private static void EnsureCameraCanRenderGeneratedObjects(LaneRunnerController runner, StageConfig stage)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Runtime visual smoke failed: Main Camera missing after stage " + stage.StageIndex + " generation.");
            }

            if ((camera.cullingMask & (1 << 0)) == 0)
            {
                throw new InvalidOperationException("Runtime visual smoke failed: Main Camera culling mask excludes Default layer.");
            }

            if (camera.farClipPlane < stage.TrackLength)
            {
                throw new InvalidOperationException("Runtime visual smoke failed: Main Camera far clip is shorter than stage track length.");
            }

            if (runner == null || runner.transform == null)
            {
                throw new InvalidOperationException("Runtime visual smoke failed: runner target missing for camera validation.");
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

        // Verifies the approved tap-to-start title image and the separate MainMenu background are both wired.
        private static void EnsureMainMenuBackgroundReferences()
        {
            if (!File.Exists(TitleScreenAssetPath))
            {
                throw new InvalidOperationException("Title screen image is missing: " + TitleScreenAssetPath);
            }

            if (!File.Exists(MainMenuBackgroundAssetPath))
            {
                throw new InvalidOperationException("Main menu background image is missing: " + MainMenuBackgroundAssetPath);
            }

            string runtimeUiPath = "Assets/_Project/Scripts/Runtime/RuntimeUi.cs";
            if (!File.Exists(runtimeUiPath))
            {
                throw new InvalidOperationException("RuntimeUi.cs is missing; main menu background cannot be validated.");
            }

            string runtimeUiSource = File.ReadAllText(runtimeUiPath);
            if (!runtimeUiSource.Contains("ColorGateRush/Images/TitleScreen")
                || !runtimeUiSource.Contains("CreateTitlePanel")
                || !runtimeUiSource.Contains("LoadTitleScreenSprite")
                || !runtimeUiSource.Contains("TitleScreenPanel")
                || !runtimeUiSource.Contains("ApplyFullscreenImageFit"))
            {
                throw new InvalidOperationException("RuntimeUi must load the approved tap-to-start title image before MainMenu and fit it without cropping on mobile aspect ratios.");
            }

            if (!runtimeUiSource.Contains("ColorGateRush/Images/MainMenuBackground")
                || !runtimeUiSource.Contains("CreateMenuBackground")
                || !runtimeUiSource.Contains("MainMenuBackgroundReadabilityOverlay"))
            {
                throw new InvalidOperationException("RuntimeUi must load the main menu background from Resources and keep a readability overlay.");
            }

            if (!runtimeUiSource.Contains("TitleText")
                || !runtimeUiSource.Contains("StartButton")
                || !runtimeUiSource.Contains("EndlessModeButton")
                || !runtimeUiSource.Contains("RulesButton")
                || !runtimeUiSource.Contains("SettingsButton")
                || !runtimeUiSource.Contains("QuitButton"))
            {
                throw new InvalidOperationException("MainMenu must keep runtime UI title text and Start/Endless/Rules/Settings/Quit buttons after the title screen.");
            }
        }

        // Fails validation if imported media assets outside the approved BGM clips and approved release images are placed under the project folder.
        private static void EnsureNoProjectRuntimeAssets()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project" });
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project" });
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project" });
            string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets/_Project" });
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
            List<string> disallowedTexturePaths = new List<string>();
            List<string> disallowedAudioPaths = new List<string>();
            foreach (string guid in textureGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (!IsApprovedProjectTextureAsset(assetPath))
                {
                    disallowedTexturePaths.Add(assetPath);
                }
            }

            foreach (string guid in audioGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (!IsApprovedBgmAsset(assetPath))
                {
                    disallowedAudioPaths.Add(assetPath);
                }
            }

            if (disallowedTexturePaths.Count > 0 || disallowedAudioPaths.Count > 0 || modelGuids.Length > 0 || fontGuids.Length > 0 || prefabGuids.Length > 0)
            {
                throw new InvalidOperationException("Imported media found under Assets/_Project. Only the approved BGM clips, tap-to-start title image, main menu background, app icon, archived unused splash art, and retired unused button texture are allowed: "
                    + MenuBgmAssetPath + ", " + GameplayBgmAssetPath + ", " + TitleScreenAssetPath + ", " + MainMenuBackgroundAssetPath + ", " + AppIconAssetPath + ", " + SplashTitleArtAssetPath + ", " + RetiredPrimaryButtonAssetPath
                    + ". Extra textures: " + string.Join(", ", disallowedTexturePaths)
                    + ". Extra audio: " + string.Join(", ", disallowedAudioPaths));
            }
        }

        // Allows only the user-provided UI textures that are intentionally bundled through Resources.
        private static bool IsApprovedProjectTextureAsset(string assetPath)
        {
            return assetPath == TitleScreenAssetPath
                || assetPath == MainMenuBackgroundAssetPath
                || assetPath == AppIconAssetPath
                || assetPath == SplashTitleArtAssetPath
                || assetPath == RetiredPrimaryButtonAssetPath;
        }

        // Allows only the two user-provided BGM clips that are intentionally bundled through Resources.
        private static bool IsApprovedBgmAsset(string assetPath)
        {
            return assetPath == MenuBgmAssetPath || assetPath == GameplayBgmAssetPath;
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
