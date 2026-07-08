using UnityEngine;

namespace ColorGateRush
{
    public enum PlaytestExitReason
    {
        None,
        Completed,
        Failed,
        QuitToMainMenu,
        QuitToStageSelect,
        Restarted
    }

    public readonly struct StagePlaytestStats
    {
        public readonly int StageIndex;
        public readonly int Attempts;
        public readonly int Clears;
        public readonly int Fails;
        public readonly int ObstacleFails;
        public readonly int WrongShardFails;
        public readonly int WrongShardLimitFails;
        public readonly int Quits;
        public readonly int BestScore;
        public readonly int BestStars;
        public readonly int TotalScore;
        public readonly int LastScore;
        public readonly int LastStars;
        public readonly int LastWrongShardCount;
        public readonly int ThreeStarCount;
        public readonly float TotalPlayTimeSeconds;
        public readonly PlaytestExitReason LastExitReason;

        // Stores one stage's local-only playtest counters loaded from PlayerPrefs.
        public StagePlaytestStats(
            int stageIndex,
            int attempts,
            int clears,
            int fails,
            int obstacleFails,
            int wrongShardFails,
            int wrongShardLimitFails,
            int quits,
            int bestScore,
            int bestStars,
            int totalScore,
            int lastScore,
            int lastStars,
            int lastWrongShardCount,
            int threeStarCount,
            float totalPlayTimeSeconds,
            PlaytestExitReason lastExitReason)
        {
            StageIndex = stageIndex;
            Attempts = attempts;
            Clears = clears;
            Fails = fails;
            ObstacleFails = obstacleFails;
            WrongShardFails = wrongShardFails;
            WrongShardLimitFails = wrongShardLimitFails;
            Quits = quits;
            BestScore = bestScore;
            BestStars = bestStars;
            TotalScore = totalScore;
            LastScore = lastScore;
            LastStars = lastStars;
            LastWrongShardCount = lastWrongShardCount;
            ThreeStarCount = threeStarCount;
            TotalPlayTimeSeconds = totalPlayTimeSeconds;
            LastExitReason = lastExitReason;
        }

        public float AverageScore => Attempts > 0 ? TotalScore / (float)Attempts : 0f;
        public float ClearRate => Attempts > 0 ? Clears / (float)Attempts : 0f;
    }

    public static class PlaytestStats
    {
        public const string StatsPrefix = "CGR_Stats_";
        public const string StagePrefix = "CGR_Stats_Stage_";

        // Records that a fresh stage attempt has started.
        public static void RecordStageStarted(int stageIndex)
        {
            if (!IsValidStage(stageIndex))
            {
                return;
            }

            PlayerPrefs.SetInt(Key(stageIndex, "Attempts"), PlayerPrefs.GetInt(Key(stageIndex, "Attempts"), 0) + 1);
            PlayerPrefs.Save();
        }

        // Records a completed run and preserves best score/star counters.
        public static void RecordCompleted(int stageIndex, int score, int stars, float elapsedSeconds)
        {
            RecordCompleted(stageIndex, score, stars, elapsedSeconds, 0);
        }

        // Records a completed run and remembers how many wrong-shard chances were used.
        public static void RecordCompleted(int stageIndex, int score, int stars, float elapsedSeconds, int wrongShardCount)
        {
            if (!IsValidStage(stageIndex))
            {
                return;
            }

            PlayerPrefs.SetInt(Key(stageIndex, "Clears"), PlayerPrefs.GetInt(Key(stageIndex, "Clears"), 0) + 1);
            if (stars >= 3)
            {
                PlayerPrefs.SetInt(Key(stageIndex, "ThreeStarCount"), PlayerPrefs.GetInt(Key(stageIndex, "ThreeStarCount"), 0) + 1);
            }

            RecordOutcome(stageIndex, score, Mathf.Clamp(stars, 0, 3), elapsedSeconds, PlaytestExitReason.Completed, wrongShardCount);
        }

        // Records a failed run with the default obstacle reason for older call sites.
        public static void RecordFailed(int stageIndex, int score, float elapsedSeconds)
        {
            RecordFailed(stageIndex, score, elapsedSeconds, StageFailReason.ObstacleHit, 0);
        }

