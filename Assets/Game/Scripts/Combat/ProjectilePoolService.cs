using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Scene-level shared pool for projectile prefabs.
    /// Place one instance under your scene "System" object.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public class ProjectilePoolService : MonoBehaviour
    {
        [Header("Default")]
        [SerializeField] private int defaultPrewarmCount = 0;

        public static ProjectilePoolService Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> _inactiveByPrefab = new();
        private readonly Dictionary<GameObject, GameObject> _prefabByInstance = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            if (prefab == null) return null;

            TryEnsureInstance();

            if (Instance == null)
                return Object.Instantiate(prefab, position, rotation, parent);

            return Instance.SpawnInternal(prefab, position, rotation, parent);
        }

        public static void Despawn(Component instance)
        {
            if (instance == null) return;

            TryEnsureInstance();

            if (Instance == null || !Instance.TryDespawnInternal(instance.gameObject))
            {
                Object.Destroy(instance.gameObject);
            }
        }

        public static void Despawn(Component instance, float delay)
        {
            if (instance == null) return;

            if (delay <= 0f)
            {
                Despawn(instance);
                return;
            }

            if (Instance == null)
            {
                Object.Destroy(instance.gameObject, delay);
                return;
            }

            Instance.StartCoroutine(Instance.DespawnAfterDelay(instance.gameObject, delay));
        }

        public void Prewarm<T>(T prefab, int count) where T : Component
        {
            if (prefab == null) return;
            count = Mathf.Max(0, count);
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                T instance = CreateFresh(prefab, transform);
                ReturnToPool(instance.gameObject, prefab.gameObject);
            }
        }

        private IEnumerator DespawnAfterDelay(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go == null) yield break;

            if (!TryDespawnInternal(go))
                Destroy(go);
        }

        private T SpawnInternal<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component
        {
            GameObject prefabKey = prefab.gameObject;
            EnsurePool(prefabKey);

            Queue<GameObject> q = _inactiveByPrefab[prefabKey];
            GameObject go = null;

            while (q.Count > 0 && go == null)
                go = q.Dequeue();

            if (go == null)
                go = CreateFresh(prefab, parent).gameObject;
            else
                go.transform.SetParent(parent, worldPositionStays: false);

            _prefabByInstance[go] = prefabKey;

            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);

            return go.GetComponent<T>();
        }

        private bool TryDespawnInternal(GameObject instance)
        {
            if (instance == null) return true;
            if (!_prefabByInstance.TryGetValue(instance, out var prefabKey)) return false;
            ReturnToPool(instance, prefabKey);
            return true;
        }

        private void ReturnToPool(GameObject instance, GameObject prefabKey)
        {
            if (instance == null || prefabKey == null) return;
            EnsurePool(prefabKey);

            // Reset common runtime state before parking.
            var rb2d = instance.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.velocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }

            instance.SetActive(false);
            instance.transform.SetParent(transform, worldPositionStays: false);
            _inactiveByPrefab[prefabKey].Enqueue(instance);
        }

        private void EnsurePool(GameObject prefabKey)
        {
            if (_inactiveByPrefab.ContainsKey(prefabKey)) return;

            _inactiveByPrefab[prefabKey] = new Queue<GameObject>();

            if (defaultPrewarmCount <= 0) return;

            for (int i = 0; i < defaultPrewarmCount; i++)
            {
                var go = Instantiate(prefabKey, transform);
                _prefabByInstance[go] = prefabKey;
                go.SetActive(false);
                _inactiveByPrefab[prefabKey].Enqueue(go);
            }
        }

        private T CreateFresh<T>(T prefab, Transform parent) where T : Component
        {
            T instance = Instantiate(prefab, parent);
            _prefabByInstance[instance.gameObject] = prefab.gameObject;
            return instance;
        }

        private static void TryEnsureInstance()
        {
            if (Instance != null) return;
            Instance = FindObjectOfType<ProjectilePoolService>();
        }
    }
}
