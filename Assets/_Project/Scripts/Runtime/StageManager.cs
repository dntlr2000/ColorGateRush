using UnityEngine;

namespace ColorGateRush
{
    public sealed class StageManager
    {
        public const string UnlockedStageKey = "CGR_UnlockedStage";
        public const string StageStarsKeyPrefix = "CGR_StageStars_";
        public const string SelectedStageKey = "CGR_SelectedStage";
        public const int TotalStageCount = 10;

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
            _unlockedStage = Mathf.Clamp(PlayerPrefs.GetInt(UnlockedStageKey, 1), 1, TotalStageCount);
            _selectedStage = Mathf.Clamp(PlayerPrefs.GetInt(SelectedStageKey, _unlockedStage), 1, _unlockedStage);
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

        // Saves a cleared stage result and unlocks the next stage only on three stars.
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
            if (stars == 3 && hasNext && _unlockedStage < stage.StageIndex + 1)
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
            int previousBest = GetBestStars(stage.StageIndex);
            return new StageResult(stage.StageIndex, score, 0, previousBest, previousBest, false, false, stage.StageIndex < TotalStageCount);
        }

        // Reports whether a star result is eligible to unlock the next stage.
        public bool WouldUnlockNextStage(StageConfig stage, int stars)
        {
            return stars == 3 && stage.StageIndex < TotalStageCount;
        }

        // Builds ten progressively harder procedural stages without external data files.
        private static StageConfig[] BuildStageConfigs()
        {
            StageConfig[] stages = new StageConfig[TotalStageCount];
            for (int i = 0; i < stages.Length; i++)
            {
                int stage = i + 1;
                int shardRows = 18 + stage * 2;
                float trackLength = 150f + stage * 8f;
                float obstacleChance = GetObstacleLaneChance(stage);
                float matchingShardChance = GetMatchingShardLaneChance(stage);
                float offColorShardChance = GetOffColorShardLaneChance(stage);
                float safeEmptyLaneChance = GetSafeEmptyLaneChance(stage);
                float gateInterval = GetGateInterval(stage);
                int colorCount = Mathf.Clamp(3 + stage / 4, 3, 4);
                float recoveryBoost = stage <= 2 ? 1.15f : (stage == 3 ? 1.12f : 1.08f);
                int baselineScore = Mathf.RoundToInt(shardRows * GameConstants.LaneCount * matchingShardChance * GameConstants.SameColorShardScore * recoveryBoost + stage * 10f);
                baselineScore = Mathf.Max(GameConstants.SameColorShardScore * 6, baselineScore);
                int twoStar = Mathf.RoundToInt(baselineScore * 0.65f);
                int threeStar = Mathf.RoundToInt(baselineScore * 0.90f);
                float speed = 7.4f + stage * 0.35f;
                float laneMove = 10.5f + stage * 0.25f;
                stages[i] = new StageConfig(stage, 12345 + stage * 97, trackLength, shardRows, obstacleChance, matchingShardChance, offColorShardChance, safeEmptyLaneChance, gateInterval, colorCount, twoStar, threeStar, speed, laneMove);
            }

            return stages;
        }

        // Returns a gentle per-lane obstacle chance so early stages stay collection-focused.
        private static float GetObstacleLaneChance(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 0.02f;
                case 2:
                    return 0.035f;
                case 3:
                    return 0.055f;
                default:
                    return Mathf.Clamp(0.065f + (stage - 4) * 0.012f, 0.065f, 0.14f);
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
                    return Mathf.Clamp(0.17f - (stage - 4) * 0.005f, 0.13f, 0.17f);
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
                    return Mathf.Clamp(0.35f + (stage - 4) * 0.01f, 0.35f, 0.41f);
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
                    return Mathf.Clamp(0.13f - (stage - 4) * 0.006f, 0.08f, 0.13f);
            }
        }

        // Returns wider early gate spacing so Stage 1 teaches color changes without crowding rows.
        private static float GetGateInterval(int stage)
        {
            switch (stage)
            {
                case 1:
                    return 62f;
                case 2:
                    return 58f;
                case 3:
                    return 54f;
                default:
                    return Mathf.Max(30f, 54f - stage * 2.2f);
            }
        }
    }
}
