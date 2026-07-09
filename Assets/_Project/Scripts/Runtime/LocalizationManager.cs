using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorGateRush
{
    public static class LocalizationManager
    {
        public const string LanguageKey = "CGR_Language";

        private static readonly Dictionary<LocalizationKey, string> Korean = new Dictionary<LocalizationKey, string>
        {
            { LocalizationKey.Start, "시작" },
            { LocalizationKey.EndlessMode, "엔드리스 모드" },
            { LocalizationKey.StageSelect, "스테이지 선택" },
            { LocalizationKey.Rules, "규칙" },
            { LocalizationKey.Settings, "설정" },
            { LocalizationKey.Quit, "종료" },
            { LocalizationKey.Back, "뒤로" },
            { LocalizationKey.Confirm, "확인" },
            { LocalizationKey.Cancel, "취소" },
            { LocalizationKey.Retry, "다시하기" },
            { LocalizationKey.Restart, "재시작" },
            { LocalizationKey.TryAgain, "다시 도전" },
            { LocalizationKey.Resume, "계속하기" },
            { LocalizationKey.MainMenu, "메인 메뉴" },
            { LocalizationKey.NextStage, "다음 스테이지" },
            { LocalizationKey.Pause, "일시정지" },
            { LocalizationKey.ResetProgress, "진행도 초기화" },
            { LocalizationKey.ResetEndlessRecords, "엔드리스 기록 초기화" },
            { LocalizationKey.General, "일반" },
            { LocalizationKey.Data, "데이터" },
            { LocalizationKey.StageProgressReset, "메인 스테이지 진행도 초기화" },
            { LocalizationKey.ResetCannotBeUndone, "이 작업은 되돌릴 수 없습니다." },
            { LocalizationKey.Language, "언어" },
            { LocalizationKey.Korean, "한국어" },
            { LocalizationKey.English, "English" },
            { LocalizationKey.On, "On" },
            { LocalizationKey.Off, "Off" },
            { LocalizationKey.Music, "음악" },
            { LocalizationKey.Sfx, "효과음" },
            { LocalizationKey.MusicVolume, "음악 볼륨" },
            { LocalizationKey.SfxVolume, "효과음 볼륨" },
            { LocalizationKey.CameraShake, "카메라 흔들림" },
            { LocalizationKey.HighContrast, "고대비 표시" },
            { LocalizationKey.ColorAssist, "색상 보조" },
            { LocalizationKey.TitleSubtitle, "색을 바꾸며 달리고, 같은 색 샤드를 모으세요" },
            { LocalizationKey.StageLabel, "Stage {0}" },
            { LocalizationKey.StageLockedLabel, "Stage {0} 이전 클리어" },
            { LocalizationKey.Score, "점수" },
            { LocalizationKey.ScoreUpper, "점수" },
            { LocalizationKey.CurrentScore, "현재 점수 {0}" },
            { LocalizationKey.FinalScore, "최종 점수 {0}" },
            { LocalizationKey.StarsEarned, "획득 별점 {0}" },
            { LocalizationKey.BestStars, "최고 별점 {0}" },
            { LocalizationKey.BestStarsImproved, "최고 별점 갱신!" },
            { LocalizationKey.Clear, "클리어" },
            { LocalizationKey.Failed, "실패" },
            { LocalizationKey.Finish, "피니시" },
            { LocalizationKey.ToThreeStar, "3성까지 {0}" },
            { LocalizationKey.ThreeStarReady, "3성 준비" },
            { LocalizationKey.Chances, "기회" },
            { LocalizationKey.Current, "현재" },
            { LocalizationKey.Combo, "콤보" },
            { LocalizationKey.Distance, "거리" },
            { LocalizationKey.Best, "최고" },
            { LocalizationKey.BestScore, "최고 점수" },
            { LocalizationKey.BestDistance, "최고 거리" },
            { LocalizationKey.Speed, "속도" },
            { LocalizationKey.NewRecord, "신기록!" },
            { LocalizationKey.BestRecord, "최고 기록" },
            { LocalizationKey.RecordEnded, "기록 종료!" },
            { LocalizationKey.WrongShards, "다른 색: {0}/{1}" },
            { LocalizationKey.FailureReason, "실패 원인: {0}" },
            { LocalizationKey.ObstacleHitReason, "장애물에 부딪혔습니다." },
            { LocalizationKey.WrongShardLimitReason, "다른 색 샤드를 3번 먹었습니다." },
            { LocalizationKey.StageUnlock, "다음 스테이지 해금!" },
            { LocalizationKey.AllStagesComplete, "모든 스테이지 완료!" },
            { LocalizationKey.ClearGeneric, "클리어!" },
            { LocalizationKey.ClearToUnlock, "다음 스테이지는 클리어하면 열립니다" },
            { LocalizationKey.NearPerfect, "완벽에 가까운 플레이!" },
            { LocalizationKey.GoodRun, "좋아요! 더 높은 별점을 노려보세요" },
            { LocalizationKey.ImproveStars, "클리어! 더 높은 별점을 노려보세요" },
            { LocalizationKey.FinishForOneStar, "피니시에 도달하면 별 1개를 얻습니다." },
            { LocalizationKey.StageStartHint, "같은 색/모양 샤드만 모으세요\n다른 색 3회 = 실패" },
            { LocalizationKey.EndlessStartHint, "점점 빨라지는 기록 도전\n다른 색 3회 = 실패" },
            { LocalizationKey.TutorialTitle, "첫 플레이 안내" },
            { LocalizationKey.TutorialBody, "화면을 좌우로 스와이프하거나 좌우 영역을 터치해 이동하세요.\n현재 색/모양과 같은 샤드를 모으세요.\n다른 색 샤드를 3번 먹으면 실패합니다.\n장애물에 부딪히면 즉시 실패합니다.\n게이트를 통과하면 현재 색과 모양이 바뀝니다.\n스테이지를 클리어하면 다음 스테이지가 열립니다.\n별점은 더 높은 점수에 도전하는 목표입니다." },
            { LocalizationKey.RulesTitle, "플레이 방법" },
            { LocalizationKey.RulesBody, "화면을 좌우로 스와이프하거나 좌우 영역을 터치해 이동하세요.\n현재 색/모양과 같은 샤드를 모으세요.\n다른 색 샤드를 3번 먹으면 실패합니다.\n장애물에 부딪히면 즉시 실패합니다.\n게이트를 통과하면 현재 색과 모양이 바뀝니다.\n스테이지를 클리어하면 다음 스테이지가 열립니다.\n별점은 더 높은 점수에 도전하는 목표입니다.\n엔드리스 모드는 실패할 때까지 계속 달리는 기록 도전 모드입니다." },
            { LocalizationKey.ResetProgressConfirm, "메인 스테이지 진행도를 초기화할까요?\n이 작업은 되돌릴 수 없습니다." },
            { LocalizationKey.ResetEndlessConfirm, "엔드리스 기록을 초기화할까요?\n이 작업은 되돌릴 수 없습니다." },
            { LocalizationKey.Reset, "초기화" },
            { LocalizationKey.ResetRecords, "기록 초기화" },
            { LocalizationKey.Rows, "Rows" },
            { LocalizationKey.QuitWebGlNotice, "WebGL에서는 브라우저 탭을 닫아주세요." },
            { LocalizationKey.QuitEditorNotice, "Editor에서는 종료되지 않습니다." },
            { LocalizationKey.WrongShardToastLimit, "다른 색 샤드 3회! 실패" },
            { LocalizationKey.WrongShardToastOneChance, "주의! 기회 1회 남음" },
            { LocalizationKey.WrongShardToastRemaining, "다른 색 샤드! 기회 {0}회 남음" },
            { LocalizationKey.MatchingShardHint, "같은 색/모양 샤드를 모으세요" },
            { LocalizationKey.GateChangedFloating, "색상 변경! {0}" },
            { LocalizationKey.ClearFloating, "클리어!" },
            { LocalizationKey.FailedFloating, "실패!" },
            { LocalizationKey.WrongLimitFloating, "다른 색 3회!" },
            { LocalizationKey.ColorCyan, "시안" },
            { LocalizationKey.ColorMagenta, "마젠타" },
            { LocalizationKey.ColorYellow, "노랑" },
            { LocalizationKey.ColorLime, "라임" },
            { LocalizationKey.ColorGeneric, "색상" },
            { LocalizationKey.ShapeOrb, "구슬" },
            { LocalizationKey.ShapeCube, "큐브" },
            { LocalizationKey.ShapeCapsule, "캡슐" },
            { LocalizationKey.ShapeDiamond, "다이아" },
            { LocalizationKey.ShapeGeneric, "형태" }
        };

        private static readonly Dictionary<LocalizationKey, string> English = new Dictionary<LocalizationKey, string>
        {
            { LocalizationKey.Start, "Start" },
            { LocalizationKey.EndlessMode, "Endless Mode" },
            { LocalizationKey.StageSelect, "Stage Select" },
            { LocalizationKey.Rules, "Rules" },
            { LocalizationKey.Settings, "Settings" },
            { LocalizationKey.Quit, "Quit" },
            { LocalizationKey.Back, "Back" },
            { LocalizationKey.Confirm, "Confirm" },
            { LocalizationKey.Cancel, "Cancel" },
            { LocalizationKey.Retry, "Retry" },
            { LocalizationKey.Restart, "Restart" },
            { LocalizationKey.TryAgain, "Try Again" },
            { LocalizationKey.Resume, "Resume" },
            { LocalizationKey.MainMenu, "Main Menu" },
            { LocalizationKey.NextStage, "Next Stage" },
            { LocalizationKey.Pause, "Pause" },
            { LocalizationKey.ResetProgress, "Reset Progress" },
            { LocalizationKey.ResetEndlessRecords, "Reset Endless Records" },
            { LocalizationKey.General, "General" },
            { LocalizationKey.Data, "Data" },
            { LocalizationKey.StageProgressReset, "Reset Stage Progress" },
            { LocalizationKey.ResetCannotBeUndone, "This cannot be undone." },
            { LocalizationKey.Language, "Language" },
            { LocalizationKey.Korean, "Korean" },
            { LocalizationKey.English, "English" },
            { LocalizationKey.On, "On" },
            { LocalizationKey.Off, "Off" },
            { LocalizationKey.Music, "Music" },
            { LocalizationKey.Sfx, "SFX" },
            { LocalizationKey.MusicVolume, "Music Volume" },
            { LocalizationKey.SfxVolume, "SFX Volume" },
            { LocalizationKey.CameraShake, "Camera Shake" },
            { LocalizationKey.HighContrast, "High Contrast" },
            { LocalizationKey.ColorAssist, "Color Assist" },
            { LocalizationKey.TitleSubtitle, "Switch colors, run fast, collect matching shards" },
            { LocalizationKey.StageLabel, "Stage {0}" },
            { LocalizationKey.StageLockedLabel, "Stage {0} clear previous" },
            { LocalizationKey.Score, "Score" },
            { LocalizationKey.ScoreUpper, "SCORE" },
            { LocalizationKey.CurrentScore, "Current Score {0}" },
            { LocalizationKey.FinalScore, "Final Score {0}" },
            { LocalizationKey.StarsEarned, "Stars {0}" },
            { LocalizationKey.BestStars, "Best Stars {0}" },
            { LocalizationKey.BestStarsImproved, "Best Stars Updated!" },
            { LocalizationKey.Clear, "Clear" },
            { LocalizationKey.Failed, "Failed" },
            { LocalizationKey.Finish, "Finish" },
            { LocalizationKey.ToThreeStar, "To 3★: {0}" },
            { LocalizationKey.ThreeStarReady, "3★ Ready" },
            { LocalizationKey.Chances, "Chances" },
            { LocalizationKey.Current, "Current" },
            { LocalizationKey.Combo, "Combo" },
            { LocalizationKey.Distance, "Distance" },
            { LocalizationKey.Best, "Best" },
            { LocalizationKey.BestScore, "Best Score" },
            { LocalizationKey.BestDistance, "Best Distance" },
            { LocalizationKey.Speed, "Speed" },
            { LocalizationKey.NewRecord, "New Record!" },
            { LocalizationKey.BestRecord, "Best Record" },
            { LocalizationKey.RecordEnded, "Record Ended!" },
            { LocalizationKey.WrongShards, "Wrong shards: {0}/{1}" },
            { LocalizationKey.FailureReason, "Reason: {0}" },
            { LocalizationKey.ObstacleHitReason, "You hit an obstacle." },
            { LocalizationKey.WrongShardLimitReason, "You picked 3 wrong color shards." },
            { LocalizationKey.StageUnlock, "Next stage unlocked!" },
            { LocalizationKey.AllStagesComplete, "All stages complete!" },
            { LocalizationKey.ClearGeneric, "Clear!" },
            { LocalizationKey.ClearToUnlock, "Clear a stage to unlock the next one" },
            { LocalizationKey.NearPerfect, "Near-perfect run!" },
            { LocalizationKey.GoodRun, "Nice! Aim for a higher star score" },
            { LocalizationKey.ImproveStars, "Clear! Aim for a higher star score" },
            { LocalizationKey.FinishForOneStar, "Reach the finish to earn 1 star." },
            { LocalizationKey.StageStartHint, "Collect matching color/shape shards\n3 wrong shards = fail" },
            { LocalizationKey.EndlessStartHint, "Endless speed challenge\n3 wrong shards = fail" },
            { LocalizationKey.TutorialTitle, "First Run Guide" },
            { LocalizationKey.TutorialBody, "Swipe left or right, or tap the left/right side of the screen to move.\nCollect shards that match your current color and shape.\nPicking 3 wrong shards fails the run.\nHitting an obstacle fails immediately.\nGates change your current color and shape.\nClear a stage to unlock the next one.\nStars are score goals for better runs." },
            { LocalizationKey.RulesTitle, "How to Play" },
            { LocalizationKey.RulesBody, "Swipe left or right, or tap the left/right side of the screen to move.\nCollect shards that match your current color and shape.\nPicking 3 wrong shards fails the run.\nHitting an obstacle fails immediately.\nGates change your current color and shape.\nClear a stage to unlock the next one.\nStars are score goals for better runs.\nEndless Mode continues until you fail." },
            { LocalizationKey.ResetProgressConfirm, "Reset stage progress?\nThis cannot be undone." },
            { LocalizationKey.ResetEndlessConfirm, "Reset Endless records?\nThis cannot be undone." },
            { LocalizationKey.Reset, "Reset" },
            { LocalizationKey.ResetRecords, "Reset Records" },
            { LocalizationKey.Rows, "Rows" },
            { LocalizationKey.QuitWebGlNotice, "On WebGL, close the browser tab to quit." },
            { LocalizationKey.QuitEditorNotice, "Quit is ignored in the Unity Editor." },
            { LocalizationKey.WrongShardToastLimit, "3 wrong shards! Failed" },
            { LocalizationKey.WrongShardToastOneChance, "Careful! 1 chance left" },
            { LocalizationKey.WrongShardToastRemaining, "Wrong shard! {0} chances left" },
            { LocalizationKey.MatchingShardHint, "Collect matching color/shape shards" },
            { LocalizationKey.GateChangedFloating, "Changed! {0}" },
            { LocalizationKey.ClearFloating, "Clear!" },
            { LocalizationKey.FailedFloating, "Failed!" },
            { LocalizationKey.WrongLimitFloating, "3 wrong shards!" },
            { LocalizationKey.ColorCyan, "Cyan" },
            { LocalizationKey.ColorMagenta, "Magenta" },
            { LocalizationKey.ColorYellow, "Yellow" },
            { LocalizationKey.ColorLime, "Lime" },
            { LocalizationKey.ColorGeneric, "Color" },
            { LocalizationKey.ShapeOrb, "Orb" },
            { LocalizationKey.ShapeCube, "Cube" },
            { LocalizationKey.ShapeCapsule, "Capsule" },
            { LocalizationKey.ShapeDiamond, "Diamond" },
            { LocalizationKey.ShapeGeneric, "Shape" }
        };

        private static bool _loaded;
        private static Language _currentLanguage;

        public static event Action OnLanguageChanged;

        public static Language CurrentLanguage
        {
            get
            {
                EnsureLoaded();
                return _currentLanguage;
            }
        }

        // Stores the selected language and notifies active UI labels immediately.
        public static void SetLanguage(Language language)
        {
            EnsureLoaded();
            if (_currentLanguage == language)
            {
                return;
            }

            _currentLanguage = language;
            PlayerPrefs.SetInt(LanguageKey, (int)language);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        // Returns translated text for a key and applies string.Format arguments when present.
        public static string T(LocalizationKey key, params object[] args)
        {
            EnsureLoaded();
            string value = DictionaryFor(_currentLanguage).TryGetValue(key, out string text)
                ? text
                : Korean.TryGetValue(key, out string fallback)
                    ? fallback
                    : key.ToString();
            return args != null && args.Length > 0 ? string.Format(value, args) : value;
        }

        // Returns a localized color name for the active gameplay palette id.
        public static string ColorName(ColorId colorId)
        {
            switch (colorId)
            {
                case ColorId.Cyan:
                    return T(LocalizationKey.ColorCyan);
                case ColorId.Magenta:
                    return T(LocalizationKey.ColorMagenta);
                case ColorId.Yellow:
                    return T(LocalizationKey.ColorYellow);
                case ColorId.Lime:
                    return T(LocalizationKey.ColorLime);
                default:
                    return T(LocalizationKey.ColorGeneric);
            }
        }

        // Returns a localized shape name for the procedural current-target shape.
        public static string ShapeName(ColorShapeType shapeType)
        {
            switch (shapeType)
            {
                case ColorShapeType.Cube:
                    return T(LocalizationKey.ShapeCube);
                case ColorShapeType.Capsule:
                    return T(LocalizationKey.ShapeCapsule);
                case ColorShapeType.Diamond:
                    return T(LocalizationKey.ShapeDiamond);
                case ColorShapeType.Sphere:
                    return T(LocalizationKey.ShapeOrb);
                default:
                    return T(LocalizationKey.ShapeGeneric);
            }
        }

        // Returns true when both supported languages have a translation for the key.
        public static bool HasAllTranslations(LocalizationKey key)
        {
            return Korean.ContainsKey(key) && English.ContainsKey(key);
        }

        // Returns the number of indexed format placeholders in one localized string.
        public static int PlaceholderCount(Language language, LocalizationKey key)
        {
            Dictionary<LocalizationKey, string> dictionary = DictionaryFor(language);
            if (!dictionary.TryGetValue(key, out string text))
            {
                return -1;
            }

            int highest = -1;
            for (int i = 0; i < text.Length - 2; i++)
            {
                if (text[i] == '{' && char.IsDigit(text[i + 1]) && text[i + 2] == '}')
                {
                    highest = Mathf.Max(highest, text[i + 1] - '0');
                }
            }

            return highest + 1;
        }

        // Loads the saved language once; Korean is default to preserve the existing UI for current users.
        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            int saved = PlayerPrefs.GetInt(LanguageKey, (int)Language.Korean);
            _currentLanguage = Enum.IsDefined(typeof(Language), saved) ? (Language)saved : Language.Korean;
            _loaded = true;
        }

        // Selects the in-memory translation table for one supported language.
        private static Dictionary<LocalizationKey, string> DictionaryFor(Language language)
        {
            return language == Language.English ? English : Korean;
        }
    }
}
