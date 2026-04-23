using UnityEngine;
using Game.Enemies;
using Game.Utils.Physics2D;

/// <summary>
/// Vengefly attack controller — mirrors HVLAttackController.
/// Manages dive physics independently of the Behavior Tree so the BT
/// never busy-loops on the Dive task (the root cause of the stuck-animation bug).
/// The BT only calls RequestDiveAttack(); this MonoBehaviour drives the actual
/// movement in Update/FixedUpdate and exposes CanAttackNow / IsAttackBusy for
/// the BT nodes to query.
/// </summary>
[DisallowMultipleComponent]
public class VENAttackController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyRoot root;
    [SerializeField] private EnemyBlackboard blackboard;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GroundSensor2D groundSensor;   // optional — time-based fallback if null
    [SerializeField] private EnemyAggroSensor2D aggroSensor;
    [SerializeField] private EnemyContactDamage contactDamage;

    [Header("Animation")]
    [SerializeField] private string chaseBoolParam = "Chase";
    [SerializeField] private string flyStateName = "Fly";
    [SerializeField] private float flyCrossFadeDuration = 0.08f;

    [Header("Dive")]
    [SerializeField] private float diveSpeed = 8f;
    [SerializeField] private float minDownwardFactor = 0.35f;
    [SerializeField] private float targetYOffset = 0f;
    [SerializeField] private float maxDiveTime = 1.4f;

    [Header("Recovery")]
    [SerializeField] private Vector2 bounceVelocity = new Vector2(1.25f, 4.5f);
    [SerializeField] private float recoverLockTime = 0.35f;
    [SerializeField] private float attackCooldown = 0.7f;

    private Vector2 diveDirection = Vector2.down;
    private float diveEndTime;
    private float recoverEndTime;
    private bool isDiving;
    private bool isRecovering;
    private int chaseBoolHash;
    private int flyStateHash;

    public bool IsAttackBusy => isDiving || isRecovering;
    public bool IsDiving => isDiving;
    public bool IsRecovering => isRecovering;
    public bool CanAttackNow => !IsAttackBusy && Time.time >= GetNextAttackTime();

    private void Reset() { AutoWire(); }

    private void Awake()
    {
        AutoWire();
        chaseBoolHash = Animator.StringToHash(chaseBoolParam);
        flyStateHash  = Animator.StringToHash(flyStateName);
    }

    private void OnEnable()
    {
        if (contactDamage != null)
            contactDamage.DamageApplied += OnContactDamageApplied;
    }

    private void OnDisable()
    {
        if (contactDamage != null)
            contactDamage.DamageApplied -= OnContactDamageApplied;
    }

    private void Update()
    {
        if (!IsAttackBusy)
        {
            EnsureFlyAnimation();
            return;
        }

        if (isDiving)
        {
            if (groundSensor != null && groundSensor.IsGrounded)
            {
                BeginRecovery();
                return;
            }
            if (Time.time >= diveEndTime)
                BeginRecovery();
        }
        else if (isRecovering && Time.time >= recoverEndTime)
        {
            FinishAttack();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (isDiving)
        {
            rb.velocity = diveDirection * Mathf.Max(0f, diveSpeed);
            return;
        }

        if (isRecovering)
            ClampInsideMoveRange();
    }

    private void AutoWire()
    {
        if (!root) root = GetComponent<EnemyRoot>();
        if (!root) root = GetComponentInParent<EnemyRoot>();

        if (!blackboard) blackboard = GetComponent<EnemyBlackboard>();
        if (!blackboard && root != null) blackboard = root.Blackboard;
        if (!blackboard) blackboard = GetComponentInParent<EnemyBlackboard>();

        if (!animator) animator = root != null ? root.Animator : GetComponentInChildren<Animator>();

        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();

        if (!groundSensor) groundSensor = GetComponentInChildren<GroundSensor2D>();
        if (!groundSensor) groundSensor = GetComponentInParent<GroundSensor2D>();

        if (!aggroSensor) aggroSensor = GetComponentInChildren<EnemyAggroSensor2D>();
        if (!aggroSensor) aggroSensor = GetComponentInParent<EnemyAggroSensor2D>();

        if (!contactDamage) contactDamage = GetComponentInChildren<EnemyContactDamage>();
        if (!contactDamage) contactDamage = GetComponentInParent<EnemyContactDamage>();
    }

    // ── Public API for BT nodes ────────────────────────────────────────────

    public bool RequestDiveAttack(Transform target)
    {
        if (!CanAttackNow) return false;
        if (target == null) return false;
        if (rb == null) return false;

        Vector2 aimPoint = (Vector2)target.position + new Vector2(0f, targetYOffset);
        Vector2 rawDirection = aimPoint - rb.position;
        if (rawDirection.sqrMagnitude <= 0.0001f)
            rawDirection = Vector2.down;

        Vector2 normalized = rawDirection.normalized;
        if (normalized.y > -Mathf.Abs(minDownwardFactor))
        {
            normalized.y = -Mathf.Abs(minDownwardFactor);
            normalized = normalized.normalized;
        }

        diveDirection = normalized;
        diveEndTime   = Time.time + Mathf.Max(0.1f, maxDiveTime);
        isDiving      = true;
        isRecovering  = false;
        SetAttacking(true);

        if (animator != null)
            animator.SetBool(chaseBoolHash, true);

        return true;
    }

    public void EnsureFlyAnimation()
    {
        if (animator == null || IsAttackBusy) return;
        if (string.IsNullOrEmpty(flyStateName)) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(flyStateName) || state.shortNameHash == flyStateHash) return;

        animator.SetBool(chaseBoolHash, false);
        animator.CrossFadeInFixedTime(flyStateName, Mathf.Max(0f, flyCrossFadeDuration));
    }

    public bool FaceToward(Transform target)
    {
        if (IsAttackBusy || target == null) return false;
        return FaceByX(target.position.x - transform.position.x);
    }

    public bool FaceByX(float x)
    {
        if (IsAttackBusy || blackboard == null) return false;

        bool shouldFaceRight = x >= 0f;
        if (blackboard.facingRight == shouldFaceRight) return false;

        if (root != null)
            root.ApplyFacing(shouldFaceRight);
        else
        {
            blackboard.facingRight = shouldFaceRight;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (shouldFaceRight ? 1f : -1f);
            transform.localScale = s;
        }
        return true;
    }

    public Transform ResolveTarget()
    {
        if (aggroSensor != null && aggroSensor.HasTarget)
            return aggroSensor.Target;
        return blackboard != null ? blackboard.player : null;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void OnContactDamageApplied(Collider2D _)
    {
        if (isDiving) BeginRecovery();
    }

    private void BeginRecovery()
    {
        if (!IsAttackBusy) return;

        isDiving      = false;
        isRecovering  = true;
        recoverEndTime = Time.time + Mathf.Max(0.05f, recoverLockTime);

        if (animator != null)
            animator.SetBool(chaseBoolHash, false);

        if (rb != null)
        {
            float hSign = blackboard != null && blackboard.facingRight ? -1f : 1f;
            rb.velocity = new Vector2(
                Mathf.Abs(bounceVelocity.x) * hSign,
                Mathf.Max(0f, bounceVelocity.y));
        }
    }

    private void FinishAttack()
    {
        isDiving     = false;
        isRecovering = false;
        SetNextAttackTime(Time.time + Mathf.Max(0f, attackCooldown));
        SetAttacking(false);
    }

    private void SetAttacking(bool value)
    {
        if (blackboard != null) blackboard.isAttacking = value;
    }

    private float GetNextAttackTime()
        => blackboard != null ? blackboard.nextAttackTime : 0f;

    private void SetNextAttackTime(float value)
    {
        if (blackboard != null) blackboard.nextAttackTime = value;
    }

    private void ClampInsideMoveRange()
    {
        if (rb == null || root == null || root.MoveRange == null) return;

        float cx = root.MoveRange.ClampX(rb.position.x, 0.1f);
        float cy = root.MoveRange.ClampY(rb.position.y, 0.1f);
        bool outX = Mathf.Abs(cx - rb.position.x) > 0.0001f;
        bool outY = Mathf.Abs(cy - rb.position.y) > 0.0001f;
        if (!outX && !outY) return;

        rb.position = new Vector2(cx, cy);
        rb.velocity = new Vector2(outX ? 0f : rb.velocity.x, outY ? 0f : rb.velocity.y);
    }
}
