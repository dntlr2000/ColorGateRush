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
            EnsureSceneAssetExists();
            EnsureBuildSceneRegistered();
            EnsureTypeExists<GameManager>();
            EnsureTypeExists<LevelGenerator>();
            EnsureTypeExists<LaneRunnerController>();
            EnsureTypeExists<RuntimeUi>();
            EnsureTypeExists<ProceduralAudio>();
            EnsureInputConfiguration();
            EnsureSceneContents();
            EnsureNoProjectRuntimeAssets();
            WarnAboutTemplateAssets();

            Debug.Log("Color Gate Rush validation passed. Open Main.unity and enter Play Mode for runtime acceptance checks.");
        }

        // Provides a stable entry point for Unity batchmode validation.
        public static void ValidateFromCommandLine()
        {
            Validate();
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
                EnsureRuntimeGenerationSmoke(scene);
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

        // Generates a temporary level in edit mode and verifies core gameplay object categories exist.
        private static void EnsureRuntimeGenerationSmoke(Scene scene)
        {
            GameManager manager = FindSceneComponent<GameManager>(scene);
            if (manager == null)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: missing GameManager.");
            }

            LevelGenerator generator = manager.GetComponent<LevelGenerator>();
            if (generator == null)
            {
                throw new InvalidOperationException("Runtime generation smoke failed: missing LevelGenerator on GameManager.");
            }

            try
            {
                LaneRunnerController runner = generator.ClearAndGenerate(manager, 777);
                if (runner == null)
                {
                    throw new InvalidOperationException("Runtime generation smoke failed: runner was not created.");
                }

                EnsureRunnerPhysics(runner);
                Transform generatedRoot = manager.transform.Find("GeneratedLevel");
                if (generatedRoot == null)
                {
                    throw new InvalidOperationException("Runtime generation smoke failed: GeneratedLevel root was not created.");
                }

                EnsureGeneratedCount<CollectibleShard>(generatedRoot, 1);
                EnsureGeneratedCount<ColorGate>(generatedRoot, 1);
                EnsureGeneratedCount<ObstacleBlock>(generatedRoot, 1);
                EnsureGeneratedCount<FinishLine>(generatedRoot, 1);
                EnsureTriggerColliders<CollectibleShard>(generatedRoot);
                EnsureTriggerColliders<ColorGate>(generatedRoot);
                EnsureTriggerColliders<ObstacleBlock>(generatedRoot);
                EnsureTriggerColliders<FinishLine>(generatedRoot);
            }
            finally
            {
                ClearSmokeGeneratedLevel(manager.transform);
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
            if (textureGuids.Length > 0 || audioGuids.Length > 0)
            {
                throw new InvalidOperationException("Imported Texture2D or AudioClip assets found under Assets/_Project. MVP assets must be procedural.");
            }
        }

        // Warns about Unity template assets that are outside the generated game folder and not used by the MVP.
        private static void WarnAboutTemplateAssets()
        {
            WarnAboutAssetsOutsideProject("t:Texture2D", "texture assets");
            WarnAboutAssetsOutsideProject("t:AudioClip", "audio assets");
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
