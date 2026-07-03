using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ColorGateRush
{
    public sealed class GameManager : MonoBehaviour
    {
        private enum GameState
        {
            MainMenu,
            StageSelect,
            Rules,
            Settings,
            Tutorial,
            Playing,
            Paused,
            Completed,
            Failed
        }

        public static GameManager Instance { get; private set; }

        [SerializeField] private int seed = 12345;

        private GameState _state = GameState.MainMenu;
        private LevelGenerator _levelGenerator;
        private RuntimeUi _ui;
        private ProceduralAudio _audio;
        private StageManager _stageManager;
        private StageConfig _currentStage;
        private StageResult _lastStageResult;
        private int _score;
        private int _combo;
        private LaneRunnerController _runner;

        public bool IsRunning => _state == GameState.Playing;
        public int CurrentSeed => seed;

        // Initializes the singleton and required runtime systems on the scene systems object.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 60;
            _levelGenerator = GetComponent<LevelGenerator>();
            if (_levelGenerator == null)
            {
                _levelGenerator = gameObject.AddComponent<LevelGenerator>();
            }

            _ui = GetComponent<RuntimeUi>();
            if (_ui == null)
            {
                _ui = gameObject.AddComponent<RuntimeUi>();
            }

            _audio = GetComponent<ProceduralAudio>();
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<ProceduralAudio>();
            }

            _stageManager = new StageManager();
            _currentStage = _stageManager.GetStageConfig(_stageManager.SelectedStageIndex);
            _ui.Configure(
                ShowStageSelect,
                ShowStageSelect,
                ShowRules,
                ShowSettings,
                RestartCurrentRun,
                ReturnToMainMenu,
                StartNextStage,
                StartStageFromSelect,
                PauseGame,
                ResumeGame,
                ToggleSound,
                ToggleCameraShake,
                ToggleColorAssist,
                ResetLocalProgress,
                DismissTutorial);
        }

        // Starts in the main menu instead of immediately running gameplay.
        private void Start()
        {
            ReturnToMainMenu();
        }

        // Routes explicit keyboard commands for pause, resume, retry, and menu navigation.
        private void Update()
        {
            if (_state == GameState.Playing)
            {
                if (WasPausePressed())
                {
                    PauseGame();
                }
            }
            else if (_state == GameState.Paused)
            {
                HandlePausedInput();
            }
            else if (_state == GameState.Failed || _state == GameState.Completed)
            {
                HandleRestartInput();
            }
            else if ((_state == GameState.Rules || _state == GameState.Settings) && WasBackPressed())
            {
                ReturnToMainMenu();
            }
        }

        // Clears the singleton only when this manager instance is being destroyed.
        private void OnDestroy()
        {
            if (Instance == this)
            {
                RestoreTimeScale();
                Instance = null;
            }
        }

        // Resets score state and asks the generator to rebuild the level for the given seed.
        public void StartRun(int runSeed)
        {
            CancelInvoke();
            RestoreTimeScale();
            StageConfig stage = _stageManager != null ? _stageManager.GetStageConfig(_stageManager.SelectedStageIndex) : _currentStage;
            StartRun(stage);
        }

        // Resets score state and asks the generator to rebuild the level for the selected stage.
        public void StartRun(StageConfig stage)
        {
            CancelInvoke();
            RestoreTimeScale();
            _currentStage = stage;
            seed = stage.Seed;
            _score = 0;
            _combo = 0;
            _state = GameState.Playing;
            _runner = _levelGenerator.ClearAndGenerate(this, stage);
            _ui.ShowPlayingHud(stage, _score, _combo, _runner.CurrentColor, seed);
            _ui.ShowStageStartHint(stage);
            ShowTutorialIfNeeded(stage);
        }

        // Applies color-match scoring and feedback when the runner enters a shard trigger.
        public void HandleCollect(CollectibleShard shard, LaneRunnerController runner)
        {
            if (!IsRunning || shard == null || runner == null)
            {
                return;
            }

            if (shard.ColorId == runner.CurrentColor)
            {
                _combo = Mathf.Min(GameConstants.ComboCap, _combo + 1);
                int gain = GameConstants.SameColorShardScore * Mathf.Max(1, _combo);
                _score += gain;
                ProceduralFactory.CollectBurst(shard.transform.position, GameConstants.ToUnityColor(shard.ColorId));
                ProceduralFactory.FloatingText(shard.transform.position + Vector3.up * 0.65f, "+" + gain, GameConstants.ToUnityColor(shard.ColorId));
                _audio.PlayCollect(_combo);
                if (_combo >= 3)
                {
                    _ui.ShowMessage("콤보 x" + _combo);
                }
                Destroy(shard.gameObject);
            }
            else
            {
                _combo = 0;
                _score = Mathf.Max(0, _score - GameConstants.WrongColorShardPenalty);
                ProceduralFactory.FailBurst(shard.transform.position);
                ProceduralFactory.FloatingText(shard.transform.position + Vector3.up * 0.65f, "-" + GameConstants.WrongColorShardPenalty, Color.red);
                _ui.ShowMessage("다른 색/모양! 콤보 초기화");
                _audio.PlayWrong();
                Destroy(shard.gameObject);
            }

            UpdateHud();
        }

        // Changes the runner color and plays gate feedback when a color gate is crossed.
        public void HandleGate(ColorGate gate, LaneRunnerController runner)
        {
            if (!IsRunning || gate == null || runner == null)
            {
                return;
            }

            runner.SetColor(gate.TargetColor);
            _score += GameConstants.GateScore;
            ProceduralFactory.GateBurst(runner.transform.position + Vector3.up * 0.8f, GameConstants.ToUnityColor(gate.TargetColor));
            ProceduralFactory.FloatingText(runner.transform.position + Vector3.up * 1.3f, "색상 변경! " + GameConstants.ShapeName(gate.TargetColor), GameConstants.ToUnityColor(gate.TargetColor));
            _audio.PlayGate();
            UpdateHud();
            _ui.ShowMessage("색상 변경! 이제 " + GameConstants.GetVisualProfile(gate.TargetColor).HudLabel);
        }

        // Ends the current run as failed when the runner hits an obstacle.
        public void HandleObstacle(ObstacleBlock obstacle, LaneRunnerController runner)
        {
            if (!IsRunning)
            {
                return;
            }

            _state = GameState.Failed;
            RestoreTimeScale();
            _combo = 0;
            _score = Mathf.Max(0, _score - GameConstants.ObstaclePenalty);
            if (runner != null)
            {
                ProceduralFactory.FailBurst(runner.transform.position + Vector3.up * 0.5f);
                ProceduralFactory.FloatingText(runner.transform.position + Vector3.up * 1.1f, "실패!", Color.red);
            }
            ShakeCamera(0.18f, 0.22f);
            _audio.PlayWrong();
            UpdateHud();
            _lastStageResult = _stageManager.CreateFailedResult(_currentStage, _score);
            _ui.ShowResult(false, _currentStage, _lastStageResult, _stageManager.IsStageUnlocked(_currentStage.StageIndex + 1));

        }

        // Ends the run when the finish trigger is reached and preserves the HUD score for star rating.
        public void HandleFinish(FinishLine finishLine, LaneRunnerController runner)
        {
            if (!IsRunning)
            {
                return;
            }

            _state = GameState.Completed;
            RestoreTimeScale();
            _combo = 0;

            Vector3 burstPosition = runner != null ? runner.transform.position + Vector3.up : transform.position;
            ProceduralFactory.FinishBurst(burstPosition);
            ProceduralFactory.FloatingText(burstPosition + Vector3.up * 0.75f, "클리어!", Color.white);
            ShakeCamera(0.10f, 0.18f);
            _audio.PlayFinish();
            UpdateHud();
            _lastStageResult = _stageManager.SaveStageResult(_currentStage, _score);
            _ui.ShowResult(true, _currentStage, _lastStageResult, _stageManager.IsStageUnlocked(_currentStage.StageIndex + 1));

        }

        // Opens the stage select panel from the main menu or result screen.
        private void ShowStageSelect()
        {
            CancelInvoke();
            RestoreTimeScale();
            _state = GameState.StageSelect;
            ClearCurrentLevel();
            _ui.ShowStageSelect(_stageManager.Stages, _stageManager.UnlockedStage, _stageManager.SelectedStageIndex, GetBestStarsForUi);
        }

        // Opens the rules panel from the main menu.
        private void ShowRules()
        {
            CancelInvoke();
            RestoreTimeScale();
            _state = GameState.Rules;
            ClearCurrentLevel();
            _ui.ShowRules();
        }

        // Opens the settings panel from the main menu without starting gameplay.
        private void ShowSettings()
        {
            CancelInvoke();
            RestoreTimeScale();
            _state = GameState.Settings;
            ClearCurrentLevel();
            _ui.ShowSettings();
        }

        // Restarts the current stage from result or pause screens.
        private void RestartCurrentRun()
        {
            StartRun(_currentStage);
        }

        // Stops gameplay, clears generated level content, and returns to the main menu.
        private void ReturnToMainMenu()
        {
            CancelInvoke();
            RestoreTimeScale();
            _state = GameState.MainMenu;
            ClearCurrentLevel();
            _ui.ShowMainMenu();
        }

        // Toggles procedural sound playback and refreshes the settings panel labels.
        private void ToggleSound()
        {
            GameSettings.SetBool(GameSettings.SoundEnabledKey, !GameSettings.SoundEnabled);
            _ui.ShowSettings();
        }

        // Toggles short camera shake feedback and refreshes the settings panel labels.
        private void ToggleCameraShake()
        {
            GameSettings.SetBool(GameSettings.CameraShakeEnabledKey, !GameSettings.CameraShakeEnabled);
            _ui.ShowSettings();
        }

        // Toggles color-assist visuals and refreshes the settings panel labels.
        private void ToggleColorAssist()
        {
            GameSettings.SetBool(GameSettings.ColorAssistEnabledKey, !GameSettings.ColorAssistEnabled);
            _ui.ShowSettings();
        }

        // Resets only CGR-prefixed local progress and returns settings to the default stage state.
        private void ResetLocalProgress()
        {
            GameSettings.ResetLocalProgress();
            _stageManager = new StageManager();
            _currentStage = _stageManager.GetStageConfig(_stageManager.SelectedStageIndex);
            _ui.ShowSettings();
        }

        // Pauses the active run without saving stars or changing stage progress.
        private void PauseGame()
        {
            if (_state != GameState.Playing)
            {
                return;
            }

            _state = GameState.Paused;
            Time.timeScale = 0f;
            _ui.ShowPauseMenu(_currentStage, _score);
        }

        // Resumes the paused run and restores normal time flow.
        private void ResumeGame()
        {
            if (_state != GameState.Paused)
            {
                return;
            }

            RestoreTimeScale();
            _state = GameState.Playing;
            ColorId color = _runner != null ? _runner.CurrentColor : ColorId.Cyan;
            _ui.ShowPlayingHud(_currentStage, _score, _combo, color, seed);
            _ui.ShowMessage(string.Empty);
        }

        // Shows the Stage 1 tutorial once before movement begins.
        private void ShowTutorialIfNeeded(StageConfig stage)
        {
            if (stage.StageIndex != 1 || GameSettings.TutorialSeen)
            {
                return;
            }

            _state = GameState.Tutorial;
            Time.timeScale = 0f;
            _ui.ShowTutorial();
        }

        // Dismisses the first-run tutorial and resumes the current Stage 1 run.
        private void DismissTutorial()
        {
            if (_state != GameState.Tutorial)
            {
                return;
            }

            GameSettings.MarkTutorialSeen();
            RestoreTimeScale();
            _state = GameState.Playing;
            ColorId color = _runner != null ? _runner.CurrentColor : ColorId.Cyan;
            _ui.ShowPlayingHud(_currentStage, _score, _combo, color, seed);
            _ui.ShowMessage("같은 색/모양 샤드를 모으세요");
        }

        // Starts an unlocked stage and persists it as the selected stage.
        private void StartStage(int stageIndex)
        {
            if (!_stageManager.SelectStage(stageIndex))
            {
                ShowStageSelect();
                return;
            }

            StartRun(_stageManager.GetStageConfig(stageIndex));
        }

        // Starts a stage selected from the stage select UI.
        private void StartStageFromSelect(int stageIndex)
        {
            StartStage(stageIndex);
        }

        // Starts the next stage only when it is unlocked.
        private void StartNextStage()
        {
            int nextStage = _currentStage.StageIndex + 1;
            if (nextStage <= StageManager.TotalStageCount && _stageManager.IsStageUnlocked(nextStage))
            {
                StartStage(nextStage);
            }
        }

        // Returns saved stars for the stage select UI.
        private int GetBestStarsForUi(int stageIndex)
        {
            return _stageManager.GetBestStars(stageIndex);
        }

        // Clears generated level content and detaches the follow camera from any previous runner.
        private void ClearCurrentLevel()
        {
            _runner = null;
            if (_levelGenerator != null)
            {
                _levelGenerator.ClearGeneratedLevel();
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                CameraFollow follow = camera.GetComponent<CameraFollow>();
                if (follow != null)
                {
                    follow.SetTarget(null);
                }
            }
        }

        // Removes the temporary start prompt after the run is underway.
        private void ClearMessage()
        {
            if (IsRunning)
            {
                _ui.ClearMessage();
            }
        }

        // Refreshes the runtime HUD with score, combo, color, and seed.
        private void UpdateHud()
        {
            ColorId color = _runner != null ? _runner.CurrentColor : ColorId.Cyan;
            _ui.SetHud(_score, _combo, color, seed);
        }

        // Triggers a small camera shake when the active camera has the follow component.
        private static void ShakeCamera(float strength, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow != null)
            {
                follow.Shake(strength, duration);
            }
        }

        // Restores normal time flow when leaving pause or starting another screen.
        private static void RestoreTimeScale()
        {
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                Time.timeScale = 1f;
            }
        }

        // Handles pause-menu keyboard shortcuts without touching stage progress.
        private void HandlePausedInput()
        {
            if (WasPausePressed())
            {
                ResumeGame();
                return;
            }

            if (WasRetryPressed(includeSpace: false))
            {
                RestartCurrentRun();
                return;
            }

            if (WasMainMenuPressed())
            {
                ReturnToMainMenu();
            }
        }

        // Checks explicit keyboard retry keys without enabling global tap restart on result screens.
        private void HandleRestartInput()
        {
            if (WasRetryPressed(includeSpace: true))
            {
                RestartCurrentRun();
            }
        }

        // Checks ESC or P through the enabled Unity input backend.
        private static bool WasPausePressed()
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            pressed |= keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            pressed |= Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif

            return pressed;
        }

        // Checks Escape for non-gameplay back navigation.
        private static bool WasBackPressed()
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            pressed |= keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            pressed |= Input.GetKeyDown(KeyCode.Escape);
#endif

            return pressed;
        }

        // Checks retry keys, optionally including Space for result screens.
        private static bool WasRetryPressed(bool includeSpace)
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            pressed |= keyboard != null && (keyboard.rKey.wasPressedThisFrame || (includeSpace && keyboard.spaceKey.wasPressedThisFrame));
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            pressed |= Input.GetKeyDown(KeyCode.R) || (includeSpace && Input.GetKeyDown(KeyCode.Space));
#endif

            return pressed;
        }

        // Checks M for returning from pause to the main menu.
        private static bool WasMainMenuPressed()
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            pressed |= keyboard != null && keyboard.mKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            pressed |= Input.GetKeyDown(KeyCode.M);
#endif

            return pressed;
        }
    }
}
