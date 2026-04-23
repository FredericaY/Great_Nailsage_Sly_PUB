#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Game.Player;
using Game.Systems.Charm;
using Game.Systems.Environment;

public static class MoneySystemSetup
{
    [MenuItem("Game/Setup Money System (Geo)")]
    public static void Setup()
    {
        SetupPlayerCurrency();
        SetupHUDGeo();
        SetupCoinDrops();
        Debug.Log("[MoneySystemSetup] Money system setup complete. Enemies now drop coins!");
    }

    [MenuItem("Game/Create Charm Vendor Zone")]
    public static void CreateCharmVendorZone()
    {
        var go = new GameObject("CharmVendor");
        Undo.RegisterCreatedObjectUndo(go, "Create Charm Vendor");

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.5f, 2.5f);

        go.AddComponent<CharmVendor>();

        var dj = AssetDatabase.LoadAssetAtPath<CharmDefinition>(
            "Assets/Game/Scripts/Systems/Charm/CharmList/DoubleJump.asset");
        var st = AssetDatabase.LoadAssetAtPath<CharmDefinition>(
            "Assets/Game/Scripts/Systems/Charm/CharmList/Strength.asset");
        var gm = AssetDatabase.LoadAssetAtPath<CharmDefinition>(
            "Assets/Game/Scripts/Systems/Charm/CharmList/GeoMagnet.asset");
        var qh = AssetDatabase.LoadAssetAtPath<CharmDefinition>(
            "Assets/Game/Scripts/Systems/Charm/CharmList/QuickHeal.asset");

        var vendor = go.GetComponent<CharmVendor>();
        var so = new SerializedObject(vendor);
        var catalog = so.FindProperty("catalog");
        catalog.arraySize = 0;
        if (gm != null)
        {
            catalog.arraySize++;
            catalog.GetArrayElementAtIndex(catalog.arraySize - 1).objectReferenceValue = gm;
        }
        if (qh != null)
        {
            catalog.arraySize++;
            catalog.GetArrayElementAtIndex(catalog.arraySize - 1).objectReferenceValue = qh;
        }
        if (dj != null)
        {
            catalog.arraySize++;
            catalog.GetArrayElementAtIndex(catalog.arraySize - 1).objectReferenceValue = dj;
        }
        if (st != null)
        {
            catalog.arraySize++;
            catalog.GetArrayElementAtIndex(catalog.arraySize - 1).objectReferenceValue = st;
        }
        so.ApplyModifiedProperties();

