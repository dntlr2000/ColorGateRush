using System;
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
        private Canvas _canvas;
        private GameObject _menuPanel;
        private GameObject _stageSelectPanel;
        private GameObject _rulesPanel;
        private GameObject _settingsPanel;
        private GameObject _resetConfirmPanel;
        private GameObject _hudPanel;
        private GameObject _tutorialPanel;
        private GameObject _pausePanel;
        private GameObject _resultPanel;
        private Transform _stageButtonRoot;
        private Text _scoreText;
        private Text _messageText;
        private Text _debugText;
        private Text _hintText;
        private Text _soundButtonText;
        private Text _cameraShakeButtonText;
        private Text _colorAssistButtonText;
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
        private Action _onToggleCameraShake;
        private Action _onToggleColorAssist;
        private Action _onResetProgress;
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
            Action onToggleCameraShake,
            Action onToggleColorAssist,
            Action onResetProgress,
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
            _onToggleCameraShake = onToggleCameraShake;
            _onToggleColorAssist = onToggleColorAssist;
            _onResetProgress = onResetProgress;
            _onTutorialOk = onTutorialOk;
        }

        // Shows the main menu and hides gameplay/result panels.
        public void ShowMainMenu()
        {
            EnsureCanvas();
            SetPanel(_menuPanel);
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
            RefreshSettingsLabels();
        }

        // Shows the gameplay HUD and refreshes its values.
        public void ShowPlayingHud(StageConfig stage, int score, int combo, ColorId color, int seed)
        {
            EnsureCanvas();
            SetPanel(_hudPanel);
            _hudStageIndex = stage.StageIndex;
            _hudTwoStarScore = stage.TwoStarScore;
            _hudThreeStarScore = stage.ThreeStarScore;
            SetHud(score, combo, color, seed);
        }

        // Shows a short stage-start briefing that disappears automatically and never changes game state.
        public void ShowStageStartHint(StageConfig stage)
        {
            EnsureCanvas();
            ShowMessage(
                "Stage " + stage.StageIndex
                + "\n같은 색/모양 샤드를 모으세요"
                + "\n★2: " + stage.TwoStarScore + "  ★3: " + stage.ThreeStarScore
                + "\n클리어하면 다음 스테이지가 열립니다",
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
                _resultInfoText.text = "피니시에 도달하면 별 1개를 얻습니다.";
            }

            _nextStageButton.gameObject.SetActive(completed && result.HasNextStage);
            _nextStageButton.interactable = completed && nextStageAvailable;
        }

        // Updates score, combo, color, seed, and compact in-game rule hints.
        public void SetHud(int score, int combo, ColorId color, int seed)
        {
            EnsureCanvas();
            ColorVisualProfile profile = GameConstants.GetVisualProfile(color);
            int threeStarRemaining = Mathf.Max(0, _hudThreeStarScore - score);
            _scoreText.text = $"Stage {_hudStageIndex}\nScore: {score}\n★1: 피니시\n★2: {_hudTwoStarScore}   ★3: {_hudThreeStarScore}\n3성까지: {threeStarRemaining}\n현재: {profile.HudLabel}\nCombo x{Mathf.Max(1, combo)}";
            _debugText.text = "Seed " + seed;
            _hintText.text = string.Empty;
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
            GameObject panel = CreatePanel(parent, "MainMenuPanel", ScreenPanelColor(0.92f));
            CreateText(panel.transform, "TitleText", new Vector2(0f, 360f), TextAnchor.MiddleCenter, 86, new Vector2(900f, 140f), "Color Gate Rush");
            CreateText(panel.transform, "SubtitleText", new Vector2(0f, 240f), TextAnchor.MiddleCenter, 34, new Vector2(820f, 100f), "색을 바꾸며 달리고, 같은 색 샤드를 모으세요");
            CreateButton(panel.transform, "StartButton", new Vector2(0f, 90f), "시작", () => _onStart?.Invoke());
            CreateButton(panel.transform, "RulesButton", new Vector2(0f, -45f), "플레이 방법", () => _onRules?.Invoke());
            CreateButton(panel.transform, "SettingsButton", new Vector2(0f, -180f), "설정", () => _onSettings?.Invoke());
            return panel;
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
                + "다른 색 샤드는 피하세요. 먹으면 -" + GameConstants.WrongColorShardPenalty + ", 콤보 초기화\n"
                + "게이트를 통과하면 플레이어 색과 목표 모양이 바뀝니다. +" + GameConstants.GateScore + "\n"
                + "현재 목표 색상/모양은 HUD와 플레이어 주변 accent에서 확인합니다.\n"
                + "장애물에 부딪히면 실패합니다. -" + GameConstants.ObstaclePenalty + "\n"
                + "피니시에 도달하면 클리어하고 별 1개를 얻습니다.\n"
                + "★2/★3 목표 점수는 플레이 중 HUD에서 확인합니다.\n"
                + "★2는 ★3 목표 점수의 2/3 이상입니다.\n"
                + "클리어하면 다음 스테이지가 열립니다.\n"
                + "★3은 거의 완벽한 수집 보상입니다. 1~2번 실수해도 어려울 수 있습니다.\n"
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
            _soundButtonText = CreateButton(panel.transform, "SoundToggleButton", new Vector2(0f, 250f), "Sound", () => _onToggleSound?.Invoke()).GetComponentInChildren<Text>();
            _cameraShakeButtonText = CreateButton(panel.transform, "CameraShakeToggleButton", new Vector2(0f, 110f), "Camera", () => _onToggleCameraShake?.Invoke()).GetComponentInChildren<Text>();
            _colorAssistButtonText = CreateButton(panel.transform, "ColorAssistToggleButton", new Vector2(0f, -30f), "Assist", () => _onToggleColorAssist?.Invoke()).GetComponentInChildren<Text>();
            CreateButton(panel.transform, "ResetProgressButton", new Vector2(0f, -190f), "진행 초기화", ShowResetConfirm);
            CreateButton(panel.transform, "SettingsBackButton", new Vector2(0f, -390f), "메인 메뉴", () => _onMainMenu?.Invoke());
            _resetConfirmPanel = CreateResetConfirmPanel(panel.transform);
            _resetConfirmPanel.SetActive(false);
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
            _resetConfirmPanel.SetActive(true);
        }

        // Updates Settings button labels from the current PlayerPrefs values.
        private void RefreshSettingsLabels()
        {
            _soundButtonText.text = "Sound " + (GameSettings.SoundEnabled ? "On" : "Off");
            _cameraShakeButtonText.text = "Camera Shake " + (GameSettings.CameraShakeEnabled ? "On" : "Off");
            _colorAssistButtonText.text = "Color Assist " + (GameSettings.ColorAssistEnabled ? "On" : "Off");
        }

        // Builds the first-run Stage 1 tutorial confirmation panel.
        private GameObject CreateTutorialPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "TutorialPanel", ScreenPanelColor(0.95f));
            CreateText(panel.transform, "TutorialTitleText", new Vector2(0f, 360f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), "첫 플레이 안내");
            string body = "좌우로 이동해 색상과 모양이 같은 샤드를 모으세요.\n"
                + "게이트를 통과하면 색과 목표 모양이 바뀝니다.\n"
                + "현재 목표는 좌상단 HUD에서 확인할 수 있습니다.\n"
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
                new Vector2(540f, 318f),
                HudPanelColor(0.78f));
            _scoreText = CreateText(infoPanel.transform, "ScoreText", new Vector2(24f, -18f), TextAnchor.UpperLeft, 34, new Vector2(492f, 282f), string.Empty);
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
