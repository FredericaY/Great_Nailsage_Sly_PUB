using UnityEngine;
using UnityEngine.UI;
using Game.Player;
using Game.Systems.Charm;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HUDGeo : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private PlayerCurrency currencySource;
    [SerializeField] private PlayerConsumables consumablesSource;
    [SerializeField] private PlayerCharmInventory charmInventory;
    [SerializeField] private string playerTag = "Player";

    [Header("UI")]
    [SerializeField] private Text geoText;
    [SerializeField] private string format = "Geo: {0}";
    [SerializeField] private RectTransform quickHealSlotRoot;
    [SerializeField] private Image quickHealIconImage;
    [SerializeField] private Text quickHealCountText;
    [SerializeField] private Sprite quickHealIconSprite;
    [SerializeField] private string quickHealCountFormat = "x{0}";
    [SerializeField] private Vector2 quickHealTopRightOffset = new Vector2(-20f, -20f);

    [Header("Visual Feedback")]
    [SerializeField] private bool enablePopOnGain = true;
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popDuration = 0.15f;
    [SerializeField] private Color gainFlashColor = new Color(1f, 0.9f, 0.4f);
    [SerializeField] private float useFeedbackDuration = 1.4f;

    [Header("Onboarding Tutorials")]
    [SerializeField] private float tutorialDuration = 4.5f;
    [SerializeField] private float tutorialFadeSpeed = 8f;
    [SerializeField] private Vector2 tutorialAnchoredPosition = new Vector2(0f, -42f);

    private bool _subscribed;
    private bool _consumablesSubscribed;
    private bool _inventorySubscribed;
    private float _nextAutoFindTime;
    private float _popEndTime;
    private Color _originalTextColor;
    private string _useFeedbackMessage;
    private float _useFeedbackUntil;
    private Font _builtinFont;
    private RectTransform _tutorialRoot;
    private CanvasGroup _tutorialCanvasGroup;
    private Text _tutorialTitleText;
    private Text _tutorialBodyText;
    private float _tutorialVisibleUntil;
    private bool _tutorialShowing;
    private readonly Queue<TutorialEntry> _tutorialQueue = new();
    private static readonly HashSet<string> _shownTutorialIds = new();

    private struct TutorialEntry
    {
        public string Id;
        public string Title;
        public string Body;
    }

    private void Reset()
    {
        geoText = GetComponentInChildren<Text>();
    }

    private void OnEnable()
    {
        EnsureQuickHealSlot();
        EnsureTutorialOverlay();
        TryConnectSource(force: true);
        CharmVendor.OnAnyShopOpened += HandleShopOpened;
        RefreshText();
    }

    private void OnDisable()
    {
        Unsubscribe();
        UnsubscribeConsumables();
        UnsubscribeInventory();
        CharmVendor.OnAnyShopOpened -= HandleShopOpened;
    }

    private void LateUpdate()
    {
        if (currencySource == null || consumablesSource == null)
        {
            TryConnectSource(force: false);
        }

        if (_useFeedbackUntil > 0f && Time.time >= _useFeedbackUntil)
        {
            _useFeedbackUntil = 0f;
            _useFeedbackMessage = string.Empty;
            RefreshText();
        }

        UpdateTutorialOverlay();
    }

    private void TryConnectSource(bool force)
    {
        if (!force && Time.unscaledTime < _nextAutoFindTime) return;
        _nextAutoFindTime = Time.unscaledTime + 0.5f;

        if (currencySource == null)
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player != null)
                currencySource = player.GetComponent<PlayerCurrency>() ?? player.GetComponentInChildren<PlayerCurrency>();
        }

        if (consumablesSource == null)
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player != null)
                consumablesSource = player.GetComponent<PlayerConsumables>() ?? player.GetComponentInChildren<PlayerConsumables>();
        }
        if (charmInventory == null)
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player != null)
                charmInventory = player.GetComponent<PlayerCharmInventory>() ?? player.GetComponentInChildren<PlayerCharmInventory>();
        }

        if (currencySource != null)
            Subscribe();
        if (consumablesSource != null)
        {
            SubscribeConsumables();
            RefreshText();
        }
        if (charmInventory != null)
            SubscribeInventory();
    }

    private void Subscribe()
    {
        if (_subscribed || currencySource == null) return;
        currencySource.OnGeoChanged += HandleGeoChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || currencySource == null) return;
        currencySource.OnGeoChanged -= HandleGeoChanged;
        _subscribed = false;
    }

    private void SubscribeConsumables()
    {
        if (_consumablesSubscribed || consumablesSource == null) return;
        consumablesSource.OnQuickHealChargesChanged += HandleQuickHealChargesChanged;
        consumablesSource.OnConsumableUseFeedback += HandleConsumableUseFeedback;
        _consumablesSubscribed = true;
    }

    private void UnsubscribeConsumables()
    {
        if (!_consumablesSubscribed || consumablesSource == null) return;
        consumablesSource.OnQuickHealChargesChanged -= HandleQuickHealChargesChanged;
        consumablesSource.OnConsumableUseFeedback -= HandleConsumableUseFeedback;
        _consumablesSubscribed = false;
    }

    private void SubscribeInventory()
    {
        if (_inventorySubscribed || charmInventory == null) return;
        charmInventory.OnCharmAdded += HandleCharmAdded;
        charmInventory.OnEquippedCharmChanged += HandleEquippedCharmChanged;
        _inventorySubscribed = true;
        MaybeQueueDoubleJumpTutorial(charmInventory.EquippedCharm);
    }

    private void UnsubscribeInventory()
    {
        if (!_inventorySubscribed || charmInventory == null) return;
        charmInventory.OnCharmAdded -= HandleCharmAdded;
        charmInventory.OnEquippedCharmChanged -= HandleEquippedCharmChanged;
        _inventorySubscribed = false;
    }

    private void HandleGeoChanged(int previous, int newAmount)
    {
        RefreshText();
        int added = newAmount - previous;
        if (added > 0)
            EnqueueTutorialOnce("geo_intro", "Geo Collected", "Geo is your currency. Defeat enemies and collect drops to buy charms and consumables.");
        if (enablePopOnGain && added > 0 && geoText != null)
        {
            _originalTextColor = geoText.color;
            _popEndTime = Time.time + popDuration;
        }
    }

    private void HandleQuickHealChargesChanged(int _)
    {
        RefreshText();
    }

    private void HandleConsumableUseFeedback(string message)
    {
        _useFeedbackMessage = message ?? string.Empty;
        _useFeedbackUntil = Time.time + Mathf.Max(0.2f, useFeedbackDuration);
        RefreshText();
    }

    private void HandleCharmAdded(CharmDefinition charm)
    {
        if (charm == null)
            return;
        EnqueueTutorialOnce("equip_charm_intro", "Equip Your Charm", "Open Pause -> CHARM and equip a charm to activate its effects.");
    }

    private void HandleEquippedCharmChanged(CharmDefinition charm)
    {
        MaybeQueueDoubleJumpTutorial(charm);
    }

    private void MaybeQueueDoubleJumpTutorial(CharmDefinition charm)
    {
        if (charm == null)
            return;
        if ((charm.GrantedAbilities & CharmAbility.DoubleJump) != CharmAbility.DoubleJump)
            return;
        EnqueueTutorialOnce("double_jump_intro", "Double Jump", "Press Jump again while in the air to cross larger gaps and reach high platforms.");
    }

    private void HandleShopOpened()
    {
        EnqueueTutorialOnce("shop_intro", "Charm Shop", "Spend Geo here. Charms need equipping in Pause, while Quick Heal can be used directly with F / B.");
    }

    private void Update()
    {
        if (geoText != null && _popEndTime > 0f)
        {
            float t = 1f - (Time.time - (_popEndTime - popDuration)) / popDuration;
            if (t <= 0f)
            {
                _popEndTime = 0f;
                geoText.transform.localScale = Vector3.one;
                geoText.color = _originalTextColor;
            }
            else
            {
                float scale = Mathf.Lerp(1f, popScale, t);
                geoText.transform.localScale = Vector3.one * scale;
                geoText.color = Color.Lerp(_originalTextColor, gainFlashColor, t);
            }
        }
    }

    private void RefreshText()
    {
        if (geoText == null) return;
        int amount = currencySource != null ? currencySource.Geo : 0;
        int quickHealCharges = consumablesSource != null ? consumablesSource.QuickHealCharges : 0;
        var displayFormat = string.IsNullOrWhiteSpace(format) ? "Geo: {0}" : format;
        string text = string.Format(displayFormat, amount);
        if (!string.IsNullOrWhiteSpace(_useFeedbackMessage) && Time.time < _useFeedbackUntil)
            text += "\n" + _useFeedbackMessage;
        geoText.text = text;

        EnsureQuickHealSlot();
        if (quickHealSlotRoot != null)
            quickHealSlotRoot.gameObject.SetActive(quickHealCharges > 0);
        if (quickHealCountText != null)
        {
            string countFormat = string.IsNullOrWhiteSpace(quickHealCountFormat) ? "x{0}" : quickHealCountFormat;
            quickHealCountText.text = string.Format(countFormat, quickHealCharges);
        }
    }

    private void EnsureQuickHealSlot()
    {
        if (quickHealSlotRoot != null && quickHealCountText != null && quickHealIconImage != null)
            return;

        RectTransform parent = null;
        if (geoText != null && geoText.canvas != null && geoText.canvas.rootCanvas != null)
            parent = geoText.canvas.rootCanvas.transform as RectTransform;
        if (parent == null)
            parent = transform as RectTransform;
        if (parent == null)
            return;

        if (_builtinFont == null)
            _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject slot = new GameObject("QuickHealSlot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(parent, false);
        quickHealSlotRoot = slot.GetComponent<RectTransform>();
        quickHealSlotRoot.anchorMin = new Vector2(1f, 1f);
        quickHealSlotRoot.anchorMax = new Vector2(1f, 1f);
        quickHealSlotRoot.pivot = new Vector2(1f, 1f);
        quickHealSlotRoot.anchoredPosition = quickHealTopRightOffset;
        quickHealSlotRoot.sizeDelta = new Vector2(132f, 34f);

        Image slotBg = slot.GetComponent<Image>();
        slotBg.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(slot.transform, false);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(6f, 0f);
        iconRect.sizeDelta = new Vector2(22f, 22f);
        quickHealIconImage = icon.GetComponent<Image>();
        quickHealIconImage.color = new Color(0.95f, 0.35f, 0.35f, 1f);
        quickHealIconImage.sprite = quickHealIconSprite;

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(slot.transform, false);
        Text labelText = label.GetComponent<Text>();
        labelText.font = _builtinFont;
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.text = "Quick Heal";
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(34f, 0f);
        labelRect.offsetMax = new Vector2(-40f, 0f);

        GameObject count = new GameObject("Count", typeof(RectTransform), typeof(Text));
        count.transform.SetParent(slot.transform, false);
        quickHealCountText = count.GetComponent<Text>();
        quickHealCountText.font = _builtinFont;
        quickHealCountText.fontSize = 16;
        quickHealCountText.fontStyle = FontStyle.Bold;
        quickHealCountText.color = new Color(1f, 0.9f, 0.4f, 1f);
        quickHealCountText.alignment = TextAnchor.MiddleRight;
        RectTransform countRect = count.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 1f);
        countRect.pivot = new Vector2(1f, 0.5f);
        countRect.sizeDelta = new Vector2(40f, 0f);
        countRect.anchoredPosition = new Vector2(-8f, 0f);
    }

    private void EnsureTutorialOverlay()
    {
        if (_tutorialRoot != null && _tutorialCanvasGroup != null && _tutorialTitleText != null && _tutorialBodyText != null)
            return;

        RectTransform parent = null;
        if (geoText != null && geoText.canvas != null && geoText.canvas.rootCanvas != null)
            parent = geoText.canvas.rootCanvas.transform as RectTransform;
        if (parent == null)
            parent = transform as RectTransform;
        if (parent == null)
            return;

        if (_builtinFont == null)
            _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject root = new GameObject("OnboardingTutorialOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        _tutorialRoot = root.GetComponent<RectTransform>();
        _tutorialRoot.anchorMin = new Vector2(0.5f, 1f);
        _tutorialRoot.anchorMax = new Vector2(0.5f, 1f);
        _tutorialRoot.pivot = new Vector2(0.5f, 1f);
        _tutorialRoot.anchoredPosition = tutorialAnchoredPosition;
        _tutorialRoot.sizeDelta = new Vector2(700f, 86f);
        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        _tutorialCanvasGroup = root.GetComponent<CanvasGroup>();
        _tutorialCanvasGroup.alpha = 0f;
        _tutorialCanvasGroup.blocksRaycasts = false;
        _tutorialCanvasGroup.interactable = false;

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(root.transform, false);
        _tutorialTitleText = titleGo.GetComponent<Text>();
        _tutorialTitleText.font = _builtinFont;
        _tutorialTitleText.fontSize = 20;
        _tutorialTitleText.fontStyle = FontStyle.Bold;
        _tutorialTitleText.alignment = TextAnchor.UpperCenter;
        _tutorialTitleText.color = Color.white;
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(14f, -34f);
        titleRect.offsetMax = new Vector2(-14f, -8f);

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
        bodyGo.transform.SetParent(root.transform, false);
        _tutorialBodyText = bodyGo.GetComponent<Text>();
        _tutorialBodyText.font = _builtinFont;
        _tutorialBodyText.fontSize = 16;
        _tutorialBodyText.alignment = TextAnchor.UpperCenter;
        _tutorialBodyText.color = new Color(1f, 1f, 1f, 0.9f);
        _tutorialBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _tutorialBodyText.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform bodyRect = bodyGo.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(16f, 10f);
        bodyRect.offsetMax = new Vector2(-16f, -36f);
    }

    private void EnqueueTutorialOnce(string id, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(id) || _shownTutorialIds.Contains(id))
            return;

        _shownTutorialIds.Add(id);
        _tutorialQueue.Enqueue(new TutorialEntry { Id = id, Title = title, Body = body });
        if (!_tutorialShowing)
            ShowNextTutorial();
    }

    private void ShowNextTutorial()
    {
        EnsureTutorialOverlay();
        if (_tutorialQueue.Count == 0 || _tutorialTitleText == null || _tutorialBodyText == null)
            return;

        TutorialEntry entry = _tutorialQueue.Dequeue();
        _tutorialTitleText.text = entry.Title;
        _tutorialBodyText.text = entry.Body;
        _tutorialVisibleUntil = Time.unscaledTime + Mathf.Max(1.5f, tutorialDuration);
        _tutorialShowing = true;
    }

    private void UpdateTutorialOverlay()
    {
        if (_tutorialCanvasGroup == null)
            return;

        float targetAlpha = 0f;
        if (_tutorialShowing)
        {
            targetAlpha = Time.unscaledTime < _tutorialVisibleUntil ? 1f : 0f;
            if (targetAlpha <= 0f && _tutorialCanvasGroup.alpha <= 0.01f)
            {
                _tutorialShowing = false;
                ShowNextTutorial();
            }
        }
        else if (_tutorialQueue.Count > 0)
        {
            ShowNextTutorial();
            targetAlpha = 1f;
        }

        _tutorialCanvasGroup.alpha = Mathf.MoveTowards(_tutorialCanvasGroup.alpha, targetAlpha, tutorialFadeSpeed * Time.unscaledDeltaTime);
    }
}
