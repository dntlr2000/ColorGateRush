using UnityEngine;

namespace ColorGateRush
{
    public readonly struct VisualThemeProfile
    {
        public readonly Color CameraBackgroundColor;
        public readonly Color FogColor;
        public readonly Color AmbientColor;
        public readonly Color DirectionalLightColor;
        public readonly float DirectionalLightIntensity;
        public readonly Color BackdropTopColor;
        public readonly Color BackdropBottomColor;
        public readonly Color SidePanelColor;
        public readonly Color TrackBaseColor;
        public readonly Color TrackEdgeColor;
        public readonly Color TrackAccentColor;
        public readonly Color PlatformColor;
        public readonly Color ObstacleColor;
        public readonly Color ObstacleWarningColor;
        public readonly Color GatePanelColor;
        public readonly Color FinishColor;
        public readonly Color HudPanelColor;
        public readonly Color HudTextColor;
        public readonly Color HudAccentColor;
        public readonly float ShardGlowAlpha;
        public readonly float VfxIntensity;

        // Stores the complete procedural visual palette used by world objects, VFX, and UI.
        public VisualThemeProfile(
            Color cameraBackgroundColor,
            Color fogColor,
            Color ambientColor,
            Color directionalLightColor,
            float directionalLightIntensity,
            Color backdropTopColor,
            Color backdropBottomColor,
            Color sidePanelColor,
            Color trackBaseColor,
            Color trackEdgeColor,
            Color trackAccentColor,
            Color platformColor,
            Color obstacleColor,
            Color obstacleWarningColor,
            Color gatePanelColor,
            Color finishColor,
            Color hudPanelColor,
            Color hudTextColor,
            Color hudAccentColor,
            float shardGlowAlpha,
            float vfxIntensity)
        {
            CameraBackgroundColor = cameraBackgroundColor;
            FogColor = fogColor;
            AmbientColor = ambientColor;
            DirectionalLightColor = directionalLightColor;
            DirectionalLightIntensity = directionalLightIntensity;
            BackdropTopColor = backdropTopColor;
            BackdropBottomColor = backdropBottomColor;
            SidePanelColor = sidePanelColor;
            TrackBaseColor = trackBaseColor;
            TrackEdgeColor = trackEdgeColor;
            TrackAccentColor = trackAccentColor;
            PlatformColor = platformColor;
            ObstacleColor = obstacleColor;
            ObstacleWarningColor = obstacleWarningColor;
            GatePanelColor = gatePanelColor;
            FinishColor = finishColor;
            HudPanelColor = hudPanelColor;
            HudTextColor = hudTextColor;
            HudAccentColor = hudAccentColor;
            ShardGlowAlpha = shardGlowAlpha;
            VfxIntensity = vfxIntensity;
        }
    }

    public static class VisualTheme
    {
        public const int ThemeVariationCount = 5;
        public static int ActiveThemeIndex { get; private set; }

        // Stores the active procedural theme index for the next generated stage.
        public static void SetActiveThemeIndex(int themeIndex)
        {
            ActiveThemeIndex = Mathf.Abs(themeIndex) % ThemeVariationCount;
        }

        // Returns the active theme, switching to a higher-contrast palette when color assist is enabled.
        public static VisualThemeProfile Current()
        {
            return GameSettings.ColorAssistEnabled ? HighContrastCandyNeon() : CandyNeonVariant(ActiveThemeIndex);
        }

        // Returns one of a small set of mobile-readable procedural theme variations.
        private static VisualThemeProfile CandyNeonVariant(int themeIndex)
        {
            switch (Mathf.Abs(themeIndex) % ThemeVariationCount)
            {
                case 1:
                    return BuildThemeVariant(
                        new Color(0.045f, 0.115f, 0.110f),
                        new Color(0.032f, 0.065f, 0.070f),
                        new Color(0.16f, 0.92f, 0.74f),
                        new Color(0.05f, 0.12f, 0.10f),
                        new Color(0.0f, 0.92f, 0.72f),
                        new Color(0.0f, 0.86f, 0.68f));
                case 2:
                    return BuildThemeVariant(
                        new Color(0.130f, 0.080f, 0.110f),
                        new Color(0.080f, 0.040f, 0.075f),
                        new Color(1.0f, 0.54f, 0.38f),
                        new Color(0.12f, 0.06f, 0.09f),
                        new Color(1.0f, 0.47f, 0.36f),
                        new Color(1.0f, 0.72f, 0.38f));
                case 3:
                    return BuildThemeVariant(
                        new Color(0.040f, 0.050f, 0.115f),
                        new Color(0.020f, 0.025f, 0.060f),
                        new Color(0.62f, 0.38f, 1.0f),
                        new Color(0.045f, 0.050f, 0.115f),
                        new Color(0.58f, 0.40f, 1.0f),
                        new Color(0.92f, 0.74f, 1.0f));
                case 4:
                    return BuildThemeVariant(
                        new Color(0.035f, 0.075f, 0.130f),
                        new Color(0.018f, 0.045f, 0.080f),
                        new Color(0.40f, 0.78f, 1.0f),
                        new Color(0.030f, 0.055f, 0.100f),
                        new Color(0.30f, 0.72f, 1.0f),
                        new Color(0.80f, 0.92f, 1.0f));
                default:
                    return CandyNeon();
            }
        }

