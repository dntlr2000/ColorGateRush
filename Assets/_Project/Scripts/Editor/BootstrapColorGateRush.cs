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
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.12f);
            EnsureAudioListener(cameraGo);
            cameraGo.AddComponent<CameraFollow>();
            cameraGo.transform.position = new Vector3(0f, 8.5f, -9.5f);
            cameraGo.transform.rotation = Quaternion.Euler(48f, 0f, 0f);

            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Color Gate Rush bootstrap complete: " + ScenePath);
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
    }
}
#endif
