using UnityEngine;
using Game.Enemies;
using Game.Utils.Physics2D;

[DisallowMultipleComponent]
public class HVLAttackController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyRoot root;
    [SerializeField] private EnemyBlackboard blackboard;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GroundSensor2D groundSensor;
    [SerializeField] private EnemyAggroSensor2D aggroSensor;
    [SerializeField] private EnemyContactDamage contactDamage;

    [Header("Animation")]
    [SerializeField] private string dropTrigger = "Drop";
    [SerializeField] private string turnTrigger = "Turn";
    [SerializeField] private string flyStateName = "Fly";
    [SerializeField] private string turnStateName = "Turn";
    [SerializeField] private float flyCrossFadeDuration = 0.08f;

    [Header("Dive")]
    [SerializeField] private float diveSpeed = 9f;
    [SerializeField] private float minDownwardFactor = 0.35f;
    [SerializeField] private float targetYOffset = 0f;
    [SerializeField] private float maxDiveTime = 1.2f;

    [Header("Recovery")]
    [SerializeField] private Vector2 bounceVelocity = new Vector2(1.25f, 4.5f);
    [SerializeField] private float recoverLockTime = 0.35f;
    [SerializeField] private float attackCooldown = 1.2f;

    private Vector2 diveDirection = Vector2.down;
    private float diveEndTime;
    private float recoverEndTime;
    private bool isDiving;
    private bool isRecovering;
    private bool turnAnimationQueued;
    private int flyStateHash;
    private int turnStateHash;

    public bool IsAttackBusy => isDiving || isRecovering;
    public bool IsDiving => isDiving;
    public bool IsRecovering => isRecovering;
    public bool CanAttackNow => !IsAttackBusy && Time.time >= GetNextAttackTime();

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
        flyStateHash = Animator.StringToHash(flyStateName);
        turnStateHash = Animator.StringToHash(turnStateName);
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
            {
                BeginRecovery();
            }
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
        {
            ClampInsideMoveRange();
        }
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

    public bool FaceToward(Transform target)
    {
        if (IsAttackBusy) return false;
        if (target == null) return false;
        return FaceByX(target.position.x - transform.position.x);
    }

    public bool FaceByX(float x)
    {
        if (IsAttackBusy) return false;
        if (blackboard == null) return false;

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

        turnAnimationQueued = true;
        return true;
    }

    public bool PlayQueuedTurnAnimation()
    {
        if (IsAttackBusy) return false;
        if (!turnAnimationQueued) return false;
        turnAnimationQueued = false;
        TryTrigger(turnTrigger);
        return true;
    }

    public void EnsureFlyAnimation()
    {
        if (animator == null) return;
        if (IsAttackBusy) return;
        if (string.IsNullOrEmpty(flyStateName)) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!string.IsNullOrEmpty(turnStateName) &&
            (state.IsName(turnStateName) || state.shortNameHash == turnStateHash))
            return;
        if (state.IsName(flyStateName) || state.shortNameHash == flyStateHash)
            return;

        animator.CrossFadeInFixedTime(flyStateName, Mathf.Max(0f, flyCrossFadeDuration));
    }

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
        diveEndTime = Time.time + Mathf.Max(0.1f, maxDiveTime);
        isDiving = true;
        isRecovering = false;
        SetAttacking(true);
        TryTrigger(dropTrigger);
        return true;
    }

    public Transform ResolveTarget()
    {
        if (aggroSensor != null && aggroSensor.HasTarget)
            return aggroSensor.Target;
        return blackboard != null ? blackboard.player : null;
    }

    private void OnContactDamageApplied(Collider2D _)
    {
        if (!isDiving) return;
        BeginRecovery();
    }

    private void BeginRecovery()
    {
        if (!IsAttackBusy) return;

        isDiving = false;
        isRecovering = true;
        recoverEndTime = Time.time + Mathf.Max(0.05f, recoverLockTime);

        if (rb != null)
        {
            float horizontalSign = blackboard != null && blackboard.facingRight ? -1f : 1f;
            rb.velocity = new Vector2(
                Mathf.Abs(bounceVelocity.x) * horizontalSign,
                Mathf.Max(0f, bounceVelocity.y));
        }
    }

    private void FinishAttack()
    {
        isDiving = false;
        isRecovering = false;
        SetNextAttackTime(Time.time + Mathf.Max(0f, attackCooldown));
        SetAttacking(false);
    }

    private void SetAttacking(bool value)
    {
        if (blackboard != null)
            blackboard.isAttacking = value;
    }

    private float GetNextAttackTime()
    {
        return blackboard != null ? blackboard.nextAttackTime : 0f;
    }

    private void SetNextAttackTime(float value)
    {
        if (blackboard != null)
            blackboard.nextAttackTime = value;
    }

    private void TryTrigger(string trigger)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(trigger)) return;
        animator.SetTrigger(trigger);
    }

    private void ClampInsideMoveRange()
    {
        if (rb == null || root == null || root.MoveRange == null) return;

        float clampedX = root.MoveRange.ClampX(rb.position.x, 0.1f);
        float clampedY = root.MoveRange.ClampY(rb.position.y, 0.1f);
        bool outX = Mathf.Abs(clampedX - rb.position.x) > 0.0001f;
        bool outY = Mathf.Abs(clampedY - rb.position.y) > 0.0001f;
        if (!outX && !outY) return;

        rb.position = new Vector2(clampedX, clampedY);
        rb.velocity = new Vector2(outX ? 0f : rb.velocity.x, outY ? 0f : rb.velocity.y);
    }
}
