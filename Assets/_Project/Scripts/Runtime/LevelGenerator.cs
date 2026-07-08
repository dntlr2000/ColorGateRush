using System.Collections.Generic;
using UnityEngine;

namespace ColorGateRush
{
    public sealed class LevelGenerator : MonoBehaviour
    {
        private const string GeneratedLevelRootName = "GeneratedLevel";

        [SerializeField] private int shardRows = 26;

        private Transform _levelRoot;
        private float _trackLength = GameConstants.TrackLength;
        private LevelGenerationReport _lastReport;

        public LevelGenerationReport LastReport => _lastReport;

        private readonly struct GatePlan
        {
            public readonly int Index;
            public readonly float Z;
            public readonly ColorId TargetColor;

            // Stores a mandatory full-width gate position and its resulting player color.
            public GatePlan(int index, float z, ColorId targetColor)
            {
                Index = index;
                Z = z;
                TargetColor = targetColor;
            }
        }

        private delegate void LaneObjectCreator(int rowIndex, int laneIndex, float rowZ, GeneratedLaneContent content, ColorId color);

        // Clears any generated content and builds a deterministic level for the supplied seed.
        public LaneRunnerController ClearAndGenerate(GameManager manager, int seed, bool configureScene = true)
        {
            StageConfig stage = new StageConfig(1, seed, GameConstants.TrackLength, shardRows, 0.02f, 0.22f, 0.30f, 0.18f, 62f, 4, 105, 146, GameConstants.BaseForwardSpeed, GameConstants.LaneMoveSharpness);
            return ClearAndGenerate(manager, stage, configureScene);
        }

        // Clears any generated content and builds a deterministic level for the supplied stage.
        public LaneRunnerController ClearAndGenerate(GameManager manager, StageConfig stage, bool configureScene = true)
        {
            ClearExistingLevel();
            VisualTheme.SetActiveThemeIndex(stage.ThemeIndex);
            Random.InitState(stage.Seed);
            _trackLength = stage.TrackLength;
            _lastReport = new LevelGenerationReport(stage.StageIndex, stage.Seed);
            List<GatePlan> gatePlans = BuildGatePlans(stage);

            GameObject rootGo = new GameObject(GeneratedLevelRootName);
            _levelRoot = rootGo.transform;
            _levelRoot.SetParent(transform, false);

            if (configureScene)
            {
                CreateEnvironment();
            }

            CreateBackground(stage);
            LaneRunnerController runner = CreatePlayer(manager, stage);
            CreateTrack(stage);
            CreateGates(gatePlans);
            CreateRows(stage, gatePlans);
            CreateFinish(stage);
            if (configureScene)
            {
                ConfigureCamera(runner.transform);
            }

            ValidateSpawnedRuntimeObjects(runner);
            _lastReport.RecordScoreEstimate(StageScoreAnalyzer.AnalyzeReport(stage, _lastReport));
            ValidateGeneratedLevel(_lastReport);
            return runner;
        }

        // Builds deterministic generation metadata for balance analysis without creating scene objects.
        public static LevelGenerationReport BuildGenerationReport(StageConfig stage)
        {
            Random.State previousState = Random.state;
            Random.InitState(stage.Seed);
            try
            {
                LevelGenerationReport report = new LevelGenerationReport(stage.StageIndex, stage.Seed);
                List<GatePlan> gatePlans = BuildGatePlans(stage);
                for (int i = 0; i < gatePlans.Count; i++)
                {
                    report.RecordGate(gatePlans[i].Index, gatePlans[i].Z, gatePlans[i].TargetColor);
                }

                GenerateRows(stage, gatePlans, report, null);
                report.RecordFinish();
                return report;
            }
            finally
            {
                Random.state = previousState;
            }
        }

        // Clears generated level content without starting a new run.
        public void ClearGeneratedLevel()
        {
            ClearExistingLevel();
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
            if (existing != null && existing.transform.IsChildOf(transform))
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
            VisualThemeProfile theme = VisualTheme.Current();
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = theme.CameraBackgroundColor;
                camera.fieldOfView = 58f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 300f;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = theme.FogColor;
            RenderSettings.fogDensity = 0.010f;
            RenderSettings.ambientLight = theme.AmbientColor;
            RenderSettings.skybox = null;
            ApplySceneLighting(theme);
        }

