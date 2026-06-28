using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ColorGateRush
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LaneRunnerController : MonoBehaviour
    {
        [SerializeField] private int lane = 1;
        [SerializeField] private ColorId currentColor = ColorId.Cyan;

        private GameManager _manager;
        private Renderer _renderer;
        private Vector2 _touchStart;
        private bool _hasTouchStart;
        private float _runTime;

        public ColorId CurrentColor => currentColor;

        // Wires the runner to the active game manager and assigns its initial color and lane.
        public void Configure(GameManager manager, ColorId startingColor)
        {
            _manager = manager;
            lane = 1;
            SetColor(startingColor);
        }

        // Caches renderer and configures physics for trigger-based kinematic movement.
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Reads input and advances the runner while the game manager is in the running state.
        private void Update()
        {
            if (_manager == null || !_manager.IsRunning)
            {
                return;
            }

            _runTime += Time.deltaTime;
            HandleInput();
            MoveForwardAndLane();
        }

        // Updates the gameplay color and swaps the procedural material to match it.
        public void SetColor(ColorId colorId)
        {
            currentColor = colorId;
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_renderer != null)
            {
                _renderer.sharedMaterial = ProceduralFactory.ColorMaterial(colorId);
            }
        }

        // Routes input through the enabled Unity input backend for keyboard and touch lane changes.
        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            HandleInputSystemKeyboard();
            HandleInputSystemTouch();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            HandleLegacyKeyboard();
            HandleLegacyTouch();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        // Reads keyboard lane changes from Unity's Input System package.
        private void HandleInputSystemKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                ChangeLane(-1);
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                ChangeLane(1);
            }
        }

        // Reads touch lane changes from Unity's Input System package.
        private void HandleInputSystemTouch()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                _hasTouchStart = false;
                return;
            }

            UnityEngine.InputSystem.Controls.TouchControl primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch.press.wasPressedThisFrame)
            {
                _touchStart = primaryTouch.position.ReadValue();
                _hasTouchStart = true;
            }
            else if (_hasTouchStart && primaryTouch.press.wasReleasedThisFrame)
            {
                ResolveTouchLane(primaryTouch.position.ReadValue());
                _hasTouchStart = false;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        // Reads keyboard lane changes from Unity's legacy input manager.
        private void HandleLegacyKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ChangeLane(-1);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                ChangeLane(1);
            }
        }

        // Reads touch lane changes from Unity's legacy input manager.
        private void HandleLegacyTouch()
        {
            if (Input.touchCount <= 0)
            {
                _hasTouchStart = false;
                return;
            }

            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                _touchStart = touch.position;
                _hasTouchStart = true;
            }
            else if (_hasTouchStart && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                ResolveTouchLane(touch.position);
                _hasTouchStart = false;
            }
        }
#endif

        // Converts a swipe or half-screen tap into a lane direction.
        private void ResolveTouchLane(Vector2 touchEnd)
        {
            Vector2 delta = touchEnd - _touchStart;
            if (Mathf.Abs(delta.x) > 40f && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                ChangeLane(delta.x > 0f ? 1 : -1);
            }
            else
            {
                ChangeLane(touchEnd.x > Screen.width * 0.5f ? 1 : -1);
            }
        }

        // Applies a signed lane delta while keeping the player inside the track.
        private void ChangeLane(int delta)
        {
            lane = GameConstants.ClampLane(lane + delta);
        }

        // Moves the runner forward and smoothly interpolates toward the selected lane center.
        private void MoveForwardAndLane()
        {
            float speed = Mathf.Lerp(GameConstants.BaseForwardSpeed, GameConstants.MaxForwardSpeed, Mathf.Clamp01(_runTime / 45f));
            Vector3 position = transform.position;
            position.z += speed * Time.deltaTime;
            position.x = Mathf.Lerp(position.x, GameConstants.LaneX[lane], Time.deltaTime * GameConstants.LaneMoveSharpness);
            position.y = GameConstants.PlayerY;
            transform.position = position;
            transform.Rotate(Vector3.right, speed * 70f * Time.deltaTime, Space.Self);
        }

        // Dispatches trigger interactions to the game manager by gameplay object type.
        private void OnTriggerEnter(Collider other)
        {
            if (_manager == null)
            {
                _manager = GameManager.Instance;
            }

            if (_manager == null)
            {
                return;
            }

            if (other.TryGetComponent(out CollectibleShard shard))
            {
                _manager.HandleCollect(shard, this);
            }
            else if (other.TryGetComponent(out ColorGate gate))
            {
                _manager.HandleGate(gate, this);
            }
            else if (other.TryGetComponent(out ObstacleBlock obstacle))
            {
                _manager.HandleObstacle(obstacle, this);
            }
            else if (other.TryGetComponent(out FinishLine finishLine))
            {
                _manager.HandleFinish(finishLine, this);
            }
        }
    }
}