        // Records a failed run without changing progression or best-star save data.
        public static void RecordFailed(int stageIndex, int score, float elapsedSeconds, StageFailReason failReason)
        {
            RecordFailed(stageIndex, score, elapsedSeconds, failReason, failReason == StageFailReason.WrongShardLimit ? GameConstants.MaxWrongShardCount : 0);
        }

        // Records a failed run and captures the wrong-shard count at the moment of failure.
        public static void RecordFailed(int stageIndex, int score, float elapsedSeconds, StageFailReason failReason, int wrongShardCount)
        {
            if (!IsValidStage(stageIndex))
            {
                return;
            }

            PlayerPrefs.SetInt(Key(stageIndex, "Fails"), PlayerPrefs.GetInt(Key(stageIndex, "Fails"), 0) + 1);
            if (failReason == StageFailReason.WrongShardLimit)
            {
                PlayerPrefs.SetInt(Key(stageIndex, "WrongShardFails"), PlayerPrefs.GetInt(Key(stageIndex, "WrongShardFails"), 0) + 1);
                PlayerPrefs.SetInt(Key(stageIndex, "WrongShardLimitFails"), PlayerPrefs.GetInt(Key(stageIndex, "WrongShardLimitFails"), 0) + 1);
            }
            else
            {
                PlayerPrefs.SetInt(Key(stageIndex, "ObstacleFails"), PlayerPrefs.GetInt(Key(stageIndex, "ObstacleFails"), 0) + 1);
            }

            RecordOutcome(stageIndex, score, 0, elapsedSeconds, PlaytestExitReason.Failed, wrongShardCount);
        }

        // Records an abandoned active run from pause/menu navigation as a quit, not a failure.
        public static void RecordQuit(int stageIndex, int score, float elapsedSeconds, PlaytestExitReason reason)
        {
            if (!IsValidStage(stageIndex))
            {
                return;
            }

            PlayerPrefs.SetInt(Key(stageIndex, "Quits"), PlayerPrefs.GetInt(Key(stageIndex, "Quits"), 0) + 1);
            RecordOutcome(stageIndex, score, 0, elapsedSeconds, reason, 0);
        }

        // Loads one stage's local playtest stats from CGR_Stats_ PlayerPrefs keys.
        public static StagePlaytestStats LoadStage(int stageIndex)
        {
            stageIndex = Mathf.Clamp(stageIndex, 1, StageManager.TotalStageCount);
            return new StagePlaytestStats(
                stageIndex,
                PlayerPrefs.GetInt(Key(stageIndex, "Attempts"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "Clears"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "Fails"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "ObstacleFails"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "WrongShardFails"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "WrongShardLimitFails"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "Quits"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "BestScore"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "BestStars"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "TotalScore"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "LastScore"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "LastStars"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "LastWrongShardCount"), 0),
                PlayerPrefs.GetInt(Key(stageIndex, "ThreeStarCount"), 0),
                PlayerPrefs.GetFloat(Key(stageIndex, "TotalPlayTimeSeconds"), 0f),
                (PlaytestExitReason)PlayerPrefs.GetInt(Key(stageIndex, "LastExitReason"), (int)PlaytestExitReason.None));
        }

        // Deletes only CGR_Stats_ keys for the configured stage range.
        public static void ResetAll(int stageCount)
        {
            int clampedStageCount = Mathf.Clamp(stageCount, 1, StageManager.TotalStageCount);
            for (int stage = 1; stage <= clampedStageCount; stage++)
            {
                PlayerPrefs.DeleteKey(Key(stage, "Attempts"));
                PlayerPrefs.DeleteKey(Key(stage, "Clears"));
                PlayerPrefs.DeleteKey(Key(stage, "Fails"));
                PlayerPrefs.DeleteKey(Key(stage, "ObstacleFails"));
                PlayerPrefs.DeleteKey(Key(stage, "WrongShardFails"));
                PlayerPrefs.DeleteKey(Key(stage, "WrongShardLimitFails"));
                PlayerPrefs.DeleteKey(Key(stage, "Quits"));
                PlayerPrefs.DeleteKey(Key(stage, "BestScore"));
                PlayerPrefs.DeleteKey(Key(stage, "BestStars"));
                PlayerPrefs.DeleteKey(Key(stage, "TotalScore"));
                PlayerPrefs.DeleteKey(Key(stage, "LastScore"));
                PlayerPrefs.DeleteKey(Key(stage, "LastStars"));
                PlayerPrefs.DeleteKey(Key(stage, "LastWrongShardCount"));
                PlayerPrefs.DeleteKey(Key(stage, "ThreeStarCount"));
                PlayerPrefs.DeleteKey(Key(stage, "TotalPlayTimeSeconds"));
                PlayerPrefs.DeleteKey(Key(stage, "LastExitReason"));
            }

            PlayerPrefs.Save();
        }

