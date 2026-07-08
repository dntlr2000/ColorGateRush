using UnityEngine;

namespace ColorGateRush
{
    public readonly struct StageScoreEstimate
    {
        public readonly int EstimatedMaxAchievableScore;
        public readonly int EstimatedMaxCollectibleCount;
        public readonly int TwoStarScore;
        public readonly int ThreeStarScore;
        public readonly int ThreeStarMistakeAllowance;
        public readonly int NaiveMaxScore;
        public readonly int NaiveMinusRouteAwareMax;
        public readonly int RowsWithMultipleMatchingShards;
        public readonly int RowsWhereOnlyOneShardCanBeCollected;
        public readonly bool ClearRouteExists;
        public readonly bool PerfectOrNearPerfectRouteExists;

        // Stores route-aware score targets derived from one deterministic stage layout.
        public StageScoreEstimate(
            int estimatedMaxAchievableScore,
            int estimatedMaxCollectibleCount,
            int twoStarScore,
            int threeStarScore,
            int threeStarMistakeAllowance,
            int naiveMaxScore,
            int naiveMinusRouteAwareMax,
            int rowsWithMultipleMatchingShards,
            int rowsWhereOnlyOneShardCanBeCollected,
            bool clearRouteExists,
            bool perfectOrNearPerfectRouteExists)
        {
            EstimatedMaxAchievableScore = estimatedMaxAchievableScore;
            EstimatedMaxCollectibleCount = estimatedMaxCollectibleCount;
            TwoStarScore = twoStarScore;
            ThreeStarScore = threeStarScore;
            ThreeStarMistakeAllowance = threeStarMistakeAllowance;
            NaiveMaxScore = naiveMaxScore;
            NaiveMinusRouteAwareMax = naiveMinusRouteAwareMax;
            RowsWithMultipleMatchingShards = rowsWithMultipleMatchingShards;
            RowsWhereOnlyOneShardCanBeCollected = rowsWhereOnlyOneShardCanBeCollected;
            ClearRouteExists = clearRouteExists;
            PerfectOrNearPerfectRouteExists = perfectOrNearPerfectRouteExists;
        }

        // Returns the three-star target as a fraction of the estimated best route score.
        public float GetThreeStarRatio()
        {
            return EstimatedMaxAchievableScore <= 0 ? 0f : ThreeStarScore / (float)EstimatedMaxAchievableScore;
        }
    }

    public static class StageScoreAnalyzer
    {
        private const int InvalidScore = int.MinValue / 4;
        private const int ScoreStep = 5;

        // Builds a deterministic report for a stage and converts it into route-aware score targets.
        public static StageScoreEstimate EstimateStage(StageConfig stage)
        {
            LevelGenerationReport report = LevelGenerator.BuildGenerationReport(stage);
            return AnalyzeReport(stage, report);
        }

