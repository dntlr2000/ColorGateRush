using UnityEngine;

namespace ColorGateRush
{
    public readonly struct EndlessRunConfig
    {
        public const int DefaultWrongShardLimit = GameConstants.MaxWrongShardCount;

        public readonly int Seed;
        public readonly float StartForwardSpeed;
        public readonly float LaneMoveSpeed;
        public readonly float GenerateAheadDistance;
        public readonly float CleanupDistance;
        public readonly float FirstGateZ;
        public readonly float RowSpacingStart;
        public readonly float RowSpacingEnd;
        public readonly float GateIntervalStart;
        public readonly float GateIntervalEnd;
        public readonly float ObstacleChanceStart;
        public readonly float ObstacleChanceMax;
        public readonly float MatchingShardChanceStart;
        public readonly float MatchingShardChanceEnd;
        public readonly float OffColorShardChanceStart;
        public readonly float OffColorShardChanceEnd;
        public readonly float SafeEmptyLaneChanceStart;
        public readonly float SafeEmptyLaneChanceEnd;
        public readonly float DifficultyRampDistance;
        public readonly float SpeedGrowthPerSecond;
        public readonly float DistanceSpeedGrowthPerMeter;
        public readonly float LaneMoveGrowthFactor;
        public readonly int WrongShardLimit;

        // Stores the runtime generation and difficulty parameters for Endless Mode.
        public EndlessRunConfig(
            int seed,
            float startForwardSpeed,
            float laneMoveSpeed,
            float generateAheadDistance,
            float cleanupDistance,
            float firstGateZ,
            float rowSpacingStart,
            float rowSpacingEnd,
            float gateIntervalStart,
            float gateIntervalEnd,
            float obstacleChanceStart,
            float obstacleChanceMax,
            float matchingShardChanceStart,
            float matchingShardChanceEnd,
            float offColorShardChanceStart,
            float offColorShardChanceEnd,
            float safeEmptyLaneChanceStart,
            float safeEmptyLaneChanceEnd,
            float difficultyRampDistance,
            float speedGrowthPerSecond,
            float distanceSpeedGrowthPerMeter,
            float laneMoveGrowthFactor,
            int wrongShardLimit)
        {
            Seed = seed;
            StartForwardSpeed = startForwardSpeed;
            LaneMoveSpeed = laneMoveSpeed;
            GenerateAheadDistance = generateAheadDistance;
            CleanupDistance = cleanupDistance;
            FirstGateZ = firstGateZ;
            RowSpacingStart = rowSpacingStart;
            RowSpacingEnd = rowSpacingEnd;
            GateIntervalStart = gateIntervalStart;
            GateIntervalEnd = gateIntervalEnd;
            ObstacleChanceStart = obstacleChanceStart;
            ObstacleChanceMax = obstacleChanceMax;
            MatchingShardChanceStart = matchingShardChanceStart;
            MatchingShardChanceEnd = matchingShardChanceEnd;
            OffColorShardChanceStart = offColorShardChanceStart;
            OffColorShardChanceEnd = offColorShardChanceEnd;
            SafeEmptyLaneChanceStart = safeEmptyLaneChanceStart;
            SafeEmptyLaneChanceEnd = safeEmptyLaneChanceEnd;
            DifficultyRampDistance = difficultyRampDistance;
            SpeedGrowthPerSecond = speedGrowthPerSecond;
            DistanceSpeedGrowthPerMeter = distanceSpeedGrowthPerMeter;
            LaneMoveGrowthFactor = laneMoveGrowthFactor;
            WrongShardLimit = Mathf.Max(1, wrongShardLimit);
        }

        // Creates the default Endless MVP tuning without adding external data files.
        public static EndlessRunConfig CreateDefault()
        {
            return new EndlessRunConfig(
                77331,
                9.35f,
                13.4f,
                210f,
                70f,
                42f,
                6.35f,
                10.40f,
                54f,
                30f,
                0.060f,
                0.285f,
                0.170f,
                0.120f,
                0.365f,
                0.560f,
                0.120f,
                0.045f,
                1500f,
                0.055f,
                0.00075f,
                0.55f,
                DefaultWrongShardLimit);
        }

        // Returns the same Endless tuning with a fresh per-run seed.
        public EndlessRunConfig WithSeed(int seed)
        {
            return new EndlessRunConfig(
                seed,
                StartForwardSpeed,
                LaneMoveSpeed,
                GenerateAheadDistance,
                CleanupDistance,
                FirstGateZ,
                RowSpacingStart,
                RowSpacingEnd,
                GateIntervalStart,
                GateIntervalEnd,
                ObstacleChanceStart,
                ObstacleChanceMax,
                MatchingShardChanceStart,
                MatchingShardChanceEnd,
                OffColorShardChanceStart,
                OffColorShardChanceEnd,
                SafeEmptyLaneChanceStart,
                SafeEmptyLaneChanceEnd,
                DifficultyRampDistance,
                SpeedGrowthPerSecond,
                DistanceSpeedGrowthPerMeter,
                LaneMoveGrowthFactor,
                WrongShardLimit);
        }

        // Returns normalized difficulty by distance for gradual Endless pressure growth.
        public float Difficulty01(float distance)
        {
            return Mathf.Clamp01(Mathf.Max(0f, distance) / Mathf.Max(1f, DifficultyRampDistance));
        }

        // Returns normalized difficulty from both elapsed play time and distance so pressure rises even during dense sections.
        public float Difficulty01(float elapsedTime, float distance)
        {
            float distanceFactor = Mathf.Max(0f, distance) / Mathf.Max(1f, DifficultyRampDistance);
            float timeFactor = Mathf.Max(0f, elapsedTime) / 90f;
            return Mathf.Clamp01(Mathf.Max(distanceFactor, timeFactor));
        }

        // Returns the current Endless forward speed; unlike probability pressure, this keeps growing over time.
        public float ForwardSpeed(float elapsedTime, float distance)
        {
            return Mathf.Max(
                StartForwardSpeed,
                StartForwardSpeed
                    + Mathf.Max(0f, elapsedTime) * SpeedGrowthPerSecond
                    + Mathf.Max(0f, distance) * DistanceSpeedGrowthPerMeter);
        }

        // Returns a movement multiplier used for HUD feedback and validator reporting.
        public float SpeedMultiplier(float elapsedTime, float distance)
        {
            return ForwardSpeed(elapsedTime, distance) / Mathf.Max(0.1f, StartForwardSpeed);
        }

        // Returns lane movement sharpness scaled with speed so later Endless is fast but still controllable.
        public float LaneMoveSharpness(float elapsedTime, float distance)
        {
            float speedExtra = Mathf.Max(0f, SpeedMultiplier(elapsedTime, distance) - 1f);
            return LaneMoveSpeed * (1f + Mathf.Min(1.35f, speedExtra * LaneMoveGrowthFactor));
        }

        // Returns the active row spacing so reaction time scales with speed.
        public float RowSpacing(float distance)
        {
            return Mathf.Lerp(RowSpacingStart, RowSpacingEnd, Difficulty01(distance));
        }

        // Returns the active row spacing using time and distance, with a small long-run tail after peak difficulty.
        public float RowSpacing(float elapsedTime, float distance)
        {
            float baseSpacing = Mathf.Lerp(RowSpacingStart, RowSpacingEnd, Difficulty01(elapsedTime, distance));
            float longRunTail = Mathf.Max(0f, elapsedTime - 90f) * 0.008f;
            return baseSpacing + longRunTail;
        }

        // Returns the active gate interval as color decisions become more frequent.
        public float GateInterval(float distance)
        {
            return Mathf.Lerp(GateIntervalStart, GateIntervalEnd, Difficulty01(distance));
        }

        // Returns the active gate interval from time and distance while keeping a fair lower bound.
        public float GateInterval(float elapsedTime, float distance)
        {
            return Mathf.Lerp(GateIntervalStart, GateIntervalEnd, Difficulty01(elapsedTime, distance));
        }
    }
}
