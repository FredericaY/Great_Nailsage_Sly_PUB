using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Combat;

[DisallowMultipleComponent]
public class HUDHearts : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private HeartsHealth heartsSource;
    [SerializeField] private string playerTag = "Player";

    [Header("UI")]
    [SerializeField] private RectTransform heartsContainer;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Vector2 heartSize = new Vector2(28f, 28f);
    [SerializeField] private float manualSpacing = 4f;

    [Header("Colors")]
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    private readonly List<Image> heartIcons = new();
    private int cachedMaxHearts = -1;
    private bool subscribed;
    private float nextAutoFindTime;

    private void Reset()
    {
        if (heartsContainer == null)
            heartsContainer = transform as RectTransform;
    }

    private void OnEnable()
    {
        if (heartsContainer == null)
            heartsContainer = transform as RectTransform;

        TryConnectSource(force: true);
        RebuildIfNeeded();
        RefreshHeartsVisual();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (heartsSource == null)
        {
            TryConnectSource(force: false);
            return;
        }

        if (cachedMaxHearts != heartsSource.MaxHearts)
        {
            RebuildIfNeeded();
            RefreshHeartsVisual();
        }
    }

    private void TryConnectSource(bool force)
    {
        if (!force && Time.time < nextAutoFindTime) return;
        nextAutoFindTime = Time.time + 0.5f;

        if (heartsSource == null)
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player != null)
                heartsSource = player.GetComponent<HeartsHealth>() ?? player.GetComponentInChildren<HeartsHealth>();
        }

        if (heartsSource != null)
            Subscribe();
    }

    private void Subscribe()
    {
        if (subscribed || heartsSource == null) return;
        heartsSource.OnHeartsChanged += HandleHeartsChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || heartsSource == null) return;
        heartsSource.OnHeartsChanged -= HandleHeartsChanged;
        subscribed = false;
    }

    private void HandleHeartsChanged(int _)
    {
        RefreshHeartsVisual();
    }

    private void RebuildIfNeeded()
    {
        if (heartsSource == null || heartsContainer == null || heartSprite == null) return;
        if (cachedMaxHearts == heartsSource.MaxHearts && heartIcons.Count == cachedMaxHearts) return;

        ClearIcons();

        int max = Mathf.Max(0, heartsSource.MaxHearts);
        cachedMaxHearts = max;
        for (int i = 0; i < max; i++)
        {
            var go = new GameObject($"Heart_{i + 1}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(heartsContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = heartSize;

            var img = go.GetComponent<Image>();
            img.sprite = heartSprite;
            img.raycastTarget = false;
            heartIcons.Add(img);
        }

        RepositionIfNoLayoutGroup();
    }

    private void RepositionIfNoLayoutGroup()
    {
        if (heartsContainer == null) return;
        if (heartsContainer.GetComponent<HorizontalLayoutGroup>() != null) return;

        for (int i = 0; i < heartIcons.Count; i++)
        {
            var rt = heartIcons[i].rectTransform;
            float x = i * (heartSize.x + manualSpacing);
            rt.anchoredPosition = new Vector2(x, 0f);
        }
    }

    private void RefreshHeartsVisual()
    {
        if (heartsSource == null) return;
        if (heartIcons.Count == 0) return;

        int current = Mathf.Clamp(heartsSource.Hearts, 0, heartIcons.Count);
        for (int i = 0; i < heartIcons.Count; i++)
        {
            heartIcons[i].color = (i < current) ? fullColor : emptyColor;
        }
    }

    private void ClearIcons()
    {
        for (int i = heartIcons.Count - 1; i >= 0; i--)
        {
            if (heartIcons[i] != null)
                Destroy(heartIcons[i].gameObject);
        }
        heartIcons.Clear();
    }
}
