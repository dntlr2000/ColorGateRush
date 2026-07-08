using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ColorGateRush
{
    public sealed class RuntimeUi : MonoBehaviour
    {
        private const string MainMenuBackgroundResourcePath = "ColorGateRush/Images/MainMenuBackground";

        private Canvas _canvas;
        private GameObject _menuPanel;
        private GameObject _stageSelectPanel;
        private GameObject _rulesPanel;
        private GameObject _settingsPanel;
        private GameObject _resetConfirmPanel;
        private GameObject _endlessResetConfirmPanel;
        private GameObject _playtestStatsPanel;
        private GameObject _statsResetConfirmPanel;
        private GameObject _hudPanel;
        private GameObject _tutorialPanel;
        private GameObject _pausePanel;
        private GameObject _resultPanel;
        private Transform _stageButtonRoot;
        private Text _scoreText;
        private Text _messageText;
        private Text _menuNoticeText;
        private Text _debugText;
        private Text _hintText;
        private Text _musicButtonText;
        private Text _musicVolumeButtonText;
        private Text _sfxButtonText;
        private Text _sfxVolumeButtonText;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;
        private Text _cameraShakeButtonText;
        private Text _colorAssistButtonText;
        private Text _statsBodyText;
        private Text _pauseStageText;
        private Text _pauseScoreText;
        private Text _resultTitleText;
        private Text _resultScoreText;
        private Text _resultInfoText;
        private Button _nextStageButton;
        private Text _restartButtonText;
        private float _messageHideAt;
        private Action _onStart;
        private Action _onStageSelect;
        private Action _onRules;
        private Action _onSettings;
        private Action _onRestart;
        private Action _onMainMenu;
        private Action _onNextStage;
        private Action _onPause;
        private Action _onResume;
        private Action _onToggleSound;
        private Action _onToggleMusic;
        private Action _onToggleSfx;
        private Action<float> _onSetMusicVolume;
        private Action<float> _onSetSfxVolume;
        private Action _onToggleCameraShake;
        private Action _onToggleColorAssist;
        private Action _onResetProgress;
        private Action _onPlaytestStats;
        private Action _onResetPlaytestStats;
        private Action _onStartEndless;
        private Action _onQuit;
        private Action _onResetEndlessRecords;
        private Action _onTutorialOk;
        private Action<int> _onStageSelected;
        private int _hudStageIndex = 1;
        private int _hudTwoStarScore;
        private int _hudThreeStarScore;

        // Builds the runtime UI tree as soon as the systems object wakes.
        private void Awake()
        {
            EnsureCanvas();
        }

        // Hides transient center toast text after its unscaled display window expires.
        private void Update()
        {
            if (_messageText != null && _messageHideAt > 0f && Time.unscaledTime >= _messageHideAt)
            {
                ClearMessage();
            }
        }

        // Connects UI button callbacks to the game manager flow.
        public void Configure(
            Action onStart,
            Action onStageSelect,
            Action onRules,
            Action onSettings,
            Action onRestart,
            Action onMainMenu,
            Action onNextStage,
            Action<int> onStageSelected,
            Action onPause,
            Action onResume,
            Action onToggleSound,
            Action onToggleMusic,
            Action onToggleSfx,
            Action<float> onSetMusicVolume,
            Action<float> onSetSfxVolume,
            Action onToggleCameraShake,
            Action onToggleColorAssist,
            Action onResetProgress,
            Action onPlaytestStats,
            Action onResetPlaytestStats,
            Action onStartEndless,
            Action onQuit,
            Action onResetEndlessRecords,
            Action onTutorialOk)
        {
            _onStart = onStart;
            _onStageSelect = onStageSelect;
            _onRules = onRules;
            _onSettings = onSettings;
            _onRestart = onRestart;
            _onMainMenu = onMainMenu;
            _onNextStage = onNextStage;
            _onStageSelected = onStageSelected;
            _onPause = onPause;
            _onResume = onResume;
            _onToggleSound = onToggleSound;
            _onToggleMusic = onToggleMusic;
            _onToggleSfx = onToggleSfx;
            _onSetMusicVolume = onSetMusicVolume;
            _onSetSfxVolume = onSetSfxVolume;
            _onToggleCameraShake = onToggleCameraShake;
            _onToggleColorAssist = onToggleColorAssist;
            _onResetProgress = onResetProgress;
            _onPlaytestStats = onPlaytestStats;
            _onResetPlaytestStats = onResetPlaytestStats;
            _onStartEndless = onStartEndless;
            _onQuit = onQuit;
            _onResetEndlessRecords = onResetEndlessRecords;
            _onTutorialOk = onTutorialOk;
        }

        // Shows the main menu and hides gameplay/result panels.
        public void ShowMainMenu()
        {
            EnsureCanvas();
            SetPanel(_menuPanel);
            if (_menuNoticeText != null)
            {
                _menuNoticeText.text = string.Empty;
            }
        }

        // Shows stage selection with lock state and saved best stars.
        public void ShowStageSelect(StageConfig[] stages, int unlockedStage, int selectedStage, Func<int, int> getBestStars)
        {
            EnsureCanvas();
            RebuildStageButtons(stages, unlockedStage, selectedStage, getBestStars);
            SetPanel(_stageSelectPanel);
        }

        // Shows the Korean rules panel and hides other panels.
        public void ShowRules()
        {
            EnsureCanvas();
            SetPanel(_rulesPanel);
        }

        // Shows settings and refreshes setting labels from PlayerPrefs-backed values.
        public void ShowSettings()
        {
            EnsureCanvas();
            SetPanel(_settingsPanel);
            _resetConfirmPanel.SetActive(false);
            _endlessResetConfirmPanel.SetActive(false);
            RefreshSettingsLabels();
        }

        // Shows local playtest counters in a scrollable panel without changing gameplay state.
        public void ShowPlaytestStats(int stageCount)
        {
            EnsureCanvas();
            RebuildPlaytestStats(stageCount);
            SetPanel(_playtestStatsPanel);
            _statsResetConfirmPanel.SetActive(false);
        }

        // Shows a short main-menu notice for platform-specific actions such as Editor/WebGL quit handling.
        public void ShowMenuNotice(string message)
        {
            EnsureCanvas();
            if (_menuNoticeText != null)
            {
                _menuNoticeText.text = message;
            }
        }

        // Shows the gameplay HUD and refreshes its values.
        public void ShowPlayingHud(StageConfig stage, int score, int combo, ColorId color, int seed, int wrongShardCount, int wrongShardLimit)
        {
            EnsureCanvas();
            SetPanel(_hudPanel);
            _hudStageIndex = stage.StageIndex;
            _hudTwoStarScore = stage.TwoStarScore;
            _hudThreeStarScore = stage.ThreeStarScore;
            SetHud(score, combo, color, seed, wrongShardCount, wrongShardLimit);
        }

        // Shows the Endless HUD with score, distance, best record, and current color target.
        public void ShowEndlessHud(
            int score,
            int combo,
            ColorId color,
            float distance,
            int bestScore,
            float bestDistance,
            int seed,
            int wrongShardCount,
            int wrongShardLimit,
            float speedMultiplier)
        {
            EnsureCanvas();
            SetPanel(_hudPanel);
            SetEndlessHud(score, combo, color, distance, bestScore, bestDistance, seed, wrongShardCount, wrongShardLimit, speedMultiplier);
        }

        // Shows a short stage-start briefing that disappears automatically and never changes game state.
        public void ShowStageStartHint(StageConfig stage)
        {
            EnsureCanvas();
            ShowMessage(
                "Stage " + stage.StageIndex
                + "\n같은 색/모양 샤드만 모으세요"
                + "\n다른 색 3회 = 실패"
                + "\n★2: " + stage.TwoStarScore + "  ★3: " + stage.ThreeStarScore
                + "\n클리어하면 다음 스테이지가 열립니다",
                2.4f);
        }

        // Shows a short Endless briefing that disappears automatically and never changes game state.
        public void ShowEndlessStartHint()
        {
            EnsureCanvas();
            ShowMessage(
                "Endless Mode\n점점 빨라집니다\n다른 색 샤드 3회면 기록 종료",
                2.4f);
        }

        // Shows the first-run tutorial overlay before Stage 1 begins moving.
        public void ShowTutorial()
        {
            EnsureCanvas();
            SetPanel(_tutorialPanel);
        }

        // Shows the pause menu for the current run without changing stage progress.
        public void ShowPauseMenu(StageConfig stage, int score)
        {
            EnsureCanvas();
            SetPanel(_pausePanel);
            ClearMessage();
            _pauseStageText.text = "Stage " + stage.StageIndex;
            _pauseScoreText.text = "현재 점수 " + score;
        }

        // Shows the pause menu for Endless Mode without mentioning stage stars or unlocks.
        public void ShowEndlessPauseMenu(int score, float distance)
        {
            EnsureCanvas();
            SetPanel(_pausePanel);
            ClearMessage();
            _pauseStageText.text = "Endless Mode";
            _pauseScoreText.text = "Score " + score + "   Distance " + Mathf.FloorToInt(distance) + "m";
        }

        // Shows the failure or clear result panel with final score and navigation buttons.
        public void ShowResult(bool completed, StageConfig stage, StageResult result, bool nextStageAvailable)
        {
            EnsureCanvas();
            SetPanel(_resultPanel);
            _resultTitleText.text = completed ? "클리어!" : "실패";
            _resultScoreText.text = "Stage " + stage.StageIndex + "\n최종 점수 " + result.Score + "\n획득 별점 " + StarsText(result.Stars);
            _restartButtonText.text = completed ? "재시작" : "다시 도전";
            if (completed)
            {
                string bestText = result.BestStarsImproved ? "최고 별점 갱신!" : "최고 별점 " + StarsText(result.BestStars);
                int threeStarShortfall = Mathf.Max(0, stage.ThreeStarScore - result.Score);
                string starPraise = result.Stars == 3 ? "완벽에 가까운 플레이!" : (result.Stars == 2 ? "좋아요! 더 높은 별점을 노려보세요" : "클리어! 더 높은 별점을 노려보세요");
                string unlockText = result.NextStageUnlocked
                    ? "다음 스테이지 해금!"
                    : (!result.HasNextStage
                        ? "모든 스테이지 완료!"
                        : (nextStageAvailable ? "클리어!" : "다음 스테이지는 클리어하면 열립니다"));
                string shortfallText = result.Stars < 3 ? "\n3성까지 부족한 점수: " + threeStarShortfall : string.Empty;
                _resultInfoText.text = starPraise + "\n" + bestText + "\n" + unlockText + shortfallText;
            }
            else
            {
                _resultInfoText.text = StageFailReasonText(result.FailReason)
                    + "\n피니시에 도달하면 별 1개를 얻습니다.";
            }

            _nextStageButton.gameObject.SetActive(completed && result.HasNextStage);
            _nextStageButton.interactable = completed && nextStageAvailable;
        }

        // Shows an Endless-only failure result with best score and distance records.
        public void ShowEndlessResult(EndlessRunResult result)
        {
            EnsureCanvas();
            SetPanel(_resultPanel);
            _resultTitleText.text = "기록 종료!";
            _resultScoreText.text = "Endless Mode\nScore " + result.Score
                + "\nDistance " + Mathf.FloorToInt(result.Distance) + "m"
                + "\n기회: " + FormatMistakeIcons(result.WrongShardCount, result.WrongShardLimit);
            string recordText = result.NewBestScore || result.NewBestDistance ? "New Record!" : "Best Record";
            _resultInfoText.text = recordText
                + "\n실패 원인: " + EndlessFailReasonText(result.FailReason)
                + "\nBest Score " + result.BestScore
                + "   Best Distance " + Mathf.FloorToInt(result.BestDistance) + "m"
                + "\n생성 Row " + result.RowsGenerated;
            _restartButtonText.text = "다시 도전";
            _nextStageButton.gameObject.SetActive(false);
            _nextStageButton.interactable = false;
        }

        // Returns player-facing text for the Endless failure reason shown on the result panel.
        private static string EndlessFailReasonText(EndlessFailReason failReason)
        {
            return failReason == EndlessFailReason.WrongShardLimit
                ? "다른 색 샤드를 3번 먹었습니다"
                : "장애물 충돌";
        }

        // Returns player-facing text for finite Stage Mode failure causes.
        private static string StageFailReasonText(StageFailReason failReason)
        {
            return failReason == StageFailReason.WrongShardLimit
                ? "다른 색 샤드를 3번 먹었습니다."
                : "장애물에 부딪혔습니다.";
        }

        // Updates score, combo, color, seed, mistake chances, and compact in-game rule hints.
        public void SetHud(int score, int combo, ColorId color, int seed, int wrongShardCount, int wrongShardLimit)
        {
            EnsureCanvas();
            ColorVisualProfile profile = GameConstants.GetVisualProfile(color);
            int threeStarRemaining = Mathf.Max(0, _hudThreeStarScore - score);
            _scoreText.text = $"Stage {_hudStageIndex}\nScore: {score}\n★1: 피니시\n★2: {_hudTwoStarScore}   ★3: {_hudThreeStarScore}\n3성까지: {threeStarRemaining}\n기회: {FormatMistakeIcons(wrongShardCount, wrongShardLimit)}\n현재: {profile.HudLabel}\nCombo x{Mathf.Max(1, combo)}";
            _debugText.text = "Seed " + seed;
            _hintText.text = string.Empty;
        }

        // Updates the Endless HUD without star targets or stage unlock messaging.
        public void SetEndlessHud(
            int score,
            int combo,
            ColorId color,
            float distance,
            int bestScore,
            float bestDistance,
            int seed,
            int wrongShardCount,
            int wrongShardLimit,
            float speedMultiplier)
        {
            EnsureCanvas();
            ColorVisualProfile profile = GameConstants.GetVisualProfile(color);
            int safeWrongLimit = Mathf.Max(1, wrongShardLimit);
            int safeWrongCount = Mathf.Clamp(wrongShardCount, 0, safeWrongLimit);
            _scoreText.text = "Endless Mode"
                + "\nScore: " + score
                + "\nDistance: " + Mathf.FloorToInt(distance) + "m"
                + "\nBest: " + bestScore + " / " + Mathf.FloorToInt(bestDistance) + "m"
                + "\n기회: " + FormatMistakeIcons(safeWrongCount, safeWrongLimit)
                + "\nSpeed x" + Mathf.Max(1f, speedMultiplier).ToString("0.0")
                + "\n현재: " + profile.HudLabel
                + "\nCombo x" + Mathf.Max(1, combo);
            _debugText.text = "Seed " + seed;
            _hintText.text = string.Empty;
        }

        // Formats remaining wrong-shard chances as filled and empty HUD glyphs.
        private static string FormatMistakeIcons(int wrongShardCount, int wrongShardLimit)
        {
            int safeLimit = Mathf.Max(1, wrongShardLimit);
            int used = Mathf.Clamp(wrongShardCount, 0, safeLimit);
            int remaining = safeLimit - used;
            StringBuilder builder = new StringBuilder(safeLimit * 2);
            for (int i = 0; i < safeLimit; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(i < remaining ? '◆' : '◇');
            }

            return builder.ToString();
        }

        // Updates the central gameplay message text.
        public void ShowMessage(string message)
        {
            ShowMessage(message, 1.6f);
        }

        // Updates the central gameplay message text for a bounded unscaled duration.
        public void ShowMessage(string message, float seconds)
        {
            EnsureCanvas();
            _messageText.text = message;
            _messageHideAt = string.IsNullOrEmpty(message) ? 0f : Time.unscaledTime + Mathf.Max(0.1f, seconds);
        }

        // Clears the transient center gameplay message immediately.
        public void ClearMessage()
        {
            if (_messageText == null)
            {
                return;
            }

            _messageText.text = string.Empty;
            _messageHideAt = 0f;
        }

        // Reports whether the current pointer is over a generated UI element.
        public bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // Creates the overlay canvas, event system, panels, labels, and buttons.
        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            EnsureEventSystem();
            GameObject canvasGo = new GameObject("RuntimeCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _menuPanel = CreateMenuPanel(canvasGo.transform);
            _stageSelectPanel = CreateStageSelectPanel(canvasGo.transform);
            _rulesPanel = CreateRulesPanel(canvasGo.transform);
            _settingsPanel = CreateSettingsPanel(canvasGo.transform);
            _playtestStatsPanel = CreatePlaytestStatsPanel(canvasGo.transform);
            _tutorialPanel = CreateTutorialPanel(canvasGo.transform);
            _hudPanel = CreateHudPanel(canvasGo.transform);
            _pausePanel = CreatePausePanel(canvasGo.transform);
            _resultPanel = CreateResultPanel(canvasGo.transform);
            SetPanel(_menuPanel);
        }

        // Ensures uGUI has an input module compatible with the active project input backend.
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventGo = new GameObject("EventSystem");
            eventGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventGo.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
#else
            eventGo.AddComponent<StandaloneInputModule>();
#endif
        }

        // Activates one root panel while hiding the rest.
        private void SetPanel(GameObject activePanel)
        {
            _menuPanel.SetActive(_menuPanel == activePanel);
            _stageSelectPanel.SetActive(_stageSelectPanel == activePanel);
            _rulesPanel.SetActive(_rulesPanel == activePanel);
            _settingsPanel.SetActive(_settingsPanel == activePanel);
            _playtestStatsPanel.SetActive(_playtestStatsPanel == activePanel);
            _tutorialPanel.SetActive(_tutorialPanel == activePanel);
            _hudPanel.SetActive(_hudPanel == activePanel);
            _pausePanel.SetActive(_pausePanel == activePanel);
            _resultPanel.SetActive(_resultPanel == activePanel);
            if (activePanel != _hudPanel)
            {
                ClearMessage();
            }
        }

        // Builds the title menu with start and rules buttons.
        private GameObject CreateMenuPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "MainMenuPanel", Color.black);
            CreateMenuBackground(panel.transform);
            Text titleText = CreateText(panel.transform, "TitleText", new Vector2(0f, 390f), TextAnchor.MiddleCenter, 86, new Vector2(900f, 140f), "Color Gate Rush");
            AddTextShadow(titleText);
            Text subtitleText = CreateText(panel.transform, "SubtitleText", new Vector2(0f, 285f), TextAnchor.MiddleCenter, 34, new Vector2(820f, 100f), "색을 바꾸며 달리고, 같은 색 샤드를 모으세요");
            AddTextShadow(subtitleText);
            CreateButton(panel.transform, "StartButton", new Vector2(0f, 145f), "시작", () => _onStart?.Invoke());
            CreateButton(panel.transform, "EndlessModeButton", new Vector2(0f, 25f), "Endless Mode", () => _onStartEndless?.Invoke());
            CreateButton(panel.transform, "RulesButton", new Vector2(0f, -95f), "플레이 방법", () => _onRules?.Invoke());
            CreateButton(panel.transform, "SettingsButton", new Vector2(0f, -215f), "설정", () => _onSettings?.Invoke());
            CreateButton(panel.transform, "PlaytestStatsButton", new Vector2(0f, -335f), "플레이테스트 통계", () => _onPlaytestStats?.Invoke());
            CreateButton(panel.transform, "QuitButton", new Vector2(0f, -475f), "게임 종료", () => _onQuit?.Invoke());
            _menuNoticeText = CreateText(panel.transform, "MenuNoticeText", new Vector2(0f, -585f), TextAnchor.MiddleCenter, 28, new Vector2(860f, 80f), string.Empty);
            AddTextShadow(_menuNoticeText);
            return panel;
        }

        // Adds the user-provided main-menu image and a readability overlay behind menu controls.
        private static void CreateMenuBackground(Transform parent)
        {
            GameObject backgroundGo = new GameObject("MainMenuBackgroundImage");
            backgroundGo.transform.SetParent(parent, false);
            RectTransform backgroundRect = backgroundGo.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = backgroundGo.AddComponent<Image>();
            backgroundImage.raycastTarget = false;
            Sprite backgroundSprite = LoadMainMenuBackgroundSprite();
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.color = backgroundSprite != null ? Color.white : new Color(0.02f, 0.03f, 0.10f, 1f);
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;

            GameObject overlayGo = new GameObject("MainMenuBackgroundReadabilityOverlay");
            overlayGo.transform.SetParent(parent, false);
            RectTransform overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            Image overlayImage = overlayGo.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.34f);
            overlayImage.raycastTarget = false;
        }

        // Loads the Resources-backed menu background with a Texture2D fallback for import setting changes.
        private static Sprite LoadMainMenuBackgroundSprite()
        {
            Sprite sprite = Resources.Load<Sprite>(MainMenuBackgroundResourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(MainMenuBackgroundResourcePath);
            if (texture == null)
            {
                Debug.LogWarning("Main menu background image missing from Resources: " + MainMenuBackgroundResourcePath);
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        // Builds the stage select panel root and a container for dynamic stage buttons.
        private GameObject CreateStageSelectPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "StageSelectPanel", ScreenPanelColor(0.94f));
            CreateText(panel.transform, "StageSelectTitleText", new Vector2(0f, 430f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), "스테이지 선택");

            GameObject scrollView = new GameObject("StageScrollView");
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(930f, 720f);
            scrollRect.anchoredPosition = new Vector2(0f, -5f);
            Image scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = HudPanelColor(0.36f);

            GameObject viewport = new GameObject("StageScrollViewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(16f, 16f);
            viewportRect.offsetMax = new Vector2(-16f, -16f);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject root = new GameObject("StageButtonRoot");
            root.transform.SetParent(viewport.transform, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 720f);
            rect.anchoredPosition = Vector2.zero;
            GridLayoutGroup grid = root.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(410f, 86f);
            grid.spacing = new Vector2(24f, 20f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(28, 28, 22, 22);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = rect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 28f;
            _stageButtonRoot = root.transform;
            CreateButton(panel.transform, "StageBackButton", new Vector2(0f, -430f), "메인 메뉴", () => _onMainMenu?.Invoke());
            return panel;
        }

        // Rebuilds stage buttons so unlock and best-star state are always current.
        private void RebuildStageButtons(StageConfig[] stages, int unlockedStage, int selectedStage, Func<int, int> getBestStars)
        {
            for (int i = _stageButtonRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_stageButtonRoot.GetChild(i).gameObject);
            }

            RectTransform contentRect = _stageButtonRoot.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                int rowCount = Mathf.CeilToInt(stages.Length / 2f);
                contentRect.sizeDelta = new Vector2(0f, Mathf.Max(720f, rowCount * 106f + 44f));
                contentRect.anchoredPosition = Vector2.zero;
            }

            for (int i = 0; i < stages.Length; i++)
            {
                StageConfig stage = stages[i];
                bool unlocked = stage.StageIndex <= unlockedStage;
                int stageIndex = stage.StageIndex;
                string label = unlocked
                    ? "Stage " + stage.StageIndex + " " + StarsText(getBestStars(stage.StageIndex))
                    : "Stage " + stage.StageIndex + " 이전 클리어";
                Button button = CreateButton(_stageButtonRoot, "StageButton_" + stage.StageIndex, Vector2.zero, label, () => _onStageSelected?.Invoke(stageIndex));
                button.interactable = unlocked;
                if (!unlocked)
                {
                    button.GetComponent<Image>().color = LockedButtonColor();
                }
            }
        }

        // Builds the rules panel using the same constants as gameplay scoring.
        private GameObject CreateRulesPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "RulesPanel", ScreenPanelColor(0.94f));
            CreateText(panel.transform, "RulesTitleText", new Vector2(0f, 430f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), "플레이 방법");
            string rules = "색상과 모양이 같은 샤드를 먹으면 점수를 얻습니다. +" + GameConstants.SameColorShardScore + " x 콤보\n"
                + "연속 수집 콤보가 오르면 점수와 효과음이 조금 커집니다.\n"
                + "다른 색 샤드를 3번 먹으면 실패합니다.\n"
                + "남은 기회는 HUD의 아이콘으로 확인합니다.\n"
                + "Endless Mode도 같은 규칙으로 다른 색 샤드 3회째에 기록 종료됩니다.\n"
                + "게이트를 통과하면 플레이어 색과 목표 모양이 바뀝니다. +" + GameConstants.GateScore + "\n"
                + "현재 목표 색상/모양은 HUD와 플레이어 주변 accent에서 확인합니다.\n"
                + "장애물에 부딪히면 즉시 실패합니다. -" + GameConstants.ObstaclePenalty + "\n"
                + "피니시에 도달하면 클리어하고 별 1개를 얻습니다.\n"
                + "★2/★3 목표 점수는 플레이 중 HUD에서 확인합니다.\n"
                + "★2는 ★3 목표 점수의 2/3 이상입니다.\n"
                + "클리어하면 다음 스테이지가 열립니다.\n"
                + "★3은 거의 완벽한 수집 보상입니다. 놓친 샤드가 있으면 어려울 수 있습니다.\n"
                + "Endless Mode는 점점 빨라지는 기록 도전이며 별점/해금과 독립입니다.\n"
                + "Pause: 버튼 또는 ESC/P, Retry: R, 메뉴: M\n\n"
                + "조작: A/D 또는 ←/→\n"
                + "모바일: 좌우 스와이프 또는 화면 좌우 탭";
            CreateText(panel.transform, "RulesBodyText", new Vector2(0f, 65f), TextAnchor.MiddleLeft, 34, new Vector2(880f, 660f), rules);
            CreateButton(panel.transform, "RulesBackButton", new Vector2(0f, -420f), "메인 메뉴", () => _onMainMenu?.Invoke());
            return panel;
        }

        // Builds the settings screen with CGR-prefixed PlayerPrefs controls.
        private GameObject CreateSettingsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "SettingsPanel", ScreenPanelColor(0.94f));
            CreateText(panel.transform, "SettingsTitleText", new Vector2(0f, 430f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), "설정");
            _musicButtonText = CreateButton(panel.transform, "MusicToggleButton", new Vector2(0f, 305f), "Music", () => _onToggleMusic?.Invoke()).GetComponentInChildren<Text>();
            _musicVolumeButtonText = CreateText(panel.transform, "MusicVolumeLabel", new Vector2(0f, 220f), TextAnchor.MiddleCenter, 34, new Vector2(760f, 52f), "Music Vol");
            AddTextShadow(_musicVolumeButtonText);
            _musicVolumeSlider = CreateSlider(panel.transform, "MusicVolumeSlider", new Vector2(0f, 165f), GameSettings.MusicVolume, HandleMusicVolumeChanged);
            _sfxButtonText = CreateButton(panel.transform, "SfxToggleButton", new Vector2(0f, 70f), "SFX", () => _onToggleSfx?.Invoke()).GetComponentInChildren<Text>();
            _sfxVolumeButtonText = CreateText(panel.transform, "SfxVolumeLabel", new Vector2(0f, -15f), TextAnchor.MiddleCenter, 34, new Vector2(760f, 52f), "SFX Vol");
            AddTextShadow(_sfxVolumeButtonText);
            _sfxVolumeSlider = CreateSlider(panel.transform, "SfxVolumeSlider", new Vector2(0f, -70f), GameSettings.SfxVolume, HandleSfxVolumeChanged);
            _cameraShakeButtonText = CreateButton(panel.transform, "CameraShakeToggleButton", new Vector2(0f, -180f), "Camera", () => _onToggleCameraShake?.Invoke()).GetComponentInChildren<Text>();
            _colorAssistButtonText = CreateButton(panel.transform, "ColorAssistToggleButton", new Vector2(0f, -290f), "Assist", () => _onToggleColorAssist?.Invoke()).GetComponentInChildren<Text>();
            CreateButton(panel.transform, "ResetProgressButton", new Vector2(-255f, -395f), "진행 초기화", ShowResetConfirm);
            CreateButton(panel.transform, "ResetEndlessRecordsButton", new Vector2(255f, -395f), "Endless 기록", ShowEndlessResetConfirm);
            CreateButton(panel.transform, "SettingsBackButton", new Vector2(0f, -515f), "메인 메뉴", () => _onMainMenu?.Invoke());
            _resetConfirmPanel = CreateResetConfirmPanel(panel.transform);
            _resetConfirmPanel.SetActive(false);
            _endlessResetConfirmPanel = CreateEndlessResetConfirmPanel(panel.transform);
            _endlessResetConfirmPanel.SetActive(false);
            return panel;
        }

        // Builds the reset confirmation overlay so progress cannot be wiped by one tap.
        private GameObject CreateResetConfirmPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "ResetConfirmPanel", ScreenPanelColor(0.86f));
            CreateText(panel.transform, "ResetConfirmText", new Vector2(0f, 70f), TextAnchor.MiddleCenter, 38, new Vector2(860f, 160f), "CGR 진행 데이터만 초기화할까요?");
            CreateButton(panel.transform, "ResetConfirmYesButton", new Vector2(-250f, -90f), "초기화", () => _onResetProgress?.Invoke());
            CreateButton(panel.transform, "ResetConfirmNoButton", new Vector2(250f, -90f), "취소", () => _resetConfirmPanel.SetActive(false));
            return panel;
        }

        // Shows the reset confirmation overlay on top of Settings.
        private void ShowResetConfirm()
        {
            _endlessResetConfirmPanel.SetActive(false);
            _resetConfirmPanel.SetActive(true);
        }

        // Builds the confirmation overlay for Endless record reset only.
        private GameObject CreateEndlessResetConfirmPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "EndlessResetConfirmPanel", ScreenPanelColor(0.86f));
            CreateText(panel.transform, "EndlessResetConfirmText", new Vector2(0f, 70f), TextAnchor.MiddleCenter, 38, new Vector2(860f, 160f), "Endless 최고 기록만 초기화할까요?");
            CreateButton(panel.transform, "EndlessResetConfirmYesButton", new Vector2(-250f, -90f), "기록 초기화", () => _onResetEndlessRecords?.Invoke());
            CreateButton(panel.transform, "EndlessResetConfirmNoButton", new Vector2(250f, -90f), "취소", () => _endlessResetConfirmPanel.SetActive(false));
            return panel;
        }

        // Shows the Endless reset confirmation overlay on top of Settings.
        private void ShowEndlessResetConfirm()
        {
            _resetConfirmPanel.SetActive(false);
            _endlessResetConfirmPanel.SetActive(true);
        }

        // Builds a scrollable local-only playtest stats panel for stage-by-stage review.
        private GameObject CreatePlaytestStatsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "PlaytestStatsPanel", ScreenPanelColor(0.94f));
            CreateText(panel.transform, "PlaytestStatsTitleText", new Vector2(0f, 430f), TextAnchor.MiddleCenter, 60, new Vector2(900f, 100f), "플레이테스트 통계");

            GameObject scrollView = new GameObject("PlaytestStatsScrollView");
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(930f, 700f);
            scrollRect.anchoredPosition = new Vector2(0f, 0f);
            Image scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = HudPanelColor(0.36f);

            GameObject viewport = new GameObject("PlaytestStatsViewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(18f, 18f);
            viewportRect.offsetMax = new Vector2(-18f, -18f);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = new GameObject("PlaytestStatsContent");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 1320f);
            contentRect.anchoredPosition = Vector2.zero;

            _statsBodyText = CreateText(content.transform, "PlaytestStatsBodyText", Vector2.zero, TextAnchor.UpperLeft, 28, new Vector2(850f, 1320f), string.Empty);
            AddTextShadow(_statsBodyText);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 28f;

            CreateButton(panel.transform, "ResetPlaytestStatsButton", new Vector2(-255f, -430f), "통계 초기화", ShowStatsResetConfirm);
            CreateButton(panel.transform, "PlaytestStatsBackButton", new Vector2(255f, -430f), "메인 메뉴", () => _onMainMenu?.Invoke());
            _statsResetConfirmPanel = CreateStatsResetConfirmPanel(panel.transform);
            _statsResetConfirmPanel.SetActive(false);
            return panel;
        }

        // Rebuilds the playtest stats text from CGR_Stats_ PlayerPrefs keys.
        private void RebuildPlaytestStats(int stageCount)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("로컬 저장 통계입니다. 네트워크 전송은 하지 않습니다.");
            builder.AppendLine("중단은 Pause에서 재시작/메뉴/스테이지 선택으로 나간 횟수입니다.");
            builder.AppendLine("실패 색한도/장은 다른 색 3회 실패/장애물 실패 횟수입니다.");
            builder.AppendLine("Endless 최고 점수: " + EndlessRecords.BestScore + " / 최고 거리: " + Mathf.FloorToInt(EndlessRecords.BestDistance) + "m / 시도: " + EndlessRecords.Attempts + " / 색한도 실패: " + EndlessRecords.WrongShardLimitFails);
            builder.AppendLine();
            int clampedStageCount = Mathf.Clamp(stageCount, 1, StageManager.TotalStageCount);
            for (int stage = 1; stage <= clampedStageCount; stage++)
            {
                builder.AppendLine(PlaytestStats.BuildSummaryLine(stage));
            }

            _statsBodyText.text = builder.ToString();
        }

        // Builds a separate confirmation overlay for playtest stat reset only.
        private GameObject CreateStatsResetConfirmPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "StatsResetConfirmPanel", ScreenPanelColor(0.86f));
            CreateText(panel.transform, "StatsResetConfirmText", new Vector2(0f, 70f), TextAnchor.MiddleCenter, 38, new Vector2(860f, 160f), "플레이테스트 통계만 초기화할까요?");
            CreateButton(panel.transform, "StatsResetConfirmYesButton", new Vector2(-250f, -90f), "통계 초기화", () => _onResetPlaytestStats?.Invoke());
            CreateButton(panel.transform, "StatsResetConfirmNoButton", new Vector2(250f, -90f), "취소", () => _statsResetConfirmPanel.SetActive(false));
            return panel;
        }

        // Shows the playtest stat reset confirmation overlay on top of the stats panel.
        private void ShowStatsResetConfirm()
        {
            _statsResetConfirmPanel.SetActive(true);
        }

        // Updates Settings button labels from the current PlayerPrefs values.
        private void RefreshSettingsLabels()
        {
            _musicButtonText.text = "Music " + (GameSettings.MusicEnabled ? "On" : "Off");
            _musicVolumeButtonText.text = "Music Vol " + VolumePercent(GameSettings.MusicVolume);
            _sfxButtonText.text = "SFX " + (GameSettings.SfxEnabled ? "On" : "Off");
            _sfxVolumeButtonText.text = "SFX Vol " + VolumePercent(GameSettings.SfxVolume);
            _cameraShakeButtonText.text = "Camera Shake " + (GameSettings.CameraShakeEnabled ? "On" : "Off");
            _colorAssistButtonText.text = "Color Assist " + (GameSettings.ColorAssistEnabled ? "On" : "Off");
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            }
        }

        // Applies a dragged Music volume value to labels and game settings immediately.
        private void HandleMusicVolumeChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            _musicVolumeButtonText.text = "Music Vol " + VolumePercent(clamped);
            _onSetMusicVolume?.Invoke(clamped);
        }

        // Applies a dragged SFX volume value to labels and game settings immediately.
        private void HandleSfxVolumeChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            _sfxVolumeButtonText.text = "SFX Vol " + VolumePercent(clamped);
            _onSetSfxVolume?.Invoke(clamped);
        }

        // Builds the first-run Stage 1 tutorial confirmation panel.
        private GameObject CreateTutorialPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "TutorialPanel", ScreenPanelColor(0.95f));
            CreateText(panel.transform, "TutorialTitleText", new Vector2(0f, 360f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), "첫 플레이 안내");
            string body = "좌우로 이동해 색상과 모양이 같은 샤드를 모으세요.\n"
                + "게이트를 통과하면 색과 목표 모양이 바뀝니다.\n"
                + "현재 목표는 좌상단 HUD에서 확인할 수 있습니다.\n"
                + "다른 색 샤드는 3번 먹으면 실패합니다.\n"
                + "장애물은 피하세요.\n"
                + "클리어하면 다음 스테이지가 열립니다.\n"
                + "별 3개는 완벽에 가까운 도전 목표입니다.\n"
                + "Pause 버튼 또는 ESC/P로 일시정지할 수 있습니다.";
            CreateText(panel.transform, "TutorialBodyText", new Vector2(0f, 95f), TextAnchor.MiddleLeft, 40, new Vector2(850f, 420f), body);
            CreateButton(panel.transform, "TutorialOkButton", new Vector2(0f, -315f), "확인", () => _onTutorialOk?.Invoke());
            return panel;
        }

        // Builds the compact in-game HUD.
        private GameObject CreateHudPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "HudPanel", new Color(0f, 0f, 0f, 0f));
            GameObject infoPanel = CreateAnchoredPanel(
                panel.transform,
                "HudInfoPanel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(560f, 362f),
                HudPanelColor(0.78f));
            _scoreText = CreateText(infoPanel.transform, "ScoreText", new Vector2(24f, -18f), TextAnchor.UpperLeft, 32, new Vector2(512f, 326f), string.Empty);
            AddTextShadow(_scoreText);
            _messageText = CreateText(panel.transform, "MessageText", Vector2.zero, TextAnchor.MiddleCenter, 54, new Vector2(900f, 260f), string.Empty);
            AddTextShadow(_messageText);
            _debugText = CreateText(panel.transform, "DebugText", new Vector2(-32f, 32f), TextAnchor.LowerRight, 28, new Vector2(450f, 100f), string.Empty);
            AddTextShadow(_debugText);
            _hintText = CreateText(panel.transform, "HintText", new Vector2(0f, -150f), TextAnchor.UpperCenter, 30, new Vector2(960f, 90f), string.Empty);
            AddTextShadow(_hintText);
            CreateAnchoredButton(
                panel.transform,
                "PauseButton",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-32f, -32f),
                new Vector2(250f, 76f),
                "일시정지",
                32,
                () => _onPause?.Invoke());
            return panel;
        }

        // Builds the pause menu with resume and run navigation actions.
        private GameObject CreatePausePanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "PausePanel", ScreenPanelColor(0.94f));
            CreateText(panel.transform, "PauseTitleText", new Vector2(0f, 330f), TextAnchor.MiddleCenter, 76, new Vector2(860f, 120f), "일시정지");
            _pauseStageText = CreateText(panel.transform, "PauseStageText", new Vector2(0f, 215f), TextAnchor.MiddleCenter, 42, new Vector2(760f, 80f), string.Empty);
            _pauseScoreText = CreateText(panel.transform, "PauseScoreText", new Vector2(0f, 140f), TextAnchor.MiddleCenter, 38, new Vector2(760f, 80f), string.Empty);
            CreateButton(panel.transform, "ResumeButton", new Vector2(0f, 25f), "계속하기", () => _onResume?.Invoke());
            CreateButton(panel.transform, "PauseRetryButton", new Vector2(0f, -105f), "다시 도전", () => _onRestart?.Invoke());
            CreateButton(panel.transform, "PauseStageSelectButton", new Vector2(0f, -235f), "스테이지 선택", () => _onStageSelect?.Invoke());
            CreateButton(panel.transform, "PauseMainMenuButton", new Vector2(0f, -365f), "메인 메뉴", () => _onMainMenu?.Invoke());
            return panel;
        }

        // Builds the result panel with restart and main-menu actions.
        private GameObject CreateResultPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "ResultPanel", ScreenPanelColor(0.92f));
            _resultTitleText = CreateText(panel.transform, "ResultTitleText", new Vector2(0f, 280f), TextAnchor.MiddleCenter, 82, new Vector2(860f, 130f), string.Empty);
            _resultScoreText = CreateText(panel.transform, "ResultScoreText", new Vector2(0f, 150f), TextAnchor.MiddleCenter, 48, new Vector2(860f, 100f), string.Empty);
            _resultInfoText = CreateText(panel.transform, "ResultInfoText", new Vector2(0f, 10f), TextAnchor.MiddleCenter, 34, new Vector2(860f, 120f), string.Empty);
            Button restartButton = CreateButton(panel.transform, "RestartButton", new Vector2(0f, -145f), "재시작", () => _onRestart?.Invoke());
            _restartButtonText = restartButton.GetComponentInChildren<Text>();
            _nextStageButton = CreateButton(panel.transform, "NextStageButton", new Vector2(0f, -265f), "다음 스테이지", () => _onNextStage?.Invoke());
            CreateButton(panel.transform, "ResultStageButton", new Vector2(-250f, -390f), "스테이지 선택", () => _onStageSelect?.Invoke());
            CreateButton(panel.transform, "ResultMenuButton", new Vector2(250f, -390f), "메인 메뉴", () => _onMainMenu?.Invoke());
            return panel;
        }

        // Formats a numeric star count as a three-slot star string.
        private static string StarsText(int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            return new string('★', stars) + new string('☆', 3 - stars);
        }

        // Formats a normalized volume value as a compact settings label.
        private static string VolumePercent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        // Creates a full-screen panel with a simple procedural color fill.
        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > 0.01f;
            return go;
        }

        // Creates a screen-anchored panel for compact HUD groups.
        private static GameObject CreateAnchoredPanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        // Creates a text button using uGUI primitives only.
        private static Button CreateButton(Transform parent, string name, Vector2 anchoredPosition, string label, Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480f, 96f);
            rect.anchoredPosition = anchoredPosition;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = go.AddComponent<Image>();
            image.color = ButtonColor();
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(go.transform, name + "Text", Vector2.zero, TextAnchor.MiddleCenter, 40, rect.sizeDelta, label);
            text.color = ButtonTextColor();
            return button;
        }

        // Creates a horizontal uGUI slider for precise settings values without external sprites.
        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, float value, Action<float> onValueChanged)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(620f, 52f);
            rect.anchoredPosition = anchoredPosition;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            RectTransform backgroundRect = CreateSliderImage(go.transform, "Background", new Vector2(0f, 0f), new Vector2(620f, 16f), HudPanelColor(0.74f)).rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -8f);
            backgroundRect.offsetMax = new Vector2(0f, 8f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(0f, 0f);
            fillAreaRect.offsetMax = new Vector2(0f, 0f);
            Image fill = CreateSliderImage(fillArea.transform, "Fill", Vector2.zero, new Vector2(0f, 22f), ButtonColor());
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            fill.rectTransform.offsetMin = new Vector2(0f, -11f);
            fill.rectTransform.offsetMax = new Vector2(0f, 11f);

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(0f, 0f);
            handleAreaRect.offsetMax = new Vector2(0f, 0f);
            Image handle = CreateSliderImage(handleArea.transform, "Handle", Vector2.zero, new Vector2(46f, 46f), VisualTheme.Current().HudTextColor);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            slider.onValueChanged.AddListener(v => onValueChanged?.Invoke(v));
            return slider;
        }

        // Creates one image element used by generated sliders.
        private static Image CreateSliderImage(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // Creates a text button anchored to a specific screen-relative point for HUD controls.
        private static Button CreateAnchoredButton(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            string label,
            int fontSize,
            Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.color = ButtonColor();
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(go.transform, name + "Text", Vector2.zero, TextAnchor.MiddleCenter, fontSize, size, label);
            text.color = ButtonTextColor();
            return button;
        }

        // Creates an anchored uGUI text element.
        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size, string initialText)
        {
            Text text = CreateText(parent, name, anchoredPosition, anchor, fontSize, size);
            text.text = initialText;
            return text;
        }

        // Creates an anchored uGUI text element.
        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = BuiltinFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = VisualTheme.Current().HudTextColor;
            text.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (anchor == TextAnchor.LowerRight)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            return text;
        }

        // Adds a subtle shadow to keep white HUD text readable on bright backgrounds.
        private static void AddTextShadow(Text text)
        {
            if (text == null)
            {
                return;
            }

            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        // Returns a theme-driven full-screen panel color with a controlled alpha.
        private static Color ScreenPanelColor(float alpha)
        {
            return HudPanelColor(alpha);
        }

        // Returns the active HUD panel color with the requested alpha.
        private static Color HudPanelColor(float alpha)
        {
            Color color = VisualTheme.Current().HudPanelColor;
            color.a = alpha;
            return color;
        }

        // Returns the active accent color for enabled buttons.
        private static Color ButtonColor()
        {
            Color color = VisualTheme.Current().HudAccentColor;
            color.a = 0.90f;
            return color;
        }

        // Returns the muted color used by locked stage buttons.
        private static Color LockedButtonColor()
        {
            Color color = VisualTheme.Current().TrackEdgeColor;
            color.a = 0.88f;
            return color;
        }

        // Returns a readable text color for bright accent buttons.
        private static Color ButtonTextColor()
        {
            return Color.black;
        }

        // Loads a built-in Unity font so no external font asset is required.
        private static Font BuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
