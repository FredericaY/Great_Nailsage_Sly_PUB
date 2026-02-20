using UnityEngine;
using Game.Enemies;

[DisallowMultipleComponent]
public class FKAttackEmitter : MonoBehaviour
{
    public enum AttackKind
    {
        None = 0,
        Normal = 1,
        Wave = 2,
        JumpAttack = 3
    }

    [Header("Refs")]
    [SerializeField] private EnemyRoot root;
    [SerializeField] private EnemyBlackboard blackboard;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private EnemyAttack attackData;
    [SerializeField] private EnemyProjectile projectileData;

    [Header("Prefabs")]
    [SerializeField] private FKAttackHitbox normalHitboxPrefab;
    [SerializeField] private FKShockwaveProjectileHitbox waveProjectilePrefab;
    [SerializeField] private FKJumpAttackHitbox jumpAttackHitboxPrefab;

    [Header("Animation Triggers")]
    [SerializeField] private string normalAttackTrigger = "NormalAttack";
    [SerializeField] private string waveAttackTrigger = "WaveAttack";
    [SerializeField] private string jumpAttackTrigger = "JumpAttack";

    [Header("Spawn Offsets (local to spawnPoint)")]
    [SerializeField] private Vector2 normalOffset = new Vector2(1.25f, 0.8f);
    [SerializeField] private Vector2 waveOffset = new Vector2(1.4f, 0.35f);
    [SerializeField] private Vector2 jumpAttackOffset = new Vector2(1.25f, 0.8f);
    [SerializeField] private bool waveOffsetInRootLocalSpace = true;

    [Header("Spawn Parenting")]
    [SerializeField] private bool parentMeleeHitboxToRoot = true;
    [SerializeField] private bool detachProjectileFromRootAfterSpawn = true;

    [Header("Safety")]
    [SerializeField] private float attackLockTimeout = 3f;

    [Header("State")]
    [SerializeField] private AttackKind pendingAttackKind = AttackKind.None;
    [SerializeField] private bool isAttacking;
    private float attackLockExpireTime;

