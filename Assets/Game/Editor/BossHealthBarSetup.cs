#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class BossHealthBarSetup
{
    [MenuItem("Game/Create Boss Health Bar")]
    public static void Create()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[BossHealthBarSetup] No Canvas in scene. Create a Canvas first.");
            return;
        }

        var root = new GameObject("BossHealthBar");
        root.transform.SetParent(canvas.transform, worldPositionStays: false);

        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(300f, 24f);

        var barRoot = new GameObject("Bar");
        barRoot.transform.SetParent(root.transform, false);
        var barRect = barRoot.AddComponent<RectTransform>();
        barRect.anchorMin = Vector2.zero;
        barRect.anchorMax = Vector2.one;
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;

        var bg = barRoot.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barRoot.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        var bar = root.AddComponent<BossHealthBar>();
        var so = new SerializedObject(bar);
        so.FindProperty("fillImage").objectReferenceValue = fill;
        so.FindProperty("barRoot").objectReferenceValue = barRoot;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(root, "Create Boss Health Bar");
        Selection.activeGameObject = root;
        Debug.Log("[BossHealthBarSetup] Boss Health Bar created. Assign the Encounter (e.g. Encounter_Boss_FalseKnight) to link the boss.");
    }
}
#endif
