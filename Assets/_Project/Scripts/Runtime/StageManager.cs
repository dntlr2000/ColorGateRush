using UnityEngine;

namespace ColorGateRush
{
    public sealed class StageManager
    {
        public const string UnlockedStageKey = "CGR_UnlockedStage";
        public const string StageStarsKeyPrefix = "CGR_StageStars_";
        public const string SelectedStageKey = "CGR_SelectedStage";
        public const int TotalStageCount = 30;

        private readonly StageConfig[] _stages;
        private int _unlockedStage;
        private int _selectedStage;

        public StageConfig[] Stages => _stages;
        public int UnlockedStage => _unlockedStage;
        public int SelectedStageIndex => _selectedStage;

        // Builds stage configs and loads saved progress from PlayerPrefs.
        public StageManager()
        {
            _stages = BuildStageConfigs();
            _unlockedStage = NormalizeProgressForCurrentRules();
            _selectedStage = Mathf.Clamp(PlayerPrefs.GetInt(SelectedStageKey, _unlockedStage), 1, _unlockedStage);
            PlayerPrefs.SetInt(SelectedStageKey, _selectedStage);
            PlayerPrefs.Save();
        }

        // Returns the deterministic config for the requested stage.
        public StageConfig GetStageConfig(int stageIndex)
        {
            return _stages[Mathf.Clamp(stageIndex, 1, TotalStageCount) - 1];
        }

        // Returns true when the requested stage is currently unlocked.
        public bool IsStageUnlocked(int stageIndex)
        {
            return stageIndex >= 1 && stageIndex <= _unlockedStage;
        }

