namespace ColorGateRush
{
    public readonly struct StageConfig
    {
        public readonly int StageIndex;
        public readonly int Seed;
        public readonly float TrackLength;
        public readonly int ShardRowCount;
        public readonly float ObstacleChance;
        public readonly float MatchingShardChance;
        public readonly float OffColorShardChance;
        public readonly float SafeEmptyLaneChance;
        public readonly float GateInterval;
        public readonly int AvailableColorCount;
        public readonly int TwoStarScore;
        public readonly int ThreeStarScore;
        public readonly float PlayerForwardSpeed;
        public readonly float LaneMoveSpeed;
        public readonly int EstimatedMaxAchievableScore;
        public readonly int EstimatedMaxCollectibleCount;
        public readonly int ThreeStarMistakeAllowance;
        public readonly int DifficultyTier;
        public readonly int ThemeIndex;

        // Stores deterministic generation and scoring parameters for one stage.
        // Keeps older stage construction call sites working with balanced row-generation defaults.
        public StageConfig(
            int stageIndex,
            int seed,
            float trackLength,
            int shardRowCount,
            float obstacleChance,
            float gateInterval,
            int availableColorCount,
            int twoStarScore,
            int threeStarScore,
            float playerForwardSpeed,
            float laneMoveSpeed)
            : this(
                stageIndex,
                seed,
                trackLength,
                shardRowCount,
                obstacleChance,
                0.20f,
                0.30f,
                0.18f,
                gateInterval,
                availableColorCount,
                twoStarScore,
                threeStarScore,
                playerForwardSpeed,
                laneMoveSpeed)
        {
        }

        // Stores deterministic generation, scoring, and movement settings for one stage.
        public StageConfig(
            int stageIndex,
            int seed,
            float trackLength,
            int shardRowCount,
            float obstacleChance,
            float matchingShardChance,
            float offColorShardChance,
            float safeEmptyLaneChance,
            float gateInterval,
            int availableColorCount,
            int twoStarScore,
            int threeStarScore,
            float playerForwardSpeed,
            float laneMoveSpeed)
            : this(
                stageIndex,
                seed,
                trackLength,
                shardRowCount,
                obstacleChance,
                matchingShardChance,
                offColorShardChance,
                safeEmptyLaneChance,
                gateInterval,
                availableColorCount,
                twoStarScore,
                threeStarScore,
                playerForwardSpeed,
                laneMoveSpeed,
                0,
                0,
                2,
                1,
                0)
        {
        }

        // Stores all deterministic generation, scoring, movement, analysis, and visual settings for one stage.
        public StageConfig(
            int stageIndex,
            int seed,
            float trackLength,
            int shardRowCount,
            float obstacleChance,
            float matchingShardChance,
            float offColorShardChance,
            float safeEmptyLaneChance,
            float gateInterval,
            int availableColorCount,
            int twoStarScore,
            int threeStarScore,
            float playerForwardSpeed,
            float laneMoveSpeed,
            int estimatedMaxAchievableScore,
            int estimatedMaxCollectibleCount,
            int threeStarMistakeAllowance,
            int difficultyTier,
            int themeIndex)
        {
            StageIndex = stageIndex;
            Seed = seed;
            TrackLength = trackLength;
            ShardRowCount = shardRowCount;
            ObstacleChance = obstacleChance;
            MatchingShardChance = matchingShardChance;
            OffColorShardChance = offColorShardChance;
            SafeEmptyLaneChance = safeEmptyLaneChance;
            GateInterval = gateInterval;
            AvailableColorCount = availableColorCount;
            TwoStarScore = twoStarScore;
            ThreeStarScore = threeStarScore;
            PlayerForwardSpeed = playerForwardSpeed;
            LaneMoveSpeed = laneMoveSpeed;
            EstimatedMaxAchievableScore = estimatedMaxAchievableScore;
            EstimatedMaxCollectibleCount = estimatedMaxCollectibleCount;
            ThreeStarMistakeAllowance = threeStarMistakeAllowance;
            DifficultyTier = difficultyTier;
            ThemeIndex = themeIndex;
        }
    }
}
