#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorGateRush.EditorTools
{
    public static class BootstrapColorGateRush
    {
        private const string SceneDirectory = "Assets/_Project/Scenes";
        private const string ScenePath = SceneDirectory + "/Main.unity";
        private const string AppIconAssetPath = "Assets/_Project/Art/AppIcon/color_gate_rush_icon_1024.png";

        [MenuItem("Tools/Color Gate Rush/Bootstrap Project")]
        // Creates the one-scene Color Gate Rush setup and registers it as the build scene.
        public static void BootstrapProject()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject systems = new GameObject("ColorGateRushSystems");
            systems.AddComponent<GameManager>();
            systems.AddComponent<LevelGenerator>();
            systems.AddComponent<ProceduralAudio>();
            systems.AddComponent<RuntimeUi>();

            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 58f;
            camera.backgroundColor = VisualTheme.Current().CameraBackgroundColor;
            EnsureAudioListener(cameraGo);
            cameraGo.AddComponent<CameraFollow>();
            cameraGo.transform.position = new Vector3(0f, 8.5f, -9.5f);
            cameraGo.transform.rotation = Quaternion.Euler(48f, 0f, 0f);

            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = VisualTheme.Current().DirectionalLightColor;
            light.intensity = VisualTheme.Current().DirectionalLightIntensity;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            ApplyRenderSettings();

            ApplyReleasePlayerSettings();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Color Gate Rush bootstrap complete: " + ScenePath);
        }

        [MenuItem("Tools/Color Gate Rush/Apply Visual Theme")]
        // Applies the current code-defined visual theme to the open scene without creating external assets.
        public static void ApplyVisualTheme()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = VisualTheme.Current().CameraBackgroundColor;
                camera.fieldOfView = 58f;
            }

            Light light = FindFirstDirectionalLight();
            if (light == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            light.color = VisualTheme.Current().DirectionalLightColor;
            light.intensity = VisualTheme.Current().DirectionalLightIntensity;
            ApplyRenderSettings();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Color Gate Rush visual theme applied to current scene.");
        }

        // Creates the folder layout expected by the procedural game scripts.
        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "_Project");
            CreateFolderIfMissing("Assets/_Project", "Scenes");
            CreateFolderIfMissing("Assets/_Project", "Scripts");
            CreateFolderIfMissing("Assets/_Project/Scripts", "Runtime");
            CreateFolderIfMissing("Assets/_Project/Scripts", "Editor");
            CreateFolderIfMissing("Assets/_Project", "Art");
            CreateFolderIfMissing("Assets/_Project/Art", "AppIcon");
            Directory.CreateDirectory(SceneDirectory);
        }

        // Creates a child folder through AssetDatabase when Unity has not imported it yet.
        private static void CreateFolderIfMissing(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        // Adds an AudioListener to the generated camera if one is not already present.
        private static void EnsureAudioListener(GameObject cameraGo)
        {
            if (cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }
        }

        // Applies release identity settings that are safe to restore whenever Bootstrap Project is rerun.
        private static void ApplyReleasePlayerSettings()
        {
            PlayerSettings.productName = "Color Gate Rush";
            PlayerSettings.companyName = "Nappa Studio";
            PlayerSettings.bundleVersion = "0.9.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            ConfigureAndroidApplicationIdentifier();
            ConfigureAndroidAppIcon();
        }

        // Applies the Android package id through the legacy PlayerSettings API when it is available.
        private static void ConfigureAndroidApplicationIdentifier()
        {
            MethodInfo method = typeof(PlayerSettings).GetMethod(
                "SetApplicationIdentifier",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(BuildTargetGroup), typeof(string) },
                null);
            if (method == null)
            {
                Debug.LogWarning("Unity PlayerSettings.SetApplicationIdentifier API was not found; verify com.nappa.colorgaterush manually in Player Settings.");
                return;
            }

            method.Invoke(null, new object[] { BuildTargetGroup.Android, "com.nappa.colorgaterush" });
        }

        // Assigns the approved icon through the available Unity PlayerSettings icon API when possible.
        private static void ConfigureAndroidAppIcon()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconAssetPath);
            if (icon == null)
            {
                Debug.LogWarning("Color Gate Rush Android icon asset missing: " + AppIconAssetPath);
                return;
            }

            ApplyIconsForTargetGroup(BuildTargetGroup.Unknown, CreateRepeatedIconArray(icon, GetExpectedIconCount(BuildTargetGroup.Unknown)));
            ApplyIconsForTargetGroup(BuildTargetGroup.Android, CreateRepeatedIconArray(icon, GetExpectedIconCount(BuildTargetGroup.Android)));
        }

        // Applies the same icon set to a Unity build target group when the legacy icon API is available.
        private static void ApplyIconsForTargetGroup(BuildTargetGroup buildTargetGroup, Texture2D[] icons)
        {
            MethodInfo method = typeof(PlayerSettings).GetMethod(
                "SetIconsForTargetGroup",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(BuildTargetGroup), typeof(Texture2D[]) },
                null);
            if (method == null)
            {
                Debug.LogWarning("Unity PlayerSettings.SetIconsForTargetGroup API was not found; verify Android icons manually in Player Settings.");
                return;
            }

            try
            {
                method.Invoke(null, new object[] { buildTargetGroup, icons });
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogWarning("Unity rejected icon assignment for " + buildTargetGroup + ": " + exception.InnerException?.Message);
            }
        }

        // Creates an icon array with the exact count expected by Unity for a target group.
        private static Texture2D[] CreateRepeatedIconArray(Texture2D icon, int count)
        {
            int safeCount = Mathf.Max(1, count);
            Texture2D[] icons = new Texture2D[safeCount];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = icon;
            }

            return icons;
        }

        // Reads Unity's expected icon count for a target group, falling back to one icon when unavailable.
        private static int GetExpectedIconCount(BuildTargetGroup buildTargetGroup)
        {
            MethodInfo method = typeof(PlayerSettings).GetMethod(
                "GetIconSizesForTargetGroup",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(BuildTargetGroup) },
                null);
            if (method == null)
            {
                return buildTargetGroup == BuildTargetGroup.Android ? 6 : 1;
            }

            try
            {
                int[] sizes = method.Invoke(null, new object[] { buildTargetGroup }) as int[];
                return sizes != null && sizes.Length > 0 ? sizes.Length : 1;
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogWarning("Unity icon size query failed for " + buildTargetGroup + ": " + exception.InnerException?.Message);
                return buildTargetGroup == BuildTargetGroup.Android ? 6 : 1;
            }
        }

        // Applies render settings that act as the safe fallback when URP Volume is unavailable.
        private static void ApplyRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogColor = VisualTheme.Current().FogColor;
            RenderSettings.fogDensity = 0.010f;
            RenderSettings.ambientLight = VisualTheme.Current().AmbientColor;
        }

        // Finds the first directional light in the active scene.
        private static Light FindFirstDirectionalLight()
        {
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    return lights[i];
                }
            }

            return null;
        }
    }
}
#endif
