using UnityEngine;

namespace ColorGateRush
{
    public static class GameConstants
    {
        public const int LaneCount = 3;
        public const float LaneSpacing = 2.2f;
        public const float PlayerY = 0.65f;
        public const float TrackY = 0f;
        public const float TrackWidth = 7.25f;
        public const float TrackLength = 196f;
        public const float SegmentLength = 12f;
        public const float BaseForwardSpeed = 8.5f;
        public const float MaxForwardSpeed = 13.5f;
        public const float LaneMoveSharpness = 12f;
        public const int ComboCap = 10;
        public const int FinishMultiplierCap = 10;

        public static readonly float[] LaneX = { -LaneSpacing, 0f, LaneSpacing };

        // Converts the gameplay color id into the neon palette used by procedural materials and VFX.
        public static Color ToUnityColor(ColorId colorId)
        {
            switch (colorId)
            {
                case ColorId.Cyan:
                    return new Color(0.0f, 0.84f, 1.0f);
                case ColorId.Magenta:
                    return new Color(1.0f, 0.23f, 0.95f);
                case ColorId.Yellow:
                    return new Color(1.0f, 0.91f, 0.29f);
                case ColorId.Lime:
                    return new Color(0.55f, 1.0f, 0.29f);
                default:
                    return Color.white;
            }
        }

        // Advances to the next palette color for deterministic color cycling.
        public static ColorId NextColor(ColorId current)
        {
            return (ColorId)(((int)current + 1) % 4);
        }

        // Keeps lane indices inside the three-lane runner bounds.
        public static int ClampLane(int lane)
        {
            return Mathf.Clamp(lane, 0, LaneCount - 1);
        }
    }
}
