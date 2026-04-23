using System.Collections.Generic;
using Game.Core.Input;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Systems.Charm
{
    /// <summary>
    /// Trigger zone: press Interact to open a shop that sells charms for geo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class CharmVendor : MonoBehaviour
    {
        public static event System.Action OnAnyShopOpened;

        // Charms that must never appear in the shop. Matched by CharmDefinition.name / CharmId.
        // These are obtainable via world pickups only.
        private static readonly HashSet<string> ExcludedCharmIds = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "DoubleJump",
            "Strength",
        };

        [Header("Catalog")]
        [SerializeField] private CharmDefinition[] catalog;

        [Header("Interaction")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [Tooltip("If physics triggers fail (layers), player can still open shop within this distance (world units).")]
        [SerializeField] private float distanceFallbackRadius = 3.5f;

        [Header("Shop UI")]
        [Tooltip("Optional. If null, a simple panel is created under the first Canvas in the scene.")]
        [SerializeField] private RectTransform shopPanelRoot;

        [Header("Time")]
        [SerializeField] private bool pauseTimeWhileOpen = true;

        [Header("Zone Visual")]
        [SerializeField] private bool showZoneVisual = true;
        [SerializeField] private Color zoneFillColor = new Color(0.1f, 0.8f, 0.35f, 0.22f);
        [SerializeField] private Color zoneBorderTextColor = new Color(0.7f, 1f, 0.8f, 1f);
        [SerializeField] private string shopLabel = "CHARM SHOP";
        [SerializeField] private Vector3 labelOffset = new Vector3(0f, 1f, 0f);

        private Collider2D _trigger;
        /// <summary>Any overlapping player collider (for reference). Zone uses overlap count for multi-collider bodies.</summary>
        private Collider2D _playerCollider;
        private int _playerTriggerOverlapCount;
        private GameInputRouter _inputRouter;
        private PlayerLock _playerLock;
        private PlayerCurrency _currency;
        private PlayerCharmInventory _inventory;
        private PlayerConsumables _consumables;

        private RectTransform _runtimePanelRoot;
        private RectTransform _rowsParent;
        private Text _hintText;
        private readonly List<GameObject> _rowObjects = new();
        private bool _shopOpen;
#if ENABLE_INPUT_SYSTEM
        private InputAction _interactAction;
#endif
        private PlayerRoot _cachedPlayerRoot;
        private GameObject _zoneVisualRoot;
        private SpriteRenderer _zoneSprite;
        private TextMesh _zoneText;
        private static Sprite _whiteSprite;
#if UNITY_EDITOR
        private bool _editorVisualRefreshQueued;
#endif

        private void Awake()
        {
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null && !_trigger.isTrigger)
                _trigger.isTrigger = true;

            // Kinematic Rigidbody2D helps 2D trigger callbacks fire reliably with some layer setups.
            if (GetComponent<Rigidbody2D>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
                rb.useFullKinematicContacts = true;
            }

            RefreshInputRouter();
            CacheInteractAction();
        }

        private void Start()
        {
            EnsureZoneVisual();
            UpdateZoneVisualTransform();
        }

        private void OnValidate()
        {
            distanceFallbackRadius = Mathf.Max(0.1f, distanceFallbackRadius);
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                QueueEditorZoneVisualRefresh();
#endif
            }
        }
#if UNITY_EDITOR
        private void QueueEditorZoneVisualRefresh()
        {
            if (_editorVisualRefreshQueued)
                return;

            _editorVisualRefreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                _editorVisualRefreshQueued = false;
                if (this == null)
                    return;

                _trigger = GetComponent<Collider2D>();
                EnsureZoneVisual();
                UpdateZoneVisualTransform();
            };
        }
