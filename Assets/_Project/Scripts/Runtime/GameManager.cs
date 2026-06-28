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
            Boot,
            Running,
            Finished,
            Failed
        }

        public static GameManager Instance { get; private set; }

        [SerializeField] private int seed = 12345;
        [SerializeField] private bool restartAutomatically = true;

        private GameState _state = GameState.Boot;
        private LevelGenerator _levelGenerator;
        private RuntimeUi _ui;
        private ProceduralAudio _audio;
        private int _score;
        private int _combo;
        private LaneRunnerController _runner;

        public bool IsRunning => _state == GameState.Running;
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
        }

        // Starts the first deterministic run when the bootstrapped scene enters Play Mode.
        private void Start()
        {
            StartRun(seed);
        }

        // Allows keyboard restart after fail/finish when automatic restart is disabled or skipped.
        private void Update()
        {
            if (_state == GameState.Failed || _state == GameState.Finished)
            {
                HandleRestartInput();
            }
        }

        // Clears the singleton only when this manager instance is being destroyed.
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Resets score state and asks the generator to rebuild the level for the given seed.
        public void StartRun(int runSeed)
        {
            CancelInvoke();
            seed = runSeed;
            _score = 0;
            _combo = 0;
            _state = GameState.Running;
            _runner = _levelGenerator.ClearAndGenerate(this, seed);
            UpdateHud();
            _ui.ShowMessage("Swipe / A-D to switch lanes");
            Invoke(nameof(ClearMessage), 1.2f);
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
                _score += 10 * Mathf.Max(1, _combo);
                ProceduralFactory.CollectBurst(shard.transform.position, GameConstants.ToUnityColor(shard.ColorId));
                _audio.PlayCollect(_combo);
                Destroy(shard.gameObject);
            }
            else
            {
                _combo = 0;
                _score = Mathf.Max(0, _score - 15);
                ProceduralFactory.FailBurst(shard.transform.position);
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
            _score += 5;
            ProceduralFactory.GateBurst(runner.transform.position + Vector3.up * 0.8f, GameConstants.ToUnityColor(gate.TargetColor));
            _audio.PlayGate();
            UpdateHud();
        }

        // Ends the current run as failed when the runner hits an obstacle.
        public void HandleObstacle(ObstacleBlock obstacle, LaneRunnerController runner)
        {
            if (!IsRunning)
            {
                return;
            }

            _state = GameState.Failed;
            _combo = 0;
            _score = Mathf.Max(0, _score - 50);
            if (runner != null)
            {
                ProceduralFactory.FailBurst(runner.transform.position + Vector3.up * 0.5f);
            }
            _audio.PlayWrong();
            UpdateHud();
            _ui.ShowMessage(restartAutomatically ? "Crash! Restarting..." : "Crash! Press R");

            if (restartAutomatically)
            {
                Invoke(nameof(RestartNextSeed), 1.5f);
            }
        }

        // Applies the finish multiplier and ends the run when the finish trigger is reached.
        public void HandleFinish(FinishLine finishLine, LaneRunnerController runner)
        {
            if (!IsRunning)
            {
                return;
            }

            _state = GameState.Finished;
            int multiplier = Mathf.Clamp(1 + Mathf.FloorToInt(_score / 250f), 1, GameConstants.FinishMultiplierCap);
            _score *= multiplier;
            _combo = 0;

            Vector3 burstPosition = runner != null ? runner.transform.position + Vector3.up : transform.position;
            ProceduralFactory.FinishBurst(burstPosition);
            _audio.PlayFinish();
            UpdateHud();
            _ui.ShowMessage(restartAutomatically ? $"Finish! x{multiplier}\nScore {_score}" : $"Finish! x{multiplier}\nScore {_score}\nPress R");

            if (restartAutomatically)
            {
                Invoke(nameof(RestartNextSeed), 2.0f);
            }
        }

        // Starts the next seed after a completed or failed run.
        private void RestartNextSeed()
        {
            StartRun(seed + 1);
        }

        // Removes the temporary start prompt after the run is underway.
        private void ClearMessage()
        {
            if (IsRunning)
            {
                _ui.ShowMessage(string.Empty);
            }
        }

        // Refreshes the runtime HUD with score, combo, color, and seed.
        private void UpdateHud()
        {
            ColorId color = _runner != null ? _runner.CurrentColor : ColorId.Cyan;
            _ui.SetHud(_score, _combo, color, seed);
        }

        // Checks restart keys through the enabled Unity input backend.
        private void HandleRestartInput()
        {
            bool restartRequested = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            restartRequested |= keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            restartRequested |= Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space);
#endif

            if (restartRequested)
            {
                RestartNextSeed();
            }
        }
    }
}
