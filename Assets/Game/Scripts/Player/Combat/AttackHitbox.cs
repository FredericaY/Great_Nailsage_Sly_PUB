using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

namespace Game.Player
{
    // ─────────────────────────────
    // AttackHitbox
    // - A short-lived trigger hitbox that applies damage once per collider.
    // - Intended to be spawned/enabled by PlayerCombat.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class AttackHitbox : MonoBehaviour
    {
        // ─────────────────────────────
        // Config
        // ─────────────────────────────
        [Header("Config")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float lifeTime = 0.12f;
        [SerializeField] private LayerMask hittableLayers;

        // ─────────────────────────────
        // Runtime state
        // ─────────────────────────────
        private readonly HashSet<Collider2D> _hitOnce = new();

        private GameObject _owner;
        private Vector2 _attackDir = Vector2.right;

        private Collider2D _trigger;
        
        // Reusable buffer (avoid GC)
        private readonly Collider2D[] _overlaps = new Collider2D[16];

        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Reset()
        {
            // Ensure our Collider2D is configured as a trigger
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null) _trigger.isTrigger = true;

            // Keep values reasonable
            lifeTime = Mathf.Max(0f, lifeTime);
        }

        private void Awake()
        {
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null) _trigger.isTrigger = true;

            lifeTime = Mathf.Max(0f, lifeTime);
        }

        private void OnEnable()
        {
            _hitOnce.Clear();
            ScanOverlapsOnce();
            if (lifeTime > 0f)
                Destroy(gameObject, lifeTime);
        }

        /// <summary>
        /// Initialize the hitbox after spawn/enabling.
        /// </summary>
        public void Init(GameObject owner, int dmg, Vector2 dir)
        {
            _owner = owner;
            damage = Mathf.Max(0, dmg);
            _attackDir = (dir.sqrMagnitude > 0f) ? dir.normalized : Vector2.right;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHit(other);
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            TryHit(other);
        }

        private void ScanOverlapsOnce()
        {
            if (_trigger == null) return;
            
            int count = Physics2D.OverlapCollider(_trigger, new ContactFilter2D().NoFilter(), _overlaps);
            for (int i = 0; i < count; i++)
            {
                var c = _overlaps[i];
                if (c != null) TryHit(c);
            }
        }

        private void TryHit(Collider2D other)
        {
            // Layer filtering
            if (((1 << other.gameObject.layer) & hittableLayers) == 0) return;

            // Hit each collider only once per activation
            if (_hitOnce.Contains(other)) return;
            _hitOnce.Add(other);
            
            if (!other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable = other.GetComponentInParent<IDamageable>();
                if (damageable == null) return;
            }

            var info = new DamageInfo
            {
                damage = damage,
                type = DamageType.PlayerAttack, // ✅ 别忘了
                hitPoint = other.ClosestPoint(transform.position),
                hitDir = _attackDir,
                source = _owner
            };

            damageable.TakeDamage(info);
        }
    }
}
