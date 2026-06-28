using UnityEngine;

namespace ColorGateRush
{
    public sealed class LevelGenerator : MonoBehaviour
    {
        private const string GeneratedLevelRootName = "GeneratedLevel";
        private const float GateObstacleBuffer = 7.5f;

        [SerializeField] private int shardRows = 26;
        [SerializeField] private int obstacleRows = 11;
        [SerializeField] private int gateCount = 5;

        private Transform _levelRoot;

        // Clears any generated content and builds a deterministic level for the supplied seed.
        public LaneRunnerController ClearAndGenerate(GameManager manager, int seed)
        {
            ClearExistingLevel();
            Random.InitState(seed);

            GameObject rootGo = new GameObject(GeneratedLevelRootName);
            _levelRoot = rootGo.transform;
            _levelRoot.SetParent(transform, false);

            CreateEnvironment();
            LaneRunnerController runner = CreatePlayer(manager);
            CreateTrack();
            CreateShards(seed);
            CreateGates(seed);
            CreateObstacles(seed);
            CreateFinish();
            ConfigureCamera(runner.transform);
            return runner;
        }

        // Removes existing generated level roots before creating a fresh run.
        private void ClearExistingLevel()
        {
            if (_levelRoot != null)
            {
                DestroyGeneratedLevel(_levelRoot.gameObject);
                _levelRoot = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name.StartsWith(GeneratedLevelRootName))
                {
                    DestroyGeneratedLevel(child.gameObject);
                }
            }

            GameObject existing = GameObject.Find(GeneratedLevelRootName);
            if (existing != null)
            {
                DestroyGeneratedLevel(existing);
            }
        }

