using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core.Input
{
    // InputRebindController
    // - Performs interactive rebinding for any action binding.
    // - Persists binding overrides using JSON.
    [DisallowMultipleComponent]
    public class InputRebindController : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Save")]
        [SerializeField] private string playerPrefsKey = "game.input.binding_overrides";
        [SerializeField] private bool loadOverridesOnEnable = true;

        private InputActionRebindingExtensions.RebindingOperation _rebindOp;

        public bool IsRebinding => _rebindOp != null;

        private void OnEnable()
        {
            if (loadOverridesOnEnable)
                LoadOverrides();
        }

        private void OnDisable()
        {
            CancelCurrentRebind();
        }

        public void Configure(InputActionAsset asset)
        {
            inputActions = asset;
            if (isActiveAndEnabled && loadOverridesOnEnable)
                LoadOverrides();
        }

        public bool TryStartRebind(string actionMapName, string actionName, int bindingIndex, bool saveOnComplete = true)
        {
            if (inputActions == null) return false;
            if (string.IsNullOrWhiteSpace(actionMapName) || string.IsNullOrWhiteSpace(actionName)) return false;

            var map = inputActions.FindActionMap(actionMapName, false);
            if (map == null) return false;

            var action = map.FindAction(actionName, false);
            if (action == null) return false;

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) return false;

            CancelCurrentRebind();

            bool wasEnabled = action.enabled;
            action.Disable();

            _rebindOp = action
                .PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .OnCancel(op =>
                {
                    op.Dispose();
                    _rebindOp = null;
                    if (wasEnabled) action.Enable();
                })
                .OnComplete(op =>
                {
                    op.Dispose();
                    _rebindOp = null;
                    if (wasEnabled) action.Enable();
                    if (saveOnComplete) SaveOverrides();
                });

            _rebindOp.Start();
            return true;
        }

        public void CancelCurrentRebind()
        {
            if (_rebindOp == null) return;
            _rebindOp.Cancel();
            _rebindOp.Dispose();
            _rebindOp = null;
        }

        public void SaveOverrides()
        {
            if (inputActions == null || string.IsNullOrWhiteSpace(playerPrefsKey)) return;
            string json = inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(playerPrefsKey, json);
            PlayerPrefs.Save();
        }

        public void LoadOverrides()
        {
            if (inputActions == null || string.IsNullOrWhiteSpace(playerPrefsKey)) return;
            if (!PlayerPrefs.HasKey(playerPrefsKey)) return;

            string json = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;

            inputActions.LoadBindingOverridesFromJson(json);
        }

        public void ResetOverrides()
        {
            if (inputActions == null) return;
            inputActions.RemoveAllBindingOverrides();

            if (!string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
                PlayerPrefs.Save();
            }
        }

        public string GetBindingDisplayString(string actionMapName, string actionName, int bindingIndex)
        {
            if (inputActions == null) return string.Empty;

            var map = inputActions.FindActionMap(actionMapName, false);
            if (map == null) return string.Empty;

            var action = map.FindAction(actionName, false);
            if (action == null) return string.Empty;

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) return string.Empty;
            return InputControlPath.ToHumanReadableString(
                action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }
}
