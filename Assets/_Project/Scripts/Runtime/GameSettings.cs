using UnityEngine;

namespace ColorGateRush
{
    public static class GameSettings
    {
        public const string TutorialSeenKey = "CGR_TutorialSeen";
        public const string SoundEnabledKey = "CGR_SoundEnabled";
        public const string CameraShakeEnabledKey = "CGR_CameraShake";
        public const string ColorAssistEnabledKey = "CGR_HighContrast";

        // Returns whether procedural one-shot sound effects should play.
        public static bool SoundEnabled => PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

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