#endif

        private void RefreshInputRouter()
        {
            if (_inputRouter == null)
                _inputRouter = FindFirstObjectByType<GameInputRouter>();
        }

        private void CacheInteractAction()
        {
#if ENABLE_INPUT_SYSTEM
            if (_interactAction != null)
                return;
            var pi = FindFirstObjectByType<Game.Player.PlayerInput>();
            if (pi == null || pi.InputActionsAsset == null)
                return;
            _interactAction = pi.InputActionsAsset.FindActionMap("Gameplay", false)?.FindAction("Interact", false);
#endif
        }

        private void Update()
        {
            CacheInteractAction();
            UpdateZoneVisualTransform();

            if (_shopOpen)
            {
                if (_inputRouter != null && _inputRouter.UICancelPressed)
                {
                    CloseShop();
                    return;
                }
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    CloseShop();
#else
                if (Input.GetKeyDown(KeyCode.Escape))
                    CloseShop();
#endif
                return;
            }

            bool inTrigger = _playerTriggerOverlapCount > 0 && _playerCollider != null;
            bool inRange = inTrigger;

            if (!inRange)
            {
                PlayerRoot root = FindPlayerRoot();
                if (root != null)
                {
                    float r = distanceFallbackRadius;
                    if (_trigger != null)
                        r = Mathf.Max(r, Mathf.Max(_trigger.bounds.extents.x, _trigger.bounds.extents.y) + 0.25f);
                    if (Vector2.Distance(transform.position, root.transform.position) <= r)
                    {
                        inRange = true;
                        CachePlayerRefsFromRoot(root);
                    }
                }
            }

            if (!inRange)
                return;

            if (WasInteractPressed())
                OpenShop();
        }

        private bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_interactAction != null && _interactAction.WasPressedThisFrame())
                return true;
            if (WasKeyboardInteractPressed())
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(interactKey);
#else
            return false;
#endif
        }

        /// <summary>Child colliders are often Untagged — accept any collider under PlayerRoot.</summary>
#if ENABLE_INPUT_SYSTEM
        private bool WasKeyboardInteractPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if (!TryMapKeyCode(interactKey, out Key key))
                return false;

            return keyboard[key].wasPressedThisFrame;
        }

        private static bool TryMapKeyCode(KeyCode keyCode, out Key key)
        {
            switch (keyCode)
            {
                case KeyCode.A: key = Key.A; return true;
                case KeyCode.B: key = Key.B; return true;
                case KeyCode.C: key = Key.C; return true;
                case KeyCode.D: key = Key.D; return true;
                case KeyCode.E: key = Key.E; return true;
                case KeyCode.F: key = Key.F; return true;
                case KeyCode.G: key = Key.G; return true;
                case KeyCode.H: key = Key.H; return true;
                case KeyCode.I: key = Key.I; return true;
                case KeyCode.J: key = Key.J; return true;
                case KeyCode.K: key = Key.K; return true;
                case KeyCode.L: key = Key.L; return true;
                case KeyCode.M: key = Key.M; return true;
                case KeyCode.N: key = Key.N; return true;
                case KeyCode.O: key = Key.O; return true;
                case KeyCode.P: key = Key.P; return true;
                case KeyCode.Q: key = Key.Q; return true;
                case KeyCode.R: key = Key.R; return true;
                case KeyCode.S: key = Key.S; return true;
                case KeyCode.T: key = Key.T; return true;
                case KeyCode.U: key = Key.U; return true;
                case KeyCode.V: key = Key.V; return true;
                case KeyCode.W: key = Key.W; return true;
                case KeyCode.X: key = Key.X; return true;
                case KeyCode.Y: key = Key.Y; return true;
                case KeyCode.Z: key = Key.Z; return true;
                case KeyCode.Alpha0: key = Key.Digit0; return true;
                case KeyCode.Alpha1: key = Key.Digit1; return true;
                case KeyCode.Alpha2: key = Key.Digit2; return true;
                case KeyCode.Alpha3: key = Key.Digit3; return true;
                case KeyCode.Alpha4: key = Key.Digit4; return true;
                case KeyCode.Alpha5: key = Key.Digit5; return true;
                case KeyCode.Alpha6: key = Key.Digit6; return true;
                case KeyCode.Alpha7: key = Key.Digit7; return true;
                case KeyCode.Alpha8: key = Key.Digit8; return true;
                case KeyCode.Alpha9: key = Key.Digit9; return true;
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.KeypadEnter: key = Key.NumpadEnter; return true;
                case KeyCode.Escape: key = Key.Escape; return true;
                default:
                    key = Key.None;
                    return false;
            }
        }