        // Formats a compact per-stage row for the runtime playtest stats screen.
        public static string BuildSummaryLine(int stageIndex)
        {
            StagePlaytestStats stats = LoadStage(stageIndex);
            string stars = new string('★', Mathf.Clamp(stats.BestStars, 0, 3)) + new string('☆', 3 - Mathf.Clamp(stats.BestStars, 0, 3));
            return "Stage " + stats.StageIndex.ToString("00")
                + " | 시도 " + stats.Attempts
                + " | 클리어 " + Mathf.RoundToInt(stats.ClearRate * 100f) + "%"
                + " | 최고 " + stats.BestScore
                + " " + stars
                + " | 최근 " + stats.LastScore
                + "점/" + stats.LastStars + "★"
                + " | 최근실수 " + stats.LastWrongShardCount + "/" + GameConstants.MaxWrongShardCount
                + " | 실패 색한도/장 " + stats.WrongShardLimitFails + "/" + stats.ObstacleFails
                + " | 중단 " + stats.Quits;
        }

        // Updates shared score, star, playtime, and last-exit counters for one finished attempt.
        private static void RecordOutcome(int stageIndex, int score, int stars, float elapsedSeconds, PlaytestExitReason reason)
        {
            RecordOutcome(stageIndex, score, stars, elapsedSeconds, reason, 0);
        }

        // Updates shared score, star, playtime, wrong-shard count, and last-exit counters for one finished attempt.
        private static void RecordOutcome(int stageIndex, int score, int stars, float elapsedSeconds, PlaytestExitReason reason, int wrongShardCount)
        {
            int safeScore = Mathf.Max(0, score);
            float safeElapsed = Mathf.Max(0f, elapsedSeconds);
            PlayerPrefs.SetInt(Key(stageIndex, "LastScore"), safeScore);
            PlayerPrefs.SetInt(Key(stageIndex, "LastStars"), Mathf.Clamp(stars, 0, 3));
            PlayerPrefs.SetInt(Key(stageIndex, "LastWrongShardCount"), Mathf.Clamp(wrongShardCount, 0, GameConstants.MaxWrongShardCount));
            PlayerPrefs.SetInt(Key(stageIndex, "TotalScore"), PlayerPrefs.GetInt(Key(stageIndex, "TotalScore"), 0) + safeScore);
            PlayerPrefs.SetFloat(
                Key(stageIndex, "TotalPlayTimeSeconds"),
                PlayerPrefs.GetFloat(Key(stageIndex, "TotalPlayTimeSeconds"), 0f) + safeElapsed);
            PlayerPrefs.SetInt(Key(stageIndex, "BestScore"), Mathf.Max(PlayerPrefs.GetInt(Key(stageIndex, "BestScore"), 0), safeScore));
            PlayerPrefs.SetInt(Key(stageIndex, "BestStars"), Mathf.Max(PlayerPrefs.GetInt(Key(stageIndex, "BestStars"), 0), Mathf.Clamp(stars, 0, 3)));
            PlayerPrefs.SetInt(Key(stageIndex, "LastExitReason"), (int)reason);
            PlayerPrefs.Save();
        }

        // Returns true when a stage index maps to the shipped campaign range.
        private static bool IsValidStage(int stageIndex)
        {
            return stageIndex >= 1 && stageIndex <= StageManager.TotalStageCount;
        }

        // Builds a namespaced PlayerPrefs key for one stage statistic.
        private static string Key(int stageIndex, string suffix)
        {
            return StagePrefix + stageIndex + "_" + suffix;
        }
    }
}
