using System;
using System.Collections.Generic;
using Game.Audio;
using Game.Core.Input;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.UI
{
    public class PauseMenuSettingsPage : PauseMenuPageBase
    {
        public override bool IsBusy => _rebindController != null && _rebindController.IsRebinding;

        private enum SettingRowKind
        {
            Section,
            Volume,
            Binding
        }

        private sealed class SettingRowView
        {
            public SettingRowKind Kind;
            public string ActionMap;
            public string ActionName;
            public int BindingIndex;
            public Func<float> GetVolume;
            public Action<float> SetVolume;
            public RectTransform Root;
            public Text LabelText;
            public Text ValueText;
            public Image FillImage;
        }

        [Header("References")]
        [SerializeField] private RectTransform scrollViewport;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Image scrollBarFill;

        private readonly List<SettingRowView> _rows = new();
        private readonly List<int> _selectableRowIndices = new();
        private GameInputRouter _inputRouter;
        private InputRebindController _rebindController;
        private Text _hintText;
        private int _selectedRowIndex;
        private float _targetScrollY;
        private float _nextVerticalRepeatTime;
        private int _verticalRepeatDir;
        private float _nextHorizontalRepeatTime;
        private int _horizontalRepeatDir;
        private bool _wasRebinding;

        private const float AxisDeadZone = 0.55f;
        private const float InitialRepeatDelay = 0.28f;
        private const float RepeatInterval = 0.12f;
        private const float VolumeStep = 0.05f;
        private const float ScrollPadding = 28f;
        private const float ScrollSpeed = 18f;
        private static readonly Color SelectedColor = new Color(1f, 0.9f, 0.55f, 1f);
        private static readonly Color NormalColor = Color.white;

        public void Configure(GameInputRouter inputRouter, InputRebindController rebindController, Text hintText)
        {
            PageTitle = "SETTING";
            _inputRouter = inputRouter;
            _rebindController = rebindController;
            _hintText = hintText;
            BindReferences();
            BuildRows();
            RefreshVisuals();
        }

        public override void Tick(float unscaledDeltaTime)
        {
            if (contentRoot != null)
            {
                Vector2 scrollPos = contentRoot.anchoredPosition;
                scrollPos.y = Mathf.Lerp(scrollPos.y, _targetScrollY, 1f - Mathf.Exp(-ScrollSpeed * unscaledDeltaTime));
                contentRoot.anchoredPosition = scrollPos;
            }

            UpdateScrollBar();
            RefreshVisuals();
        }

        public override void HandleInput()
        {
            if (_inputRouter == null)
                return;

            if (_rebindController != null && _rebindController.IsRebinding)
            {
                if (_inputRouter.UICancelPressed)
                    _rebindController.CancelCurrentRebind();

                RefreshVisuals();
                if (_wasRebinding && !_rebindController.IsRebinding)
                    _wasRebinding = false;
                return;
            }

            int vertical = ReadAxisDirection(_inputRouter.UINavigate.y, ref _verticalRepeatDir, ref _nextVerticalRepeatTime);
            if (vertical != 0)
                MoveSelection(-vertical);
            else
                HandleMouseWheelNavigation();

            SettingRowView selectedRow = GetSelectedRow();
            if (selectedRow == null)
                return;

            if (selectedRow.Kind == SettingRowKind.Volume)
            {
                int horizontal = ReadAxisDirection(_inputRouter.UINavigate.x, ref _horizontalRepeatDir, ref _nextHorizontalRepeatTime);
                if (horizontal != 0)
                    AdjustVolume(selectedRow, horizontal);
            }
            else
            {
                _horizontalRepeatDir = 0;
            }

            if (_inputRouter.UISubmitPressed && selectedRow.Kind == SettingRowKind.Binding && _rebindController != null)
            {
                if (_rebindController.TryStartRebind(selectedRow.ActionMap, selectedRow.ActionName, selectedRow.BindingIndex, true))
                    _wasRebinding = true;
            }
        }

        public override void OnMenuOpened()
        {
            _targetScrollY = 0f;
            if (contentRoot != null)
                contentRoot.anchoredPosition = Vector2.zero;
            SyncScrollToSelection();
            RefreshVisuals();
        }

        public override void OnMenuClosed()
        {
            if (_rebindController != null && _rebindController.IsRebinding)
                _rebindController.CancelCurrentRebind();
            _wasRebinding = false;
        }

        private void BindReferences()
        {
            if (scrollViewport == null)
                scrollViewport = transform.Find("SettingViewport") as RectTransform;
            if (contentRoot == null && scrollViewport != null)
                contentRoot = scrollViewport.Find("Content") as RectTransform;
            if (scrollBarFill == null)
            {
                Transform bar = transform.Find("SettingScrollBar/SettingScrollFill");
                if (bar != null)
                    scrollBarFill = bar.GetComponent<Image>();
            }
        }

        private void BuildRows()
        {
            _rows.Clear();
            _selectableRowIndices.Clear();

            AddSectionRow("AUDIO");
            AddVolumeRow("MasterVolume", () => AudioService.Ensure().MasterVolume, v => AudioService.Ensure().SetMasterVolume(v));
            AddVolumeRow("BGMVolume", () => AudioService.Ensure().BgmBusVolume, v => AudioService.Ensure().SetBgmBusVolume(v));
            AddVolumeRow("SFXVolume", () => AudioService.Ensure().SfxBusVolume, v => AudioService.Ensure().SetSfxBusVolume(v));
            AddSectionRow("BINDINGS");
            AddBindingRow("MoveLeft(KB)", "Gameplay", "Move", 1);
            AddBindingRow("MoveRight(KB)", "Gameplay", "Move", 2);
            AddBindingRow("Jump(KB)", "Gameplay", "Jump", 0);
            AddBindingRow("Attack(KB)", "Gameplay", "Attack", 1);
            AddBindingRow("Pause(KB)", "Gameplay", "Pause", 0);
            AddBindingRow("Move(Pad)", "Gameplay", "Move", 10);
            AddBindingRow("Jump(Pad)", "Gameplay", "Jump", 1);
            AddBindingRow("Attack(Pad)", "Gameplay", "Attack", 2);
            AddBindingRow("Pause(Pad)", "Gameplay", "Pause", 1);
            AddBindingRow("MenuConfirm", "UI", "Submit", 0);
            AddBindingRow("MenuCancel", "UI", "Cancel", 0);
            AddBindingRow("PrevPage", "UI", "PageLeft", 0);
            AddBindingRow("NextPage", "UI", "PageRight", 0);

            if (_selectableRowIndices.Count > 0)
                _selectedRowIndex = _selectableRowIndices[0];
        }

        private void AddSectionRow(string rowObjectName)
        {
            SettingRowView row = BindRow(rowObjectName);
            if (row == null)
                return;
            row.Kind = SettingRowKind.Section;
            _rows.Add(row);
        }

        private void AddVolumeRow(string rowObjectName, Func<float> getter, Action<float> setter)
        {
            SettingRowView row = BindRow(rowObjectName);
            if (row == null)
                return;
            row.Kind = SettingRowKind.Volume;
            row.GetVolume = getter;
            row.SetVolume = setter;
            row.FillImage = FindRowFill(row.Root, rowObjectName);
            _rows.Add(row);
            _selectableRowIndices.Add(_rows.Count - 1);
        }

        private void AddBindingRow(string rowObjectName, string actionMap, string actionName, int bindingIndex)
        {
            SettingRowView row = BindRow(rowObjectName);
            if (row == null)
                return;
            row.Kind = SettingRowKind.Binding;
            row.ActionMap = actionMap;
            row.ActionName = actionName;
            row.BindingIndex = bindingIndex;
            _rows.Add(row);
            _selectableRowIndices.Add(_rows.Count - 1);
        }

        private SettingRowView BindRow(string rowObjectName)
        {
            if (contentRoot == null)
                return null;

            Transform rowTransform = contentRoot.Find(rowObjectName + "_Row");
            if (rowTransform == null)
                return null;

            SettingRowView row = new SettingRowView();
            row.Root = rowTransform as RectTransform;
            row.LabelText = FindRowText(rowTransform, rowObjectName + "_Label");
            row.ValueText = FindRowText(rowTransform, rowObjectName + "_Value");
            return row;
        }

        private static Text FindRowText(Transform rowTransform, string name)
        {
            Transform child = rowTransform.Find(name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Image FindRowFill(RectTransform rowRoot, string rowObjectName)
        {
            Transform fill = rowRoot.Find(rowObjectName + "_Bar/" + rowObjectName + "_Fill");
            return fill != null ? fill.GetComponent<Image>() : null;
        }

        private void MoveSelection(int delta)
        {
            if (_selectableRowIndices.Count == 0)
                return;

            int currentSelectable = Mathf.Max(0, _selectableRowIndices.IndexOf(_selectedRowIndex));
            currentSelectable = Mathf.Clamp(currentSelectable + delta, 0, _selectableRowIndices.Count - 1);
            _selectedRowIndex = _selectableRowIndices[currentSelectable];
            SyncScrollToSelection();
        }

        private void AdjustVolume(SettingRowView row, int direction)
        {
            if (row.GetVolume == null || row.SetVolume == null)
                return;

            float current = row.GetVolume.Invoke();
            float next = Mathf.Clamp01(current + direction * VolumeStep);
            row.SetVolume.Invoke(next);
        }

        private SettingRowView GetSelectedRow()
        {
            if (_selectedRowIndex < 0 || _selectedRowIndex >= _rows.Count)
                return null;
            return _rows[_selectedRowIndex];
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                SettingRowView row = _rows[i];
                bool isSelected = i == _selectedRowIndex;

                if (row.LabelText != null)
                    row.LabelText.color = isSelected ? SelectedColor : (row.Kind == SettingRowKind.Section ? SelectedColor : NormalColor);
                if (row.ValueText != null)
                    row.ValueText.color = isSelected ? SelectedColor : NormalColor;

                if (row.Kind == SettingRowKind.Volume && row.GetVolume != null)
                {
                    float value = row.GetVolume.Invoke();
                    if (row.ValueText != null)
                        row.ValueText.text = Mathf.RoundToInt(value * 100f) + "%";
                    if (row.FillImage != null)
                        row.FillImage.rectTransform.anchorMax = new Vector2(value, 1f);
                }
                else if (row.Kind == SettingRowKind.Binding)
                {
                    bool isRebinding = isSelected && _rebindController != null && _rebindController.IsRebinding;
                    if (row.ValueText != null)
                    {
                        row.ValueText.text = isRebinding
                            ? "Press key..."
                            : (_rebindController != null ? _rebindController.GetBindingDisplayString(row.ActionMap, row.ActionName, row.BindingIndex) : string.Empty);
                    }
                }
            }

            if (_hintText == null)
                return;

            if (_rebindController != null && _rebindController.IsRebinding)
            {
                _hintText.text = "Press a new key or button. Esc cancels rebinding.";
                return;
            }

            if (_inputRouter.LastInputSource == GameInputRouter.InputPromptSource.Gamepad)
            {
                _hintText.text = "LB / RB change page. Left Stick scrolls settings. Left / Right adjusts volume. A confirms.";
                return;
            }

            _hintText.text = "Q / E change page. W / S or mouse wheel scrolls settings. A / D adjusts volume. Enter confirms.";
        }

        private void SyncScrollToSelection()
        {
            if (scrollViewport == null || contentRoot == null)
                return;

            SettingRowView row = GetSelectedRow();
            if (row == null || row.Root == null)
                return;

            float viewportHeight = scrollViewport.rect.height;
            float contentHeight = contentRoot.sizeDelta.y;
            if (contentHeight <= viewportHeight)
            {
                _targetScrollY = 0f;
                contentRoot.anchoredPosition = Vector2.zero;
                UpdateScrollBar();
                return;
            }

            float rowTop = GetSelectionVisibleTop(_selectedRowIndex);
            float rowBottom = -row.Root.offsetMin.y;
            float visibleTop = _targetScrollY;
            float visibleBottom = _targetScrollY + viewportHeight;

            if (rowTop < visibleTop + ScrollPadding)
                _targetScrollY = Mathf.Max(0f, rowTop - ScrollPadding);
            else if (rowBottom > visibleBottom - ScrollPadding)
                _targetScrollY = Mathf.Min(contentHeight - viewportHeight, rowBottom - viewportHeight + ScrollPadding);

            UpdateScrollBar();
        }

        private float GetSelectionVisibleTop(int selectedRowIndex)
        {
            if (selectedRowIndex < 0 || selectedRowIndex >= _rows.Count)
                return 0f;

            float top = -_rows[selectedRowIndex].Root.offsetMax.y;
            int previousIndex = selectedRowIndex - 1;
            if (previousIndex < 0)
                return top;

            SettingRowView previousRow = _rows[previousIndex];
            if (previousRow.Kind != SettingRowKind.Section || previousRow.Root == null)
                return top;

            return -previousRow.Root.offsetMax.y;
        }

        private void UpdateScrollBar()
        {
            if (scrollBarFill == null || scrollViewport == null || contentRoot == null)
                return;

            float viewportHeight = scrollViewport.rect.height;
            float contentHeight = Mathf.Max(viewportHeight, contentRoot.sizeDelta.y);
            float normalizedHeight = Mathf.Clamp01(viewportHeight / contentHeight);
            float barHeight = (scrollViewport.rect.height - 8f) * normalizedHeight;
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
            float normalizedY = maxScroll <= 0.001f ? 0f : Mathf.Clamp01(contentRoot.anchoredPosition.y / maxScroll);

            RectTransform barRect = scrollBarFill.rectTransform;
            barRect.sizeDelta = new Vector2(0f, Mathf.Max(32f, barHeight));
            float trackHeight = scrollViewport.rect.height - barRect.sizeDelta.y;
            barRect.anchoredPosition = new Vector2(0f, -4f - trackHeight * normalizedY);
        }

        private int ReadAxisDirection(float axis, ref int lastDir, ref float nextRepeatTime)
        {
            int dir = axis >= AxisDeadZone ? 1 : axis <= -AxisDeadZone ? -1 : 0;
            if (dir == 0)
            {
                lastDir = 0;
                nextRepeatTime = 0f;
                return 0;
            }

            float now = Time.unscaledTime;
            if (lastDir != dir)
            {
                lastDir = dir;
                nextRepeatTime = now + InitialRepeatDelay;
                return dir;
            }

            if (now < nextRepeatTime)
                return 0;

            nextRepeatTime = now + RepeatInterval;
            return dir;
        }

        private void HandleMouseWheelNavigation()
        {
            float wheel = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                wheel = Mouse.current.scroll.ReadValue().y;
#endif
            if (Mathf.Abs(wheel) < 0.01f)
                return;

            MoveSelection(wheel > 0f ? -1 : 1);
        }
    }
}