        // Calculates the best achievable score by dynamic programming over lanes, combo, and wrong-shard count.
        public static StageScoreEstimate AnalyzeReport(StageConfig stage, LevelGenerationReport report)
        {
            int wrongShardLimit = GameConstants.MaxWrongShardCount;
            int[,,] scores = CreateScoreGrid(wrongShardLimit);
            int[,,] counts = new int[GameConstants.LaneCount, GameConstants.ComboCap + 1, wrongShardLimit];
            int allowedLaneShift = GetAllowedLaneShiftPerRow(stage);
            scores[1, 0, 0] = 0;

            for (int rowIndex = 0; rowIndex < report.Rows.Count; rowIndex++)
            {
                LevelRowReport row = report.Rows[rowIndex];
                int[,,] nextScores = CreateScoreGrid(wrongShardLimit);
                int[,,] nextCounts = new int[GameConstants.LaneCount, GameConstants.ComboCap + 1, wrongShardLimit];
                for (int lane = 0; lane < GameConstants.LaneCount; lane++)
                {
                    for (int combo = 0; combo <= GameConstants.ComboCap; combo++)
                    {
                        for (int wrongShardCount = 0; wrongShardCount < wrongShardLimit; wrongShardCount++)
                        {
                            int baseScore = scores[lane, combo, wrongShardCount];
                            if (baseScore == InvalidScore)
                            {
                                continue;
                            }

                            for (int targetLane = 0; targetLane < GameConstants.LaneCount; targetLane++)
                            {
                                if (Mathf.Abs(targetLane - lane) > allowedLaneShift)
                                {
                                    continue;
                                }

                                GeneratedLaneContent content = row.GetLaneContent(targetLane);
                                if (content == GeneratedLaneContent.Obstacle)
                                {
                                    continue;
                                }

                                int nextCombo = combo;
                                int nextScore = baseScore;
                                int nextCollectibleCount = counts[lane, combo, wrongShardCount];
                                int nextWrongShardCount = wrongShardCount;
                                ApplyLaneChoice(content, ref nextScore, ref nextCombo, ref nextCollectibleCount, ref nextWrongShardCount);
                                if (nextWrongShardCount >= wrongShardLimit)
                                {
                                    continue;
                                }

                                StoreBest(nextScores, nextCounts, targetLane, nextCombo, nextWrongShardCount, nextScore, nextCollectibleCount);
                            }
                        }
                    }
                }

                scores = nextScores;
                counts = nextCounts;
            }

            int bestScore = 0;
            int bestCollectibleCount = 0;
            bool clearRouteExists = false;
            for (int lane = 0; lane < GameConstants.LaneCount; lane++)
            {
                for (int combo = 0; combo <= GameConstants.ComboCap; combo++)
                {
                    for (int wrongShardCount = 0; wrongShardCount < wrongShardLimit; wrongShardCount++)
                    {
                        int candidate = scores[lane, combo, wrongShardCount];
                        if (candidate != InvalidScore)
                        {
                            clearRouteExists = true;
                        }

                        if (candidate > bestScore)
                        {
                            bestScore = candidate;
                            bestCollectibleCount = counts[lane, combo, wrongShardCount];
                        }
                    }
                }
            }

            bestScore += report.GateRows * GameConstants.GateScore;
            bestScore = Mathf.Max(0, bestScore);
            int allowance = GetThreeStarMistakeAllowance(stage.StageIndex);
            int threeStar = CalculateThreeStarScore(stage.StageIndex, bestScore, allowance);
            int twoStar = CalculateTwoStarFromThreeStar(threeStar);
            int naiveMaxScore = report.MaxPossibleCorrectShardScore + report.GateRows * GameConstants.GateScore;
            int naiveMinusRouteAware = naiveMaxScore - bestScore;
            bool nearPerfectRouteExists = clearRouteExists && threeStar <= bestScore;
            return new StageScoreEstimate(
                bestScore,
                bestCollectibleCount,
                twoStar,
                threeStar,
                allowance,
                naiveMaxScore,
                naiveMinusRouteAware,
                report.RowsWithMultipleMatchingShards,
                report.RowsWhereOnlyOneShardCanBeCollected,
                clearRouteExists,
                nearPerfectRouteExists);
        }

        // Estimates whether the row spacing and speed allow one-lane or two-lane movement between decisions.
        private static int GetAllowedLaneShiftPerRow(StageConfig stage)
        {
            float usableLength = Mathf.Max(20f, stage.TrackLength - 34f);
            float spacing = usableLength / Mathf.Max(1, stage.ShardRowCount);
            float rowTravelTime = spacing / Mathf.Max(1f, stage.PlayerForwardSpeed + 1.75f);
            return rowTravelTime >= 0.24f ? 2 : 1;
        }

        // Returns the current difficulty tier used for stage pacing and star target ratios.
        public static int GetDifficultyTier(int stageIndex)
        {
            if (stageIndex <= 3)
            {
                return 1;
            }

            if (stageIndex <= 10)
            {
                return 2;
            }

            if (stageIndex <= 20)
            {
                return 3;
            }

            return 4;
        }

        // Returns the allowed mistake budget used to keep three stars near perfect-play difficulty.
        public static int GetThreeStarMistakeAllowance(int stageIndex)
        {
            if (stageIndex <= 3)
            {
                return 2;
            }

            if (stageIndex <= 10)
            {
                return stageIndex <= 6 ? 2 : 1;
            }

            return 1;
        }