        // Returns the best saved star count for the requested stage.
        public int GetBestStars(int stageIndex)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(StageStarsKeyPrefix + stageIndex, 0), 0, 3);
        }

        // Selects an unlocked stage and persists the selected index.
        public bool SelectStage(int stageIndex)
        {
            if (!IsStageUnlocked(stageIndex))
            {
                return false;
            }

            _selectedStage = Mathf.Clamp(stageIndex, 1, TotalStageCount);
            PlayerPrefs.SetInt(SelectedStageKey, _selectedStage);
            PlayerPrefs.Save();
            return true;
        }

        // Calculates stars from clear state and score according to the visible UI rules.
        public int CalculateStars(StageConfig stage, int score, bool cleared)
        {
            if (!cleared)
            {
                return 0;
            }

            if (score >= stage.ThreeStarScore)
            {
                return 3;
            }

            if (score >= stage.TwoStarScore)
            {
                return 2;
            }

            return 1;
        }

        // Saves a cleared stage result and unlocks the next stage on any clear with at least one star.
        public StageResult SaveStageResult(StageConfig stage, int score)
        {
            int stars = CalculateStars(stage, score, cleared: true);
            int previousBest = GetBestStars(stage.StageIndex);
            int bestStars = Mathf.Max(previousBest, stars);
            bool improved = bestStars > previousBest;
            if (improved)
            {
                PlayerPrefs.SetInt(StageStarsKeyPrefix + stage.StageIndex, bestStars);
            }

            bool hasNext = stage.StageIndex < TotalStageCount;
            bool nextUnlocked = false;
            if (stars >= 1 && hasNext && _unlockedStage < stage.StageIndex + 1)
            {
                _unlockedStage = stage.StageIndex + 1;
                PlayerPrefs.SetInt(UnlockedStageKey, _unlockedStage);
                nextUnlocked = true;
            }

            PlayerPrefs.Save();
            return new StageResult(stage.StageIndex, score, stars, previousBest, bestStars, improved, nextUnlocked, hasNext);
        }

        // Creates an unsaved failed result so failures never reduce best stars.
        public StageResult CreateFailedResult(StageConfig stage, int score)
        {
            return CreateFailedResult(stage, score, StageFailReason.ObstacleHit);
        }

        // Creates an unsaved failed result with the reason shown on the result screen.
        public StageResult CreateFailedResult(StageConfig stage, int score, StageFailReason failReason)
        {
            int previousBest = GetBestStars(stage.StageIndex);
            return new StageResult(stage.StageIndex, score, 0, previousBest, previousBest, false, false, stage.StageIndex < TotalStageCount, failReason);
        }

        // Reports whether a star result is eligible to unlock the next stage.
        public bool WouldUnlockNextStage(StageConfig stage, int stars)
        {
            return stars >= 1 && stage.StageIndex < TotalStageCount;
        }

        // Normalizes saved progress so old data follows the current one-star unlock rule without reducing progress.
        private static int NormalizeProgressForCurrentRules()
        {
            int savedUnlocked = Mathf.Clamp(PlayerPrefs.GetInt(UnlockedStageKey, 1), 1, TotalStageCount);
            int bestStarsUnlocked = RecalculateUnlockedStageFromBestStars();
            int normalized = Mathf.Clamp(Mathf.Max(savedUnlocked, bestStarsUnlocked), 1, TotalStageCount);
            if (normalized != savedUnlocked)
            {
                PlayerPrefs.SetInt(UnlockedStageKey, normalized);
            }

            return normalized;
        }

        // Recalculates sequential unlock progress from existing best-star records under the one-star clear rule.
        private static int RecalculateUnlockedStageFromBestStars()
        {
            int unlockedStage = 1;
            for (int stage = 1; stage < TotalStageCount; stage++)
            {
                int stars = Mathf.Clamp(PlayerPrefs.GetInt(StageStarsKeyPrefix + stage, 0), 0, 3);
                if (stars < 1)
                {
                    break;
                }

                unlockedStage = stage + 1;
            }

            return unlockedStage;
        }

        // Builds thirty progressively harder procedural stages without external data files.
        private static StageConfig[] BuildStageConfigs()
        {
            StageConfig[] stages = new StageConfig[TotalStageCount];
            for (int i = 0; i < stages.Length; i++)
            {
                int stage = i + 1;
                int shardRows = 22 + stage * 2;
                float trackLength = 176f + stage * 9.2f;
                float obstacleChance = GetObstacleLaneChance(stage);
                float matchingShardChance = GetMatchingShardLaneChance(stage);
                float offColorShardChance = GetOffColorShardLaneChance(stage);
                float safeEmptyLaneChance = GetSafeEmptyLaneChance(stage);
                float gateInterval = GetGateInterval(stage);
                int colorCount = stage <= 5 ? 3 : 4;
                float speed = Mathf.Min(GameConstants.MaxForwardSpeed, 8.55f + (stage - 1) * 0.19f);
                float laneMove = 12.2f + (stage - 1) * 0.13f;
                int seed = 12345 + stage * 137;
                int tier = StageScoreAnalyzer.GetDifficultyTier(stage);
                int themeIndex = (stage - 1) % VisualTheme.ThemeVariationCount;
                int allowance = StageScoreAnalyzer.GetThreeStarMistakeAllowance(stage);
                StageConfig baseStage = new StageConfig(
                    stage,
                    seed,
                    trackLength,
                    shardRows,
                    obstacleChance,
                    matchingShardChance,
                    offColorShardChance,
                    safeEmptyLaneChance,
                    gateInterval,
                    colorCount,
                    1,
                    1,
                    speed,
                    laneMove,
                    0,
                    0,
                    allowance,
                    tier,
                    themeIndex);
                StageScoreEstimate estimate = StageScoreAnalyzer.EstimateStage(baseStage);
                stages[i] = new StageConfig(
                    stage,
                    seed,
                    trackLength,
                    shardRows,
                    obstacleChance,
                    matchingShardChance,
                    offColorShardChance,
                    safeEmptyLaneChance,
                    gateInterval,
                    colorCount,
                    estimate.TwoStarScore,
                    estimate.ThreeStarScore,
                    speed,
                    laneMove,
                    estimate.EstimatedMaxAchievableScore,
                    estimate.EstimatedMaxCollectibleCount,
                    estimate.ThreeStarMistakeAllowance,
                    tier,
                    themeIndex);
            }

            return stages;
        }

        // Returns a per-lane obstacle chance that rises slowly across the 30-stage campaign.
        private static float GetObstacleLaneChance(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 0.018f;
                case 2:
                    return 0.030f;
                case 3:
                    return 0.045f;
                default:
                    if (stage <= 10)
                    {
                        return Mathf.Clamp(0.055f + (stage - 4) * 0.006f, 0.055f, 0.092f);
                    }

                    if (stage <= 20)
                    {
                        return Mathf.Clamp(0.095f + (stage - 11) * 0.004f, 0.095f, 0.132f);
                    }

                    return Mathf.Clamp(0.135f + (stage - 21) * 0.003f, 0.135f, 0.162f);
            }
        }

        // Returns expected-color shard chance without forcing every row to contain a match.
        private static float GetMatchingShardLaneChance(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 0.22f;
                case 2:
                    return 0.20f;
                case 3:
                    return 0.18f;
                default:
                    if (stage <= 10)
                    {
                        return Mathf.Clamp(0.17f - (stage - 4) * 0.004f, 0.145f, 0.17f);
                    }

                    if (stage <= 20)
                    {
                        return Mathf.Clamp(0.145f - (stage - 11) * 0.0015f, 0.130f, 0.145f);
                    }

                    return Mathf.Clamp(0.130f - (stage - 21) * 0.0015f, 0.115f, 0.130f);
            }
        }

        // Returns off-color shard chance to keep rows populated while increasing risk gradually.
        private static float GetOffColorShardLaneChance(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 0.30f;
                case 2:
                    return 0.32f;
                case 3:
                    return 0.34f;
                default:
                    if (stage <= 10)
                    {
                        return Mathf.Clamp(0.35f + (stage - 4) * 0.010f, 0.35f, 0.41f);
                    }

                    if (stage <= 20)
                    {
                        return Mathf.Clamp(0.42f + (stage - 11) * 0.006f, 0.42f, 0.475f);
                    }

                    return Mathf.Clamp(0.47f + (stage - 21) * 0.003f, 0.47f, 0.50f);
            }
        }

        // Returns the chance to open an extra empty lane only when a row lacks empty space.
        private static float GetSafeEmptyLaneChance(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 0.18f;
                case 2:
                    return 0.16f;
                case 3:
                    return 0.14f;
                default:
                    if (stage <= 10)
                    {
                        return Mathf.Clamp(0.13f - (stage - 4) * 0.006f, 0.09f, 0.13f);
                    }

                    if (stage <= 20)
                    {
                        return Mathf.Clamp(0.09f - (stage - 11) * 0.002f, 0.07f, 0.09f);
                    }

                    return Mathf.Clamp(0.07f - (stage - 21) * 0.001f, 0.06f, 0.07f);
            }
        }

        // Returns wider early gate spacing and tighter advanced spacing for more frequent color decisions.
        private static float GetGateInterval(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 70f;
                case 2:
                    return 66f;
                case 3:
                    return 62f;
                default:
                    if (stage <= 10)
                    {
                        return Mathf.Max(48f, 60f - (stage - 4) * 2.0f);
                    }

                    if (stage <= 20)
                    {
                        return Mathf.Max(38f, 46f - (stage - 11) * 0.9f);
                    }

                    return 36f;
            }
        }
    }
}