#endif

        /// <summary>Child colliders are often Untagged 鈥?accept any collider under PlayerRoot.</summary>
        private bool IsPlayerCollider(Collider2D other)
        {
            if (other == null)
                return false;
            if (other.CompareTag(playerTag))
                return true;
            return other.GetComponentInParent<PlayerRoot>() != null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            _playerTriggerOverlapCount++;
            _playerCollider = other;
            CachePlayerRefs(other);
            UpdateZoneVisualState(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            _playerTriggerOverlapCount = Mathf.Max(0, _playerTriggerOverlapCount - 1);
            if (_playerTriggerOverlapCount == 0)
            {
                _playerCollider = null;
                if (_shopOpen)
                    CloseShop();
                UpdateZoneVisualState(false);
            }
        }

        private void CachePlayerRefs(Collider2D other)
        {
            var root = other.GetComponentInParent<PlayerRoot>();
            if (root == null)
                return;
            CachePlayerRefsFromRoot(root);
        }

        private void CachePlayerRefsFromRoot(PlayerRoot root)
        {
            if (root == null)
                return;
            _cachedPlayerRoot = root;
            _playerLock = root.GetComponent<PlayerLock>();
            _currency = root.GetComponent<PlayerCurrency>() ?? root.GetComponentInChildren<PlayerCurrency>();
            _inventory = root.GetComponent<PlayerCharmInventory>() ?? root.GetComponentInChildren<PlayerCharmInventory>();
            _consumables = root.GetComponent<PlayerConsumables>() ?? root.GetComponentInChildren<PlayerConsumables>();
        }

        private PlayerRoot FindPlayerRoot()
        {
            if (_cachedPlayerRoot != null)
                return _cachedPlayerRoot;
            var go = GameObject.FindWithTag(playerTag);
            if (go == null)
                return null;
            _cachedPlayerRoot = go.GetComponent<PlayerRoot>() ?? go.GetComponentInChildren<PlayerRoot>();
            return _cachedPlayerRoot;
        }

        private void OpenShop()
        {
            if (_shopOpen || catalog == null || catalog.Length == 0)
                return;
            if (_playerCollider != null)
                CachePlayerRefs(_playerCollider);
            else if (_cachedPlayerRoot != null)
                CachePlayerRefsFromRoot(_cachedPlayerRoot);
            if (_currency == null || _inventory == null)
            {
                Debug.LogWarning("[CharmVendor] Player needs PlayerCurrency and PlayerCharmInventory.");
                return;
            }

            RefreshInputRouter();
            EnsureShopPanel();
            if (_runtimePanelRoot == null || _rowsParent == null)
            {
                Debug.LogWarning("[CharmVendor] Shop UI root missing — check Canvas / shop panel references.");
                return;
            }
            BuildRows();
            SetPanelVisible(true);

            _shopOpen = true;
            _playerLock?.Acquire();
            _inputRouter?.EnterUIMode(false);
            OnAnyShopOpened?.Invoke();

            if (pauseTimeWhileOpen)
                Time.timeScale = 0f;
        }

        private void CloseShop()
        {
            if (!_shopOpen)
                return;

            SetPanelVisible(false);
            _shopOpen = false;
            _playerLock?.Release();
            _inputRouter?.EnterGameplayMode();

            if (pauseTimeWhileOpen)
                Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (_shopOpen && pauseTimeWhileOpen)
                Time.timeScale = 1f;
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.name = "CharmVendor_WhiteTex";
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _whiteSprite.name = "CharmVendor_WhiteSprite";
            return _whiteSprite;
        }

        private void EnsureZoneVisual()
        {
            if (!showZoneVisual)
            {
                if (_zoneVisualRoot != null)
                    _zoneVisualRoot.SetActive(false);
                return;
            }

            if (_zoneVisualRoot == null)
            {
                Transform existing = transform.Find("ShopZoneVisual");
                if (existing != null)
                    _zoneVisualRoot = existing.gameObject;
            }

            if (_zoneVisualRoot == null)
            {
                _zoneVisualRoot = new GameObject("ShopZoneVisual");
                _zoneVisualRoot.transform.SetParent(transform, false);
                _zoneVisualRoot.transform.localPosition = Vector3.zero;
            }

            _zoneVisualRoot.SetActive(true);

            if (_zoneSprite == null)
                _zoneSprite = _zoneVisualRoot.GetComponent<SpriteRenderer>();
            if (_zoneSprite == null)
                _zoneSprite = _zoneVisualRoot.AddComponent<SpriteRenderer>();

            _zoneSprite.sprite = GetOrCreateWhiteSprite();
            _zoneSprite.drawMode = SpriteDrawMode.Sliced;
            _zoneSprite.color = zoneFillColor;
            _zoneSprite.sortingOrder = 1000;

            if (_zoneText == null)
                _zoneText = _zoneVisualRoot.GetComponentInChildren<TextMesh>();
            if (_zoneText == null)
            {
                var textGo = new GameObject("ShopLabel");
                textGo.transform.SetParent(_zoneVisualRoot.transform, false);
                _zoneText = textGo.AddComponent<TextMesh>();
            }

            _zoneText.text = string.IsNullOrWhiteSpace(shopLabel) ? "SHOP" : shopLabel;
            _zoneText.alignment = TextAlignment.Center;
            _zoneText.anchor = TextAnchor.MiddleCenter;
            _zoneText.characterSize = 0.12f;
            _zoneText.fontSize = 48;
            _zoneText.color = zoneBorderTextColor;
            _zoneText.transform.localPosition = labelOffset;

            // TextMesh renders through a MeshRenderer whose default sortingOrder is 0,
            // which puts the label behind other sprites in the scene. Match (and exceed)
            // the zone sprite's sortingOrder so the label is always drawn on top.
            MeshRenderer textRenderer = _zoneText.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingLayerID = _zoneSprite.sortingLayerID;
                textRenderer.sortingOrder = _zoneSprite.sortingOrder + 1;
            }
        }

        private void UpdateZoneVisualTransform()
        {
            if (!showZoneVisual || _zoneSprite == null || _trigger == null)
                return;

            Vector2 size = _trigger.bounds.size;
            size.x = Mathf.Max(0.5f, size.x);
            size.y = Mathf.Max(0.5f, size.y);

            _zoneSprite.size = size;
            _zoneSprite.transform.position = _trigger.bounds.center;
            if (_zoneText != null)
                _zoneText.transform.position = _trigger.bounds.center + labelOffset;
        }

        private void UpdateZoneVisualState(bool playerInside)
        {
            if (_zoneSprite == null)
                return;
            _zoneSprite.color = playerInside
                ? new Color(zoneFillColor.r, zoneFillColor.g, zoneFillColor.b, Mathf.Clamp01(zoneFillColor.a + 0.2f))
                : zoneFillColor;
        }

        private static Canvas FindCanvasForShop()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvases[i];
            }
            if (canvases.Length > 0)
                return canvases[0];
            return FindFirstObjectByType<Canvas>();
        }

        private void EnsureShopPanel()
        {
            if (shopPanelRoot != null)
            {
                _runtimePanelRoot = shopPanelRoot;
                _rowsParent = _runtimePanelRoot.Find("Content/Rows") as RectTransform;
                if (_rowsParent == null)
                    _rowsParent = _runtimePanelRoot;
                return;
            }

            if (_runtimePanelRoot != null)
                return;

            Canvas canvas = FindCanvasForShop();
            if (canvas == null)
            {
                Debug.LogError("[CharmVendor] No Canvas in scene — cannot create shop UI.");
                return;
            }

            var rootGo = new GameObject("CharmShopPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            rootGo.transform.SetParent(canvas.transform, false);
            var rect = rootGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = rootGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(rootGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -40f);
            titleRect.sizeDelta = new Vector2(600f, 48f);
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = "Charm Shop";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            hintGo.transform.SetParent(rootGo.transform, false);
            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 24f);
            hintRect.sizeDelta = new Vector2(700f, 36f);
            _hintText = hintGo.GetComponent<Text>();
            _hintText.text = "Buy a charm, then equip it in Pause -> CHARM if needed.";
            _hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hintText.fontSize = 16;
            _hintText.color = new Color(0.85f, 0.85f, 0.85f);
            _hintText.alignment = TextAnchor.MiddleCenter;

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(rootGo.transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(520f, 360f);
            scrollGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var rowsGo = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            rowsGo.transform.SetParent(viewportGo.transform, false);
            _rowsParent = rowsGo.GetComponent<RectTransform>();
            _rowsParent.anchorMin = new Vector2(0f, 1f);
            _rowsParent.anchorMax = new Vector2(1f, 1f);
            _rowsParent.pivot = new Vector2(0.5f, 1f);
            _rowsParent.anchoredPosition = Vector2.zero;
            _rowsParent.sizeDelta = new Vector2(0f, 0f);
            var vlg = rowsGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            rowsGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = _rowsParent;
            scroll.vertical = true;
            scroll.horizontal = false;

            _runtimePanelRoot = rect;
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (_runtimePanelRoot != null)
                _runtimePanelRoot.gameObject.SetActive(visible);
        }

        private void BuildRows()
        {
            foreach (GameObject row in _rowObjects)
            {
                if (row != null)
                    Destroy(row);
            }
            _rowObjects.Clear();

            if (_rowsParent == null)
                return;

            foreach (CharmDefinition charm in catalog)
            {
                if (charm == null)
                    continue;
                if (IsExcludedFromShop(charm))
                    continue;
                CreateRow(charm);
            }
        }

        private static bool IsExcludedFromShop(CharmDefinition charm)
        {
            if (charm == null)
                return false;
            if (!string.IsNullOrEmpty(charm.CharmId) && ExcludedCharmIds.Contains(charm.CharmId))
                return true;
            if (!string.IsNullOrEmpty(charm.name) && ExcludedCharmIds.Contains(charm.name))
                return true;
            return false;
        }

        private void CreateRow(CharmDefinition charm)
        {
            int price = charm.ShopGeoPrice;
            bool isQuickHealConsumable = IsQuickHealConsumable(charm);
            bool owned = !isQuickHealConsumable && _inventory.HasCharm(charm);

            var row = new GameObject(charm.name + "_Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_rowsParent, false);
            var le = row.GetComponent<LayoutElement>();
            le.minHeight = 56f;
            le.preferredHeight = 56f;

            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandWidth = true;
            h.padding = new RectOffset(8, 8, 4, 4);
            h.spacing = 12f;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(row.transform, false);
            var nameText = nameGo.GetComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.color = Color.white;
            nameText.text = charm.DisplayName;
            nameText.alignment = TextAnchor.MiddleLeft;

            var priceGo = new GameObject("Price", typeof(RectTransform), typeof(Text));
            priceGo.transform.SetParent(row.transform, false);
            var priceText = priceGo.GetComponent<Text>();
            priceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceText.fontSize = 16;
            priceText.color = new Color(1f, 0.9f, 0.4f);
            if (isQuickHealConsumable)
                priceText.text = $"{price} geo  x{(_consumables != null ? _consumables.QuickHealCharges : 0)}";
            else if (owned)
                priceText.text = "Owned";
            else if (price <= 0)
                priceText.text = "Free";
            else
                priceText.text = $"{price} geo";
            priceText.alignment = TextAnchor.MiddleCenter;

            var btnGo = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(row.transform, false);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.minWidth = 100f;
            btnLe.preferredWidth = 100f;
            var btn = btnGo.GetComponent<Button>();
            btnGo.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.35f, 1f);

            var btnLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            var btnRect = btnLabelGo.GetComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            var btnText = btnLabelGo.GetComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 16;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;

            if (owned)
            {
                btnText.text = "—";
                btn.interactable = false;
            }
            else if (!_currency.CanAfford(price) && price > 0)
            {
                btnText.text = "Buy";
                btn.interactable = false;
            }
            else
            {
                btnText.text = "Buy";
                CharmDefinition c = charm;
                int p = price;
                btn.onClick.AddListener(() => TryBuy(c, p));
            }

            _rowObjects.Add(row);
        }

        private void TryBuy(CharmDefinition charm, int price)
        {
            if (charm == null)
                return;

            bool isQuickHealConsumable = IsQuickHealConsumable(charm);
            if (!isQuickHealConsumable && _inventory.HasCharm(charm))
                return;
            if (!_currency.CanAfford(price))
                return;
            if (!_currency.TrySpend(price))
                return;

            if (isQuickHealConsumable)
            {
                if (_consumables == null && _cachedPlayerRoot != null)
                    _consumables = _cachedPlayerRoot.GetComponent<PlayerConsumables>() ?? _cachedPlayerRoot.gameObject.AddComponent<PlayerConsumables>();
                if (_consumables == null)
                {
                    _currency.Add(price);
                    return;
                }

                _consumables.AddQuickHealCharges(1);
                if (_hintText != null)
                    _hintText.text = $"Quick Heal purchased. Charges: {_consumables.QuickHealCharges}. Press F / B to use.";
            }
            else
            {
                if (!_inventory.AddCharm(charm, autoEquipIfEmpty: true))
                {
                    _currency.Add(price);
                    return;
                }

                if (_hintText != null)
                {
                    bool equipped = _inventory.EquippedCharm == charm;
                    _hintText.text = equipped
                        ? $"{charm.DisplayName} equipped."
                        : $"{charm.DisplayName} purchased. Open Pause -> CHARM to equip.";
                }
            }

            BuildRows();
        }

        private static bool IsQuickHealConsumable(CharmDefinition charm)
        {
            return charm != null &&
                (charm.GrantedAbilities & CharmAbility.QuickHeal) == CharmAbility.QuickHeal;
        }
    }
}