    public bool IsAttacking => isAttacking;
    public AttackKind PendingAttackKind => pendingAttackKind;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
    }

    private void Update()
    {
        if (!isAttacking) return;
        if (Time.time <= attackLockExpireTime) return;

        // Fallback unlock if animation event was missed.
        isAttacking = false;
        pendingAttackKind = AttackKind.None;
    }

    private void AutoWire()
    {
        if (!root) root = GetComponent<EnemyRoot>();
        if (!root) root = GetComponentInParent<EnemyRoot>();

        if (!blackboard) blackboard = GetComponent<EnemyBlackboard>();
        if (!blackboard && root != null) blackboard = root.Blackboard;
        if (!blackboard) blackboard = GetComponentInParent<EnemyBlackboard>();

        if (!animator) animator = root != null ? root.Animator : GetComponentInChildren<Animator>();
        if (!spawnPoint) spawnPoint = transform;

        if (!attackData) attackData = GetComponent<EnemyAttack>();
        if (!attackData) attackData = GetComponentInParent<EnemyAttack>();

        if (!projectileData) projectileData = GetComponent<EnemyProjectile>();
        if (!projectileData) projectileData = GetComponentInParent<EnemyProjectile>();
    }

    public bool RequestNormalAttack()
    {
        if (isAttacking) return false;
        pendingAttackKind = AttackKind.Normal;
        isAttacking = true;
        attackLockExpireTime = Time.time + Mathf.Max(0.5f, attackLockTimeout);
        TryTrigger(normalAttackTrigger);
        return true;
    }

    public bool RequestWaveAttack()
    {
        if (isAttacking) return false;
        pendingAttackKind = AttackKind.Wave;
        isAttacking = true;
        attackLockExpireTime = Time.time + Mathf.Max(0.5f, attackLockTimeout);
        TryTrigger(waveAttackTrigger);
        return true;
    }

    public bool RequestJumpAttack()
    {
        if (isAttacking) return false;
        pendingAttackKind = AttackKind.JumpAttack;
        isAttacking = true;
        attackLockExpireTime = Time.time + Mathf.Max(0.5f, attackLockTimeout);
        TryTrigger(jumpAttackTrigger);
        return true;
    }

    /// <summary>
    /// Call from attack clip event at the hit frame.
    /// Normal: spawn one melee hitbox.
    /// Wave: spawn melee hitbox + shockwave projectile.
    /// </summary>
    public void AnimEvent_SpawnAttackPayload()
    {
        if (pendingAttackKind == AttackKind.None) return;

        SpawnNormalHitbox();

        if (pendingAttackKind == AttackKind.Wave)
            SpawnShockwave();
    }

    /// <summary>
    /// Call at swing frame. Works for both normal and wave attack.
    /// </summary>
    public void AnimEvent_SpawnMeleeHitbox()
    {
        if (pendingAttackKind == AttackKind.None) return;
        SpawnNormalHitbox();
    }

    /// <summary>
    /// Call at impact/ground frame. Only spawns for wave attack.
    /// </summary>
    public void AnimEvent_SpawnWaveProjectile()
    {
        if (pendingAttackKind != AttackKind.Wave) return;
        SpawnShockwave();
    }

    /// <summary>
    /// Call from JumpAttackLand clip event.
    /// </summary>
    public void AnimEvent_SpawnJumpAttackHitbox()
    {
        if (pendingAttackKind != AttackKind.JumpAttack) return;
        SpawnJumpAttackHitbox();
    }

    /// <summary>
    /// Call from animation event at the end of the attack motion.
    /// </summary>
    public void AnimEvent_AttackEnd()
    {
        isAttacking = false;
        pendingAttackKind = AttackKind.None;
    }

    private void SpawnNormalHitbox()
    {
        if (!normalHitboxPrefab) return;

        int face = GetFacingSign();
        Vector2 dir = new Vector2(face, 0f);
        Vector2 offset = new Vector2(normalOffset.x * face, normalOffset.y);
        Vector3 worldPos = spawnPoint.TransformPoint(offset);

        Transform parent = (parentMeleeHitboxToRoot && root != null) ? root.transform : null;
        FKAttackHitbox hb = Instantiate(normalHitboxPrefab, worldPos, Quaternion.identity, parent);
        hb.Init(gameObject, dir, attackData);
    }

    private void SpawnShockwave()
    {
        if (!waveProjectilePrefab) return;

        int face = GetFacingSign();
        Vector2 dir = new Vector2(face, 0f);
        Vector3 worldPos;

        if (waveOffsetInRootLocalSpace && root != null)
        {
            // Root local +X is treated as "forward"; root flip mirrors it automatically.
            worldPos = root.transform.TransformPoint(new Vector3(waveOffset.x, waveOffset.y, 0f));
        }
        else
        {
            Vector2 offset = new Vector2(waveOffset.x * face, waveOffset.y);
            worldPos = spawnPoint.TransformPoint(offset);
        }

        Transform rootParent = root != null ? root.transform : null;
        FKShockwaveProjectileHitbox wave = Instantiate(waveProjectilePrefab, worldPos, Quaternion.identity, rootParent);

        if (detachProjectileFromRootAfterSpawn)
            wave.transform.SetParent(null, true);

        wave.Init(
            gameObject,
            dir,
            root != null ? root.MoveRange : null,
            projectileData,
            blackboard != null ? blackboard.player : null
        );
    }

    private void SpawnJumpAttackHitbox()
    {
        if (!jumpAttackHitboxPrefab) return;

        int face = GetFacingSign();
        Vector2 dir = new Vector2(face, 0f);
        Vector2 offset = new Vector2(jumpAttackOffset.x * face, jumpAttackOffset.y);
        Vector3 worldPos = spawnPoint.TransformPoint(offset);

        Transform parent = (parentMeleeHitboxToRoot && root != null) ? root.transform : null;
        FKJumpAttackHitbox hb = Instantiate(jumpAttackHitboxPrefab, worldPos, Quaternion.identity, parent);
        hb.Init(gameObject, dir, attackData);
    }

    private int GetFacingSign()
    {
        if (blackboard != null) return blackboard.facingRight ? 1 : -1;
        return transform.localScale.x >= 0f ? 1 : -1;
    }

    private void TryTrigger(string trigger)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(trigger)) return;
        animator.SetTrigger(trigger);
    }
}