        Selection.activeGameObject = go;
        Debug.Log("[MoneySystemSetup] CharmVendor created. Position it in the level and save the scene.");
    }

    [MenuItem("Game/Create Geo Coin Prefab")]
    public static void CreateGeoCoinPrefab()
    {
        var prefab = CreateGeoPickupPrefab();
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            Debug.Log("[MoneySystemSetup] Geo coin prefab created at " + AssetDatabase.GetAssetPath(prefab));
        }
    }

    [MenuItem("Game/Create Hidden Wall Reward Prefab")]
    public static void CreateHiddenWallRewardPrefab()
    {
        string prefabPath = "Assets/Game/Prefabs/Environment/HiddenWall_Reward.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log("[MoneySystemSetup] Hidden wall reward prefab already exists: " + prefabPath);
            return;
        }

        var debugSprite = GetOrCreateHiddenWallDebugSprite();

        // Root trigger zone
        var root = new GameObject("HiddenWall_Reward");
        var trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3f, 3f);

        // Visible wall block that will become transparent/walk-through
        var wallVisual = new GameObject("WallVisual");
        wallVisual.transform.SetParent(root.transform, false);
        var wallSr = wallVisual.AddComponent<SpriteRenderer>();
        wallSr.sprite = debugSprite;
        wallSr.drawMode = SpriteDrawMode.Sliced;
        wallSr.size = new Vector2(3f, 3f);
        wallSr.color = new Color(0.45f, 0.65f, 1f, 0.9f);
        wallSr.sortingOrder = 0;
        var wallCol = wallVisual.AddComponent<BoxCollider2D>();
        wallCol.isTrigger = false;
        wallCol.size = new Vector2(3f, 3f);

        // Reward point
        var rewardPoint = new GameObject("RewardSpawnPoint");
        rewardPoint.transform.SetParent(root.transform, false);
        rewardPoint.transform.localPosition = Vector3.zero;

        // Hidden wall behavior
        var hidden = root.AddComponent<HiddenWallReveal>();
        var so = new SerializedObject(hidden);
        so.FindProperty("wallRoot").objectReferenceValue = wallVisual.transform;
        so.FindProperty("ghostAlpha").floatValue = 0.25f;
        so.FindProperty("grantRewardOnFirstEntry").boolValue = true;
        so.FindProperty("rewardSpawnPoint").objectReferenceValue = rewardPoint.transform;
        so.FindProperty("consumableWeight").floatValue = 0.5f;
        so.FindProperty("geoWeight").floatValue = 0.5f;
        so.FindProperty("minGeo").intValue = 8;
        so.FindProperty("maxGeo").intValue = 20;
        so.ApplyModifiedPropertiesWithoutUndo();

        string dir = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = prefab;

        Debug.Log("[MoneySystemSetup] Created one reusable hidden wall prefab with built-in rates at " + prefabPath);
    }

    private static void SetupPlayerCurrency()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[MoneySystemSetup] No GameObject with tag 'Player' found.");
            return;
        }

        var currency = player.GetComponent<PlayerCurrency>();
        if (currency == null)
        {
            currency = Undo.AddComponent<PlayerCurrency>(player);
            Debug.Log("[MoneySystemSetup] Added PlayerCurrency to Player.");
        }
    }

    private static void SetupHUDGeo()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MoneySystemSetup] No Canvas in scene.");
            return;
        }

        Transform hud = canvas.transform.Find("HUD");
        if (hud == null)
        {
            Debug.LogWarning("[MoneySystemSetup] No HUD child under Canvas.");
            return;
        }

        var hudGeo = hud.GetComponent<HUDGeo>();
        if (hudGeo == null)
        {
            hudGeo = Undo.AddComponent<HUDGeo>(hud.gameObject);

            // Create Geo text child
            var textGo = new GameObject("GeoText");
            Undo.RegisterCreatedObjectUndo(textGo, "Create Geo Text");
            textGo.transform.SetParent(hud, worldPositionStays: false);

            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(120f, 40f);

            var text = textGo.AddComponent<Text>();
            text.text = "0";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleRight;

            var so = new SerializedObject(hudGeo);
            so.FindProperty("geoText").objectReferenceValue = text;
            so.ApplyModifiedProperties();

            Debug.Log("[MoneySystemSetup] Added HUDGeo to HUD with GeoText.");
        }
    }

    private static void SetupCoinDrops()
    {
        var prefab = CreateGeoPickupPrefab();
        if (prefab == null)
        {
            Debug.LogError("[MoneySystemSetup] Failed to create GeoPickup prefab.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MoneySystemSetup] No Canvas in scene. Open Level_01 and run Setup again.");
            return;
        }

        var spawner = canvas.GetComponent<GeoPickupSpawner>();
        if (spawner == null)
        {
            spawner = Undo.AddComponent<GeoPickupSpawner>(canvas.gameObject);
        }

        var so = new SerializedObject(spawner);
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[MoneySystemSetup] Coin drops ready. Save the scene (Ctrl+S) and play!");
    }

    private static GameObject CreateGeoPickupPrefab()
    {
        string prefabPath = "Assets/Game/Prefabs/Pickups/GeoPickup.prefab";
        string spritePath = "Assets/Game/Art/Sprite/Pickups/Geo_Coin.png";

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
            return existing;

        // Create coin sprite (simple gold circle)
        if (!AssetDatabase.LoadAssetAtPath<Sprite>(spritePath))
        {
            string dir = System.IO.Path.GetDirectoryName(spritePath);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            int size = 32;
            var tex = new Texture2D(size, size);
            Color gold = new Color(1f, 0.85f, 0.3f);
            Color clear = new Color(0, 0, 0, 0);
            float center = size * 0.5f;
            float radius = center - 2f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d <= radius ? gold : clear);
            }
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(spritePath, png);
            Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.SaveAndReimport();
            }
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) return null;

        // Create prefab - use Ground layer (7) so coin collides with ground tiles
        var go = new GameObject("GeoPickup");
        go.layer = 7; // Ground
        go.transform.localScale = Vector3.one * 0.6f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = 100;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = false;
        col.sharedMaterial = GetOrCreateBounceMaterial();

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        go.AddComponent<GeoPickup>();

        string prefabDir = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(prefabDir))
            System.IO.Directory.CreateDirectory(prefabDir);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static PhysicsMaterial2D GetOrCreateBounceMaterial()
    {
        string path = "Assets/Game/Physics/GeoPickup_Bounce.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat != null) return mat;

        string dir = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        mat = new PhysicsMaterial2D("GeoPickup_Bounce");
        mat.bounciness = 0.55f;
        mat.friction = 0.2f;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Sprite GetOrCreateHiddenWallDebugSprite()
    {
        string spritePath = "Assets/Game/Art/Sprite/Environment/HiddenWall_Debug.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null)
            return existing;

        string dir = System.IO.Path.GetDirectoryName(spritePath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color c = Color.white;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, c);
        tex.Apply();

        System.IO.File.WriteAllBytes(spritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }
}
#endif
