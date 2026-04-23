using UnityEngine;

/// <summary>
/// Spawns geo coin pickups when enemies die. Add to scene (e.g. on a manager object).
/// </summary>
public class GeoPickupSpawner : MonoBehaviour
{
    [SerializeField] private GeoPickup prefab;

    private static GeoPickupSpawner _instance;

    public static GeoPickupSpawner Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<GeoPickupSpawner>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>Spawn coins at world position. Uses denominations: 5, 1 (e.g. 50 geo = 10 coins).</summary>
    /// <param name="isFlying">If true, coins scatter in a random arc (for flying enemies).</param>
    public void Spawn(int geoValue, Vector3 position, bool isFlying = false)
    {
        if (geoValue <= 0) return;
        if (prefab == null)
        {
            Debug.LogWarning("[GeoPickupSpawner] No prefab assigned! Run Game > Setup Money System (Geo) and save the scene.");
            return;
        }

        int remaining = geoValue;

        while (remaining >= 5)
        {
            SpawnOne(position, 5, isFlying);
            remaining -= 5;
        }
        while (remaining >= 1)
        {
            SpawnOne(position, 1, isFlying);
            remaining -= 1;
        }
    }

    private void SpawnOne(Vector3 position, int value, bool isFlying)
    {
        // Spawn at enemy position with tiny random offset so coins burst from the enemy
        float offsetX = Random.Range(-0.2f, 0.2f);
        float offsetY = Random.Range(0f, 0.3f);
        var spawnPos = position + new Vector3(offsetX, offsetY, 0f);

        var instance = Instantiate(prefab, spawnPos, Quaternion.identity);
        instance.Init(value, isFlying);
    }
}
