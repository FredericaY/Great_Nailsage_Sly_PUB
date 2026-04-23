using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public abstract class BaseProjectile2D : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] protected bool destroyOnHitDamageTarget = true;
        [SerializeField] protected bool destroyOnWorldCollision = true;
        [SerializeField] protected LayerMask worldCollisionLayers;
        [SerializeField] protected bool ignoreDamageTargetsForWorldCollision = true;
        [SerializeField] protected bool debugLifecycle = false;

        protected ProjectileInitData InitData { get; private set; }
        protected Vector2 Direction { get; set; } = Vector2.right;

        private readonly HashSet<Collider2D> _hitOnce = new();
        private readonly Collider2D[] _overlaps = new Collider2D[16];
        private readonly Collider2D[] _worldOverlaps = new Collider2D[16];
        private Collider2D _triggerCol;
        private float _dieTime;

        protected virtual void Awake()
        {
            _triggerCol = GetComponent<Collider2D>();
            if (_triggerCol != null) _triggerCol.isTrigger = true;
        }

        public virtual void Init(ProjectileInitData initData)
        {
            InitData = initData;
            _hitOnce.Clear();

            Direction = ResolveInitialDirection(initData.castDirection, initData.target);

            _dieTime = Time.time + Mathf.Max(0.01f, ResolveLifeTime());
            if (debugLifecycle)
            {
                Debug.Log(
                    $"[Projectile:{name}#{GetInstanceID()}] Init dir={Direction} speed={ResolveSpeed():0.###} life={ResolveLifeTime():0.###} " +
                    $"owner={(initData.owner != null ? initData.owner.name : "null")} target={(initData.target != null ? initData.target.name : "null")} " +
                    $"moveRange={(initData.moveRange != null ? initData.moveRange.name : "null")}",
                    this);
            }
            OnPostInit(initData);
            ScanOverlapsOnce();
        }

        protected virtual void Update()
        {
            UpdateDirectionIfNeeded();

            float speed = Mathf.Max(0f, ResolveSpeed());
            transform.position += (Vector3)(Direction * speed * Time.deltaTime);

            if (destroyOnWorldCollision && TryGetWorldOverlap(out Collider2D worldHit))
            {
                if (debugLifecycle)
                    Debug.Log($"[Projectile:{name}#{GetInstanceID()}] Despawn by world overlap: {FormatCollider(worldHit)}", this);
                DespawnSelf();
                return;
            }

            if (Time.time >= _dieTime)
            {
                if (debugLifecycle)
                    Debug.Log($"[Projectile:{name}#{GetInstanceID()}] Despawn by lifetime timeout", this);
                DespawnSelf();
                return;
            }

            if (ShouldUseMoveRangeBounds() && InitData.moveRange != null)
            {
                float padding = Mathf.Max(0f, ResolveBoundaryPadding());
                float minX = InitData.moveRange.MinX + padding;
                float maxX = InitData.moveRange.MaxX - padding;
                float x = transform.position.x;
                if (x < minX || x > maxX)
                {
                    if (debugLifecycle)
                        Debug.Log($"[Projectile:{name}#{GetInstanceID()}] Despawn by moveRange X out ({x:0.###} not in [{minX:0.###},{maxX:0.###}])", this);
                    DespawnSelf();
                }
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            bool damaged = TryDamage(other);
            if (damaged && destroyOnHitDamageTarget)
            {
                if (debugLifecycle)
                    Debug.Log($"[Projectile:{name}#{GetInstanceID()}] Despawn by hit damage target on trigger enter: {FormatCollider(other)}", this);
                DespawnSelf();
                return;
            }

            if (!destroyOnWorldCollision) return;
            if (other.isTrigger) return;
            if (ignoreDamageTargetsForWorldCollision && IsDamageTarget(other)) return;
            if (!IsInLayerMask(other.gameObject.layer, ResolveWorldCollisionLayers())) return;
            if (debugLifecycle)
                Debug.Log($"[Projectile:{name}#{GetInstanceID()}] Despawn by world trigger enter: {FormatCollider(other)}", this);
            DespawnSelf();
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            bool damaged = TryDamage(other);
            if (damaged && destroyOnHitDamageTarget)
            {
                if (debugLifecycle)
                    Debug.Log($"[Projectile:{name}#{GetInstanceID()}] Despawn by hit damage target on trigger stay: {FormatCollider(other)}", this);
                DespawnSelf();
            }
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (!destroyOnWorldCollision) return;
            if (ignoreDamageTargetsForWorldCollision && IsDamageTarget(collision.collider)) return;
            if (!IsInLayerMask(collision.gameObject.layer, ResolveWorldCollisionLayers())) return;
            if (debugLifecycle)
                Debug.Log($"[Projectile:{name}#{GetInstanceID()}] Despawn by collision enter: {FormatCollider(collision.collider)}", this);
            DespawnSelf();
        }

        protected bool TryDamage(Collider2D other)
        {
            if (other == null) return false;
            if (InitData.owner != null && other.transform.root == InitData.owner.transform.root) return false;

            LayerMask layers = ResolveHittableLayers();
            if (!IsInLayerMask(other.gameObject.layer, layers)) return false;

            if (_hitOnce.Contains(other)) return false;
            _hitOnce.Add(other);

            if (!other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable = other.GetComponentInParent<IDamageable>();
                if (damageable == null) return false;
            }

            var info = new DamageInfo
            {
                damage = Mathf.Max(0, ResolveDamage()),
                type = ResolveDamageType(),
                hitPoint = other.ClosestPoint(transform.position),
                hitDir = Direction,
                source = InitData.owner
            };

            return damageable.TakeDamage(info);
        }

        protected virtual void DespawnSelf()
        {
            if (debugLifecycle)
                Debug.Log($"[Projectile:{name}#{GetInstanceID()}] DespawnSelf()", this);
            ProjectilePoolService.Despawn(this);
        }

        protected virtual void ScanOverlapsOnce()
        {
            if (_triggerCol == null) return;

            int count = Physics2D.OverlapCollider(_triggerCol, new ContactFilter2D().NoFilter(), _overlaps);
            for (int i = 0; i < count; i++)
            {
                var c = _overlaps[i];
                if (c != null) TryDamage(c);
            }
        }

        protected static bool IsDamageTarget(Collider2D other)
        {
            return other.GetComponent<IDamageable>() != null
                   || other.GetComponentInParent<IDamageable>() != null;
        }

        protected static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return ((1 << layer) & mask.value) != 0;
        }

        protected virtual void OnPostInit(ProjectileInitData initData) { }
        protected virtual void UpdateDirectionIfNeeded() { }
        protected virtual LayerMask ResolveWorldCollisionLayers()
        {
            if (worldCollisionLayers.value != 0)
                return worldCollisionLayers;

            return LayerMask.GetMask("Ground", "Wall");
        }

        private bool TryGetWorldOverlap(out Collider2D firstHit)
        {
            firstHit = null;
            if (_triggerCol == null) return false;

            LayerMask worldLayers = ResolveWorldCollisionLayers();
            if (worldLayers.value == 0) return false;

            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = worldLayers,
                useTriggers = false
            };

            int count = Physics2D.OverlapCollider(_triggerCol, filter, _worldOverlaps);
            for (int i = 0; i < count; i++)
            {
                Collider2D c = _worldOverlaps[i];
                if (c == null) continue;
                if (InitData.owner != null && c.transform.root == InitData.owner.transform.root) continue;
                if (ignoreDamageTargetsForWorldCollision && IsDamageTarget(c)) continue;
                firstHit = c;
                return true;
            }

            return false;
        }

        private static string FormatCollider(Collider2D c)
        {
            if (c == null) return "null";
            return $"{c.name} (layer={LayerMask.LayerToName(c.gameObject.layer)}, trigger={c.isTrigger})";
        }

        protected abstract int ResolveDamage();
        protected abstract LayerMask ResolveHittableLayers();
        protected abstract float ResolveSpeed();
        protected abstract float ResolveLifeTime();
        protected abstract Vector2 ResolveInitialDirection(Vector2 castDirection, Transform targetRef);
        protected virtual DamageType ResolveDamageType() => DamageType.EnemyAttack;
        protected virtual bool ShouldUseMoveRangeBounds() => false;
        protected virtual float ResolveBoundaryPadding() => 0.05f;
    }
}
