namespace ColorGateRush
{
    public enum StageFailReason
    {
        None,
        ObstacleHit,
        WrongShardLimit
    }

    public readonly struct StageResult
    {
        public readonly int StageIndex;
        public readonly int Score;
        public readonly int Stars;
        public readonly int PreviousBestStars;
        public readonly int BestStars;
        public readonly bool BestStarsImproved;
        public readonly bool NextStageUnlocked;
        public readonly bool HasNextStage;
        public readonly StageFailReason FailReason;

        // Stores the saved outcome of a completed or failed stage attempt.
        public StageResult(
            int stageIndex,
            int score,
            int stars,
            int previousBestStars,
            int bestStars,
            bool bestStarsImproved,
            bool nextStageUnlocked,
            bool hasNextStage,
            StageFailReason failReason = StageFailReason.None)
        {
            StageIndex = stageIndex;
            Score = score;
            Stars = stars;
            PreviousBestStars = previousBestStars;
            BestStars = bestStars;
            BestStarsImproved = bestStarsImproved;
            NextStageUnlocked = nextStageUnlocked;
            HasNextStage = hasNextStage;
            FailReason = failReason;
        }
    }
}