        // Applies theme lighting to the active directional light or creates one when the scene lacks it.
        private void ApplySceneLighting(VisualThemeProfile theme)
        {
            Light directionalLight = null;
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    directionalLight = lights[i];
                    break;
                }
            }

            if (directionalLight == null)
            {
                GameObject lightGo = new GameObject("VisualThemeDirectionalLight");
                lightGo.transform.SetParent(_levelRoot, false);
                directionalLight = lightGo.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            directionalLight.color = theme.DirectionalLightColor;
            directionalLight.intensity = theme.DirectionalLightIntensity;
            directionalLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        // Builds low-cost procedural backdrop panels that replace the default Unity skybox feel.
        private void CreateBackground(StageConfig stage)
        {
            Transform backgroundRoot = CreateChildRoot("BackgroundRoot");
            float centerZ = stage.TrackLength * 0.5f;
            float length = stage.TrackLength + 36f;

            ProceduralFactory.VisualPrimitive(
                PrimitiveType.Cube,
                "BackdropBottomPanel",
                backgroundRoot,
                new Vector3(0f, 2.2f, centerZ + 16f),
                new Vector3(34f, 4.4f, 0.18f),
                ProceduralFactory.BackdropBottomMaterial());
            ProceduralFactory.VisualPrimitive(
                PrimitiveType.Cube,
                "BackdropTopPanel",
                backgroundRoot,
                new Vector3(0f, 6.8f, centerZ + 20f),
                new Vector3(42f, 5.6f, 0.18f),
                ProceduralFactory.BackdropTopMaterial());

            for (int side = -1; side <= 1; side += 2)
            {
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "SideGlowPanel_" + side,
                    backgroundRoot,
                    new Vector3(side * 6.6f, 1.35f, centerZ),
                    new Vector3(0.12f, 2.4f, length),
                    ProceduralFactory.SidePanelMaterial());
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "SideHorizonLine_" + side,
                    backgroundRoot,
                    new Vector3(side * 5.25f, 0.18f, centerZ),
                    new Vector3(0.08f, 0.06f, length),
                    ProceduralFactory.TrackAccentMaterial());
            }
        }

        // Creates the primitive player sphere and attaches the runner controller.
        private LaneRunnerController CreatePlayer(GameManager manager, StageConfig stage)
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
            controller.Configure(manager, ColorId.Cyan, stage.PlayerForwardSpeed, stage.LaneMoveSpeed);
            return controller;
        }

        // Builds track slabs and lane guide strips from cube primitives.
        private void CreateTrack(StageConfig stage)
        {
            Transform visualRoot = CreateChildRoot("TrackVisualRoot");
            int segmentCount = Mathf.CeilToInt(stage.TrackLength / GameConstants.SegmentLength);
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
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "LaneStrip_" + laneX.ToString("0.0"),
                    visualRoot,
                    new Vector3(laneX, 0.01f, stage.TrackLength * 0.5f),
                    new Vector3(0.08f, 0.04f, stage.TrackLength),
                    ProceduralFactory.LaneStripMaterial());
            }

            CreateTrackPolish(visualRoot, stage);
        }

        // Adds rails, lane separators, and rhythm stripes without changing gameplay collision.
        private void CreateTrackPolish(Transform visualRoot, StageConfig stage)
        {
            float centerZ = stage.TrackLength * 0.5f;
            float length = stage.TrackLength + 1f;
            float railX = GameConstants.TrackWidth * 0.5f + 0.12f;
            for (int side = -1; side <= 1; side += 2)
            {
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "TrackEdgeRail_" + side,
                    visualRoot,
                    new Vector3(side * railX, 0.12f, centerZ),
                    new Vector3(0.16f, 0.22f, length),
                    ProceduralFactory.TrackEdgeMaterial());
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "TrackEdgeGlow_" + side,
                    visualRoot,
                    new Vector3(side * (railX - 0.23f), 0.035f, centerZ),
                    new Vector3(0.05f, 0.05f, length),
                    ProceduralFactory.TrackAccentMaterial());
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "TrackSideLightStrip_" + side,
                    visualRoot,
                    new Vector3(side * (railX + 0.18f), 0.075f, centerZ),
                    new Vector3(0.055f, 0.10f, length * 0.96f),
                    ProceduralFactory.TrackAccentMaterial());
            }

            float[] separators = { -GameConstants.LaneSpacing * 0.5f, GameConstants.LaneSpacing * 0.5f };
            for (int i = 0; i < separators.Length; i++)
            {
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "LaneSeparator_" + i,
                    visualRoot,
                    new Vector3(separators[i], 0.035f, centerZ),
                    new Vector3(0.045f, 0.04f, length),
                    ProceduralFactory.TrackAccentMaterial());
            }

            int stripeIndex = 0;
            for (float z = 9f; z < stage.TrackLength - 10f; z += 18f)
            {
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "TrackRhythmStripe_" + stripeIndex.ToString("00"),
                    visualRoot,
                    new Vector3(0f, 0.045f, z),
                    new Vector3(GameConstants.TrackWidth - 0.45f, 0.035f, 0.16f),
                    ProceduralFactory.TrackAccentMaterial());
                stripeIndex++;
            }
        }

        // Creates a named child root under the generated level root for validator-friendly visual grouping.
        private Transform CreateChildRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(_levelRoot, false);
            return root.transform;
        }

        // Places deterministic color shards across lanes while leaving periodic gaps.
        // Creates one deterministic three-lane decision row at each shared row z position.
        private void CreateRows(StageConfig stage, IReadOnlyList<GatePlan> gatePlans)
        {
            GenerateRows(stage, gatePlans, _lastReport, CreateLaneObject);
        }

        // Creates one gameplay object for a generated row lane after report data has been recorded.
        private void CreateLaneObject(int rowIndex, int laneIndex, float rowZ, GeneratedLaneContent content, ColorId color)
        {
            if (content == GeneratedLaneContent.MatchingShard || content == GeneratedLaneContent.OffColorShard)
            {
                CreateShard(rowIndex, laneIndex, rowZ, color);
            }
            else if (content == GeneratedLaneContent.Obstacle)
            {
                CreateObstacle(rowIndex, laneIndex, rowZ);
            }
        }

        // Generates row reports and optionally creates matching scene objects using the same random stream.
        private static void GenerateRows(StageConfig stage, IReadOnlyList<GatePlan> gatePlans, LevelGenerationReport report, LaneObjectCreator createObject)
        {
            for (int rowIndex = 0; rowIndex < stage.ShardRowCount; rowIndex++)
            {
                float rowZ = GetRowZ(stage, rowIndex);
                ColorId expectedColor = GetExpectedColorAtZ(rowZ, gatePlans);
                GeneratedLaneContent[] contents = IsNearGateRow(rowZ, gatePlans) ? CreateEmptyRow() : GenerateRow(stage);
                UnsafeRowReason repairReason = EvaluateRowSafety(contents);
                if (repairReason != UnsafeRowReason.None)
                {
                    report.RecordRepair(repairReason);
                    RepairUnsafeRow(contents, stage, rowIndex);
                }

                LevelRowReport rowReport = BuildRowReport(rowIndex, rowZ, expectedColor, contents);
                report.RecordRow(rowReport);

                for (int laneIndex = 0; laneIndex < GameConstants.LaneCount; laneIndex++)
                {
                    ColorId objectColor = expectedColor;
                    if (contents[laneIndex] == GeneratedLaneContent.OffColorShard)
                    {
                        objectColor = RandomOffColor(stage, expectedColor);
                    }

                    createObject?.Invoke(rowIndex, laneIndex, rowZ, contents[laneIndex], objectColor);
                }
            }
        }

        // Computes the single z coordinate shared by all objects in one row.
        private static float GetRowZ(StageConfig stage, int rowIndex)
        {
            float usableLength = Mathf.Max(20f, stage.TrackLength - 34f);
            float spacing = usableLength / Mathf.Max(1, stage.ShardRowCount);
            return 10f + spacing * rowIndex;
        }

        // Returns the fixed lane x coordinate for a lane index.
        private static float GetLaneX(int laneIndex)
        {
            return GameConstants.LaneX[Mathf.Clamp(laneIndex, 0, GameConstants.LaneCount - 1)];
        }

        // Chooses row contents from stage probabilities before fairness repair.
        private static GeneratedLaneContent[] GenerateRow(StageConfig stage)
        {
            GeneratedLaneContent[] contents = new GeneratedLaneContent[GameConstants.LaneCount];
            for (int laneIndex = 0; laneIndex < GameConstants.LaneCount; laneIndex++)
            {
                float roll = Random.value;
                if (roll < stage.ObstacleChance)
                {
                    contents[laneIndex] = GeneratedLaneContent.Obstacle;
                }
                else if (roll < stage.ObstacleChance + stage.MatchingShardChance)
                {
                    contents[laneIndex] = GeneratedLaneContent.MatchingShard;
                }
                else if (roll < stage.ObstacleChance + stage.MatchingShardChance + stage.OffColorShardChance)
                {
                    contents[laneIndex] = GeneratedLaneContent.OffColorShard;
                }
                else
                {
                    contents[laneIndex] = GeneratedLaneContent.Empty;
                }
            }

            if (Random.value < stage.SafeEmptyLaneChance)
            {
                OpenEmptyLaneIfNeeded(contents);
            }

            ImproveSparseRow(contents, stage);
            SoftenEarlyObstacleRows(contents, stage);
            LimitUniformMatchingRow(contents);
            return contents;
        }

        // Opens one empty lane only when the row currently has no empty space.
        private static void OpenEmptyLaneIfNeeded(GeneratedLaneContent[] contents)
        {
            if (CountContent(contents, GeneratedLaneContent.Empty) > 0)
            {
                return;
            }

            contents[Random.Range(0, GameConstants.LaneCount)] = GeneratedLaneContent.Empty;
        }

        // Adds shards to sparse rows so early stages feel collectible without forcing every row to match.
        private static void ImproveSparseRow(GeneratedLaneContent[] contents, StageConfig stage)
        {
            int shardCount = CountShards(contents);
            int obstacleCount = CountContent(contents, GeneratedLaneContent.Obstacle);
            if (shardCount == 0 && obstacleCount <= 1 && Random.value < GetShardRowFillChance(stage))
            {
                float matchingChance = obstacleCount > 0 ? 1f : GetAddedShardMatchingChance(stage);
                AddShardToEmptyLane(contents, matchingChance);
                shardCount = CountShards(contents);
            }

            if (obstacleCount > 0 || shardCount != 1)
            {
                return;
            }

            float secondShardChance = stage.StageIndex <= 2 ? 0.58f : (stage.StageIndex == 3 ? 0.42f : 0.24f);
            if (Random.value < secondShardChance)
            {
                AddShardToEmptyLane(contents, GetAddedShardMatchingChance(stage));
            }
        }

        // Converts one early double-obstacle lane into a shard so tutorials stay reward-led.
        private static void SoftenEarlyObstacleRows(GeneratedLaneContent[] contents, StageConfig stage)
        {
            if (stage.StageIndex > 2 || CountContent(contents, GeneratedLaneContent.Obstacle) < 2)
            {
                return;
            }

            int lane = FindFirstLane(contents, GeneratedLaneContent.Obstacle);
            if (lane >= 0)
            {
                contents[lane] = GeneratedLaneContent.MatchingShard;
            }
        }

        // Avoids visually flat rows made from three expected-color shards.
        private static void LimitUniformMatchingRow(GeneratedLaneContent[] contents)
        {
            if (CountContent(contents, GeneratedLaneContent.MatchingShard) != GameConstants.LaneCount)
            {
                return;
            }

            contents[Random.Range(0, GameConstants.LaneCount)] = GeneratedLaneContent.OffColorShard;
        }

        // Adds one shard into a random empty lane when possible.
        private static bool AddShardToEmptyLane(GeneratedLaneContent[] contents, float matchingChance)
        {
            int emptyCount = CountContent(contents, GeneratedLaneContent.Empty);
            if (emptyCount <= 0)
            {
                return false;
            }

            int targetEmpty = Random.Range(0, emptyCount);
            int seenEmpty = 0;
            for (int laneIndex = 0; laneIndex < contents.Length; laneIndex++)
            {
                if (contents[laneIndex] != GeneratedLaneContent.Empty)
                {
                    continue;
                }

                if (seenEmpty == targetEmpty)
                {
                    contents[laneIndex] = Random.value < matchingChance
                        ? GeneratedLaneContent.MatchingShard
                        : GeneratedLaneContent.OffColorShard;
                    return true;
                }

                seenEmpty++;
            }

            return false;
        }

        // Counts matching or off-color shards in one row.
        private static int CountShards(GeneratedLaneContent[] contents)
        {
            return CountContent(contents, GeneratedLaneContent.MatchingShard)
                + CountContent(contents, GeneratedLaneContent.OffColorShard);
        }

        // Counts one content type in a generated row.
        private static int CountContent(GeneratedLaneContent[] contents, GeneratedLaneContent target)
        {
            int count = 0;
            for (int i = 0; i < contents.Length; i++)
            {
                if (contents[i] == target)
                {
                    count++;
                }
            }

            return count;
        }

        // Finds the first lane containing the requested row content.
        private static int FindFirstLane(GeneratedLaneContent[] contents, GeneratedLaneContent target)
        {
            for (int i = 0; i < contents.Length; i++)
            {
                if (contents[i] == target)
                {
                    return i;
                }
            }

            return -1;
        }

        // Returns target shard-row fill chance for sparse-row recovery.
        private static float GetShardRowFillChance(StageConfig stage)
        {
            if (stage.StageIndex == 1)
            {
                return 0.88f;
            }

            if (stage.StageIndex == 2)
            {
                return 0.82f;
            }

            if (stage.StageIndex == 3)
            {
                return 0.76f;
            }

            return Mathf.Clamp(0.70f - (stage.StageIndex - 4) * 0.02f, 0.58f, 0.70f);
        }

        // Returns the chance that a density-recovery shard is the current expected color.
        private static float GetAddedShardMatchingChance(StageConfig stage)
        {
            if (stage.StageIndex == 1)
            {
                return 0.45f;
            }

            if (stage.StageIndex == 2)
            {
                return 0.40f;
            }

            if (stage.StageIndex == 3)
            {
                return 0.35f;
            }

            return 0.30f;
        }

        // Creates a fully neutral row, usually used as breathing space near mandatory gates.
        private static GeneratedLaneContent[] CreateEmptyRow()
        {
            return new GeneratedLaneContent[GameConstants.LaneCount];
        }

        // Checks whether a decision row should leave space for a mandatory gate.
        private static bool IsNearGateRow(float rowZ, IReadOnlyList<GatePlan> gatePlans)
        {
            for (int i = 0; i < gatePlans.Count; i++)
            {
                if (Mathf.Abs(rowZ - gatePlans[i].Z) < 3.5f)
                {
                    return true;
                }
            }

            return false;
        }

        // Checks whether a row gives the player at least one neutral or beneficial option.
        private static UnsafeRowReason EvaluateRowSafety(GeneratedLaneContent[] contents)
        {
            int obstacles = 0;
            int offColorShards = 0;
            int safeOptions = 0;

            for (int i = 0; i < contents.Length; i++)
            {
                if (contents[i] == GeneratedLaneContent.Obstacle)
                {
                    obstacles++;
                }
                else if (contents[i] == GeneratedLaneContent.OffColorShard)
                {
                    offColorShards++;
                }
                else
                {
                    safeOptions++;
                }
            }

            if (obstacles == GameConstants.LaneCount)
            {
                return UnsafeRowReason.AllObstacle;
            }

            if (offColorShards == GameConstants.LaneCount)
            {
                return UnsafeRowReason.AllOffColor;
            }

            if (safeOptions == 0)
            {
                return UnsafeRowReason.MixedUnsafe;
            }

            return UnsafeRowReason.None;
        }

        // Repairs an unsafe row with early stages favoring collectible recovery over empty lanes.
        private static void RepairUnsafeRow(GeneratedLaneContent[] contents, StageConfig stage, int rowIndex)
        {
            if (stage.StageIndex <= 2)
            {
                if (ReplaceFirstContent(contents, GeneratedLaneContent.Obstacle, GeneratedLaneContent.MatchingShard)
                    || ReplaceFirstContent(contents, GeneratedLaneContent.OffColorShard, GeneratedLaneContent.MatchingShard))
                {
                    return;
                }
            }
            else
            {
                if (ReplaceFirstContent(contents, GeneratedLaneContent.Obstacle, GeneratedLaneContent.Empty)
                    || ReplaceFirstContent(contents, GeneratedLaneContent.OffColorShard, GeneratedLaneContent.MatchingShard))
                {
                    return;
                }
            }

            int repairLane = Mathf.Abs(stage.Seed + rowIndex) % GameConstants.LaneCount;
            contents[repairLane] = GeneratedLaneContent.Empty;
        }

        // Replaces the first lane with the requested content and reports whether it changed.
        private static bool ReplaceFirstContent(
            GeneratedLaneContent[] contents,
            GeneratedLaneContent source,
            GeneratedLaneContent replacement)
        {
            int lane = FindFirstLane(contents, source);
            if (lane < 0)
            {
                return false;
            }

            contents[lane] = replacement;
            return true;
        }

        // Builds validator-friendly row metadata from final lane contents.
        private static LevelRowReport BuildRowReport(
            int rowIndex,
            float rowZ,
            ColorId expectedColor,
            GeneratedLaneContent[] contents)
        {
            return new LevelRowReport(rowIndex, rowZ, expectedColor, contents[0], contents[1], contents[2], true);
        }

        // Creates one shard at the exact row and lane coordinate.
        private void CreateShard(int rowIndex, int laneIndex, float rowZ, ColorId color)
        {
            Vector3 position = new Vector3(GetLaneX(laneIndex), 0.8f, rowZ);
            GameObject shard = ProceduralFactory.CreateShardVisual(
                _levelRoot,
                $"Shard_Row{rowIndex:00}_Lane{laneIndex}",
                position,
                color);
            shard.AddComponent<CollectibleShard>().Configure(color);
            shard.AddComponent<ShardVisualAnimator>().Configure(rowIndex, laneIndex);
        }

        // Creates one obstacle at the exact row and lane coordinate.
        private void CreateObstacle(int rowIndex, int laneIndex, float rowZ)
        {
            Vector3 position = new Vector3(GetLaneX(laneIndex), 0.65f, rowZ);
            GameObject obstacle = ProceduralFactory.Primitive(
                PrimitiveType.Cube,
                $"Obstacle_Row{rowIndex:00}_Lane{laneIndex}",
                _levelRoot,
                position,
                new Vector3(1.25f, 1.25f, 1.25f),
                ProceduralFactory.ObstacleMaterial(),
                isTrigger: true);
            obstacle.AddComponent<ObstacleBlock>();
            CreateObstacleDetails(rowIndex, laneIndex, rowZ, position);
        }

        // Adds visual-only warning stripes and spikes to make obstacle lanes read as dangerous.
        private void CreateObstacleDetails(int rowIndex, int laneIndex, float rowZ, Vector3 basePosition)
        {
            string suffix = "_Row" + rowIndex.ToString("00") + "_Lane" + laneIndex;
            ProceduralFactory.VisualPrimitive(
                PrimitiveType.Cube,
                "ObstacleWarningTop" + suffix,
                _levelRoot,
                basePosition + new Vector3(0f, 0.66f, 0f),
                new Vector3(1.32f, 0.08f, 0.18f),
                ProceduralFactory.ObstacleWarningMaterial());
            ProceduralFactory.VisualPrimitive(
                PrimitiveType.Cube,
                "ObstacleWarningFront" + suffix,
                _levelRoot,
                new Vector3(basePosition.x, basePosition.y + 0.18f, rowZ - 0.64f),
                new Vector3(1.16f, 0.10f, 0.06f),
                ProceduralFactory.ObstacleWarningMaterial());

            for (int i = 0; i < 3; i++)
            {
                float offsetX = (i - 1) * 0.34f;
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "ObstacleSpike" + i + suffix,
                    _levelRoot,
                    new Vector3(basePosition.x + offsetX, basePosition.y + 0.88f, rowZ),
                    new Vector3(0.18f, 0.40f, 0.18f),
                    ProceduralFactory.ObstacleWarningMaterial());
            }
        }

        // Picks a random stage color that differs from the expected player color.
        private static ColorId RandomOffColor(StageConfig stage, ColorId expectedColor)
        {
            if (stage.AvailableColorCount <= 1)
            {
                return expectedColor;
            }

            ColorId color = expectedColor;
            int guard = 0;
            while (color == expectedColor && guard < 8)
            {
                color = RandomStageColor(stage);
                guard++;
            }

            if (color == expectedColor)
            {
                int next = ((int)expectedColor + 1) % stage.AvailableColorCount;
                color = (ColorId)next;
            }

            return color;
        }

        // Creates deterministic color gates along the track.
        private static List<GatePlan> BuildGatePlans(StageConfig stage)
        {
            List<GatePlan> plans = new List<GatePlan>();
            float firstGateZ = 25f;
            int plannedGateCount = Mathf.Max(1, Mathf.FloorToInt((stage.TrackLength - 65f) / stage.GateInterval) + 1);
            for (int i = 0; i < plannedGateCount; i++)
            {
                float z = Mathf.Min(stage.TrackLength - 38f, firstGateZ + i * stage.GateInterval);
                ColorId target = (ColorId)((stage.Seed + i + 1) % stage.AvailableColorCount);
                plans.Add(new GatePlan(i, z, target));
            }

            return plans;
        }

        // Creates deterministic color gates along the track.
        private void CreateGates(IReadOnlyList<GatePlan> gatePlans)
        {
            for (int i = 0; i < gatePlans.Count; i++)
            {
                GatePlan plan = gatePlans[i];
                CreateGateRow(plan.Index, plan.Z, plan.TargetColor);
                _lastReport.RecordGate(plan.Index, plan.Z, plan.TargetColor);
            }
        }

        // Builds a full-width transparent gate trigger with a visible primitive arch.
        private void CreateGateRow(int index, float z, ColorId target)
        {
            Material gateMaterial = ProceduralFactory.GatePanelMaterial(target);
            GameObject trigger = ProceduralFactory.Primitive(
                PrimitiveType.Cube,
                "GateTrigger_" + index,
                _levelRoot,
                new Vector3(0f, 1.35f, z),
                new Vector3(GameConstants.TrackWidth, 2.4f, 0.5f),
                gateMaterial,
                isTrigger: true);
            trigger.AddComponent<ColorGate>().Configure(target);
            ProceduralFactory.CreateColorShapeMarker(_levelRoot, "GateShape_" + index, new Vector3(0f, 3.05f, z - 0.35f), target, 0.7f);

            Material frameMaterial = ProceduralFactory.ColorMaterial(target);
            ProceduralFactory.VisualPrimitive(PrimitiveType.Cube, "GateLeftPost_" + index, _levelRoot, new Vector3(-3.8f, 1.2f, z), new Vector3(0.25f, 2.4f, 0.35f), frameMaterial);
            ProceduralFactory.VisualPrimitive(PrimitiveType.Cube, "GateRightPost_" + index, _levelRoot, new Vector3(3.8f, 1.2f, z), new Vector3(0.25f, 2.4f, 0.35f), frameMaterial);
            ProceduralFactory.VisualPrimitive(PrimitiveType.Cube, "GateTop_" + index, _levelRoot, new Vector3(0f, 2.45f, z), new Vector3(7.8f, 0.25f, 0.35f), frameMaterial);
            CreateGateVisualCues(index, z, target);
        }

        // Adds gate approach stripes and soft frame accents that read as color-change cues.
        private void CreateGateVisualCues(int index, float z, ColorId target)
        {
            Material cueMaterial = ProceduralFactory.TransparentMaterial("gate_cue_" + target, GameConstants.ToUnityColor(target), 0.42f);
            ProceduralFactory.VisualPrimitive(
                PrimitiveType.Cube,
                "GateApproachCue_" + index,
                _levelRoot,
                new Vector3(0f, 0.055f, z - 2.15f),
                new Vector3(GameConstants.TrackWidth - 0.65f, 0.045f, 0.22f),
                cueMaterial);
            ProceduralFactory.VisualPrimitive(
                PrimitiveType.Cube,
                "GateExitCue_" + index,
                _levelRoot,
                new Vector3(0f, 0.055f, z + 1.35f),
                new Vector3(GameConstants.TrackWidth - 1.1f, 0.045f, 0.14f),
                cueMaterial);

            for (int side = -1; side <= 1; side += 2)
            {
                ProceduralFactory.VisualPrimitive(
                    PrimitiveType.Cube,
                    "GateSideGlow_" + index + "_" + side,
                    _levelRoot,
                    new Vector3(side * 3.35f, 1.35f, z - 0.02f),
                    new Vector3(0.10f, 2.0f, 0.16f),
                    cueMaterial);
            }
        }

        // Creates the finish trigger and visible finish arch near the track end.
        private void CreateFinish(StageConfig stage)
        {
            float z = stage.TrackLength - 8f;
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
            ProceduralFactory.VisualPrimitive(PrimitiveType.Cube, "FinishLeftPost", _levelRoot, new Vector3(-3.8f, 1.35f, z), new Vector3(0.35f, 2.7f, 0.45f), finishMaterial);
            ProceduralFactory.VisualPrimitive(PrimitiveType.Cube, "FinishRightPost", _levelRoot, new Vector3(3.8f, 1.35f, z), new Vector3(0.35f, 2.7f, 0.45f), finishMaterial);
            ProceduralFactory.VisualPrimitive(PrimitiveType.Cube, "FinishTop", _levelRoot, new Vector3(0f, 2.75f, z), new Vector3(7.8f, 0.35f, 0.45f), finishMaterial);
            CreateFinishDetails(z);
            _lastReport.RecordFinish();
        }

        // Builds a texture-free checker strip and arrival accents around the finish arch.
        private void CreateFinishDetails(float finishZ)
        {
            float tileWidth = GameConstants.TrackWidth / 6f;
            for (int column = 0; column < 6; column++)
            {
                for (int row = 0; row < 2; row++)
                {
                    float x = -GameConstants.TrackWidth * 0.5f + tileWidth * 0.5f + column * tileWidth;
                    float z = finishZ - 1.0f + row * 0.48f;
                    Material material = ((column + row) % 2 == 0) ? ProceduralFactory.FinishMaterial() : ProceduralFactory.FinishDarkMaterial();
                    ProceduralFactory.VisualPrimitive(
                        PrimitiveType.Cube,
                        "FinishChecker_" + column + "_" + row,
                        _levelRoot,
                        new Vector3(x, 0.065f, z),
                        new Vector3(tileWidth * 0.92f, 0.05f, 0.40f),
                        material);
                }
            }

            ProceduralFactory.VisualPrimitive(
                PrimitiveType.Cube,
                "FinishGlowPlatform",
                _levelRoot,
                new Vector3(0f, 0.04f, finishZ + 1.2f),
                new Vector3(GameConstants.TrackWidth - 0.45f, 0.045f, 0.30f),
                ProceduralFactory.TrackAccentMaterial());
        }

        // Returns the player color expected at a shard row after all mandatory earlier gates.
        private static ColorId GetExpectedColorAtZ(float z, IReadOnlyList<GatePlan> gatePlans)
        {
            ColorId expectedColor = ColorId.Cyan;
            for (int i = 0; i < gatePlans.Count; i++)
            {
                if (z > gatePlans[i].Z)
                {
                    expectedColor = gatePlans[i].TargetColor;
                }
            }

            return expectedColor;
        }

        // Picks a deterministic color from the colors enabled by the current stage.
        private static ColorId RandomStageColor(StageConfig stage)
        {
            return (ColorId)Random.Range(0, Mathf.Clamp(stage.AvailableColorCount, 1, 4));
        }

        // Logs a compact warning if post-generation fairness validation failed.
        private static void ValidateGeneratedLevel(LevelGenerationReport report)
        {
            if (report == null || report.IsValid)
            {
                return;
            }

            Debug.LogWarning("Stage " + report.StageIndex + " generation completed with " + report.Warnings.Count + " fairness warning(s).");
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

            camera.cullingMask = ~0;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, _trackLength + 90f);
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

        // Reports generated renderer/material/collider/camera health once in editor and development builds.
        private void ValidateSpawnedRuntimeObjects(LaneRunnerController runner)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_levelRoot == null)
            {
                Debug.LogWarning("Runtime visual self-check skipped: generated level root is missing.");
                return;
            }

            MeshRenderer[] renderers = _levelRoot.GetComponentsInChildren<MeshRenderer>(true);
            MeshFilter[] meshFilters = _levelRoot.GetComponentsInChildren<MeshFilter>(true);
            Collider[] colliders = _levelRoot.GetComponentsInChildren<Collider>(true);
            int invalidMaterials = 0;
            int missingMeshes = 0;
            int inactiveObjects = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].gameObject.activeSelf)
                {
                    inactiveObjects++;
                }

                if (renderers[i] == null || !RuntimeMaterialProvider.IsMaterialUsable(renderers[i].sharedMaterial))
                {
                    invalidMaterials++;
                }
            }

            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i] == null || meshFilters[i].sharedMesh == null)
                {
                    missingMeshes++;
                }
            }

            Camera camera = Camera.main;
            string cameraSummary = camera == null
                ? "camera=missing"
                : "camera=" + camera.name + " cullingMask=" + camera.cullingMask + " farClip=" + camera.farClipPlane.ToString("0.0");
            Debug.Log(
                "Color Gate Rush runtime visual self-check: renderers=" + renderers.Length
                + ", meshFilters=" + meshFilters.Length
                + ", colliders=" + colliders.Length
                + ", invalidMaterials=" + invalidMaterials
                + ", missingMeshes=" + missingMeshes
                + ", inactiveRenderers=" + inactiveObjects
                + ", runner=" + (runner != null ? runner.name : "missing")
                + ", " + cameraSummary);

            if (renderers.Length < 20 || invalidMaterials > 0 || missingMeshes > 0 || runner == null || camera == null)
            {
                Debug.LogWarning("Color Gate Rush runtime visual self-check found a possible build visibility issue.");
            }
#endif
        }
    }
}
