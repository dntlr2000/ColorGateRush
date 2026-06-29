namespace ColorGateRush
{
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

        // Stores the saved outcome of a completed or failed stage attempt.
        public StageResult(
            int stageIndex,
            int score,
            int stars,
            int previousBestStars,
            int bestStars,
            bool bestStarsImproved,
            bool nextStageUnlocked,
            bool hasNextStage)
        {
            StageIndex = stageIndex;
            Score = score;
            Stars = stars;
            PreviousBestStars = previousBestStars;
            BestStars = bestStars;
            BestStarsImproved = bestStarsImproved;
            NextStageUnlocked = nextStageUnlocked;
            HasNextStage = hasNextStage;
        }
    }
}
