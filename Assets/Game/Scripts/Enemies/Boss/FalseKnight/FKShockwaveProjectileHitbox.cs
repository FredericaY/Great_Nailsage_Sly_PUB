using System.Collections.Generic;
using UnityEngine;
using Game.Combat;
using Game.Enemies;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class FKShockwaveProjectileHitbox : MonoBehaviour
{
    [Header("Fallback Data (if EnemyProjectile source missing)")]
    [SerializeField] private int fallbackDamage = 1;
    [SerializeField] private LayerMask fallbackHittableLayers;
    [SerializeField] private float fallbackSpeed = 10f;
    [SerializeField] private float fallbackLifeTime = 2f;

    [Header("World Collision Destroy")]
    [SerializeField] private bool destroyOnWorldCollision = true;
    [SerializeField] private LayerMask worldCollisionLayers;
    [SerializeField] private bool ignoreDamageTargetsForWorldCollision = true;

    private readonly HashSet<Collider2D> hitOnce = new();
    private readonly Collider2D[] overlaps = new Collider2D[16];

    private Collider2D triggerCol;
    private GameObject owner;
    private EnemyProjectile projectileData;
    private EnemyMoveRange2D moveRange;
    private Transform target;

    private Vector2 dir = Vector2.right;
    private float dieTime;
    private float homingEndTime;
    private float nextHomingUpdateTime;

    private void Awake()
    {
        triggerCol = GetComponent<Collider2D>();
        if (triggerCol != null) triggerCol.isTrigger = true;
    }

    public void Init(
        GameObject ownerObj,
        Vector2 forwardDirAtCast,
        EnemyMoveRange2D moveRangeRef,
        EnemyProjectile projectileDataRef,
        Transform targetRef)
    {
        owner = ownerObj;
        hitOnce.Clear();
        ScanOverlapsOnce();

        projectileData = projectileDataRef;
        moveRange = moveRangeRef;
        target = targetRef != null
            ? targetRef
            : (projectileData != null ? projectileData.DefaultTarget : null);

        dir = ResolveInitialDirection(forwardDirAtCast, target);

        float life = projectileData != null ? projectileData.MaxLifeTime : fallbackLifeTime;
        dieTime = Time.time + life;
        homingEndTime = Time.time + (projectileData != null ? projectileData.HomingDuration : 0f);
        nextHomingUpdateTime = Time.time;
    }

    private void Update()
    {
        float speed = projectileData != null ? projectileData.Speed : fallbackSpeed;

        UpdateHomingDirectionIfNeeded();

        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        if (Time.time >= dieTime)
        {
            Destroy(gameObject);
            return;
        }

        bool useRange = projectileData == null || projectileData.DestroyOnMoveRangeBound;
        if (!useRange || moveRange == null) return;

        float padding = projectileData != null ? projectileData.BoundaryPadding : 0.05f;
        float minX = moveRange.MinX + padding;
        float maxX = moveRange.MaxX - padding;
        float x = transform.position.x;

        if (x <= minX || x >= maxX)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
        if (!destroyOnWorldCollision) return;
        if (ignoreDamageTargetsForWorldCollision && IsDamageTarget(other)) return;
        if (!IsInLayerMask(other.gameObject.layer, worldCollisionLayers)) return;
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!destroyOnWorldCollision) return;
        if (ignoreDamageTargetsForWorldCollision && IsDamageTarget(collision.collider)) return;
        if (!IsInLayerMask(collision.gameObject.layer, worldCollisionLayers)) return;
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private Vector2 ResolveInitialDirection(Vector2 forwardDirAtCast, Transform targetRef)
    {
        Vector2 forward = forwardDirAtCast.sqrMagnitude > 0f ? forwardDirAtCast.normalized : Vector2.right;
        float faceX = Mathf.Sign(forward.x == 0f ? 1f : forward.x);

        if (projectileData == null) return new Vector2(faceX, 0f);

        if (projectileData.Kind == EnemyProjectile.ProjectileKind.GroundForwardX)
            return new Vector2(faceX, 0f);

        if (targetRef == null) return forward;

        Vector2 aimed = ((Vector2)targetRef.position - (Vector2)transform.position).normalized;
        if (aimed.sqrMagnitude <= 0.0001f) return forward;
        return aimed;
    }

    private void UpdateHomingDirectionIfNeeded()
    {
        if (projectileData == null) return;
        if (projectileData.Kind != EnemyProjectile.ProjectileKind.TimedHomingTarget) return;
        if (target == null) return;
        if (Time.time > homingEndTime) return;
        if (Time.time < nextHomingUpdateTime) return;

        Vector2 desired = ((Vector2)target.position - (Vector2)transform.position).normalized;
        if (desired.sqrMagnitude > 0.0001f)
            dir = desired;

        nextHomingUpdateTime = Time.time + projectileData.HomingUpdateInterval;
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }

    private void ScanOverlapsOnce()
    {
        if (triggerCol == null) return;

        int count = Physics2D.OverlapCollider(triggerCol, new ContactFilter2D().NoFilter(), overlaps);
        for (int i = 0; i < count; i++)
        {
            var c = overlaps[i];
            if (c != null) TryHit(c);
        }
    }

    private void TryHit(Collider2D other)
    {
        if (other == null) return;
        if (owner != null && other.transform.root == owner.transform.root) return;

        LayerMask layers = projectileData != null ? projectileData.ProjectileHittableLayers : fallbackHittableLayers;
        if (!IsInLayerMask(other.gameObject.layer, layers)) return;

        if (hitOnce.Contains(other)) return;
        hitOnce.Add(other);

        if (!other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
        }

        int damage = projectileData != null ? projectileData.ProjectileDamage : fallbackDamage;
        var info = new DamageInfo
        {
            damage = damage,
            type = DamageType.EnemyAttack,
            hitPoint = other.ClosestPoint(transform.position),
            hitDir = dir,
            source = owner
        };

        damageable.TakeDamage(info);
    }

    private static bool IsDamageTarget(Collider2D other)
    {
        return other.GetComponent<IDamageable>() != null
            || other.GetComponentInParent<IDamageable>() != null;
    }
}
