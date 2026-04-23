using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public class BossEncounterTitleOverlay : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, 24f);
        [SerializeField] private Vector2 panelSize = new Vector2(900f, 120f);
        [SerializeField] private int fontSize = 42;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private float fadeInDuration = 0.08f;
        [SerializeField] private float fadeOutDuration = 0.22f;

        private static BossEncounterTitleOverlay _instance;

        private RectTransform _panelRoot;
        private CanvasGroup _canvasGroup;
        private Text _titleText;
        private Font _font;
        private float _showUntil;
        private bool _isShowing;

        public static void ShowOnHud(string bossName, float duration)
        {
            if (string.IsNullOrWhiteSpace(bossName))
                return;

            BossEncounterTitleOverlay overlay = EnsureInstance();
            if (overlay == null)
                return;

            overlay.ShowTitle(bossName, duration);
        }

        private static BossEncounterTitleOverlay EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            _instance = FindFirstObjectByType<BossEncounterTitleOverlay>(FindObjectsInactive.Include);
            if (_instance != null)
                return _instance;

            Canvas hudCanvas = FindHudCanvas();
            if (hudCanvas == null)
                return null;

            _instance = hudCanvas.gameObject.GetComponent<BossEncounterTitleOverlay>();
            if (_instance == null)
                _instance = hudCanvas.gameObject.AddComponent<BossEncounterTitleOverlay>();

            return _instance;
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && string.Equals(canvases[i].gameObject.name, "HUD", System.StringComparison.OrdinalIgnoreCase))
                    return canvases[i];
            }

            return null;
        }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUiIfNeeded();
            SetVisibleImmediate(false);
        }

        private void Update()
        {
            if (_canvasGroup == null)
                return;

            float targetAlpha = 0f;
            if (_isShowing)
            {
                targetAlpha = Time.unscaledTime < _showUntil ? 1f : 0f;
                if (targetAlpha <= 0f)
                    _isShowing = false;
            }

            float fadeDuration = targetAlpha > _canvasGroup.alpha ? fadeInDuration : fadeOutDuration;
            float delta = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, delta);
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void ShowTitle(string bossName, float duration)
        {
            BuildUiIfNeeded();

            if (_titleText != null)
                _titleText.text = bossName;

            _showUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
            _isShowing = true;
            SetVisibleImmediate(true);
        }

        private void BuildUiIfNeeded()
        {
            if (_panelRoot != null)
                return;

            RectTransform host = transform as RectTransform;
            if (host == null)
                return;

            GameObject panel = new GameObject("BossEncounterTitleRoot", typeof(RectTransform), typeof(CanvasGroup));
            panel.transform.SetParent(transform, false);
            _panelRoot = panel.GetComponent<RectTransform>();
            _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.anchoredPosition = anchoredPosition;
            _panelRoot.sizeDelta = panelSize;

            _canvasGroup = panel.GetComponent<CanvasGroup>();

            GameObject title = new GameObject("BossName", typeof(RectTransform), typeof(Text), typeof(Outline));
            title.transform.SetParent(panel.transform, false);
            _titleText = title.GetComponent<Text>();
            _titleText.font = _font;
            _titleText.fontSize = fontSize;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = textColor;
            _titleText.raycastTarget = false;
            _titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _titleText.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = title.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
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
