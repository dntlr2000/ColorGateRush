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
        public const int SameColorShardScore = 10;
        public const int WrongColorShardPenalty = 15;
        public const int GateScore = 5;
        public const int ObstaclePenalty = 50;

        public static readonly float[] LaneX = { -LaneSpacing, 0f, LaneSpacing };

        // Converts the gameplay color id into the active procedural palette.
        public static Color ToUnityColor(ColorId colorId)
        {
            return GameSettings.ColorAssistEnabled ? ToHighContrastColor(colorId) : ToNeonColor(colorId);
        }

        // Converts the gameplay color id into the default neon palette used by procedural materials and VFX.
        private static Color ToNeonColor(ColorId colorId)
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

        // Converts the gameplay color id into a high-contrast assistive palette.
        private static Color ToHighContrastColor(ColorId colorId)
        {
            switch (colorId)
            {
                case ColorId.Cyan:
                    return new Color(0.0f, 0.70f, 1.0f);
                case ColorId.Magenta:
                    return new Color(1.0f, 0.18f, 0.25f);
                case ColorId.Yellow:
                    return new Color(1.0f, 0.86f, 0.0f);
                case ColorId.Lime:
                    return new Color(0.0f, 0.95f, 0.38f);
                default:
                    return Color.white;
            }
        }

        // Returns a readable Korean color label for UI and tutorial text.
        public static string ColorName(ColorId colorId)
        {
            switch (colorId)
            {
                case ColorId.Cyan:
                    return "시안";
                case ColorId.Magenta:
                    return "마젠타";
                case ColorId.Yellow:
                    return "노랑";
                case ColorId.Lime:
                    return "라임";
                default:
                    return "색상";
            }
        }

        // Returns the shape symbol paired with a gameplay color for color-assist readability.
        public static string ColorSymbol(ColorId colorId)
        {
            switch (colorId)
            {
                case ColorId.Cyan:
                    return "●";
                case ColorId.Magenta:
                    return "■";
                case ColorId.Yellow:
                    return "◆";
                case ColorId.Lime:
                    return "▲";
                default:
                    return "?";
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