        // Returns the target ratio that prevents three stars from being awarded on low-score clears.
        public static float GetThreeStarRatio(int stageIndex)
        {
            if (stageIndex == 1)
            {
                return 0.93f;
            }

            if (stageIndex == 2)
            {
                return 0.94f;
            }

            if (stageIndex == 3)
            {
                return 0.95f;
            }

            if (stageIndex <= 10)
            {
                return 0.96f;
            }

            if (stageIndex <= 20)
            {
                return 0.97f;
            }

            return 0.98f;
        }

        // Applies the finite Stage Mode score rules for one row-lane choice, including wrong-shard chances.
        private static void ApplyLaneChoice(
            GeneratedLaneContent content,
            ref int score,
            ref int combo,
            ref int collectibleCount,
            ref int wrongShardCount)
        {
            if (content == GeneratedLaneContent.MatchingShard)
            {
                combo = Mathf.Min(GameConstants.ComboCap, combo + 1);
                score += GameConstants.SameColorShardScore * Mathf.Max(1, combo);
                collectibleCount++;
            }
            else if (content == GeneratedLaneContent.OffColorShard)
            {
                combo = 0;
                wrongShardCount++;
                score = Mathf.Max(0, score - GameConstants.WrongColorShardPenalty);
            }
        }

        // Calculates the two-star target as the rounded-up two-thirds point of the three-star target.
        public static int CalculateTwoStarFromThreeStar(int threeStarScore)
        {
            if (threeStarScore <= ScoreStep)
            {
                return ScoreStep;
            }

            int target = CeilToStep(Mathf.CeilToInt(threeStarScore * 2f / 3f));
            return Mathf.Clamp(target, ScoreStep, Mathf.Max(ScoreStep, threeStarScore - ScoreStep));
        }

        // Calculates a three-star target near the estimated best route and never above it.
        private static int CalculateThreeStarScore(int stageIndex, int estimatedMaxScore, int mistakeAllowance)
        {
            if (estimatedMaxScore <= ScoreStep * 3)
            {
                return Mathf.Max(ScoreStep, estimatedMaxScore);
            }

            int ratioTarget = CeilToStep(Mathf.CeilToInt(estimatedMaxScore * GetThreeStarRatio(stageIndex)));
            int mistakeTarget = estimatedMaxScore - EstimateAverageMistakeCost(stageIndex) * mistakeAllowance;
            int target = Mathf.Max(ratioTarget, mistakeTarget);
            return Mathf.Clamp(target, ScoreStep * 2, estimatedMaxScore);
        }

        // Estimates the average score damage caused by a missed match or wrong shard under the shared three-chance rule.
        private static int EstimateAverageMistakeCost(int stageIndex)
        {
            int comboPressure = stageIndex <= 3 ? 2 : (stageIndex <= 10 ? 3 : 4);
            return GameConstants.SameColorShardScore * comboPressure + GameConstants.WrongColorShardPenalty;
        }

        // Creates a lane-combo-wrong-count score grid initialized to invalid path values.
        private static int[,,] CreateScoreGrid(int wrongShardLimit)
        {
            int[,,] scores = new int[GameConstants.LaneCount, GameConstants.ComboCap + 1, wrongShardLimit];
            for (int lane = 0; lane < GameConstants.LaneCount; lane++)
            {
                for (int combo = 0; combo <= GameConstants.ComboCap; combo++)
                {
                    for (int wrongShardCount = 0; wrongShardCount < wrongShardLimit; wrongShardCount++)
                    {
                        scores[lane, combo, wrongShardCount] = InvalidScore;
                    }
                }
            }

            return scores;
        }

        // Keeps the best score for a lane-combo-wrong-count state, using collectible count as the tie breaker.
        private static void StoreBest(
            int[,,] scores,
            int[,,] counts,
            int lane,
            int combo,
            int wrongShardCount,
            int score,
            int collectibleCount)
        {
            if (score > scores[lane, combo, wrongShardCount]
                || (score == scores[lane, combo, wrongShardCount] && collectibleCount > counts[lane, combo, wrongShardCount]))
            {
                scores[lane, combo, wrongShardCount] = score;
                counts[lane, combo, wrongShardCount] = collectibleCount;
            }
        }

        // Rounds a score up so target UI never understates the intended cutoff.
        private static int CeilToStep(int value)
        {
            return Mathf.CeilToInt(value / (float)ScoreStep) * ScoreStep;
        }
    }
}
