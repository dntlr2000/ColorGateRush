using System.Collections.Generic;
using UnityEngine;

namespace ColorGateRush
{
    public sealed class LevelGenerationReport
    {
        private readonly List<string> _warnings = new List<string>();
        private readonly List<LevelRowReport> _rows = new List<LevelRowReport>();

        public int StageIndex { get; }
        public int Seed { get; }
        public int TotalRows { get; private set; }
        public int ShardRows { get; private set; }
        public int EmptyRows { get; private set; }
        public int RowsWithMatchingShard { get; private set; }
        public int RowsWithoutMatchingShard { get; private set; }
        public int GateRows { get; private set; }
        public int ObstacleRows { get; private set; }
        public int TotalShards { get; private set; }
        public int TotalObstacles { get; private set; }
        public int FinishCount { get; private set; }
        public int MaxPossibleCorrectShardScore { get; private set; }
        public int UnsafeRowsRepaired { get; private set; }
        public int AllObstacleRowsPrevented { get; private set; }
        public int AllOffColorRowsPrevented { get; private set; }
        public int MixedUnsafeRowsPrevented { get; private set; }
        public IReadOnlyList<string> Warnings => _warnings;
        public IReadOnlyList<LevelRowReport> Rows => _rows;
        public bool IsValid => _warnings.Count == 0 && FinishCount > 0;

        // Initializes a report for one deterministic stage generation pass.
        public LevelGenerationReport(int stageIndex, int seed)
        {
            StageIndex = stageIndex;
            Seed = seed;
        }

        // Records a fully resolved row after safety repair has run.
        public void RecordRow(LevelRowReport row)
        {
            _rows.Add(row);
            TotalRows++;

            int shardCount = row.MatchingShardCount + row.OffColorShardCount;
            TotalShards += shardCount;
            TotalObstacles += row.ObstacleCount;

            if (shardCount > 0)
            {
                ShardRows++;
            }

            if (shardCount == 0 && row.ObstacleCount == 0)
            {
                EmptyRows++;
            }

            if (row.ObstacleCount > 0)
            {
                ObstacleRows++;
            }

            if (row.MatchingShardCount > 0)
            {
                RowsWithMatchingShard++;
            }
            else
            {
                RowsWithoutMatchingShard++;
            }

            MaxPossibleCorrectShardScore += row.MatchingShardCount * GameConstants.SameColorShardScore;

            if (!row.IsZAligned)
            {
                Warn($"Stage {StageIndex} row {row.RowIndex} has misaligned shard/obstacle z positions at z={row.RowZ:F2}.");
            }

            UnsafeRowReason reason = row.GetUnsafeReason();
            if (reason != UnsafeRowReason.None)
            {
                Warn($"Stage {StageIndex} row {row.RowIndex} is unsafe after repair: {reason} at z={row.RowZ:F2}.");
            }
        }

        // Records that an unsafe generated row was repaired before object creation.
        public void RecordRepair(UnsafeRowReason reason)
        {
            if (reason == UnsafeRowReason.None)
            {
                return;
            }

            UnsafeRowsRepaired++;
            if (reason == UnsafeRowReason.AllObstacle)
            {
                AllObstacleRowsPrevented++;
            }
            else if (reason == UnsafeRowReason.AllOffColor)
            {
                AllOffColorRowsPrevented++;
            }
            else if (reason == UnsafeRowReason.MixedUnsafe)
            {
                MixedUnsafeRowsPrevented++;
            }
        }

        // Returns the percentage of decision rows that contain at least one matching shard.
        public float GetMatchingShardRatio()
        {
            if (TotalRows <= 0)
            {
                return 0f;
            }

            return RowsWithMatchingShard / (float)TotalRows;
        }

        // Returns the percentage of rows containing at least one shard.
        public float GetShardRowRatio()
        {
            if (TotalRows <= 0)
            {
                return 0f;
            }

            return ShardRows / (float)TotalRows;
        }

        // Returns the percentage of rows containing at least one obstacle.
        public float GetObstacleRowRatio()
        {
            if (TotalRows <= 0)
            {
                return 0f;
            }

            return ObstacleRows / (float)TotalRows;
        }

        // Returns the average number of shards placed per generated row.
        public float GetAverageShardsPerRow()
        {
            if (TotalRows <= 0)
            {
                return 0f;
            }

            return TotalShards / (float)TotalRows;
        }

        // Records a gate row for expected-color progression checks.
        public void RecordGate(int index, float z, ColorId targetColor)
        {
            GateRows++;
        }

        // Records that a finish trigger was generated.
        public void RecordFinish()
        {
            FinishCount++;
        }

        // Adds a warning and emits one concise log entry for generation QA.
        public void Warn(string message)
        {
            _warnings.Add(message);
            Debug.LogWarning(message);
        }
    }
}
