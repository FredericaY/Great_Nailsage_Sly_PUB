using UnityEngine;
using Game.Combat;
using Game.Enemies;
using Game.Player;

[DisallowMultipleComponent]
public class ASPAttackEmitter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyRoot root;
    [SerializeField] private EnemyBlackboard blackboard;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private EnemyProjectileData projectileData;
    [SerializeField] private EnemyAggroSensor2D aggroSensor;
    [SerializeField] private ASPAudioEmitter audioEmitter;

    [Header("Projectile Prefab")]
    [SerializeField] private ASPProjectileHitbox projectilePrefab;

    [Header("Animation")]
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Spawn Offset (local to spawnPoint)")]
    [SerializeField] private Vector2 projectileOffset = new Vector2(0.8f, 0.1f);

    [Header("Attack Gate")]
    [SerializeField] private float attackLockTimeout = 1.5f;

    [Header("Range Clamp")]
    [SerializeField] private bool clampToMoveRangeAlways = true;
    [SerializeField] private float moveRangeEdgePadding = 0.2f;

    private bool isAttacking;
    private float attackLockExpireTime;
    private Rigidbody2D rb;

    public bool IsAttacking => isAttacking;
    public bool CanAttackNow => !isAttacking;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
    }

    private void FixedUpdate()
    {
        if (!clampToMoveRangeAlways) return;
        if (rb == null) return;
        if (root == null || root.MoveRange == null) return;

        float pad = Mathf.Max(0f, moveRangeEdgePadding);
        float clampedX = root.MoveRange.ClampX(rb.position.x, pad);
        float clampedY = root.MoveRange.ClampY(rb.position.y, pad);

        bool outX = Mathf.Abs(clampedX - rb.position.x) > 0.0001f;
        bool outY = Mathf.Abs(clampedY - rb.position.y) > 0.0001f;
        if (!outX && !outY) return;

        rb.position = new Vector2(clampedX, clampedY);
        rb.velocity = new Vector2(outX ? 0f : rb.velocity.x, outY ? 0f : rb.velocity.y);
    }

    private void Update()
    {
        if (!isAttacking) return;
        if (Time.time <= attackLockExpireTime) return;

        isAttacking = false;
        if (blackboard != null) blackboard.isAttacking = false;
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

        if (!projectileData) projectileData = GetComponent<EnemyProjectileData>();
        if (!projectileData) projectileData = GetComponentInParent<EnemyProjectileData>();

        if (!aggroSensor) aggroSensor = GetComponentInChildren<EnemyAggroSensor2D>();
        if (!aggroSensor) aggroSensor = GetComponentInParent<EnemyAggroSensor2D>();

        if (!audioEmitter) audioEmitter = GetComponent<ASPAudioEmitter>();
        if (!audioEmitter) audioEmitter = GetComponentInParent<ASPAudioEmitter>();

        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();
    }

    public bool RequestAttack()
    {
        if (isAttacking) return false;
        if (animator == null) return false;

        isAttacking = true;
        attackLockExpireTime = Time.time + Mathf.Max(0.3f, attackLockTimeout);

        if (blackboard != null) blackboard.isAttacking = true;
        animator.SetTrigger(attackTrigger);
        return true;
    }

    /// <summary>
    /// Animation event: spawn the projectile at fire frame.
    /// </summary>
    public void AnimEvent_SpawnProjectile()
    {
        if (!projectilePrefab || !spawnPoint) return;
        FaceTowardTargetNow();

        int face = GetFacingSign();
        Vector2 castDir = new Vector2(face, 0f);
        Vector2 offset = new Vector2(projectileOffset.x * face, projectileOffset.y);
        Vector3 worldPos = spawnPoint.TransformPoint(offset);

        ASPProjectileHitbox p = ProjectilePoolService.Spawn(projectilePrefab, worldPos, Quaternion.identity, null);
        var initData = new ProjectileInitData
        {
            owner = gameObject,
            castDirection = castDir,
            moveRange = root != null ? root.MoveRange : null,
            enemyProjectileData = projectileData,
            target = ResolveTarget()
        };
        p.Init(initData);
    }

    /// <summary>
    /// Animation event: release attack lock.
    /// </summary>
    public void AnimEvent_AttackEnd()
    {
        isAttacking = false;
        if (blackboard != null) blackboard.isAttacking = false;
    }

    /// <summary>
    /// Legacy forwarder. Use ASPAudioEmitter.AnimEvent_SfxEnemyAspidAttack for animation event binding.
    /// </summary>
    
    private Transform ResolveTarget()
    {
        Transform rawTarget = null;
        if (aggroSensor != null && aggroSensor.HasTarget) rawTarget = aggroSensor.Target;
        if (rawTarget == null && blackboard != null) rawTarget = blackboard.player;
        return ResolveAimPoint(rawTarget);
    }

    private int GetFacingSign()
    {
        if (blackboard != null)
        {
            if (root != null) return root.GetFacingScaleSign(blackboard.facingRight);
            return blackboard.facingRight ? 1 : -1;
        }

        Transform flip = root != null ? root.transform : transform;
        return flip.localScale.x >= 0f ? 1 : -1;
    }

    private void FaceTowardTargetNow()
    {
        Transform target = ResolveTarget();
        if (target == null) return;

        bool shouldFaceRight = target.position.x >= transform.position.x;
        if (root != null)
            root.ApplyFacing(shouldFaceRight);
        else
        {
            if (blackboard != null)
                blackboard.facingRight = shouldFaceRight;

            Transform flip = transform;
            Vector3 s = flip.localScale;
            s.x = Mathf.Abs(s.x) * (shouldFaceRight ? 1f : -1f);
            flip.localScale = s;
        }
    }

    private static Transform ResolveAimPoint(Transform rawTarget)
    {
        if (rawTarget == null) return null;
        PlayerRoot playerRoot = rawTarget.GetComponentInParent<PlayerRoot>();
        if (playerRoot != null && playerRoot.AimPoint != null)
            return playerRoot.AimPoint;
        return rawTarget;
    }
}
