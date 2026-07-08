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
            PlaytestStats,
            Tutorial,
            Playing,
            Paused,
            Completed,
            Failed
        }

        private enum GameMode
        {
            Stage,
            Endless
        }

        public static GameManager Instance { get; private set; }

        [SerializeField] private int seed = 12345;

        private GameState _state = GameState.MainMenu;
        private GameMode _mode = GameMode.Stage;
        private LevelGenerator _levelGenerator;
        private RuntimeUi _ui;
        private ProceduralAudio _audio;
        private StageManager _stageManager;
        private StageConfig _currentStage;
        private StageResult _lastStageResult;
        private int _score;
        private int _combo;
        private LaneRunnerController _runner;
        private float _runStartedAt;
        private bool _runStatsOpen;
        private readonly EndlessRunConfig _endlessConfig = EndlessRunConfig.CreateDefault();
        private float _endlessDistance;
        private float _endlessElapsedTime;
        private float _endlessSpeedMultiplier = 1f;
        private int _wrongShardCount;
        private int _lastEndlessDistanceHud = -1;

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
                ToggleMusic,
                ToggleSfx,
                SetMusicVolume,
                SetSfxVolume,
                ToggleCameraShake,
                ToggleColorAssist,
                ResetLocalProgress,
                ShowPlaytestStats,
                ResetPlaytestStats,
                StartEndlessRun,
                RequestQuit,
                ResetEndlessRecords,
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
                    return;
                }

                if (_mode == GameMode.Endless)
                {
                    UpdateEndlessRun();
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
            else if ((_state == GameState.Rules || _state == GameState.Settings || _state == GameState.PlaytestStats) && WasBackPressed())
            {
                ReturnToMainMenu();
            }
            else if (_state == GameState.StageSelect && WasBackPressed())
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
            RecordRunQuitIfOpen(PlaytestExitReason.Restarted);
            _mode = GameMode.Stage;
            ResetEndlessRuntimeState();
            _currentStage = stage;
            seed = stage.Seed;
            _score = 0;
            _combo = 0;
            _wrongShardCount = 0;
            _state = GameState.Playing;
            _runner = _levelGenerator.ClearAndGenerate(this, stage);
            BeginPlaytestAttempt(stage);
            _audio.PlayMusic(MusicType.Gameplay, stage.StageIndex);
            _ui.ShowPlayingHud(stage, _score, _combo, _runner.CurrentColor, seed, _wrongShardCount, GameConstants.MaxWrongShardCount);
            _ui.ShowStageStartHint(stage);
            ShowTutorialIfNeeded(stage);
        }

        // Starts Endless Mode as an independent record run with no finish, stars, or stage unlock writes.
        private void StartEndlessRun()
        {
            CancelInvoke();
            RestoreTimeScale();
            RecordRunQuitIfOpen(PlaytestExitReason.Restarted);
            _mode = GameMode.Endless;
            seed = _endlessConfig.Seed;
            _score = 0;
            _combo = 0;
            _endlessDistance = 0f;
            _endlessElapsedTime = 0f;
            _endlessSpeedMultiplier = 1f;
            _wrongShardCount = 0;
            _lastEndlessDistanceHud = -1;
            _state = GameState.Playing;
            _runner = _levelGenerator.BeginEndless(this, _endlessConfig);
            EndlessRecords.RecordAttempt();
            _audio.PlayMusic(MusicType.Gameplay, MusicStageIndex());
            _ui.ShowEndlessHud(
                _score,
                _combo,
                _runner.CurrentColor,
                _endlessDistance,
                EndlessRecords.BestScore,
                EndlessRecords.BestDistance,
                seed,
                _wrongShardCount,
                _endlessConfig.WrongShardLimit,
                _endlessSpeedMultiplier);
            _ui.ShowEndlessStartHint();
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
                _audio.PlayWrong();
                bool reachedWrongLimit = RegisterWrongShard();
                Destroy(shard.gameObject);
                if (reachedWrongLimit)
                {
                    UpdateHud();
                    if (_mode == GameMode.Endless)
                    {
                        FailEndlessRun(EndlessFailReason.WrongShardLimit);
                    }
                    else
                    {
                        FailStageRun(StageFailReason.WrongShardLimit, runner != null ? runner.transform.position + Vector3.up * 0.5f : shard.transform.position, playFeedback: false);
                    }

                    return;
                }
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

            if (_mode == GameMode.Endless)
            {
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
                _audio.PlayMusic(MusicType.Failed, MusicStageIndex());
                UpdateHud();
                EndEndlessRun(EndlessFailReason.ObstacleHit);
                return;
            }

            Vector3 feedbackPosition = runner != null ? runner.transform.position + Vector3.up * 0.5f : transform.position;
            FailStageRun(StageFailReason.ObstacleHit, feedbackPosition, playFeedback: true);
        }

        // Ends a finite Stage Mode run with a recorded fail reason and without writing stars or unlocks.
        private void FailStageRun(StageFailReason failReason, Vector3 feedbackPosition, bool playFeedback)
        {
            if (_mode != GameMode.Stage || _state != GameState.Playing)
            {
                return;
            }

            _state = GameState.Failed;
            RestoreTimeScale();
            _combo = 0;
            if (failReason == StageFailReason.ObstacleHit)
            {
                _score = Mathf.Max(0, _score - GameConstants.ObstaclePenalty);
            }

            if (playFeedback)
            {
                ProceduralFactory.FailBurst(feedbackPosition);
                ProceduralFactory.FloatingText(
                    feedbackPosition + Vector3.up * 0.6f,
                    failReason == StageFailReason.WrongShardLimit ? "다른 색 3회!" : "실패!",
                    Color.red);
                ShakeCamera(failReason == StageFailReason.WrongShardLimit ? 0.14f : 0.18f, 0.22f);
                _audio.PlayWrong();
            }

            _audio.PlayMusic(MusicType.Failed, MusicStageIndex());
            UpdateHud();
            _lastStageResult = _stageManager.CreateFailedResult(_currentStage, _score, failReason);
            RecordRunFailed(failReason, _wrongShardCount);
            _ui.ShowResult(false, _currentStage, _lastStageResult, _stageManager.IsStageUnlocked(_currentStage.StageIndex + 1));
        }

        // Ends the run when the finish trigger is reached and preserves the HUD score for star rating.
        public void HandleFinish(FinishLine finishLine, LaneRunnerController runner)
        {
            if (!IsRunning || _mode == GameMode.Endless)
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
            _audio.PlayMusic(MusicType.Completed, MusicStageIndex());
            UpdateHud();
            _lastStageResult = _stageManager.SaveStageResult(_currentStage, _score);
            RecordRunCompleted(_lastStageResult);
            _ui.ShowResult(true, _currentStage, _lastStageResult, _stageManager.IsStageUnlocked(_currentStage.StageIndex + 1));

        }

        // Opens the stage select panel from the main menu or result screen.
        private void ShowStageSelect()
        {
            CancelInvoke();
            RestoreTimeScale();
            RecordRunQuitIfOpen(PlaytestExitReason.QuitToStageSelect);
            _mode = GameMode.Stage;
            ResetEndlessRuntimeState();
            _state = GameState.StageSelect;
            ClearCurrentLevel();
            _audio.PlayMusic(MusicType.Menu);
            _ui.ShowStageSelect(_stageManager.Stages, _stageManager.UnlockedStage, _stageManager.SelectedStageIndex, GetBestStarsForUi);
        }

        // Opens the rules panel from the main menu.
        private void ShowRules()
        {
            CancelInvoke();
            RestoreTimeScale();
            _state = GameState.Rules;
            ClearCurrentLevel();
            _audio.PlayMusic(MusicType.Menu);
            _ui.ShowRules();
        }

        // Opens the settings panel from the main menu without starting gameplay.
        private void ShowSettings()
        {
            CancelInvoke();
            RestoreTimeScale();
            _state = GameState.Settings;
            ClearCurrentLevel();
            _audio.PlayMusic(MusicType.Menu);
            _ui.ShowSettings();
        }

        // Opens the local-only playtest stats panel without mutating stage progress.
        private void ShowPlaytestStats()
        {
            CancelInvoke();
            RestoreTimeScale();
            _state = GameState.PlaytestStats;
            ClearCurrentLevel();
            _audio.PlayMusic(MusicType.Menu);
            _ui.ShowPlaytestStats(StageManager.TotalStageCount);
        }

        // Restarts the current stage from result or pause screens.
        private void RestartCurrentRun()
        {
            if (_mode == GameMode.Endless)
            {
                StartEndlessRun();
                return;
            }

            StartRun(_currentStage);
        }

        // Stops gameplay, clears generated level content, and returns to the main menu.
        private void ReturnToMainMenu()
        {
            CancelInvoke();
            RestoreTimeScale();
            RecordRunQuitIfOpen(PlaytestExitReason.QuitToMainMenu);
            _mode = GameMode.Stage;
            ResetEndlessRuntimeState();
            _state = GameState.MainMenu;
            ClearCurrentLevel();
            _audio.PlayMusic(MusicType.Menu);
            _ui.ShowMainMenu();
        }

        // Toggles legacy sound playback and keeps split music/SFX settings in sync for old saves.
        private void ToggleSound()
        {
            bool enabled = !GameSettings.SoundEnabled;
            GameSettings.SetBool(GameSettings.SoundEnabledKey, enabled);
            GameSettings.SetBool(GameSettings.MusicEnabledKey, enabled);
            GameSettings.SetBool(GameSettings.SfxEnabledKey, enabled);
            _audio.RefreshSettings();
            if (enabled)
            {
                _audio.PlayMusic(_state == GameState.Playing ? MusicType.Gameplay : MusicType.Menu, MusicStageIndex());
            }

            _ui.ShowSettings();
        }

        // Toggles looped BGM without changing procedural one-shot SFX.
        private void ToggleMusic()
        {
            GameSettings.SetBool(GameSettings.MusicEnabledKey, !GameSettings.MusicEnabled);
            _audio.RefreshSettings();
            if (GameSettings.MusicEnabled)
            {
                _audio.PlayMusic(_state == GameState.Playing ? MusicType.Gameplay : MusicType.Menu, MusicStageIndex());
            }

            _ui.ShowSettings();
        }

        // Toggles procedural one-shot sound effects without changing music.
        private void ToggleSfx()
        {
            GameSettings.SetBool(GameSettings.SfxEnabledKey, !GameSettings.SfxEnabled);
            _audio.RefreshSettings();
            _ui.ShowSettings();
        }

        // Stores a precise music volume value from the Settings slider and applies it immediately.
        private void SetMusicVolume(float value)
        {
            GameSettings.SetVolume(GameSettings.MusicVolumeKey, value);
            _audio.RefreshSettings();
        }

        // Stores a precise SFX volume value from the Settings slider for future one-shot sounds.
        private void SetSfxVolume(float value)
        {
            GameSettings.SetVolume(GameSettings.SfxVolumeKey, value);
            _audio.RefreshSettings();
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

        // Clears only CGR_Stats_ playtest counters and refreshes the stats panel.
        private void ResetPlaytestStats()
        {
            PlaytestStats.ResetAll(StageManager.TotalStageCount);
            _ui.ShowPlaytestStats(StageManager.TotalStageCount);
        }

        // Clears only Endless record keys and keeps stage progress, settings, and playtest stats intact.
        private void ResetEndlessRecords()
        {
            EndlessRecords.Reset();
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
            _audio.SetMusicDucked(true);
            if (_mode == GameMode.Endless)
            {
                _ui.ShowEndlessPauseMenu(_score, _endlessDistance);
            }
            else
            {
                _ui.ShowPauseMenu(_currentStage, _score);
            }
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
            _audio.SetMusicDucked(false);
            ColorId color = _runner != null ? _runner.CurrentColor : ColorId.Cyan;
            if (_mode == GameMode.Endless)
            {
                _ui.ShowEndlessHud(
                    _score,
                    _combo,
                    color,
                    _endlessDistance,
                    EndlessRecords.BestScore,
                    EndlessRecords.BestDistance,
                    seed,
                    _wrongShardCount,
                    _endlessConfig.WrongShardLimit,
                    _endlessSpeedMultiplier);
            }
            else
            {
                _ui.ShowPlayingHud(_currentStage, _score, _combo, color, seed, _wrongShardCount, GameConstants.MaxWrongShardCount);
            }
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
            _audio.SetMusicDucked(true);
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
            _audio.SetMusicDucked(false);
            ColorId color = _runner != null ? _runner.CurrentColor : ColorId.Cyan;
            _ui.ShowPlayingHud(_currentStage, _score, _combo, color, seed, _wrongShardCount, GameConstants.MaxWrongShardCount);
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

        // Clears transient Endless counters when leaving the mode so later Stage runs cannot inherit them.
        private void ResetEndlessRuntimeState()
        {
            _endlessDistance = 0f;
            _endlessElapsedTime = 0f;
            _endlessSpeedMultiplier = 1f;
            _wrongShardCount = 0;
            _lastEndlessDistanceHud = -1;
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
            if (_mode == GameMode.Endless)
            {
                _ui.SetEndlessHud(
                    _score,
                    _combo,
                    color,
                    _endlessDistance,
                    EndlessRecords.BestScore,
                    EndlessRecords.BestDistance,
                    seed,
                    _wrongShardCount,
                    _endlessConfig.WrongShardLimit,
                    _endlessSpeedMultiplier);
            }
            else
            {
                _ui.SetHud(_score, _combo, color, seed, _wrongShardCount, GameConstants.MaxWrongShardCount);
            }
        }

        // Updates Endless distance, rolling generation, and compact HUD values while gameplay is running.
        private void UpdateEndlessRun()
        {
            if (_runner == null)
            {
                return;
            }

            _endlessElapsedTime += Time.deltaTime;
            _endlessDistance = Mathf.Max(0f, _runner.transform.position.z);
            float currentSpeed = _endlessConfig.ForwardSpeed(_endlessElapsedTime, _endlessDistance);
            _endlessSpeedMultiplier = _endlessConfig.SpeedMultiplier(_endlessElapsedTime, _endlessDistance);
            _runner.SetForwardSpeedRange(currentSpeed, currentSpeed);
            _runner.SetLaneMoveSharpness(_endlessConfig.LaneMoveSharpness(_endlessElapsedTime, _endlessDistance));
            _levelGenerator.UpdateEndlessGeneration(_endlessDistance, _endlessConfig, _endlessElapsedTime);
            int distanceBucket = Mathf.FloorToInt(_endlessDistance);
            if (distanceBucket != _lastEndlessDistanceHud)
            {
                _lastEndlessDistanceHud = distanceBucket;
                UpdateHud();
            }
        }

        // Saves an Endless result once the runner fails and shows the record-oriented result screen.
        private void EndEndlessRun(EndlessFailReason failReason)
        {
            _endlessDistance = _runner != null ? Mathf.Max(_endlessDistance, _runner.transform.position.z) : _endlessDistance;
            EndlessRunResult result = EndlessRecords.SaveResult(
                _score,
                _endlessDistance,
                _levelGenerator.EndlessGeneratedRows,
                _wrongShardCount,
                _endlessConfig.WrongShardLimit,
                failReason);
            _ui.ShowEndlessResult(result);
        }

        // Increments the active run's wrong shard count and returns true when the shared limit is reached.
        private bool RegisterWrongShard()
        {
            int limit = GetWrongShardLimit();
            _wrongShardCount = Mathf.Min(limit, _wrongShardCount + 1);
            int remaining = Mathf.Max(0, limit - _wrongShardCount);
            if (_wrongShardCount >= limit)
            {
                _ui.ShowMessage("다른 색 샤드 3회! 실패");
                return true;
            }

            string warning = remaining == 1
                ? "주의! 기회 1회 남음"
                : "다른 색 샤드! 기회 " + remaining + "회 남음";
            _ui.ShowMessage(warning);
            return false;
        }

        // Returns the wrong-shard limit used by the current mode.
        private int GetWrongShardLimit()
        {
            return _mode == GameMode.Endless ? _endlessConfig.WrongShardLimit : GameConstants.MaxWrongShardCount;
        }

        // Ends Endless immediately from non-obstacle failure conditions without touching Stage Mode progress.
        private void FailEndlessRun(EndlessFailReason failReason)
        {
            if (_mode != GameMode.Endless)
            {
                return;
            }

            _state = GameState.Failed;
            RestoreTimeScale();
            _combo = 0;
            _audio.PlayMusic(MusicType.Failed, MusicStageIndex());
            UpdateHud();
            EndEndlessRun(failReason);
        }

        // Requests application quit only from explicit menu input, with safe Editor/WebGL handling.
        private void RequestQuit()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _ui.ShowMenuNotice("WebGL에서는 브라우저 탭을 닫아주세요.");
#elif UNITY_EDITOR
            Debug.Log("Quit requested from Main Menu.");
            _ui.ShowMenuNotice("Editor에서는 종료되지 않습니다.");
#else
            Application.Quit();
#endif
        }

        // Returns the representative stage index used for gameplay music fallback tiering.
        private int MusicStageIndex()
        {
            return _mode == GameMode.Endless ? 5 : _currentStage.StageIndex;
        }

        // Starts a local-only playtest attempt timer and counter for the active stage.
        private void BeginPlaytestAttempt(StageConfig stage)
        {
            _runStartedAt = Time.unscaledTime;
            _runStatsOpen = true;
            PlaytestStats.RecordStageStarted(stage.StageIndex);
        }

        // Records a completed attempt once and then closes the active stats window.
        private void RecordRunCompleted(StageResult result)
        {
            if (!_runStatsOpen)
            {
                return;
            }

            PlaytestStats.RecordCompleted(result.StageIndex, result.Score, result.Stars, GetRunElapsedSeconds(), _wrongShardCount);
            _runStatsOpen = false;
        }

        // Records a failed attempt once with its cause and then closes the active stats window.
        private void RecordRunFailed(StageFailReason failReason, int wrongShardCount)
        {
            if (!_runStatsOpen || _currentStage.StageIndex <= 0)
            {
                return;
            }

            PlaytestStats.RecordFailed(_currentStage.StageIndex, _score, GetRunElapsedSeconds(), failReason, wrongShardCount);
            _runStatsOpen = false;
        }

        // Records pause/menu/retry abandonment as a quit so playtesters can separate it from obstacle fails.
        private void RecordRunQuitIfOpen(PlaytestExitReason reason)
        {
            if (!_runStatsOpen || _currentStage.StageIndex <= 0)
            {
                return;
            }

            PlaytestStats.RecordQuit(_currentStage.StageIndex, _score, GetRunElapsedSeconds(), reason);
            _runStatsOpen = false;
        }

        // Calculates elapsed real time for one attempt without being affected by pause timeScale.
        private float GetRunElapsedSeconds()
        {
            return Mathf.Max(0f, Time.unscaledTime - _runStartedAt);
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
        private void RestoreTimeScale()
        {
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                Time.timeScale = 1f;
            }

            if (_audio != null)
            {
                _audio.SetMusicDucked(false);
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
