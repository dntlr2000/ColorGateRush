namespace ColorGateRush
{
    public enum GeneratedLaneContent
    {
        Empty,
        MatchingShard,
        OffColorShard,
        Obstacle
    }

    public enum UnsafeRowReason
    {
        None,
        AllObstacle,
        AllOffColor,
        MixedUnsafe
    }

    public readonly struct LevelRowReport
    {
        public readonly int RowIndex;
        public readonly float RowZ;
        public readonly ColorId ExpectedColor;
        public readonly GeneratedLaneContent Lane0;
        public readonly GeneratedLaneContent Lane1;
        public readonly GeneratedLaneContent Lane2;
        public readonly int SafeLaneCount;
        public readonly int MatchingShardCount;
        public readonly int OffColorShardCount;
        public readonly int ObstacleCount;
        public readonly bool IsZAligned;

        // Captures one generated decision row for validator and QA reporting.
        public LevelRowReport(
            int rowIndex,
            float rowZ,
            ColorId expectedColor,
            GeneratedLaneContent lane0,
            GeneratedLaneContent lane1,
            GeneratedLaneContent lane2,
            bool isZAligned)
        {
            RowIndex = rowIndex;
            RowZ = rowZ;
            ExpectedColor = expectedColor;
            Lane0 = lane0;
            Lane1 = lane1;
            Lane2 = lane2;
            IsZAligned = isZAligned;

            MatchingShardCount = CountContent(lane0, lane1, lane2, GeneratedLaneContent.MatchingShard);
            OffColorShardCount = CountContent(lane0, lane1, lane2, GeneratedLaneContent.OffColorShard);
            ObstacleCount = CountContent(lane0, lane1, lane2, GeneratedLaneContent.Obstacle);
            SafeLaneCount = CountSafeOptions(lane0, lane1, lane2);
        }

        // Returns true when at least one lane is neutral or beneficial for the expected player color.
        public bool HasSafeOption()
        {
            return SafeLaneCount > 0;
        }

        // Finds whether this row is invalid under the safe-option invariant.
        public UnsafeRowReason GetUnsafeReason()
        {
            if (ObstacleCount == GameConstants.LaneCount)
            {
                return UnsafeRowReason.AllObstacle;
            }

            if (OffColorShardCount == GameConstants.LaneCount)
            {
                return UnsafeRowReason.AllOffColor;
            }

            if (!HasSafeOption())
            {
                return UnsafeRowReason.MixedUnsafe;
            }

            return UnsafeRowReason.None;
        }

        // Returns one lane's recorded content for inspector/debug tooling.
        public GeneratedLaneContent GetLaneContent(int laneIndex)
        {
            switch (laneIndex)
            {
                case 0:
                    return Lane0;
                case 1:
                    return Lane1;
                default:
                    return Lane2;
            }
        }

        // Counts a target lane content in three lane slots.
        private static int CountContent(
            GeneratedLaneContent lane0,
            GeneratedLaneContent lane1,
            GeneratedLaneContent lane2,
            GeneratedLaneContent target)
        {
            int count = 0;
            if (lane0 == target)
            {
                count++;
            }

            if (lane1 == target)
            {
                count++;
            }

            if (lane2 == target)
            {
                count++;
            }

            return count;
        }

        // Counts lanes that do not immediately fail or penalize the player.
        private static int CountSafeOptions(
            GeneratedLaneContent lane0,
            GeneratedLaneContent lane1,
            GeneratedLaneContent lane2)
        {
            int count = 0;
            if (IsSafeContent(lane0))
            {
                count++;
            }

            if (IsSafeContent(lane1))
            {
                count++;
            }

            if (IsSafeContent(lane2))
            {
                count++;
            }

            return count;
        }

        // Treats empty lanes and matching shards as safe choices for the row.
        private static bool IsSafeContent(GeneratedLaneContent content)
        {
            return content == GeneratedLaneContent.Empty || content == GeneratedLaneContent.MatchingShard;
        }
    }
}
