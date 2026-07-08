using UnityEngine;

namespace ColorGateRush
{
    public enum EndlessFailReason
    {
        ObstacleHit,
        WrongShardLimit
    }

    public readonly struct EndlessRunResult
    {
        public readonly int Score;
        public readonly float Distance;
        public readonly int RowsGenerated;
        public readonly int WrongShardCount;
        public readonly int WrongShardLimit;
        public readonly EndlessFailReason FailReason;
        public readonly int BestScore;
        public readonly float BestDistance;
        public readonly int BestRows;
        public readonly bool NewBestScore;
        public readonly bool NewBestDistance;

        // Stores one finished Endless run result and record comparison data.
        public EndlessRunResult(
            int score,
            float distance,
            int rowsGenerated,
            int wrongShardCount,
            int wrongShardLimit,
            EndlessFailReason failReason,
            int bestScore,
            float bestDistance,
            int bestRows,
            bool newBestScore,
            bool newBestDistance)
        {
            Score = score;
            Distance = distance;
            RowsGenerated = rowsGenerated;
            WrongShardCount = wrongShardCount;
            WrongShardLimit = wrongShardLimit;
            FailReason = failReason;
            BestScore = bestScore;
            BestDistance = bestDistance;
            BestRows = bestRows;
            NewBestScore = newBestScore;
            NewBestDistance = newBestDistance;
        }
    }

    public static class EndlessRecords
    {
        public const string BestScoreKey = "CGR_EndlessBestScore";
        public const string BestDistanceKey = "CGR_EndlessBestDistance";
        public const string BestRowsKey = "CGR_EndlessBestRows";
        public const string AttemptsKey = "CGR_EndlessAttempts";
        public const string TotalRunsKey = "CGR_EndlessTotalRuns";
        public const string WrongShardLimitFailsKey = "CGR_EndlessWrongShardLimitFails";

        public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
        public static float BestDistance => PlayerPrefs.GetFloat(BestDistanceKey, 0f);
        public static int BestRows => PlayerPrefs.GetInt(BestRowsKey, 0);
        public static int Attempts => PlayerPrefs.GetInt(AttemptsKey, 0);
        public static int TotalRuns => PlayerPrefs.GetInt(TotalRunsKey, 0);
        public static int WrongShardLimitFails => PlayerPrefs.GetInt(WrongShardLimitFailsKey, 0);

        // Records that an Endless attempt has started without changing stage progression.
        public static void RecordAttempt()
        {
            PlayerPrefs.SetInt(AttemptsKey, Attempts + 1);
            PlayerPrefs.Save();
        }

        // Saves an Endless failure result and updates best score/distance independently from Stage Mode.
        public static EndlessRunResult SaveResult(
            int score,
            float distance,
            int rowsGenerated,
            int wrongShardCount,
            int wrongShardLimit,
            EndlessFailReason failReason)
        {
            int safeScore = Mathf.Max(0, score);
            float safeDistance = Mathf.Max(0f, distance);
            int safeRows = Mathf.Max(0, rowsGenerated);
            int safeWrongCount = Mathf.Clamp(wrongShardCount, 0, Mathf.Max(1, wrongShardLimit));
            int safeWrongLimit = Mathf.Max(1, wrongShardLimit);
            int previousBestScore = BestScore;
            float previousBestDistance = BestDistance;
            int previousBestRows = BestRows;
            bool newBestScore = safeScore > previousBestScore;
            bool newBestDistance = safeDistance > previousBestDistance;

            if (newBestScore)
            {
                PlayerPrefs.SetInt(BestScoreKey, safeScore);
            }

            if (newBestDistance)
            {
                PlayerPrefs.SetFloat(BestDistanceKey, safeDistance);
            }

            if (safeRows > previousBestRows)
            {
                PlayerPrefs.SetInt(BestRowsKey, safeRows);
            }

            if (failReason == EndlessFailReason.WrongShardLimit)
            {
                PlayerPrefs.SetInt(WrongShardLimitFailsKey, WrongShardLimitFails + 1);
            }

            PlayerPrefs.SetInt(TotalRunsKey, TotalRuns + 1);
            PlayerPrefs.Save();
            return new EndlessRunResult(
                safeScore,
                safeDistance,
                safeRows,
                safeWrongCount,
                safeWrongLimit,
                failReason,
                Mathf.Max(previousBestScore, safeScore),
                Mathf.Max(previousBestDistance, safeDistance),
                Mathf.Max(previousBestRows, safeRows),
                newBestScore,
                newBestDistance);
        }

        // Deletes only Endless record keys and preserves stage progress, settings, and playtest stats.
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(BestScoreKey);
            PlayerPrefs.DeleteKey(BestDistanceKey);
            PlayerPrefs.DeleteKey(BestRowsKey);
            PlayerPrefs.DeleteKey(AttemptsKey);
            PlayerPrefs.DeleteKey(TotalRunsKey);
            PlayerPrefs.DeleteKey(WrongShardLimitFailsKey);
            PlayerPrefs.Save();
        }
    }
}
