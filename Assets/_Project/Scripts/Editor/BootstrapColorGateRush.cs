#if UNITY_EDITOR
using System.IO;
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

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
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
