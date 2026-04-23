using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Player
{
    // PlayerInput
    // - Reads runtime input from Unity Input System action asset.
    // - Exposes frame-safe values for controller modules.
    // - Supports consume methods for one-shot requests.
    [DisallowMultipleComponent]
    public class PlayerInput : MonoBehaviour
    {
        // ------------------------------
        // Config: Input Actions
        // ------------------------------
        [Header("Input Actions")]
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string attackActionName = "Attack";
#else
        [Header("Legacy Fallback")]
#endif
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string jumpButton = "Jump";
        [SerializeField] private string attackButton = "Fire1";

        // ------------------------------
        // Public state (read-only)
        // ------------------------------
        public float MoveX { get; private set; }
        public float MoveY { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }

        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackReleased { get; private set; }

#if ENABLE_INPUT_SYSTEM
        /// <summary>Exposed for systems (e.g. CharmVendor) that read the same asset.</summary>
        public InputActionAsset InputActionsAsset => inputActions;
#endif

        // ------------------------------
        // Runtime: consume suppress flags
        // ------------------------------
        private int _suppressJumpPressedFrame = -1;
        private int _suppressJumpReleasedFrame = -1;
        private int _suppressAttackPressedFrame = -1;

#if ENABLE_INPUT_SYSTEM
        // ------------------------------
        // Runtime: cached actions
        // ------------------------------
        private InputActionMap _gameplayMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;
#endif

        // ------------------------------
        // Methods
        // ------------------------------
        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            CacheActions();
#endif
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnableActions();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            DisableActions();
#endif
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (TryReadFromInputSystem())
                return;

            ClearInputState();
            return;
#elif ENABLE_LEGACY_INPUT_MANAGER
            ReadFromLegacyInput();
#else
            ClearInputState();
            return;
#endif
        }

        private void ClearInputState()
        {
            MoveX = 0f;
            MoveY = 0f;
            JumpPressed = false;
            JumpHeld = false;
            JumpReleased = false;
            AttackPressed = false;
            AttackHeld = false;
            AttackReleased = false;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private void ReadFromLegacyInput()
        {
            // Movement
            MoveX = Input.GetAxisRaw(horizontalAxis);
            MoveY = Input.GetAxisRaw(verticalAxis);

            // Jump
            bool jumpPressedRaw = Input.GetButtonDown(jumpButton);
            JumpHeld = Input.GetButton(jumpButton);
            bool jumpReleasedRaw = Input.GetButtonUp(jumpButton);
            
            bool attackPressedRaw = Input.GetButtonDown(attackButton) || Input.GetKeyDown(KeyCode.X);
            AttackHeld = Input.GetButton(attackButton) || Input.GetKey(KeyCode.X);
            AttackReleased = Input.GetButtonUp(attackButton) || Input.GetKeyUp(KeyCode.X);


            JumpPressed = jumpPressedRaw && _suppressJumpPressedFrame != Time.frameCount;
            JumpReleased = jumpReleasedRaw && _suppressJumpReleasedFrame != Time.frameCount;
            AttackPressed = attackPressedRaw && _suppressAttackPressedFrame != Time.frameCount;
        }
#endif

#if ENABLE_INPUT_SYSTEM
        private void CacheActions()
        {
            _gameplayMap = null;
            _moveAction = null;
            _jumpAction = null;
            _attackAction = null;

            if (inputActions == null || string.IsNullOrWhiteSpace(gameplayMapName))
                return;

            _gameplayMap = inputActions.FindActionMap(gameplayMapName, false);
            if (_gameplayMap == null) return;

            _moveAction = _gameplayMap.FindAction(moveActionName, false);
            _jumpAction = _gameplayMap.FindAction(jumpActionName, false);
            _attackAction = _gameplayMap.FindAction(attackActionName, false);
        }

        private void EnableActions()
        {
            if (_gameplayMap == null)
                CacheActions();
            if (_gameplayMap == null) return;
            try { _gameplayMap.Enable(); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerInput] Map enable failed: {e.Message}. Re-caching.");
                CacheActions();
                if (_gameplayMap != null) _gameplayMap.Enable();
            }
        }

        private void DisableActions()
        {
            if (_gameplayMap == null) return;
            try { _gameplayMap.Disable(); }
            catch { /* ignore on disable */ }
        }

        private bool TryReadFromInputSystem()
        {
            if (_gameplayMap == null)
                CacheActions();
            if (_gameplayMap == null) return false;

            Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            MoveX = move.x;
            MoveY = move.y;

            bool jumpPressedRaw = _jumpAction != null && _jumpAction.WasPressedThisFrame();
            JumpHeld = _jumpAction != null && _jumpAction.IsPressed();
            bool jumpReleasedRaw = _jumpAction != null && _jumpAction.WasReleasedThisFrame();

            bool attackPressedRaw = _attackAction != null && _attackAction.WasPressedThisFrame();
            AttackHeld = _attackAction != null && _attackAction.IsPressed();
            AttackReleased = _attackAction != null && _attackAction.WasReleasedThisFrame();

            JumpPressed = jumpPressedRaw && _suppressJumpPressedFrame != Time.frameCount;
            JumpReleased = jumpReleasedRaw && _suppressJumpReleasedFrame != Time.frameCount;
            AttackPressed = attackPressedRaw && _suppressAttackPressedFrame != Time.frameCount;

            return true;
        }
#endif

        public void ConsumeJump()
        {
            _suppressJumpPressedFrame = Time.frameCount;
            JumpPressed = false;
        }
        public void ConsumeJumpReleased()
        {
            _suppressJumpReleasedFrame = Time.frameCount;
            JumpReleased = false;
        }

        public void ConsumeAttack()
        {
            _suppressAttackPressedFrame = Time.frameCount;
            AttackPressed = false;
        }
    }
}
