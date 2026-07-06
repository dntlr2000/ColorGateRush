using UnityEngine;

namespace ColorGateRush
{
    public static class GameSettings
    {
        public const string TutorialSeenKey = "CGR_TutorialSeen";
        public const string SoundEnabledKey = "CGR_SoundEnabled";
        public const string MusicEnabledKey = "CGR_MusicEnabled";
        public const string SfxEnabledKey = "CGR_SfxEnabled";
        public const string MusicVolumeKey = "CGR_MusicVolume";
        public const string SfxVolumeKey = "CGR_SfxVolume";
        public const string CameraShakeEnabledKey = "CGR_CameraShake";
        public const string ColorAssistEnabledKey = "CGR_HighContrast";

        // Returns the legacy sound toggle value used to seed split Music/SFX settings.
        public static bool SoundEnabled => PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

        // Returns whether procedural loop music should play.
        public static bool MusicEnabled => PlayerPrefs.GetInt(MusicEnabledKey, SoundEnabled ? 1 : 0) == 1;

        // Returns whether procedural one-shot sound effects should play.
        public static bool SfxEnabled => PlayerPrefs.GetInt(SfxEnabledKey, SoundEnabled ? 1 : 0) == 1;

        // Returns the clamped music volume used by looped procedural BGM.
        public static float MusicVolume => Mathf.Clamp(PlayerPrefs.GetFloat(MusicVolumeKey, 0.62f), 0f, 1f);

        // Returns the clamped SFX volume used by short procedural one-shots.
        public static float SfxVolume => Mathf.Clamp(PlayerPrefs.GetFloat(SfxVolumeKey, 0.82f), 0f, 1f);

        // Returns whether short camera shake feedback is allowed.
        public static bool CameraShakeEnabled => PlayerPrefs.GetInt(CameraShakeEnabledKey, 1) == 1;

        // Returns whether high-contrast colors and clearer procedural shapes should be used.
        public static bool ColorAssistEnabled => PlayerPrefs.GetInt(ColorAssistEnabledKey, 0) == 1;

        // Returns whether the first-run tutorial has already been acknowledged.
        public static bool TutorialSeen => PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

        // Stores a boolean setting as a CGR-prefixed PlayerPrefs value.
        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // Stores a normalized volume as a CGR-prefixed PlayerPrefs value.
        public static void SetVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        // Cycles a volume through three mobile-friendly preset levels.
        public static float NextVolumeStep(float value)
        {
            if (value < 0.55f)
            {
                return 0.70f;
            }

            if (value < 0.85f)
            {
                return 1.00f;
            }

            return 0.40f;
        }

        // Marks the first-run tutorial as acknowledged.
        public static void MarkTutorialSeen()
        {
            SetBool(TutorialSeenKey, true);
        }

        // Removes only Color Gate Rush progress keys from PlayerPrefs.
        public static void ResetLocalProgress()
        {
            PlayerPrefs.DeleteKey(StageManager.UnlockedStageKey);
            PlayerPrefs.DeleteKey(StageManager.SelectedStageKey);
            PlayerPrefs.DeleteKey(TutorialSeenKey);

            for (int stage = 1; stage <= StageManager.TotalStageCount; stage++)
            {
                PlayerPrefs.DeleteKey(StageManager.StageStarsKeyPrefix + stage);
            }

            PlayerPrefs.Save();
        }
    }
}
