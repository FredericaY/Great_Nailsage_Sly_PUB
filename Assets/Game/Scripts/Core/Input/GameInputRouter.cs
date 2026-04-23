using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core.Input
{
    // GameInputRouter
    // - Owns ActionMap enable/disable routing between Gameplay and UI.
    // - Exposes UI input state for menus/pages.
    [DisallowMultipleComponent]
    public class GameInputRouter : MonoBehaviour
    {
        public enum InputPromptSource
        {
            KeyboardMouse,
            Gamepad
        }

        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Map Names")]
        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string uiMapName = "UI";

        [Header("Boot State")]
        [SerializeField] private bool enableGameplayOnEnable = true;
        [SerializeField] private bool enableUIOnEnable = false;

        [Header("UI Action Names")]
        [SerializeField] private string navigateActionName = "Navigate";
        [SerializeField] private string submitActionName = "Submit";
        [SerializeField] private string cancelActionName = "Cancel";
        [SerializeField] private string pauseActionName = "Pause";
        [SerializeField] private string pageLeftActionName = "PageLeft";
        [SerializeField] private string pageRightActionName = "PageRight";

        [Header("Gameplay Action Names")]
        [SerializeField] private string gameplayPauseActionName = "Pause";
        [SerializeField] private string gameplayUseConsumableActionName = "UseConsumable";

        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;

        private InputAction _gameplayPause;
        private InputAction _gameplayUseConsumable;
        private InputAction _navigate;
        private InputAction _submit;
        private InputAction _cancel;
        private InputAction _pause;
        private InputAction _pageLeft;
        private InputAction _pageRight;

        public Vector2 UINavigate { get; private set; }
        public bool UISubmitPressed { get; private set; }
        public bool UICancelPressed { get; private set; }
        public bool UIPausePressed { get; private set; }
        public bool UIPageLeftPressed { get; private set; }
        public bool UIPageRightPressed { get; private set; }
        public bool GameplayPausePressed { get; private set; }
        public bool UseConsumablePressedThisFrame { get; private set; }
        public bool PausePressedThisFrame => GameplayPausePressed || UIPausePressed;
        public InputPromptSource LastInputSource { get; private set; } = InputPromptSource.KeyboardMouse;

        public bool IsGameplayEnabled => _gameplayMap != null && _gameplayMap.enabled;
        public bool IsUIEnabled => _uiMap != null && _uiMap.enabled;

        private void Awake()
        {
            CacheMapsAndActions();
        }

        private void OnEnable()
        {
            CacheMapsAndActions();
            SetGameplayEnabled(enableGameplayOnEnable);
            SetUIEnabled(enableUIOnEnable);
        }

        private void OnDisable()
        {
            SetGameplayEnabled(false);
            SetUIEnabled(false);
            ClearUIState();
        }

        private void Update()
        {
            GameplayPausePressed = _gameplayPause != null && _gameplayPause.WasPressedThisFrame();
            UseConsumablePressedThisFrame = _gameplayUseConsumable != null && _gameplayUseConsumable.WasPressedThisFrame();

            if (!IsUIEnabled)
            {
                UINavigate = Vector2.zero;
                UISubmitPressed = false;
                UICancelPressed = false;
                UIPausePressed = false;
                UIPageLeftPressed = false;
                UIPageRightPressed = false;
                return;
            }

            UINavigate = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
            UISubmitPressed = _submit != null && _submit.WasPressedThisFrame();
            UICancelPressed = _cancel != null && _cancel.WasPressedThisFrame();
            UIPausePressed = _pause != null && _pause.WasPressedThisFrame();
            UIPageLeftPressed = _pageLeft != null && _pageLeft.WasPressedThisFrame();
            UIPageRightPressed = _pageRight != null && _pageRight.WasPressedThisFrame();

            UpdateLastInputSource();
        }

        public void SetGameplayEnabled(bool enabled)
        {
            if (_gameplayMap == null) return;
            if (enabled) _gameplayMap.Enable();
            else _gameplayMap.Disable();
        }

        public void SetUIEnabled(bool enabled)
        {
            if (_uiMap == null) return;
            if (enabled) _uiMap.Enable();
            else _uiMap.Disable();
            if (!enabled) ClearUIState();
        }

        public void EnterGameplayMode()
        {
            SetGameplayEnabled(true);
            SetUIEnabled(false);
        }

        public void EnterUIMode(bool keepGameplayEnabled = false)
        {
            SetGameplayEnabled(keepGameplayEnabled);
            SetUIEnabled(true);
        }

        public void Configure(InputActionAsset asset)
        {
            inputActions = asset;
            CacheMapsAndActions();
            if (isActiveAndEnabled)
            {
                SetGameplayEnabled(enableGameplayOnEnable);
                SetUIEnabled(enableUIOnEnable);
            }
        }

        private void CacheMapsAndActions()
        {
            _gameplayMap = null;
            _uiMap = null;
            _gameplayPause = null;
            _gameplayUseConsumable = null;
            _navigate = null;
            _submit = null;
            _cancel = null;
            _pause = null;
            _pageLeft = null;
            _pageRight = null;

            if (inputActions == null) return;

            _gameplayMap = inputActions.FindActionMap(gameplayMapName, false);
            _uiMap = inputActions.FindActionMap(uiMapName, false);
            if (_gameplayMap != null)
            {
                _gameplayPause = _gameplayMap.FindAction(gameplayPauseActionName, false);
                _gameplayUseConsumable = _gameplayMap.FindAction(gameplayUseConsumableActionName, false);
            }
            if (_uiMap == null) return;

            _navigate = _uiMap.FindAction(navigateActionName, false);
            _submit = _uiMap.FindAction(submitActionName, false);
            _cancel = _uiMap.FindAction(cancelActionName, false);
            _pause = _uiMap.FindAction(pauseActionName, false);
            _pageLeft = _uiMap.FindAction(pageLeftActionName, false);
            _pageRight = _uiMap.FindAction(pageRightActionName, false);
        }

        private void ClearUIState()
        {
            GameplayPausePressed = false;
            UseConsumablePressedThisFrame = false;
            UINavigate = Vector2.zero;
            UISubmitPressed = false;
            UICancelPressed = false;
            UIPausePressed = false;
            UIPageLeftPressed = false;
            UIPageRightPressed = false;
        }

        private void UpdateLastInputSource()
        {
            if (TryCaptureSource(_gameplayPause)) return;
            if (TryCaptureSource(_pageLeft)) return;
            if (TryCaptureSource(_pageRight)) return;
            if (TryCaptureSource(_submit)) return;
            if (TryCaptureSource(_cancel)) return;
            if (TryCaptureSource(_pause)) return;
            if (TryCaptureSource(_navigate)) return;
        }

        private bool TryCaptureSource(InputAction action)
        {
            if (action == null || action.activeControl == null)
                return false;

            InputDevice device = action.activeControl.device;
            if (device == null)
                return false;

            if (device is Gamepad)
                LastInputSource = InputPromptSource.Gamepad;
            else if (device is Keyboard || device is Mouse)
                LastInputSource = InputPromptSource.KeyboardMouse;
            else
                return false;

            return true;
        }
    }
}
