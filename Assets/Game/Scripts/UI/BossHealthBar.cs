using UnityEngine;
using UnityEngine.UI;
using Game.Combat;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BossHealthBar : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Assign directly, or leave empty and set Encounter to use the spawned boss.")]
    [SerializeField] private HpHealth bossHealth;
    [Tooltip("When the boss is spawned by this encounter, the bar will use its health.")]
    [SerializeField] private EnemyEncounter encounter;

    [Header("UI")]
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.")]
    [SerializeField] private Image fillImage;
    [Tooltip("Optional: hide this when boss is dead or not present.")]
    [SerializeField] private GameObject barRoot;

    private HpHealth _resolvedHealth;
    private EnemyRoot _cachedBossRoot;

    private void Start()
    {
        ResolveFillImage();
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        ResolveHealth();
    }

    private void ResolveFillImage()
    {
        if (fillImage == null || barRoot == null) return;
        if (fillImage.gameObject.name == "Bar")
        {
            var fill = barRoot.transform.Find("Fill");
            if (fill != null)
            {
                var img = fill.GetComponent<Image>();
                if (img != null) fillImage = img;
            }
        }
        if (fillImage != null)
            fillImage.transform.SetAsLastSibling();
    }

    private void Update()
    {
        ResolveHealth();

        if (_resolvedHealth == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (fillImage != null)
        {
            float max = _resolvedHealth.MaxHP;
            float amount = max > 0f ? Mathf.Clamp01(_resolvedHealth.HP / max) : 0f;
            fillImage.fillAmount = amount;
            fillImage.SetAllDirty();
            var fillRect = fillImage.GetComponent<RectTransform>();
            if (fillRect != null)
            {
                fillRect.anchorMax = new Vector2(amount, 1f);
                fillRect.offsetMax = Vector2.zero;
            }
        }

        if (_resolvedHealth.IsDead)
        {
            SetVisible(false);
            _resolvedHealth = null;
            _cachedBossRoot = null;
        }
    }

    private void ResolveHealth()
    {
        if (bossHealth != null)
        {
            _resolvedHealth = bossHealth;
            return;
        }

        if (encounter == null) return;

        var root = encounter.SpawnedBossRoot;
        if (root == null)
        {
            _resolvedHealth = null;
            _cachedBossRoot = null;
            return;
        }

        if (root == _cachedBossRoot && _resolvedHealth != null)
            return;

        _cachedBossRoot = root;
        _resolvedHealth = root.HpHealth != null ? root.HpHealth : root.GetComponentInChildren<HpHealth>();
    }

    private void SetVisible(bool visible)
    {
        if (barRoot != null)
            barRoot.SetActive(visible);
    }
}
