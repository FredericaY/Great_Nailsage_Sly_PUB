using Game.Systems.Charm;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public class CharmPickupHint : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private PlayerCharmInventory inventory;
        [SerializeField] private string playerTag = "Player";

        [Header("Presentation")]
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-48f, 0f);
        [SerializeField] private Vector2 panelSize = new Vector2(340f, 92f);
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color iconTint = Color.white;
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color descriptionColor = new Color(1f, 1f, 1f, 0.86f);
        [SerializeField] private float showDuration = 2.2f;
        [SerializeField] private float fadeSpeed = 10f;
        [SerializeField] private string equipTutorialMessage = "Open Pause -> CHARM and press Enter / A to equip and activate it.";

        private RectTransform _panelRoot;
        private CanvasGroup _canvasGroup;
        private Image _iconImage;
        private Text _titleText;
        private Text _descriptionText;
        private Font _font;
        private float _visibleUntil;
        private bool _subscribed;
        private bool _shownEquipTutorial;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUiIfNeeded();
            SetVisibleImmediate(false);
        }

        private void OnEnable()
        {
            TryBindInventory(true);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (inventory == null)
            {
                TryBindInventory(false);
                return;
            }

            bool shouldShow = Time.unscaledTime < _visibleUntil;
            float target = shouldShow ? 1f : 0f;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        private void TryBindInventory(bool force)
        {
            if (inventory == null)
            {
                GameObject player = GameObject.FindWithTag(playerTag);
                if (player != null)
                    inventory = player.GetComponent<PlayerCharmInventory>() ?? player.GetComponentInChildren<PlayerCharmInventory>();
            }

            if (inventory != null && !_subscribed)
            {
                inventory.OnCharmAdded += HandleCharmAdded;
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (!_subscribed || inventory == null)
                return;

            inventory.OnCharmAdded -= HandleCharmAdded;
            _subscribed = false;
        }

        private void HandleCharmAdded(CharmDefinition charm)
        {
            if (charm == null)
                return;

            BuildUiIfNeeded();
            if (_iconImage != null)
            {
                _iconImage.sprite = charm.Icon;
                _iconImage.enabled = charm.Icon != null;
                _iconImage.color = iconTint;
            }

            if (_titleText != null)
                _titleText.text = charm.DisplayName;

            if (_descriptionText != null)
                _descriptionText.text = BuildDescription(charm);

            _visibleUntil = Time.unscaledTime + showDuration;
            SetVisibleImmediate(true);
        }

        private string BuildDescription(CharmDefinition charm)
        {
            string baseDescription = string.IsNullOrWhiteSpace(charm.Description) ? "New charm acquired." : charm.Description;
            bool isEquippedNow = inventory != null && inventory.EquippedCharm == charm;
            if (isEquippedNow)
                return $"{baseDescription} Equipped now.";

            if (!_shownEquipTutorial)
            {
                _shownEquipTutorial = true;
                return $"{baseDescription} {equipTutorialMessage}";
            }

            return $"{baseDescription} Equip it in Pause -> CHARM to activate.";
        }

        private void BuildUiIfNeeded()
        {
            if (_panelRoot != null)
                return;

            RectTransform host = transform as RectTransform;
            if (host == null)
                return;

            GameObject panel = new GameObject("CharmPickupHintRoot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(transform, false);
            _panelRoot = panel.GetComponent<RectTransform>();
            _panelRoot.anchorMin = new Vector2(1f, 0.5f);
            _panelRoot.anchorMax = new Vector2(1f, 0.5f);
            _panelRoot.pivot = new Vector2(1f, 0.5f);
            _panelRoot.anchoredPosition = anchoredPosition;
            _panelRoot.sizeDelta = panelSize;

            Image bg = panel.GetComponent<Image>();
            bg.color = panelColor;
            _canvasGroup = panel.GetComponent<CanvasGroup>();

            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(panel.transform, false);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(14f, 0f);
            iconRect.sizeDelta = new Vector2(52f, 52f);
            _iconImage = icon.GetComponent<Image>();
            _iconImage.preserveAspect = true;

            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(panel.transform, false);
            _titleText = title.GetComponent<Text>();
            _titleText.font = _font;
            _titleText.fontSize = 20;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.UpperLeft;
            _titleText.color = titleColor;
            _titleText.raycastTarget = false;
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(78f, -34f);
            titleRect.offsetMax = new Vector2(-14f, -10f);

            GameObject description = new GameObject("Description", typeof(RectTransform), typeof(Text));
            description.transform.SetParent(panel.transform, false);
            _descriptionText = description.GetComponent<Text>();
            _descriptionText.font = _font;
            _descriptionText.fontSize = 16;
            _descriptionText.fontStyle = FontStyle.Normal;
            _descriptionText.alignment = TextAnchor.UpperLeft;
            _descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
            _descriptionText.color = descriptionColor;
            _descriptionText.raycastTarget = false;
            RectTransform descRect = description.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.offsetMin = new Vector2(78f, 12f);
            descRect.offsetMax = new Vector2(-14f, -38f);
        }

        private void SetVisibleImmediate(bool visible)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }
}
