using System.Collections.Generic;
using UnityEngine;
using Game.Combat;
using Game.Enemies;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class FKAttackHitbox : MonoBehaviour
{
    [Header("Fallback Data (used if EnemyAttack source missing)")]
    [SerializeField] private int fallbackDamage = 1;
    [SerializeField] private float fallbackLifeTime = 0.12f;
    [SerializeField] private LayerMask fallbackHittableLayers;

    private readonly HashSet<Collider2D> hitOnce = new();
    private readonly Collider2D[] overlaps = new Collider2D[16];

    private GameObject owner;
    private Vector2 attackDir = Vector2.right;
    private Collider2D triggerCol;
    private EnemyAttack attackData;
    private float lifeTime;

    private void Reset()
    {
        triggerCol = GetComponent<Collider2D>();
        if (triggerCol != null) triggerCol.isTrigger = true;
        fallbackLifeTime = Mathf.Max(0f, fallbackLifeTime);
    }

    private void Awake()
    {
        triggerCol = GetComponent<Collider2D>();
        if (triggerCol != null) triggerCol.isTrigger = true;
        fallbackLifeTime = Mathf.Max(0f, fallbackLifeTime);
    }

    private void OnEnable()
    {
        hitOnce.Clear();
    }

    public void Init(
        GameObject ownerObj,
        Vector2 dir,
        EnemyAttack sourceData)
    {
        owner = ownerObj;
        attackDir = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.right;
        attackData = sourceData;
        lifeTime = attackData != null ? attackData.MeleeHitboxLifetime : fallbackLifeTime;

        // Init happens right after Instantiate+OnEnable, so schedule lifetime here.
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
        ScanOverlapsOnce();
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
        LayerMask layers = attackData != null ? attackData.MeleeHittableLayers : fallbackHittableLayers;
        if (((1 << other.gameObject.layer) & layers) == 0) return;

        if (hitOnce.Contains(other)) return;
        hitOnce.Add(other);

        if (!other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
        }

        int damage = fallbackDamage;
        if (attackData != null)
            damage = attackData.MeleeDamage;

        var info = new DamageInfo
        {
            damage = damage,
            type = DamageType.EnemyAttack,
            hitPoint = other.ClosestPoint(transform.position),
            hitDir = attackDir,
            source = owner
        };

        damageable.TakeDamage(info);
    }
}
