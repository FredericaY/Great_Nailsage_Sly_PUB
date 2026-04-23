using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Vengefly Dive (HK-faithful): locks the player's position at the moment the dive begins,
/// then charges straight toward that fixed point at high speed.
/// The dive ends when Vengefly reaches the target OR travels the max distance —
/// it does NOT continuously track the player, so the player can dodge by moving after
/// the lock-on. Returns Success when the dive finishes, allowing the BT to loop back
/// and check aggro again for a follow-up dive.
/// </summary>
[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_Dive : Action
{
    [Header("Dive")]
    public float diveSpeed = 8.0f;
    public float maxDiveDistance = 14f;
    public float reachDistance = 0.4f;

    [Header("Cooldown")]
    public float attackCooldown = 0.7f;

    [Header("Animation")]
    public string chaseBoolParam = "Chase";

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private EnemyAggroSensor2D sensor;
    private Animator anim;

    private Vector2 diveTarget;
    private Vector2 diveStartPos;
    private Vector2 diveDir;
    private int chaseBoolHash;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        if (root == null) root = gameObject.GetComponentInParent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
        if (bb == null) bb = gameObject.GetComponentInParent<EnemyBlackboard>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.GetComponentInChildren<Rigidbody2D>();
        if (rb == null) rb = gameObject.GetComponentInParent<Rigidbody2D>();
        sensor = gameObject.GetComponentInChildren<EnemyAggroSensor2D>();
        if (sensor == null) sensor = gameObject.GetComponentInParent<EnemyAggroSensor2D>();
        anim = root != null ? root.Animator : gameObject.GetComponentInChildren<Animator>();

        chaseBoolHash = Animator.StringToHash(chaseBoolParam);
    }

    public override void OnStart()
    {
        // Lock the player's current position — this is the key HK mechanic.
        // The Vengefly commits to a fixed point; moving after the lock-on dodges the dive.
        Transform player = sensor != null && sensor.HasTarget ? sensor.Target : bb?.player;
        diveTarget = player != null ? (Vector2)player.position : (rb != null ? rb.position : Vector2.zero);

        diveStartPos = rb != null ? rb.position : Vector2.zero;

        // Pre-calculate the direction so it stays constant during the dive.
        Vector2 toTarget = diveTarget - diveStartPos;
        diveDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;

        // Face toward target.
        if (root != null)
            root.ApplyFacing(diveDir.x >= 0f);

        // Switch to Chase (dive) animation.
        if (anim != null)
            anim.SetBool(chaseBoolHash, true);

        if (bb != null)
            bb.isAttacking = true;
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (rb == null) return TaskStatus.Failure;

        // Pause during hurt-lock but keep dive intent (Running keeps the dive task alive).
        if (bb.isHurtLocked) return TaskStatus.Running;

        float distToTarget = Vector2.Distance(rb.position, diveTarget);
        float distTraveled = Vector2.Distance(rb.position, diveStartPos);

        // End conditions: reached target point OR traveled too far.
        if (distToTarget <= Mathf.Max(0.01f, reachDistance) || distTraveled >= Mathf.Max(0.1f, maxDiveDistance))
        {
            rb.velocity = Vector2.zero;
            if (anim != null) anim.SetBool(chaseBoolHash, false);
            if (bb != null)
            {
                bb.isAttacking = false;
                bb.nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
            }
            return TaskStatus.Success;
        }

        // Fly in the pre-locked direction at full speed.
        rb.velocity = diveDir * Mathf.Max(0f, diveSpeed);

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        // Ensure velocity and animation are cleaned up if the task is interrupted.
        if (rb != null) rb.velocity = Vector2.zero;
        if (anim != null) anim.SetBool(chaseBoolHash, false);
        if (bb != null) bb.isAttacking = false;
    }
}
