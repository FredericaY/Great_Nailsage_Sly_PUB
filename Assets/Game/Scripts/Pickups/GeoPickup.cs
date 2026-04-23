using UnityEngine;
using Game.Player;
using Game.Systems.Charm;

/// <summary>
/// Coin that drops when enemies die. Drops with physics, collected when player walks over it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GeoPickup : MonoBehaviour
{
    [Header("Value")]
    [SerializeField] private int geoValue = 5;

    [Header("Drop")]
    [SerializeField] private float scatterForceMin = 1.5f;
    [SerializeField] private float scatterForceMax = 3.5f;
    [SerializeField] private float upwardBias = 1.2f;
    [SerializeField] private float bounciness = 0.4f;

    [Header("Collection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float collectDelay = 0.2f;
    [SerializeField] private float magnetRadius = 2.5f;
    [SerializeField] private float magnetPullSpeed = 10f;
    [SerializeField] private float magnetCollectDistance = 0.4f;

    private float _spawnTime;
    private bool _collected;
    private bool _scatterFromFlying;
    private PlayerRoot _playerRoot;
    private PlayerCharmRuntime _playerCharmRuntime;
    private Rigidbody2D _rb;

    private static PhysicsMaterial2D _bounceMaterial;

    public void Init(int value, bool scatterFromFlying = false)
    {
        geoValue = Mathf.Max(1, value);
        _scatterFromFlying = scatterFromFlying;
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            // Always apply bounce at runtime (prefab material reference can break)
            if (_bounceMaterial == null)
                _bounceMaterial = new PhysicsMaterial2D("GeoBounce") { bounciness = bounciness, friction = 0.2f };
            col.sharedMaterial = _bounceMaterial;
        }

        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.gravityScale = 1f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation; // No rolling
        }

        CachePlayerRefs();
    }

    private void Start()
    {
        _spawnTime = Time.time;

        if (_rb != null)
        {
            Vector2 dir;
            if (_scatterFromFlying)
            {
                // Random arc in any direction for flying enemies
                float angle = Random.Range(0f, 2f * Mathf.PI);
                dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            else
            {
                // Ground enemies: burst up and slightly sideways
                float angle = Random.Range(-0.5f, 0.5f) * Mathf.PI;
                dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle) + upwardBias).normalized;
            }
            float force = Random.Range(scatterForceMin, scatterForceMax);
            _rb.AddForce(dir * force, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        if (_collected || Time.time - _spawnTime < collectDelay)
            return;

        if (_playerRoot == null || _playerCharmRuntime == null)
            CachePlayerRefs();
        if (_playerRoot == null || _playerCharmRuntime == null)
            return;
        if (!_playerCharmRuntime.HasGeoMagnetAbility())
            return;

        Vector2 toPlayer = _playerRoot.transform.position - transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;
        float radius = Mathf.Max(0.1f, magnetRadius);
        if (sqrDistance > radius * radius)
            return;

        Vector2 dir = toPlayer.normalized;
        transform.position += (Vector3)(dir * magnetPullSpeed * Time.deltaTime);
        if (_rb != null)
            _rb.velocity = Vector2.Lerp(_rb.velocity, dir * magnetPullSpeed, 0.35f);

        if (Mathf.Sqrt(sqrDistance) <= Mathf.Max(0.05f, magnetCollectDistance))
            CollectToPlayer(_playerRoot);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_collected) return;

        var other = collision.collider;

        // Simulate bounce when hitting ground (ground has no physics material, so physics bounce = 0)
        if (other.gameObject.layer == 7) // Ground layer
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null && rb.velocity.y < -0.3f)
            {
                float bounceUp = Mathf.Abs(rb.velocity.y) * bounciness * 0.3f;
                rb.velocity = new Vector2(rb.velocity.x * 0.6f, bounceUp);
            }
        }

        if (Time.time - _spawnTime < collectDelay) return;
        if (!other.CompareTag(playerTag)) return;

        var player = other.GetComponentInParent<PlayerRoot>();
        if (player == null) return;

        CollectToPlayer(player);
    }

    private void CollectToPlayer(PlayerRoot player)
    {
        if (player == null || _collected)
            return;

        var currency = player.GetComponent<PlayerCurrency>() ?? player.GetComponentInChildren<PlayerCurrency>();
        if (currency != null)
        {
            _collected = true;
            currency.Add(geoValue);
            Destroy(gameObject);
        }
    }

    private void CachePlayerRefs()
    {
        GameObject playerGo = GameObject.FindWithTag(playerTag);
        if (playerGo == null)
            return;

        _playerRoot = playerGo.GetComponent<PlayerRoot>() ?? playerGo.GetComponentInChildren<PlayerRoot>();
        _playerCharmRuntime = playerGo.GetComponent<PlayerCharmRuntime>() ?? playerGo.GetComponentInChildren<PlayerCharmRuntime>();
    }
}