        // Disables and destroys a generated root using the correct API for play mode or edit mode.
        private static void DestroyGeneratedLevel(GameObject generatedRoot)
        {
            if (generatedRoot == null)
            {
                return;
            }

            generatedRoot.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(generatedRoot);
            }
            else
            {
                DestroyImmediate(generatedRoot);
            }
        }

        // Applies camera and lighting environment values for the generated runner scene.
        private void CreateEnvironment()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.055f, 0.07f, 0.12f);
                camera.fieldOfView = 58f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 300f;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.055f, 0.07f, 0.12f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.68f);
        }

        // Creates the primitive player sphere and attaches the runner controller.
        private LaneRunnerController CreatePlayer(GameManager manager)
        {
            GameObject player = ProceduralFactory.Primitive(
                PrimitiveType.Sphere,
                "Player",
                _levelRoot,
                new Vector3(0f, GameConstants.PlayerY, 0f),
                Vector3.one * 1.1f,
                ProceduralFactory.ColorMaterial(ColorId.Cyan),
                isTrigger: false);

            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            LaneRunnerController controller = player.AddComponent<LaneRunnerController>();
            controller.Configure(manager, ColorId.Cyan);
            return controller;
        }

        // Builds track slabs and lane guide strips from cube primitives.
        private void CreateTrack()
        {
            int segmentCount = Mathf.CeilToInt(GameConstants.TrackLength / GameConstants.SegmentLength);
            for (int i = 0; i < segmentCount; i++)
            {
                float z = i * GameConstants.SegmentLength + GameConstants.SegmentLength * 0.5f;
                ProceduralFactory.Primitive(
                    PrimitiveType.Cube,
                    "TrackSegment_" + i,
                    _levelRoot,
                    new Vector3(0f, -0.12f, z),
                    new Vector3(GameConstants.TrackWidth, 0.2f, GameConstants.SegmentLength + 0.05f),
                    ProceduralFactory.TrackMaterial(),
                    isTrigger: false);
            }

            foreach (float laneX in GameConstants.LaneX)
            {
                ProceduralFactory.Primitive(
                    PrimitiveType.Cube,
                    "LaneStrip_" + laneX.ToString("0.0"),
                    _levelRoot,
                    new Vector3(laneX, 0.01f, GameConstants.TrackLength * 0.5f),
                    new Vector3(0.08f, 0.04f, GameConstants.TrackLength),
                    ProceduralFactory.LaneStripMaterial(),
                    isTrigger: false);
            }
        }

        // Places deterministic color shards across lanes while leaving periodic gaps.
        private void CreateShards(int seed)
        {
            float startZ = 10f;
            float spacing = (GameConstants.TrackLength - 35f) / shardRows;
            for (int row = 0; row < shardRows; row++)
            {
                float z = startZ + row * spacing;
                int laneToSkip = Random.Range(0, GameConstants.LaneCount);
                for (int lane = 0; lane < GameConstants.LaneCount; lane++)
                {
                    if (lane == laneToSkip && row % 3 == 0)
                    {
                        continue;
                    }

                    ColorId color = (ColorId)((row + lane + seed) % 4);
                    GameObject shard = ProceduralFactory.Primitive(
                        PrimitiveType.Sphere,
                        "Shard",
                        _levelRoot,
                        new Vector3(GameConstants.LaneX[lane], 0.8f, z + Random.Range(-0.8f, 0.8f)),
                        Vector3.one * 0.52f,
                        ProceduralFactory.ColorMaterial(color),
                        isTrigger: true);
                    shard.AddComponent<CollectibleShard>().Configure(color);
                }
            }
        }

        // Creates deterministic color gates along the track.
        private void CreateGates(int seed)
        {
            float firstGateZ = 25f;
            float spacing = (GameConstants.TrackLength - 70f) / Mathf.Max(1, gateCount - 1);
            for (int i = 0; i < gateCount; i++)
            {
                float z = firstGateZ + i * spacing;
                ColorId target = (ColorId)((seed + i + 1) % 4);
                CreateGateRow(i, z, target);
            }
        }

        // Builds a full-width transparent gate trigger with a visible primitive arch.
        private void CreateGateRow(int index, float z, ColorId target)
        {
            Material gateMaterial = ProceduralFactory.TransparentMaterial("gate_" + target, GameConstants.ToUnityColor(target), 0.35f);
            GameObject trigger = ProceduralFactory.Primitive(
                PrimitiveType.Cube,
                "GateTrigger_" + index,
                _levelRoot,
                new Vector3(0f, 1.35f, z),
                new Vector3(GameConstants.TrackWidth, 2.4f, 0.5f),
                gateMaterial,
                isTrigger: true);
            trigger.AddComponent<ColorGate>().Configure(target);

            Material frameMaterial = ProceduralFactory.ColorMaterial(target);
            ProceduralFactory.Primitive(PrimitiveType.Cube, "GateLeftPost_" + index, _levelRoot, new Vector3(-3.8f, 1.2f, z), new Vector3(0.25f, 2.4f, 0.35f), frameMaterial, false);
            ProceduralFactory.Primitive(PrimitiveType.Cube, "GateRightPost_" + index, _levelRoot, new Vector3(3.8f, 1.2f, z), new Vector3(0.25f, 2.4f, 0.35f), frameMaterial, false);
            ProceduralFactory.Primitive(PrimitiveType.Cube, "GateTop_" + index, _levelRoot, new Vector3(0f, 2.45f, z), new Vector3(7.8f, 0.25f, 0.35f), frameMaterial, false);
        }

        // Places obstacle triggers away from gate rows so each gate has readable recovery space.
        private void CreateObstacles(int seed)
        {
            float startZ = 18f;
            float spacing = (GameConstants.TrackLength - 45f) / obstacleRows;
            for (int row = 0; row < obstacleRows; row++)
            {
                float z = startZ + row * spacing + Random.Range(-1.5f, 1.5f);
                z = ResolveObstacleZ(z);

                int lane = Random.Range(0, GameConstants.LaneCount);
                GameObject obstacle = ProceduralFactory.Primitive(
                    PrimitiveType.Cube,
                    "Obstacle_" + row,
                    _levelRoot,
                    new Vector3(GameConstants.LaneX[lane], 0.65f, z),
                    new Vector3(1.25f, 1.25f, 1.25f),
                    ProceduralFactory.ObstacleMaterial(),
                    isTrigger: true);
                obstacle.AddComponent<ObstacleBlock>();
            }
        }

        // Moves an obstacle z-position outside gate buffer zones while keeping it on the track.
        private float ResolveObstacleZ(float proposedZ)
        {
            float z = Mathf.Clamp(proposedZ, 12f, GameConstants.TrackLength - 18f);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float nearestGateZ = FindNearestGateZ(z);
                if (nearestGateZ < 0f || Mathf.Abs(z - nearestGateZ) >= GateObstacleBuffer)
                {
                    return z;
                }

                float direction = z >= nearestGateZ ? 1f : -1f;
                z = Mathf.Clamp(nearestGateZ + direction * GateObstacleBuffer, 12f, GameConstants.TrackLength - 18f);
            }

            return z;
        }

        // Finds the nearest generated gate z-position for obstacle spacing checks.
        private float FindNearestGateZ(float z)
        {
            float nearestGateZ = -1f;
            float nearestDistance = float.MaxValue;
            float firstGateZ = 25f;
            float spacing = (GameConstants.TrackLength - 70f) / Mathf.Max(1, gateCount - 1);
            for (int i = 0; i < gateCount; i++)
            {
                float gateZ = firstGateZ + i * spacing;
                float distance = Mathf.Abs(z - gateZ);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGateZ = gateZ;
                }
            }

            return nearestGateZ;
        }

        // Reports whether the given z-position is inside any gate buffer zone.
        private bool IsTooCloseToGate(float z)
        {
            float firstGateZ = 25f;
            float spacing = (GameConstants.TrackLength - 70f) / Mathf.Max(1, gateCount - 1);
            for (int i = 0; i < gateCount; i++)
            {
                if (Mathf.Abs(z - (firstGateZ + i * spacing)) < GateObstacleBuffer)
                {
                    return true;
                }
            }

            return false;
        }

        // Creates the finish trigger and visible finish arch near the track end.
        private void CreateFinish()
        {
            float z = GameConstants.TrackLength - 8f;
            GameObject trigger = ProceduralFactory.Primitive(
                PrimitiveType.Cube,
                "FinishTrigger",
                _levelRoot,
                new Vector3(0f, 1.25f, z),
                new Vector3(GameConstants.TrackWidth, 2.5f, 0.55f),
                ProceduralFactory.TransparentMaterial("finish_alpha", Color.white, 0.25f),
                isTrigger: true);
            trigger.AddComponent<FinishLine>();

            Material finishMaterial = ProceduralFactory.FinishMaterial();
            ProceduralFactory.Primitive(PrimitiveType.Cube, "FinishLeftPost", _levelRoot, new Vector3(-3.8f, 1.35f, z), new Vector3(0.35f, 2.7f, 0.45f), finishMaterial, false);
            ProceduralFactory.Primitive(PrimitiveType.Cube, "FinishRightPost", _levelRoot, new Vector3(3.8f, 1.35f, z), new Vector3(0.35f, 2.7f, 0.45f), finishMaterial, false);
            ProceduralFactory.Primitive(PrimitiveType.Cube, "FinishTop", _levelRoot, new Vector3(0f, 2.75f, z), new Vector3(7.8f, 0.35f, 0.45f), finishMaterial, false);
        }

        // Ensures a main camera exists and follows the current runner.
        private void ConfigureCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraGo = new GameObject("Main Camera");
                camera = cameraGo.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            EnsureAudioListener(camera.gameObject);

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<CameraFollow>();
            }

            follow.SetTarget(target);
        }

        // Adds an AudioListener to the active camera when the bootstrapped scene lacks one.
        private static void EnsureAudioListener(GameObject cameraGo)
        {
            if (cameraGo != null && cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }
        }
    }
}
