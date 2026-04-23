using System.Collections.Generic;
using Game.Core.Input;
using Game.Systems.Charm;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public class PauseMenuCharmPage : PauseMenuPageBase
    {
        private sealed class CharmSlotView
        {
            public GameObject Root;
            public Image Icon;
            public Image SelectionFrame;
            public Image EquippedMarker;
            public CharmDefinition Charm;
        }

        [Header("Source")]
        [SerializeField] private PlayerCharmInventory inventory;
        [SerializeField] private string playerTag = "Player";

        [Header("Navigation")]
        [SerializeField] private float axisDeadZone = 0.55f;
        [SerializeField] private float initialRepeatDelay = 0.28f;
        [SerializeField] private float repeatInterval = 0.12f;
        [SerializeField] private int gridColumns = 4;

        [Header("References")]
        [SerializeField] private RectTransform inventoryGridRoot;
        [SerializeField] private GridLayoutGroup inventoryGridLayout;
        [SerializeField] private Image equippedLargeIcon;
        [SerializeField] private Text equippedNameText;
        [SerializeField] private Image detailLargeIcon;
        [SerializeField] private Text detailNameText;
        [SerializeField] private Text detailDescriptionText;

        [Header("Layout")]
        [SerializeField] private Vector2 cellSize = new Vector2(72f, 72f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(12f, 12f);

        [Header("Colors")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.18f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.55f, 1f);
        [SerializeField] private Color equippedColor = new Color(0.42f, 1f, 0.72f, 1f);
        [SerializeField] private Color iconColor = Color.white;
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color mutedTextColor = new Color(1f, 1f, 1f, 0.7f);

        private readonly List<CharmSlotView> _slotViews = new();

        private GameInputRouter _inputRouter;
        private Text _hintText;
        private int _selectedIndex;
        private int _lastMoveXDir;
        private int _lastMoveYDir;
        private float _nextMoveXRepeatTime;
        private float _nextMoveYRepeatTime;
        private bool _built;

        public void Configure(GameInputRouter inputRouter, Text hintText)
        {
            PageTitle = "CHARM";
            _inputRouter = inputRouter;
            _hintText = hintText;
            TryBindInventory();
            BindReferences();
            RefreshAll();
        }

        public override void Tick(float unscaledDeltaTime)
        {
            TryBindInventory();
            RefreshAll();
        }

        public override void HandleInput()
        {
            if (_inputRouter == null || inventory == null || inventory.OwnedCharms.Count == 0)
                return;

            int moveX = ReadAxisDirection(_inputRouter.UINavigate.x, ref _lastMoveXDir, ref _nextMoveXRepeatTime);
            int moveY = ReadAxisDirection(_inputRouter.UINavigate.y, ref _lastMoveYDir, ref _nextMoveYRepeatTime);

            if (moveX != 0)
                MoveSelection(moveX);
            if (moveY != 0)
                MoveSelection(-moveY * gridColumns);

            if (_inputRouter.UISubmitPressed)
                EquipSelectedCharm();
        }

        public override void OnMenuOpened()
        {
            TryBindInventory();
            BindReferences();
            RefreshAll();
        }

        private void TryBindInventory()
        {
            if (inventory != null)
                return;

            GameObject player = GameObject.FindWithTag(playerTag);
            if (player == null)
                return;

            inventory = player.GetComponent<PlayerCharmInventory>() ?? player.GetComponentInChildren<PlayerCharmInventory>();
        }

        private void BindReferences()
        {
            if (_built)
                return;

            if (inventoryGridRoot == null)
                inventoryGridRoot = FindRect(transform, "InventoryGrid");
            if (inventoryGridLayout == null && inventoryGridRoot != null)
                inventoryGridLayout = inventoryGridRoot.GetComponent<GridLayoutGroup>();
            if (equippedLargeIcon == null)
                equippedLargeIcon = FindImage(transform, "EquippedIcon");
            if (equippedNameText == null)
                equippedNameText = FindText(transform, "EquippedName");
            if (detailLargeIcon == null)
                detailLargeIcon = FindImage(transform, "DetailIcon");
            if (detailNameText == null)
                detailNameText = FindText(transform, "DetailName");
            if (detailDescriptionText == null)
                detailDescriptionText = FindText(transform, "DetailDescription");

            if (inventoryGridLayout != null)
            {
                inventoryGridLayout.cellSize = cellSize;
                inventoryGridLayout.spacing = cellSpacing;
                inventoryGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                inventoryGridLayout.constraintCount = Mathf.Max(1, gridColumns);
            }

            _built = inventoryGridRoot != null;
        }

        private void RefreshAll()
        {
            if (!_built)
                return;

            RebuildSlotsIfNeeded();
            RefreshEquippedPanel();
            RefreshDetailPanel();
            RefreshHintText();
        }

        private void RebuildSlotsIfNeeded()
        {
            if (inventory == null || inventoryGridRoot == null)
                return;

            if (_selectedIndex >= inventory.OwnedCharms.Count)
                _selectedIndex = Mathf.Max(0, inventory.OwnedCharms.Count - 1);

            if (_slotViews.Count == inventory.OwnedCharms.Count)
            {
                for (int i = 0; i < _slotViews.Count; i++)
                {
                    if (_slotViews[i].Charm != inventory.OwnedCharms[i])
                    {
                        RebuildSlots();
                        return;
                    }
                }

                RefreshSlotStates();
                return;
            }

            RebuildSlots();
        }

        private void RebuildSlots()
        {
            for (int i = _slotViews.Count - 1; i >= 0; i--)
            {
                if (_slotViews[i].Root != null)
                    Destroy(_slotViews[i].Root);
            }
            _slotViews.Clear();

            if (inventory == null)
                return;

            for (int i = 0; i < inventory.OwnedCharms.Count; i++)
            {
                CharmDefinition charm = inventory.OwnedCharms[i];
                GameObject slot = new GameObject($"CharmSlot_{i}", typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(inventoryGridRoot, false);
                Image bg = slot.GetComponent<Image>();
                bg.color = new Color(1f, 1f, 1f, 0.08f);

                GameObject selection = new GameObject("Selection", typeof(RectTransform), typeof(Image));
                selection.transform.SetParent(slot.transform, false);
                RectTransform selectionRect = selection.GetComponent<RectTransform>();
                Stretch(selectionRect, new Vector2(-2f, -2f), new Vector2(2f, 2f));
                Image selectionImage = selection.GetComponent<Image>();
                selectionImage.color = selectedColor;
                selectionImage.enabled = false;

                GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(slot.transform, false);
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                Stretch(iconRect, new Vector2(10f, 10f), new Vector2(-10f, -10f));
                Image iconImage = icon.GetComponent<Image>();
                iconImage.sprite = charm != null ? charm.Icon : null;
                iconImage.color = iconColor;
                iconImage.preserveAspect = true;
                iconImage.enabled = charm != null && charm.Icon != null;

                GameObject equipped = new GameObject("EquippedMarker", typeof(RectTransform), typeof(Image));
                equipped.transform.SetParent(slot.transform, false);
                RectTransform equippedRect = equipped.GetComponent<RectTransform>();
                equippedRect.anchorMin = new Vector2(1f, 1f);
                equippedRect.anchorMax = new Vector2(1f, 1f);
                equippedRect.pivot = new Vector2(1f, 1f);
                equippedRect.anchoredPosition = new Vector2(-6f, -6f);
                equippedRect.sizeDelta = new Vector2(14f, 14f);
                Image equippedImage = equipped.GetComponent<Image>();
                equippedImage.color = equippedColor;
                equippedImage.enabled = false;

                _slotViews.Add(new CharmSlotView
                {
                    Root = slot,
                    Icon = iconImage,
                    SelectionFrame = selectionImage,
                    EquippedMarker = equippedImage,
                    Charm = charm
                });
            }

            float rows = Mathf.Ceil((float)Mathf.Max(1, inventory.OwnedCharms.Count) / Mathf.Max(1, gridColumns));
            float height = rows * cellSize.y + Mathf.Max(0f, rows - 1f) * cellSpacing.y;
            inventoryGridRoot.sizeDelta = new Vector2(0f, height);
            RefreshSlotStates();
        }

        private void RefreshSlotStates()
        {
            for (int i = 0; i < _slotViews.Count; i++)
            {
                CharmSlotView slot = _slotViews[i];
                bool isSelected = i == _selectedIndex;
                bool isEquipped = inventory != null && inventory.EquippedCharm == slot.Charm;
                if (slot.SelectionFrame != null)
                    slot.SelectionFrame.enabled = isSelected;
                if (slot.EquippedMarker != null)
                    slot.EquippedMarker.enabled = isEquipped;
            }
        }

        private void RefreshEquippedPanel()
        {
            CharmDefinition equipped = inventory != null ? inventory.EquippedCharm : null;
            if (equippedLargeIcon != null)
            {
                equippedLargeIcon.sprite = equipped != null ? equipped.Icon : null;
                equippedLargeIcon.enabled = equipped != null && equipped.Icon != null;
            }

            if (equippedNameText != null)
                equippedNameText.text = equipped != null ? equipped.DisplayName : "None";
        }

        private void RefreshDetailPanel()
        {
            CharmDefinition selected = GetSelectedCharm();
            if (detailLargeIcon != null)
            {
                detailLargeIcon.sprite = selected != null ? selected.Icon : null;
                detailLargeIcon.enabled = selected != null && selected.Icon != null;
            }

            if (detailNameText != null)
                detailNameText.text = selected != null ? selected.DisplayName : "No Charm";

            if (detailDescriptionText != null)
                detailDescriptionText.text = selected != null ? selected.Description : "Collect a charm to see its details.";
        }

        private void RefreshHintText()
        {
            if (_hintText == null)
                return;

            bool usingGamepad = _inputRouter != null && _inputRouter.LastInputSource == GameInputRouter.InputPromptSource.Gamepad;
            string pagePrompt = usingGamepad ? "LB / RB" : "Q / E";
            string movePrompt = usingGamepad ? "Stick / D-pad" : "WASD / Arrow keys";
            string equipPrompt = usingGamepad ? "A" : "Enter";

            if (inventory == null || inventory.OwnedCharms.Count == 0)
            {
                _hintText.text = $"{pagePrompt} changes page. Collect charms, then equip one here to activate it.";
                return;
            }

            if (inventory.EquippedCharm == null)
            {
                _hintText.text = $"{pagePrompt} changes page. {movePrompt} selects. {equipPrompt} equips. Charm powers stay inactive until equipped.";
                return;
            }

            _hintText.text = $"{pagePrompt} changes page. {movePrompt} selects. {equipPrompt} equips selected charm.";
        }

        private void MoveSelection(int delta)
        {
            if (inventory == null || inventory.OwnedCharms.Count == 0)
                return;

            _selectedIndex = Mathf.Clamp(_selectedIndex + delta, 0, inventory.OwnedCharms.Count - 1);
            RefreshSlotStates();
            RefreshDetailPanel();
        }

        private void EquipSelectedCharm()
        {
            CharmDefinition selected = GetSelectedCharm();
            if (selected == null || inventory == null)
                return;

            inventory.EquipCharm(selected);
            RefreshAll();
        }

        private CharmDefinition GetSelectedCharm()
        {
            if (inventory == null || inventory.OwnedCharms.Count == 0)
                return null;

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, inventory.OwnedCharms.Count - 1);
            return inventory.OwnedCharms[_selectedIndex];
        }

        private int ReadAxisDirection(float axis, ref int lastDir, ref float nextRepeatTime)
        {
            int dir = axis >= axisDeadZone ? 1 : axis <= -axisDeadZone ? -1 : 0;
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
                nextRepeatTime = now + initialRepeatDelay;
                return dir;
            }

            if (now < nextRepeatTime)
                return 0;

            nextRepeatTime = now + repeatInterval;
            return dir;
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            Transform child = FindDeepChild(root, name);
            return child as RectTransform;
        }

        private static Text FindText(Transform root, string name)
        {
            Transform child = FindDeepChild(root, name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Image FindImage(Transform root, string name)
        {
            Transform child = FindDeepChild(root, name);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name)
                    return child;

                Transform nested = FindDeepChild(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