        // Builds a theme variant while preserving the core danger and HUD contrast language.
        private static VisualThemeProfile BuildThemeVariant(
            Color cameraBackgroundColor,
            Color fogColor,
            Color accentColor,
            Color trackBaseColor,
            Color hudAccentColor,
            Color finishColor)
        {
            return new VisualThemeProfile(
                cameraBackgroundColor,
                fogColor,
                new Color(0.58f, 0.62f, 0.76f),
                new Color(0.96f, 0.92f, 1.0f),
                1.25f,
                new Color(cameraBackgroundColor.r + 0.035f, cameraBackgroundColor.g + 0.035f, cameraBackgroundColor.b + 0.055f, 0.72f),
                new Color(fogColor.r * 0.5f, fogColor.g * 0.5f, fogColor.b * 0.65f, 0.82f),
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.30f),
                trackBaseColor,
                new Color(trackBaseColor.r + 0.030f, trackBaseColor.g + 0.050f, trackBaseColor.b + 0.075f),
                accentColor,
                new Color(trackBaseColor.r + 0.015f, trackBaseColor.g + 0.020f, trackBaseColor.b + 0.030f),
                new Color(0.92f, 0.16f, 0.18f),
                new Color(1.0f, 0.78f, 0.18f),
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.34f),
                finishColor,
                new Color(0.010f, 0.014f, 0.028f, 0.78f),
                Color.white,
                hudAccentColor,
                0.26f,
                1.0f);
        }

        // Builds the default clean candy-neon palette for the runner.
        private static VisualThemeProfile CandyNeon()
        {
            return new VisualThemeProfile(
                new Color(0.070f, 0.085f, 0.135f),
                new Color(0.075f, 0.090f, 0.140f),
                new Color(0.58f, 0.62f, 0.76f),
                new Color(0.96f, 0.92f, 1.0f),
                1.25f,
                new Color(0.105f, 0.120f, 0.185f, 0.72f),
                new Color(0.025f, 0.035f, 0.070f, 0.82f),
                new Color(0.10f, 0.16f, 0.28f, 0.34f),
                new Color(0.045f, 0.055f, 0.090f),
                new Color(0.075f, 0.110f, 0.170f),
                new Color(0.18f, 0.74f, 0.98f),
                new Color(0.060f, 0.075f, 0.120f),
                new Color(0.92f, 0.16f, 0.18f),
                new Color(1.0f, 0.78f, 0.18f),
                new Color(0.70f, 0.92f, 1.0f, 0.34f),
                new Color(1.0f, 0.95f, 0.56f),
                new Color(0.010f, 0.014f, 0.028f, 0.78f),
                Color.white,
                new Color(0.0f, 0.84f, 1.0f),
                0.26f,
                1.0f);
        }

        // Builds a more contrasted version for color-assist mode without changing gameplay semantics.
        private static VisualThemeProfile HighContrastCandyNeon()
        {
            return new VisualThemeProfile(
                new Color(0.025f, 0.030f, 0.055f),
                new Color(0.025f, 0.030f, 0.055f),
                new Color(0.68f, 0.70f, 0.80f),
                Color.white,
                1.35f,
                new Color(0.06f, 0.07f, 0.11f, 0.82f),
                new Color(0.005f, 0.010f, 0.025f, 0.88f),
                new Color(0.10f, 0.20f, 0.34f, 0.46f),
                new Color(0.020f, 0.025f, 0.050f),
                new Color(0.095f, 0.145f, 0.235f),
                new Color(0.0f, 0.95f, 1.0f),
                new Color(0.035f, 0.050f, 0.085f),
                new Color(1.0f, 0.05f, 0.08f),
                new Color(1.0f, 0.92f, 0.08f),
                new Color(0.82f, 0.95f, 1.0f, 0.42f),
                new Color(1.0f, 0.98f, 0.48f),
                new Color(0.0f, 0.0f, 0.0f, 0.84f),
                Color.white,
                new Color(0.0f, 0.95f, 1.0f),
                0.34f,
                1.12f);
        }
    }
}
