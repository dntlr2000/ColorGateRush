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
        private const string TitleScreenResourcePath = "ColorGateRush/Images/TitleScreen";
        private const string MainMenuBackgroundResourcePath = "ColorGateRush/Images/MainMenuBackground";
        private const float SettingsContentWidth = 560f;
        private const float SettingsPrimaryButtonHeight = 74f;
        private const float SettingsSliderHeight = 52f;
        private const float SettingsSliderTrackHeight = 12f;
        private const float SettingsSliderHandleWidth = 20f;
        private const float SettingsSliderHandleHeight = 34f;
        private const float SettingsTabWidth = 218f;
        private const float SettingsPairButtonWidth = 264f;
        private const float SettingsBottomActionY = -585f;

        private enum SettingsTab
        {
            General,
            Language,
            Data
        }

        private Canvas _canvas;
        private GameObject _titlePanel;
        private GameObject _menuPanel;
        private GameObject _stageSelectPanel;
        private GameObject _rulesPanel;
        private GameObject _settingsPanel;
        private GameObject _resetConfirmPanel;
        private GameObject _endlessResetConfirmPanel;
        private GameObject _settingsGeneralSection;
        private GameObject _settingsLanguageSection;
        private GameObject _settingsDataSection;
        private GameObject _hudPanel;
        private GameObject _messageToastPanel;
        private GameObject _comboBadgePanel;
        private GameObject _tutorialPanel;
        private GameObject _pausePanel;
        private GameObject _resultPanel;
        private Transform _stageButtonRoot;
        private Text _scoreText;
        private Text _hudModeText;
        private Text _hudTargetText;
        private Text _hudProgressText;
        private Text _hudMistakeIconsText;
        private Text _hudCurrentText;
        private Text _hudComboText;
        private Text _messageText;
        private Text _hudScoreLabelText;
        private Text _hudMistakeLabelText;
        private Image _hudProgressFill;
        private Image _hudColorChip;
        private Text _hudShapeGlyphText;
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
        private Text _languageLabelText;
        private Button _settingsGeneralTabButton;
        private Button _settingsLanguageTabButton;
        private Button _settingsDataTabButton;
        private Button _koreanLanguageButton;
        private Button _englishLanguageButton;
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
        private Action _onStartEndless;
        private Action _onQuit;
        private Action _onResetEndlessRecords;
        private Action _onTutorialOk;
        private Action<int> _onStageSelected;
        private int _hudStageIndex = 1;
        private int _hudTwoStarScore;
        private int _hudThreeStarScore;
        private StageConfig[] _lastStages;
        private int _lastUnlockedStage;
        private int _lastSelectedStage;
        private Func<int, int> _lastGetBestStars;
        private SettingsTab _activeSettingsTab = SettingsTab.General;
        private bool _lastHudIsEndless;
        private StageConfig _lastHudStage;
        private int _lastHudScore;
        private int _lastHudCombo;
        private ColorId _lastHudColor = ColorId.Cyan;
        private int _lastHudSeed;
        private int _lastWrongShardCount;
        private int _lastWrongShardLimit;
        private float _lastEndlessDistance;
        private int _lastEndlessBestScore;
        private float _lastEndlessBestDistance;
        private float _lastEndlessSpeedMultiplier = 1f;
        private bool _lastPauseIsEndless;
        private StageConfig _lastPauseStage;
        private int _lastPauseScore;
        private float _lastPauseDistance;
        private bool _lastResultIsEndless;
        private bool _lastResultCompleted;
        private StageConfig _lastResultStage;
        private StageResult _lastStageResult;
        private bool _lastNextStageAvailable;
        private EndlessRunResult _lastEndlessResult;
        private Action _onTitleContinue;

        // Builds the runtime UI tree as soon as the systems object wakes.
        private void Awake()
        {
            EnsureCanvas();
        }

        // Subscribes this UI controller to language changes while it is alive.
        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        // Removes the language callback so destroyed UI objects are not refreshed.
        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
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
            Action onTitleContinue,
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
            Action onStartEndless,
            Action onQuit,
            Action onResetEndlessRecords,
            Action onTutorialOk)
        {
            _onTitleContinue = onTitleContinue;
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
            _onStartEndless = onStartEndless;
            _onQuit = onQuit;
            _onResetEndlessRecords = onResetEndlessRecords;
            _onTutorialOk = onTutorialOk;
        }

        // Refreshes the currently visible dynamic UI after the active language changes.
        private void HandleLanguageChanged()
        {
            if (_canvas == null)
            {
                return;
            }

            RefreshSettingsLabels();
            RefreshLanguageButtons();
            if (_stageSelectPanel != null && _stageSelectPanel.activeSelf && _lastStages != null)
            {
                RebuildStageButtons(_lastStages, _lastUnlockedStage, _lastSelectedStage, _lastGetBestStars);
            }

            if (_hudPanel != null && _hudPanel.activeSelf)
            {
                if (_lastHudIsEndless)
                {
                    SetEndlessHud(
                        _lastHudScore,
                        _lastHudCombo,
                        _lastHudColor,
                        _lastEndlessDistance,
                        _lastEndlessBestScore,
                        _lastEndlessBestDistance,
                        _lastHudSeed,
                        _lastWrongShardCount,
                        _lastWrongShardLimit,
                        _lastEndlessSpeedMultiplier);
                }
                else
                {
                    SetHud(_lastHudScore, _lastHudCombo, _lastHudColor, _lastHudSeed, _lastWrongShardCount, _lastWrongShardLimit);
                }
            }

            if (_pausePanel != null && _pausePanel.activeSelf)
            {
                if (_lastPauseIsEndless)
                {
                    ShowEndlessPauseMenu(_lastPauseScore, _lastPauseDistance);
                }
                else if (_lastPauseStage.StageIndex > 0)
                {
                    ShowPauseMenu(_lastPauseStage, _lastPauseScore);
                }
            }

            if (_resultPanel != null && _resultPanel.activeSelf)
            {
                if (_lastResultIsEndless)
                {
                    ShowEndlessResult(_lastEndlessResult);
                }
                else if (_lastResultStage.StageIndex > 0)
                {
                    ShowResult(_lastResultCompleted, _lastResultStage, _lastStageResult, _lastNextStageAvailable);
                }
            }
        }

        // Applies a new language choice without changing the current game state.
        private void SetLanguage(Language language)
        {
            LocalizationManager.SetLanguage(language);
            RefreshLanguageButtons();
        }

        // Highlights the currently selected language button in Settings.
        private void RefreshLanguageButtons()
        {
            ApplyLanguageButtonState(_koreanLanguageButton, LocalizationManager.CurrentLanguage == Language.Korean);
            ApplyLanguageButtonState(_englishLanguageButton, LocalizationManager.CurrentLanguage == Language.English);
            RefreshSettingsTabButtons();
        }

        // Shows one Settings section and keeps destructive reset controls isolated under Data.
        private void ShowSettingsTab(SettingsTab tab)
        {
            _activeSettingsTab = tab;
            if (_settingsGeneralSection != null)
            {
                _settingsGeneralSection.SetActive(tab == SettingsTab.General);
            }

            if (_settingsLanguageSection != null)
            {
                _settingsLanguageSection.SetActive(tab == SettingsTab.Language);
            }

            if (_settingsDataSection != null)
            {
                _settingsDataSection.SetActive(tab == SettingsTab.Data);
            }

            if (_resetConfirmPanel != null)
            {
                _resetConfirmPanel.SetActive(false);
            }

            if (_endlessResetConfirmPanel != null)
            {
                _endlessResetConfirmPanel.SetActive(false);
            }

            RefreshSettingsTabButtons();
        }

        // Highlights the selected Settings tab without rebuilding the whole panel.
        private void RefreshSettingsTabButtons()
        {
            ApplyLanguageButtonState(_settingsGeneralTabButton, _activeSettingsTab == SettingsTab.General);
            ApplyLanguageButtonState(_settingsLanguageTabButton, _activeSettingsTab == SettingsTab.Language);
            ApplyLanguageButtonState(_settingsDataTabButton, _activeSettingsTab == SettingsTab.Data);
        }

        // Applies selected or neutral styling to one generated language button.
        private static void ApplyLanguageButtonState(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? ButtonColor() : HudPanelColor(0.70f);
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = selected ? ButtonTextColor() : VisualTheme.Current().HudTextColor;
            }
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

        // Shows the launch title image and waits for an explicit tap/click before opening the main menu.
        public void ShowTitleScreen()
        {
            EnsureCanvas();
            SetPanel(_titlePanel);
        }

        // Shows stage selection with lock state and saved best stars.
        public void ShowStageSelect(StageConfig[] stages, int unlockedStage, int selectedStage, Func<int, int> getBestStars)
        {
            EnsureCanvas();
            _lastStages = stages;
            _lastUnlockedStage = unlockedStage;
            _lastSelectedStage = selectedStage;
            _lastGetBestStars = getBestStars;
            RebuildStageButtons(stages, unlockedStage, selectedStage, getBestStars);
            SetPanel(_stageSelectPanel);
        }

        // Shows the rules panel and hides other panels.
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
            ShowSettingsTab(SettingsTab.General);
            RefreshSettingsLabels();
            RefreshLanguageButtons();
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
            _lastHudIsEndless = false;
            _lastHudStage = stage;
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
            _lastHudIsEndless = true;
            SetEndlessHud(score, combo, color, distance, bestScore, bestDistance, seed, wrongShardCount, wrongShardLimit, speedMultiplier);
        }

        // Shows a short stage-start briefing that disappears automatically and never changes game state.
        public void ShowStageStartHint(StageConfig stage)
        {
            EnsureCanvas();
            ShowMessage(
                LocalizationManager.T(LocalizationKey.StageStartHint),
                2.0f);
        }

        // Shows a short Endless briefing that disappears automatically and never changes game state.
        public void ShowEndlessStartHint()
        {
            EnsureCanvas();
            ShowMessage(
                LocalizationManager.T(LocalizationKey.EndlessStartHint),
                2.0f);
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
            _lastPauseIsEndless = false;
            _lastPauseStage = stage;
            _lastPauseScore = score;
            _pauseStageText.text = LocalizationManager.T(LocalizationKey.StageLabel, stage.StageIndex);
            _pauseScoreText.text = LocalizationManager.T(LocalizationKey.CurrentScore, score);
        }

        // Shows the pause menu for Endless Mode without mentioning stage stars or unlocks.
        public void ShowEndlessPauseMenu(int score, float distance)
        {
            EnsureCanvas();
            SetPanel(_pausePanel);
            ClearMessage();
            _lastPauseIsEndless = true;
            _lastPauseScore = score;
            _lastPauseDistance = distance;
            _pauseStageText.text = LocalizationManager.T(LocalizationKey.EndlessMode);
            _pauseScoreText.text = LocalizationManager.T(LocalizationKey.Score) + " " + score + "   " + LocalizationManager.T(LocalizationKey.Distance) + " " + Mathf.FloorToInt(distance) + "m";
        }

        // Shows the failure or clear result panel with final score and navigation buttons.
        public void ShowResult(bool completed, StageConfig stage, StageResult result, bool nextStageAvailable)
        {
            EnsureCanvas();
            SetPanel(_resultPanel);
            _lastResultIsEndless = false;
            _lastResultCompleted = completed;
            _lastResultStage = stage;
            _lastStageResult = result;
            _lastNextStageAvailable = nextStageAvailable;
            _resultTitleText.text = completed ? LocalizationManager.T(LocalizationKey.ClearGeneric) : LocalizationManager.T(LocalizationKey.Failed);
            _resultScoreText.text = LocalizationManager.T(LocalizationKey.StageLabel, stage.StageIndex)
                + "\n" + LocalizationManager.T(LocalizationKey.FinalScore, result.Score)
                + "\n" + LocalizationManager.T(LocalizationKey.StarsEarned, StarsText(result.Stars));
            _restartButtonText.text = completed ? LocalizationManager.T(LocalizationKey.Restart) : LocalizationManager.T(LocalizationKey.TryAgain);
            if (completed)
            {
                string bestText = result.BestStarsImproved
                    ? LocalizationManager.T(LocalizationKey.BestStarsImproved)
                    : LocalizationManager.T(LocalizationKey.BestStars, StarsText(result.BestStars));
                int threeStarShortfall = Mathf.Max(0, stage.ThreeStarScore - result.Score);
                string starPraise = result.Stars == 3
                    ? LocalizationManager.T(LocalizationKey.NearPerfect)
                    : (result.Stars == 2 ? LocalizationManager.T(LocalizationKey.GoodRun) : LocalizationManager.T(LocalizationKey.ImproveStars));
                string unlockText = result.NextStageUnlocked
                    ? LocalizationManager.T(LocalizationKey.StageUnlock)
                    : (!result.HasNextStage
                        ? LocalizationManager.T(LocalizationKey.AllStagesComplete)
                        : (nextStageAvailable ? LocalizationManager.T(LocalizationKey.ClearGeneric) : LocalizationManager.T(LocalizationKey.ClearToUnlock)));
                string shortfallText = result.Stars < 3 ? "\n" + LocalizationManager.T(LocalizationKey.ToThreeStar, threeStarShortfall) : string.Empty;
                _resultInfoText.text = starPraise + "\n" + bestText + "\n" + unlockText + shortfallText;
            }
            else
            {
                _resultInfoText.text = StageFailReasonText(result.FailReason)
                    + "\n" + LocalizationManager.T(LocalizationKey.FinishForOneStar);
            }

            _nextStageButton.gameObject.SetActive(completed && result.HasNextStage);
            _nextStageButton.interactable = completed && nextStageAvailable;
        }

        // Shows an Endless-only failure result with best score and distance records.
        public void ShowEndlessResult(EndlessRunResult result)
        {
            EnsureCanvas();
            SetPanel(_resultPanel);
            _lastResultIsEndless = true;
            _lastEndlessResult = result;
            _resultTitleText.text = LocalizationManager.T(LocalizationKey.RecordEnded);
            _resultScoreText.text = LocalizationManager.T(LocalizationKey.EndlessMode)
                + "\n" + LocalizationManager.T(LocalizationKey.Score) + " " + result.Score
                + "\n" + LocalizationManager.T(LocalizationKey.Distance) + " " + Mathf.FloorToInt(result.Distance) + "m"
                + "\n" + LocalizationManager.T(LocalizationKey.Chances) + ": " + FormatMistakeIcons(result.WrongShardCount, result.WrongShardLimit);
            string recordText = result.NewBestScore || result.NewBestDistance ? LocalizationManager.T(LocalizationKey.NewRecord) : LocalizationManager.T(LocalizationKey.BestRecord);
            _resultInfoText.text = recordText
                + "\n" + LocalizationManager.T(LocalizationKey.FailureReason, EndlessFailReasonText(result.FailReason))
                + "\n" + LocalizationManager.T(LocalizationKey.BestScore) + " " + result.BestScore
                + "   " + LocalizationManager.T(LocalizationKey.BestDistance) + " " + Mathf.FloorToInt(result.BestDistance) + "m"
                + "\n" + LocalizationManager.T(LocalizationKey.Rows) + " " + result.RowsGenerated;
            _restartButtonText.text = LocalizationManager.T(LocalizationKey.TryAgain);
            _nextStageButton.gameObject.SetActive(false);
            _nextStageButton.interactable = false;
        }

        // Returns player-facing text for the Endless failure reason shown on the result panel.
        private static string EndlessFailReasonText(EndlessFailReason failReason)
        {
            return failReason == EndlessFailReason.WrongShardLimit
                ? LocalizationManager.T(LocalizationKey.WrongShardLimitReason)
                : LocalizationManager.T(LocalizationKey.ObstacleHitReason);
        }

        // Returns player-facing text for finite Stage Mode failure causes.
        private static string StageFailReasonText(StageFailReason failReason)
        {
            return failReason == StageFailReason.WrongShardLimit
                ? LocalizationManager.T(LocalizationKey.WrongShardLimitReason)
                : LocalizationManager.T(LocalizationKey.ObstacleHitReason);
        }

        // Updates score, combo, color, seed, mistake chances, and compact in-game rule hints.
        public void SetHud(int score, int combo, ColorId color, int seed, int wrongShardCount, int wrongShardLimit)
        {
            EnsureCanvas();
            ColorVisualProfile profile = GameConstants.GetVisualProfile(color);
            _lastHudIsEndless = false;
            _lastHudScore = score;
            _lastHudCombo = combo;
            _lastHudColor = color;
            _lastHudSeed = seed;
            _lastWrongShardCount = wrongShardCount;
            _lastWrongShardLimit = wrongShardLimit;
            int threeStarRemaining = Mathf.Max(0, _hudThreeStarScore - score);
            _hudModeText.text = LocalizationManager.T(LocalizationKey.StageLabel, _hudStageIndex.ToString("00"));
            _scoreText.text = score.ToString();
            _hudTargetText.text = LocalizationManager.T(LocalizationKey.Finish) + "   ★2 " + _hudTwoStarScore + "   ★3 " + _hudThreeStarScore;
            _hudProgressText.text = threeStarRemaining > 0 ? LocalizationManager.T(LocalizationKey.ToThreeStar, threeStarRemaining) : LocalizationManager.T(LocalizationKey.ThreeStarReady);
            SetHudProgress(_hudProgressFill, score / (float)Mathf.Max(1, _hudThreeStarScore), 346f);
            _hudMistakeIconsText.text = FormatMistakeIcons(wrongShardCount, wrongShardLimit);
            SetCurrentVisual(profile);
            SetComboBadge(combo);
            _debugText.text = string.Empty;
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
            _lastHudIsEndless = true;
            _lastHudScore = score;
            _lastHudCombo = combo;
            _lastHudColor = color;
            _lastHudSeed = seed;
            _lastWrongShardCount = wrongShardCount;
            _lastWrongShardLimit = wrongShardLimit;
            _lastEndlessDistance = distance;
            _lastEndlessBestScore = bestScore;
            _lastEndlessBestDistance = bestDistance;
            _lastEndlessSpeedMultiplier = speedMultiplier;
            int safeWrongLimit = Mathf.Max(1, wrongShardLimit);
            int safeWrongCount = Mathf.Clamp(wrongShardCount, 0, safeWrongLimit);
            _hudModeText.text = LocalizationManager.T(LocalizationKey.EndlessMode);
            _scoreText.text = score.ToString();
            _hudTargetText.text = LocalizationManager.T(LocalizationKey.Distance) + " " + Mathf.FloorToInt(distance) + "m   " + LocalizationManager.T(LocalizationKey.Best) + " " + bestScore;
            _hudProgressText.text = LocalizationManager.T(LocalizationKey.Speed) + " x" + Mathf.Max(1f, speedMultiplier).ToString("0.0") + "   " + LocalizationManager.T(LocalizationKey.Best) + " " + Mathf.FloorToInt(bestDistance) + "m";
            SetHudProgress(_hudProgressFill, Mathf.InverseLerp(1f, 3f, Mathf.Max(1f, speedMultiplier)), 346f);
            _hudMistakeIconsText.text = FormatMistakeIcons(safeWrongCount, safeWrongLimit);
            SetCurrentVisual(profile);
            SetComboBadge(combo);
            _debugText.text = string.Empty;
            _hintText.text = string.Empty;
        }

        // Formats remaining wrong-shard chances as bright and dim rich-text HUD glyphs.
        private static string FormatMistakeIcons(int wrongShardCount, int wrongShardLimit)
        {
            int safeLimit = Mathf.Max(1, wrongShardLimit);
            int used = Mathf.Clamp(wrongShardCount, 0, safeLimit);
            int remaining = safeLimit - used;
            string activeColor = "#" + ColorUtility.ToHtmlStringRGB(VisualTheme.Current().HudAccentColor);
            string inactiveColor = "#4C5668";
            StringBuilder builder = new StringBuilder(safeLimit * 28);
            for (int i = 0; i < safeLimit; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append("<color=");
                builder.Append(i < remaining ? activeColor : inactiveColor);
                builder.Append(">◆</color>");
            }

            return builder.ToString();
        }

        // Updates the small current-target color chip and procedural shape glyph.
        private void SetCurrentVisual(ColorVisualProfile profile)
        {
            if (_hudColorChip != null)
            {
                _hudColorChip.color = GameConstants.ToUnityColor(profile.ColorId);
            }

            if (_hudShapeGlyphText != null)
            {
                _hudShapeGlyphText.text = ShapeGlyph(profile.ShapeType);
            }

            if (_hudCurrentText != null)
            {
                _hudCurrentText.text = LocalizationManager.T(LocalizationKey.Current) + "  " + LocalizationManager.ColorName(profile.ColorId) + " / " + LocalizationManager.ShapeName(profile.ShapeType);
            }
        }

        // Updates the bottom-right combo badge without using the central toast channel.
        private void SetComboBadge(int combo)
        {
            int safeCombo = Mathf.Max(1, combo);
            if (_hudComboText != null)
            {
                _hudComboText.text = "x" + safeCombo;
                _hudComboText.color = safeCombo > 1
                    ? new Color(1f, 0.90f, 0.28f, 1f)
                    : new Color(1f, 1f, 1f, 0.82f);
            }

            if (_comboBadgePanel != null)
            {
                _comboBadgePanel.SetActive(true);
            }
        }

        // Resizes the HUD progress fill without allocating or moving gameplay objects.
        private static void SetHudProgress(Image fill, float normalizedValue, float maxWidth)
        {
            if (fill == null)
            {
                return;
            }

            RectTransform rect = fill.rectTransform;
            rect.sizeDelta = new Vector2(Mathf.Clamp01(normalizedValue) * maxWidth, rect.sizeDelta.y);
        }

        // Returns a compact text glyph that mirrors the current procedural shard shape.
        private static string ShapeGlyph(ColorShapeType shapeType)
        {
            switch (shapeType)
            {
                case ColorShapeType.Cube:
                    return "■";
                case ColorShapeType.Capsule:
                    return "▮";
                case ColorShapeType.Diamond:
                    return "◆";
                default:
                    return "●";
            }
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
            if (_messageToastPanel != null)
            {
                _messageToastPanel.SetActive(!string.IsNullOrEmpty(message));
            }

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
            if (_messageToastPanel != null)
            {
                _messageToastPanel.SetActive(false);
            }

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

            _titlePanel = CreateTitlePanel(canvasGo.transform);
            _menuPanel = CreateMenuPanel(canvasGo.transform);
            _stageSelectPanel = CreateStageSelectPanel(canvasGo.transform);
            _rulesPanel = CreateRulesPanel(canvasGo.transform);
            _settingsPanel = CreateSettingsPanel(canvasGo.transform);
            _tutorialPanel = CreateTutorialPanel(canvasGo.transform);
            _hudPanel = CreateHudPanel(canvasGo.transform);
            _pausePanel = CreatePausePanel(canvasGo.transform);
            _resultPanel = CreateResultPanel(canvasGo.transform);
            SetPanel(_titlePanel);
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
            _titlePanel.SetActive(_titlePanel == activePanel);
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

        // Builds the launch title screen using the bundled user-provided title art.
        private GameObject CreateTitlePanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "TitleScreenPanel", Color.black);
            Button tapButton = panel.AddComponent<Button>();
            tapButton.transition = Selectable.Transition.None;
            tapButton.onClick.AddListener(() => _onTitleContinue?.Invoke());

            GameObject imageGo = new GameObject("TitleScreenImage");
            imageGo.transform.SetParent(panel.transform, false);
            RectTransform imageRect = imageGo.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            Image image = imageGo.AddComponent<Image>();
            image.raycastTarget = false;
            Sprite sprite = LoadTitleScreenSprite();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(0.02f, 0.03f, 0.10f, 1f);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return panel;
        }

        // Builds the title menu with start and rules buttons.
        private GameObject CreateMenuPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "MainMenuPanel", Color.black);
            CreateMenuBackground(panel.transform);
            Text titleText = CreateText(panel.transform, "TitleText", new Vector2(0f, 390f), TextAnchor.MiddleCenter, 86, new Vector2(900f, 140f), "Color Gate Rush");
            AddTextShadow(titleText);
            Text subtitleText = CreateLocalizedText(panel.transform, "SubtitleText", new Vector2(0f, 285f), TextAnchor.MiddleCenter, 34, new Vector2(820f, 100f), LocalizationKey.TitleSubtitle);
            AddTextShadow(subtitleText);
            CreateLocalizedButton(panel.transform, "StartButton", new Vector2(0f, 145f), LocalizationKey.Start, () => _onStart?.Invoke());
            CreateLocalizedButton(panel.transform, "EndlessModeButton", new Vector2(0f, 25f), LocalizationKey.EndlessMode, () => _onStartEndless?.Invoke());
            CreateLocalizedButton(panel.transform, "RulesButton", new Vector2(0f, -95f), LocalizationKey.Rules, () => _onRules?.Invoke());
            CreateLocalizedButton(panel.transform, "SettingsButton", new Vector2(0f, -215f), LocalizationKey.Settings, () => _onSettings?.Invoke());
            CreateLocalizedButton(panel.transform, "QuitButton", new Vector2(0f, -355f), LocalizationKey.Quit, () => _onQuit?.Invoke());
            _menuNoticeText = CreateText(panel.transform, "MenuNoticeText", new Vector2(0f, -495f), TextAnchor.MiddleCenter, 28, new Vector2(860f, 80f), string.Empty);
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

        // Loads the Resources-backed title screen with a Texture2D fallback for import setting changes.
        private static Sprite LoadTitleScreenSprite()
        {
            return LoadResourceSprite(TitleScreenResourcePath, "Title screen image");
        }

        // Loads the Resources-backed menu background with a Texture2D fallback for import setting changes.
        private static Sprite LoadMainMenuBackgroundSprite()
        {
            return LoadResourceSprite(MainMenuBackgroundResourcePath, "Main menu background image");
        }

        // Loads a Resources sprite and falls back to creating a Sprite from a Texture2D import.
        private static Sprite LoadResourceSprite(string resourcePath, string label)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning(label + " missing from Resources: " + resourcePath);
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
            CreateLocalizedText(panel.transform, "StageSelectTitleText", new Vector2(0f, 430f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), LocalizationKey.StageSelect);

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
            CreateLocalizedButton(panel.transform, "StageBackButton", new Vector2(0f, -430f), LocalizationKey.MainMenu, () => _onMainMenu?.Invoke());
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
                    ? LocalizationManager.T(LocalizationKey.StageLabel, stage.StageIndex) + " " + StarsText(getBestStars(stage.StageIndex))
                    : LocalizationManager.T(LocalizationKey.StageLockedLabel, stage.StageIndex);
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
            CreateLocalizedText(panel.transform, "RulesTitleText", new Vector2(0f, 430f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), LocalizationKey.RulesTitle);
            CreateLocalizedText(panel.transform, "RulesBodyText", new Vector2(0f, 65f), TextAnchor.MiddleLeft, 34, new Vector2(880f, 660f), LocalizationKey.RulesBody);
            CreateLocalizedButton(panel.transform, "RulesBackButton", new Vector2(0f, -420f), LocalizationKey.MainMenu, () => _onMainMenu?.Invoke());
            return panel;
        }

        // Builds the settings screen with CGR-prefixed PlayerPrefs controls.
        private GameObject CreateSettingsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "SettingsPanel", ScreenPanelColor(0.94f));
            CreateLocalizedText(panel.transform, "SettingsTitleText", new Vector2(0f, 430f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), LocalizationKey.Settings);
            _settingsGeneralTabButton = CreateSettingsTabButton(panel.transform, "SettingsGeneralTab", new Vector2(-240f, 305f), LocalizationKey.General, SettingsTab.General);
            _settingsLanguageTabButton = CreateSettingsTabButton(panel.transform, "SettingsLanguageTab", new Vector2(0f, 305f), LocalizationKey.Language, SettingsTab.Language);
            _settingsDataTabButton = CreateSettingsTabButton(panel.transform, "SettingsDataTab", new Vector2(240f, 305f), LocalizationKey.Data, SettingsTab.Data);

            _settingsGeneralSection = CreateSettingsSection(panel.transform, "SettingsGeneralSection");
            GameObject musicGroup = CreateSettingsOptionGroup(_settingsGeneralSection.transform, "MusicVolumeGroup", new Vector2(0f, 155f), new Vector2(SettingsContentWidth, 232f));
            Button musicButton = CreateButton(musicGroup.transform, "MusicToggleButton", new Vector2(0f, 72f), "Music", () => _onToggleMusic?.Invoke());
            ConfigureSettingsControlButton(musicButton, new Vector2(SettingsContentWidth, SettingsPrimaryButtonHeight), 34);
            _musicButtonText = musicButton.GetComponentInChildren<Text>();
            _musicVolumeButtonText = CreateText(musicGroup.transform, "MusicVolumeLabel", new Vector2(0f, -2f), TextAnchor.MiddleCenter, 30, new Vector2(SettingsContentWidth, 44f), LocalizationManager.T(LocalizationKey.MusicVolume));
            AddTextShadow(_musicVolumeButtonText);
            _musicVolumeSlider = CreateSlider(musicGroup.transform, "MusicVolumeSlider", new Vector2(0f, -86f), GameSettings.MusicVolume, HandleMusicVolumeChanged);

            GameObject sfxGroup = CreateSettingsOptionGroup(_settingsGeneralSection.transform, "SfxVolumeGroup", new Vector2(0f, -112f), new Vector2(SettingsContentWidth, 232f));
            Button sfxButton = CreateButton(sfxGroup.transform, "SfxToggleButton", new Vector2(0f, 72f), "SFX", () => _onToggleSfx?.Invoke());
            ConfigureSettingsControlButton(sfxButton, new Vector2(SettingsContentWidth, SettingsPrimaryButtonHeight), 34);
            _sfxButtonText = sfxButton.GetComponentInChildren<Text>();
            _sfxVolumeButtonText = CreateText(sfxGroup.transform, "SfxVolumeLabel", new Vector2(0f, -2f), TextAnchor.MiddleCenter, 30, new Vector2(SettingsContentWidth, 44f), LocalizationManager.T(LocalizationKey.SfxVolume));
            AddTextShadow(_sfxVolumeButtonText);
            _sfxVolumeSlider = CreateSlider(sfxGroup.transform, "SfxVolumeSlider", new Vector2(0f, -86f), GameSettings.SfxVolume, HandleSfxVolumeChanged);
            Button cameraShakeButton = CreateButton(_settingsGeneralSection.transform, "CameraShakeToggleButton", new Vector2(-144f, -330f), "Camera", () => _onToggleCameraShake?.Invoke());
            ConfigureSettingsControlButton(cameraShakeButton, new Vector2(SettingsPairButtonWidth, SettingsPrimaryButtonHeight), 27);
            _cameraShakeButtonText = cameraShakeButton.GetComponentInChildren<Text>();
            Button colorAssistButton = CreateButton(_settingsGeneralSection.transform, "ColorAssistToggleButton", new Vector2(144f, -330f), "Assist", () => _onToggleColorAssist?.Invoke());
            ConfigureSettingsControlButton(colorAssistButton, new Vector2(SettingsPairButtonWidth, SettingsPrimaryButtonHeight), 27);
            _colorAssistButtonText = colorAssistButton.GetComponentInChildren<Text>();

            _settingsLanguageSection = CreateSettingsSection(panel.transform, "SettingsLanguageSection");
            _languageLabelText = CreateLocalizedText(_settingsLanguageSection.transform, "LanguageLabel", new Vector2(0f, 130f), TextAnchor.MiddleCenter, 38, new Vector2(SettingsContentWidth, 60f), LocalizationKey.Language);
            AddTextShadow(_languageLabelText);
            _koreanLanguageButton = CreateLocalizedButton(_settingsLanguageSection.transform, "KoreanLanguageButton", new Vector2(-144f, 20f), LocalizationKey.Korean, () => SetLanguage(Language.Korean));
            ConfigureSettingsControlButton(_koreanLanguageButton, new Vector2(SettingsPairButtonWidth, SettingsPrimaryButtonHeight), 30);
            _englishLanguageButton = CreateLocalizedButton(_settingsLanguageSection.transform, "EnglishLanguageButton", new Vector2(144f, 20f), LocalizationKey.English, () => SetLanguage(Language.English));
            ConfigureSettingsControlButton(_englishLanguageButton, new Vector2(SettingsPairButtonWidth, SettingsPrimaryButtonHeight), 30);

            _settingsDataSection = CreateSettingsSection(panel.transform, "SettingsDataSection");
            Button resetProgressButton = CreateLocalizedButton(_settingsDataSection.transform, "ResetProgressButton", new Vector2(0f, 130f), LocalizationKey.StageProgressReset, ShowResetConfirm);
            ConfigureSettingsControlButton(resetProgressButton, new Vector2(SettingsContentWidth, SettingsPrimaryButtonHeight), 30);
            Button resetEndlessButton = CreateLocalizedButton(_settingsDataSection.transform, "ResetEndlessRecordsButton", new Vector2(0f, 20f), LocalizationKey.ResetEndlessRecords, ShowEndlessResetConfirm);
            ConfigureSettingsControlButton(resetEndlessButton, new Vector2(SettingsContentWidth, SettingsPrimaryButtonHeight), 30);
            Text irreversibleText = CreateLocalizedText(_settingsDataSection.transform, "SettingsDataWarningText", new Vector2(0f, -120f), TextAnchor.MiddleCenter, 30, new Vector2(SettingsContentWidth, 96f), LocalizationKey.ResetCannotBeUndone);
            AddTextShadow(irreversibleText);

            Button backButton = CreateLocalizedButton(panel.transform, "SettingsBackButton", new Vector2(0f, SettingsBottomActionY), LocalizationKey.MainMenu, () => _onMainMenu?.Invoke());
            ConfigureSettingsControlButton(backButton, new Vector2(SettingsContentWidth, SettingsPrimaryButtonHeight), 32);
            _resetConfirmPanel = CreateResetConfirmPanel(panel.transform);
            _resetConfirmPanel.SetActive(false);
            _endlessResetConfirmPanel = CreateEndlessResetConfirmPanel(panel.transform);
            _endlessResetConfirmPanel.SetActive(false);
            ShowSettingsTab(SettingsTab.General);
            return panel;
        }

        // Builds the reset confirmation overlay so progress cannot be wiped by one tap.
        private GameObject CreateResetConfirmPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "ResetConfirmPanel", ScreenPanelColor(0.86f));
            CreateLocalizedText(panel.transform, "ResetConfirmText", new Vector2(0f, 70f), TextAnchor.MiddleCenter, 38, new Vector2(860f, 160f), LocalizationKey.ResetProgressConfirm);
            CreateLocalizedButton(panel.transform, "ResetConfirmYesButton", new Vector2(-250f, -90f), LocalizationKey.Reset, () => _onResetProgress?.Invoke());
            CreateLocalizedButton(panel.transform, "ResetConfirmNoButton", new Vector2(250f, -90f), LocalizationKey.Cancel, () => _resetConfirmPanel.SetActive(false));
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
            CreateLocalizedText(panel.transform, "EndlessResetConfirmText", new Vector2(0f, 70f), TextAnchor.MiddleCenter, 38, new Vector2(860f, 160f), LocalizationKey.ResetEndlessConfirm);
            CreateLocalizedButton(panel.transform, "EndlessResetConfirmYesButton", new Vector2(-250f, -90f), LocalizationKey.ResetRecords, () => _onResetEndlessRecords?.Invoke());
            CreateLocalizedButton(panel.transform, "EndlessResetConfirmNoButton", new Vector2(250f, -90f), LocalizationKey.Cancel, () => _endlessResetConfirmPanel.SetActive(false));
            return panel;
        }

        // Shows the Endless reset confirmation overlay on top of Settings.
        private void ShowEndlessResetConfirm()
        {
            _resetConfirmPanel.SetActive(false);
            _endlessResetConfirmPanel.SetActive(true);
        }

        // Updates Settings button labels from the current PlayerPrefs values.
        private void RefreshSettingsLabels()
        {
            if (_musicButtonText != null)
            {
                _musicButtonText.text = LocalizationManager.T(LocalizationKey.Music) + " " + LocalizationManager.T(GameSettings.MusicEnabled ? LocalizationKey.On : LocalizationKey.Off);
            }

            if (_musicVolumeButtonText != null)
            {
                _musicVolumeButtonText.text = LocalizationManager.T(LocalizationKey.MusicVolume) + " " + VolumePercent(GameSettings.MusicVolume);
            }

            if (_sfxButtonText != null)
            {
                _sfxButtonText.text = LocalizationManager.T(LocalizationKey.Sfx) + " " + LocalizationManager.T(GameSettings.SfxEnabled ? LocalizationKey.On : LocalizationKey.Off);
            }

            if (_sfxVolumeButtonText != null)
            {
                _sfxVolumeButtonText.text = LocalizationManager.T(LocalizationKey.SfxVolume) + " " + VolumePercent(GameSettings.SfxVolume);
            }

            if (_cameraShakeButtonText != null)
            {
                _cameraShakeButtonText.text = LocalizationManager.T(LocalizationKey.CameraShake) + " " + LocalizationManager.T(GameSettings.CameraShakeEnabled ? LocalizationKey.On : LocalizationKey.Off);
            }

            if (_colorAssistButtonText != null)
            {
                _colorAssistButtonText.text = LocalizationManager.T(LocalizationKey.ColorAssist) + " " + LocalizationManager.T(GameSettings.ColorAssistEnabled ? LocalizationKey.On : LocalizationKey.Off);
            }

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
            _musicVolumeButtonText.text = LocalizationManager.T(LocalizationKey.MusicVolume) + " " + VolumePercent(clamped);
            _onSetMusicVolume?.Invoke(clamped);
        }

        // Applies a dragged SFX volume value to labels and game settings immediately.
        private void HandleSfxVolumeChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            _sfxVolumeButtonText.text = LocalizationManager.T(LocalizationKey.SfxVolume) + " " + VolumePercent(clamped);
            _onSetSfxVolume?.Invoke(clamped);
        }

        // Builds the first-run Stage 1 tutorial confirmation panel.
        private GameObject CreateTutorialPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "TutorialPanel", ScreenPanelColor(0.95f));
            CreateLocalizedText(panel.transform, "TutorialTitleText", new Vector2(0f, 360f), TextAnchor.MiddleCenter, 64, new Vector2(900f, 100f), LocalizationKey.TutorialTitle);
            CreateLocalizedText(panel.transform, "TutorialBodyText", new Vector2(0f, 95f), TextAnchor.MiddleLeft, 40, new Vector2(850f, 420f), LocalizationKey.TutorialBody);
            CreateLocalizedButton(panel.transform, "TutorialOkButton", new Vector2(0f, -315f), LocalizationKey.Confirm, () => _onTutorialOk?.Invoke());
            return panel;
        }

        // Builds the compact in-game HUD as a neon arcade card with transient toast messaging.
        private GameObject CreateHudPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "HudPanel", new Color(0f, 0f, 0f, 0f));
            GameObject infoPanel = CreateAnchoredPanel(
                panel.transform,
                "HudInfoPanel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(572f, 374f),
                HudPanelColor(0.74f));
            CreateHudImage(infoPanel.transform, "HudTopAccent", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(572f, 4f), VisualTheme.Current().HudAccentColor);
            CreateHudImage(infoPanel.transform, "HudLeftAccent", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 374f), VisualTheme.Current().HudAccentColor);
            CreateHudImage(infoPanel.transform, "HudBottomAccent", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -370f), new Vector2(572f, 3f), new Color(0.85f, 0.25f, 1f, 0.72f));
            CreateHudImage(infoPanel.transform, "HudScoreDivider", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -154f), new Vector2(524f, 2f), new Color(1f, 1f, 1f, 0.13f));

            _hudModeText = CreateAnchoredText(infoPanel.transform, "HudModeText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f), TextAnchor.UpperLeft, 24, new Vector2(220f, 34f), string.Empty);
            _hudModeText.color = VisualTheme.Current().HudAccentColor;
            AddTextShadow(_hudModeText);
            _hudScoreLabelText = CreateLocalizedAnchoredText(infoPanel.transform, "HudScoreLabelText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -52f), TextAnchor.UpperLeft, 22, new Vector2(160f, 30f), LocalizationKey.ScoreUpper);
            _hudScoreLabelText.color = new Color(1f, 1f, 1f, 0.72f);
            AddTextShadow(_hudScoreLabelText);
            _scoreText = CreateAnchoredText(infoPanel.transform, "HudScoreValueText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -76f), TextAnchor.UpperLeft, 62, new Vector2(260f, 74f), string.Empty);
            AddTextShadow(_scoreText);

            _hudTargetText = CreateAnchoredText(infoPanel.transform, "HudTargetText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -162f), TextAnchor.UpperLeft, 26, new Vector2(524f, 34f), string.Empty);
            AddTextShadow(_hudTargetText);
            CreateHudImage(infoPanel.transform, "HudProgressTrack", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -205f), new Vector2(346f, 15f), new Color(1f, 1f, 1f, 0.12f));
            _hudProgressFill = CreateHudImage(infoPanel.transform, "HudProgressFill", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -205f), new Vector2(0f, 15f), VisualTheme.Current().HudAccentColor);
            _hudProgressText = CreateAnchoredText(infoPanel.transform, "HudProgressText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(386f, -197f), TextAnchor.UpperLeft, 22, new Vector2(170f, 32f), string.Empty);
            _hudProgressText.color = new Color(1f, 0.90f, 0.28f, 1f);
            AddTextShadow(_hudProgressText);

            _hudMistakeLabelText = CreateLocalizedAnchoredText(infoPanel.transform, "HudMistakeLabelText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -244f), TextAnchor.UpperLeft, 24, new Vector2(92f, 34f), LocalizationKey.Chances);
            _hudMistakeLabelText.color = new Color(1f, 1f, 1f, 0.72f);
            AddTextShadow(_hudMistakeLabelText);
            _hudMistakeIconsText = CreateAnchoredText(infoPanel.transform, "HudMistakeIconsText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(106f, -239f), TextAnchor.UpperLeft, 34, new Vector2(210f, 46f), string.Empty);
            AddTextShadow(_hudMistakeIconsText);

            _hudColorChip = CreateHudImage(infoPanel.transform, "HudCurrentColorChip", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -306f), new Vector2(48f, 48f), Color.white);
            _hudShapeGlyphText = CreateAnchoredText(infoPanel.transform, "HudCurrentShapeGlyph", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -306f), TextAnchor.MiddleCenter, 30, new Vector2(48f, 48f), string.Empty);
            _hudShapeGlyphText.color = Color.black;
            _hudCurrentText = CreateAnchoredText(infoPanel.transform, "HudCurrentText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -302f), TextAnchor.UpperLeft, 27, new Vector2(450f, 48f), string.Empty);
            AddTextShadow(_hudCurrentText);

            _messageToastPanel = CreateAnchoredPanel(
                panel.transform,
                "HudToastPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 190f),
                new Vector2(760f, 118f),
                HudPanelColor(0.62f));
            CreateHudImage(_messageToastPanel.transform, "HudToastAccent", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(760f, 4f), VisualTheme.Current().HudAccentColor);
            _messageText = CreateAnchoredText(_messageToastPanel.transform, "MessageText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), TextAnchor.MiddleCenter, 34, new Vector2(700f, 86f), string.Empty);
            AddTextShadow(_messageText);
            _messageToastPanel.SetActive(false);
            _debugText = CreateText(panel.transform, "DebugText", new Vector2(-32f, 32f), TextAnchor.LowerRight, 28, new Vector2(450f, 100f), string.Empty);
            AddTextShadow(_debugText);
            _hintText = CreateText(panel.transform, "HintText", new Vector2(0f, -150f), TextAnchor.UpperCenter, 30, new Vector2(960f, 90f), string.Empty);
            AddTextShadow(_hintText);
            _comboBadgePanel = CreateAnchoredPanel(
                panel.transform,
                "ComboBadgePanel",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-32f, 34f),
                new Vector2(132f, 78f),
                HudPanelColor(0.58f));
            CreateHudImage(_comboBadgePanel.transform, "ComboBadgeAccent", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(132f, 4f), new Color(1f, 0.90f, 0.28f, 0.92f));
            _hudComboText = CreateAnchoredText(_comboBadgePanel.transform, "ComboBadgeText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -3f), TextAnchor.MiddleCenter, 42, new Vector2(116f, 60f), "x1");
            _hudComboText.color = new Color(1f, 1f, 1f, 0.82f);
            AddTextShadow(_hudComboText);
            CreateAnchoredButton(
                panel.transform,
                "PauseButton",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-32f, -32f),
                new Vector2(96f, 76f),
                "Ⅱ",
                40,
                () => _onPause?.Invoke());
            return panel;
        }

        // Builds the pause menu with resume and run navigation actions.
        private GameObject CreatePausePanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "PausePanel", ScreenPanelColor(0.94f));
            CreateLocalizedText(panel.transform, "PauseTitleText", new Vector2(0f, 330f), TextAnchor.MiddleCenter, 76, new Vector2(860f, 120f), LocalizationKey.Pause);
            _pauseStageText = CreateText(panel.transform, "PauseStageText", new Vector2(0f, 215f), TextAnchor.MiddleCenter, 42, new Vector2(760f, 80f), string.Empty);
            _pauseScoreText = CreateText(panel.transform, "PauseScoreText", new Vector2(0f, 140f), TextAnchor.MiddleCenter, 38, new Vector2(760f, 80f), string.Empty);
            CreateLocalizedButton(panel.transform, "ResumeButton", new Vector2(0f, 25f), LocalizationKey.Resume, () => _onResume?.Invoke());
            CreateLocalizedButton(panel.transform, "PauseRetryButton", new Vector2(0f, -105f), LocalizationKey.TryAgain, () => _onRestart?.Invoke());
            CreateLocalizedButton(panel.transform, "PauseStageSelectButton", new Vector2(0f, -235f), LocalizationKey.StageSelect, () => _onStageSelect?.Invoke());
            CreateLocalizedButton(panel.transform, "PauseMainMenuButton", new Vector2(0f, -365f), LocalizationKey.MainMenu, () => _onMainMenu?.Invoke());
            return panel;
        }

        // Builds the result panel with restart and main-menu actions.
        private GameObject CreateResultPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "ResultPanel", ScreenPanelColor(0.92f));
            _resultTitleText = CreateText(panel.transform, "ResultTitleText", new Vector2(0f, 280f), TextAnchor.MiddleCenter, 82, new Vector2(860f, 130f), string.Empty);
            _resultScoreText = CreateText(panel.transform, "ResultScoreText", new Vector2(0f, 150f), TextAnchor.MiddleCenter, 48, new Vector2(860f, 100f), string.Empty);
            _resultInfoText = CreateText(panel.transform, "ResultInfoText", new Vector2(0f, 10f), TextAnchor.MiddleCenter, 34, new Vector2(860f, 120f), string.Empty);
            Button restartButton = CreateButton(panel.transform, "RestartButton", new Vector2(0f, -145f), LocalizationManager.T(LocalizationKey.Restart), () => _onRestart?.Invoke());
            _restartButtonText = restartButton.GetComponentInChildren<Text>();
            _nextStageButton = CreateLocalizedButton(panel.transform, "NextStageButton", new Vector2(0f, -265f), LocalizationKey.NextStage, () => _onNextStage?.Invoke());
            CreateLocalizedButton(panel.transform, "ResultStageButton", new Vector2(-250f, -390f), LocalizationKey.StageSelect, () => _onStageSelect?.Invoke());
            CreateLocalizedButton(panel.transform, "ResultMenuButton", new Vector2(250f, -390f), LocalizationKey.MainMenu, () => _onMainMenu?.Invoke());
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

        // Creates an anchored image rectangle for HUD cards, separators, chips, and accent lines.
        private static Image CreateHudImage(
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
            return image;
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

        // Creates a standard text label that updates from the active localization table.
        private static Text CreateLocalizedText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size, LocalizationKey key)
        {
            Text text = CreateText(parent, name, anchoredPosition, anchor, fontSize, size, LocalizationManager.T(key));
            AttachLocalizedText(text, key);
            return text;
        }

        // Creates a HUD-anchored text label that updates from the active localization table.
        private static Text CreateLocalizedAnchoredText(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            TextAnchor alignment,
            int fontSize,
            Vector2 size,
            LocalizationKey key)
        {
            Text text = CreateAnchoredText(parent, name, anchor, pivot, anchoredPosition, alignment, fontSize, size, LocalizationManager.T(key));
            AttachLocalizedText(text, key);
            return text;
        }

        // Creates a text button whose generated label updates from the active localization table.
        private static Button CreateLocalizedButton(Transform parent, string name, Vector2 anchoredPosition, LocalizationKey key, Action onClick)
        {
            Button button = CreateButton(parent, name, anchoredPosition, LocalizationManager.T(key), onClick);
            AttachLocalizedText(button.GetComponentInChildren<Text>(), key);
            return button;
        }

        // Creates one compact Settings tab button using the same generated uGUI style as other controls.
        private Button CreateSettingsTabButton(Transform parent, string name, Vector2 anchoredPosition, LocalizationKey key, SettingsTab tab)
        {
            Button button = CreateLocalizedButton(parent, name, anchoredPosition, key, () => ShowSettingsTab(tab));
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(SettingsTabWidth, 70f);
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 30;
            }

            return button;
        }

        // Resizes a Settings-only control button without changing the shared button style used by other screens.
        private static void ConfigureSettingsControlButton(Button button, Vector2 size, int fontSize)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = size;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = fontSize;
                RectTransform textRect = text.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.sizeDelta = size;
                }
            }
        }

        // Creates an empty Settings section root under the tab strip.
        private static GameObject CreateSettingsSection(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -48f);
            rect.sizeDelta = new Vector2(720f, 700f);
            return go;
        }

        // Creates a Settings-only option group so toggle, label, and slider spacing cannot overlap.
        private static GameObject CreateSettingsOptionGroup(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return go;
        }

        // Attaches the lightweight localization bridge to a generated uGUI Text element.
        private static void AttachLocalizedText(Text text, LocalizationKey key)
        {
            if (text == null)
            {
                return;
            }

            LocalizedText localizedText = text.GetComponent<LocalizedText>();
            if (localizedText == null)
            {
                localizedText = text.gameObject.AddComponent<LocalizedText>();
            }

            localizedText.Configure(key);
        }

        // Creates a horizontal uGUI slider for precise settings values without external sprites.
        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, float value, Action<float> onValueChanged)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(SettingsContentWidth, SettingsSliderHeight);
            rect.anchoredPosition = anchoredPosition;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            RectTransform backgroundRect = CreateSliderImage(go.transform, "Background", new Vector2(0f, 0f), new Vector2(SettingsContentWidth, SettingsSliderTrackHeight), HudPanelColor(0.74f)).rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -SettingsSliderTrackHeight * 0.5f);
            backgroundRect.offsetMax = new Vector2(0f, SettingsSliderTrackHeight * 0.5f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(0f, 0f);
            fillAreaRect.offsetMax = new Vector2(0f, 0f);
            Image fill = CreateSliderImage(fillArea.transform, "Fill", Vector2.zero, new Vector2(0f, SettingsSliderTrackHeight), ButtonColor());
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            fill.rectTransform.offsetMin = new Vector2(0f, -SettingsSliderTrackHeight * 0.5f);
            fill.rectTransform.offsetMax = new Vector2(0f, SettingsSliderTrackHeight * 0.5f);

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(0f, 0f);
            handleAreaRect.offsetMax = new Vector2(0f, 0f);
            Image handle = CreateSliderImage(handleArea.transform, "Handle", Vector2.zero, new Vector2(SettingsSliderHandleWidth, SettingsSliderHandleHeight), VisualTheme.Current().HudTextColor);

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
            image.color = HudPanelColor(0.78f);
            CreateHudImage(go.transform, name + "TopAccent", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(size.x, 4f), VisualTheme.Current().HudAccentColor);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(go.transform, name + "Text", Vector2.zero, TextAnchor.MiddleCenter, fontSize, size, label);
            text.color = VisualTheme.Current().HudTextColor;
            AddTextShadow(text);
            return button;
        }

        // Creates an anchored uGUI text element when HUD layout needs explicit anchor and pivot control.
        private static Text CreateAnchoredText(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            TextAnchor alignment,
            int fontSize,
            Vector2 size,
            string initialText)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = BuiltinFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = VisualTheme.Current().HudTextColor;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.text = initialText;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
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
            text.supportRichText = true;

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
