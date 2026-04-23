using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Vengefly Patrol (HK-faithful): flies horizontally back and forth within MoveRange,
/// playing the Turn animation when reaching each boundary. Returns Success every tick
/// so the parent Selector can re-check HasAggroTarget and interrupt the patrol
/// the moment the player is detected.
/// </summary>
[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_Patrol : Action
{
    [Header("Movement")]
    public float speed = 2.2f;
    public float returnSpeed = 4.0f;
    public float edgePadding = 0.4f;

    [Header("Hover Sway (Y)")]
    public float swayAmplitude = 0.15f;
    public float swayFrequency = 1.6f;

    [Header("Turn")]
    public float turnDuration = 0.3f;
    public string turnTrigger = "Turn";

    [Header("Animation")]
    public string chaseBoolParam = "Chase";

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private Animator anim;

    private bool movingRight;
    private bool isTurning;
    private float turnEndTime;
    private bool dirInitialized;

    private int turnHash;
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
        anim = root != null ? root.Animator : gameObject.GetComponentInChildren<Animator>();

        turnHash = Animator.StringToHash(turnTrigger);
        chaseBoolHash = Animator.StringToHash(chaseBoolParam);

        dirInitialized = false;
        isTurning = false;
    }

    public override void OnStart()
    {
        // Return to Idle animation when patrol resumes.
        if (anim != null)
            anim.SetBool(chaseBoolHash, false);

        // Set initial direction once MoveRange is available.
        if (!dirInitialized && root != null && root.MoveRange != null)
        {
            float mid = (root.MoveRange.MinX + root.MoveRange.MaxX) * 0.5f;
            movingRight = rb != null && rb.position.x <= mid;
            dirInitialized = true;
        }

        // Always sync facing to movingRight when re-entering patrol,
        // in case ChasePlayer flipped the sprite during the chase.
        if (root != null) root.ApplyFacing(movingRight);
    }

    public override TaskStatus OnUpdate()
    {
        if (bb != null && bb.isDead) return TaskStatus.Failure;
        if (rb == null) return TaskStatus.Failure;

        // While hurt-locked: stop but keep patrol state.
        if (bb != null && bb.isHurtLocked)
        {
            rb.velocity = Vector2.zero;
            return TaskStatus.Success;
        }

        // ── Return to MoveRange after a dive (smooth fly-back, no teleport) ──
        if (root != null && root.MoveRange != null)
        {
            bool outsideX = !root.MoveRange.ContainsX(rb.position.x, edgePadding);
            bool outsideY = !root.MoveRange.ContainsY(rb.position.y, edgePadding);
            if (outsideX || outsideY)
            {
                float targetX = root.MoveRange.ClampX(rb.position.x, edgePadding);
                float targetY = root.MoveRange.ClampY(rb.position.y, edgePadding);
                Vector2 returnTarget = new Vector2(targetX, targetY);
                Vector2 returnDir = (returnTarget - rb.position).normalized;
                rb.velocity = returnDir * Mathf.Max(0f, returnSpeed);
                // Face toward return direction.
                if (root != null && Mathf.Abs(returnDir.x) > 0.01f)
                    root.ApplyFacing(returnDir.x > 0f);
                return TaskStatus.Success;
            }
        }

        // ── Turn animation phase ──
        if (isTurning)
        {
            rb.velocity = Vector2.zero;
            if (Time.time >= turnEndTime)
            {
                isTurning = false;
                movingRight = !movingRight;
                if (root != null) root.ApplyFacing(movingRight);
            }
            return TaskStatus.Success;
        }

        // ── Check if the next step would leave MoveRange ──
        bool willExitBounds = false;
        if (root != null && root.MoveRange != null)
        {
            float nextX = rb.position.x + (movingRight ? 1f : -1f) * speed * Time.deltaTime;
            willExitBounds = !root.MoveRange.ContainsX(nextX, edgePadding);
        }

        if (willExitBounds)
        {
            // Clamp position and start turn.
            if (root != null && root.MoveRange != null)
            {
                float clampedX = root.MoveRange.ClampX(rb.position.x, edgePadding);
                rb.position = new Vector2(clampedX, rb.position.y);
            }
            rb.velocity = Vector2.zero;
            if (anim != null) anim.SetTrigger(turnHash);
            turnEndTime = Time.time + Mathf.Max(0f, turnDuration);
            isTurning = true;
            return TaskStatus.Success;
        }

        // ── Normal patrol movement ──
        float dir = movingRight ? 1f : -1f;
        float swayY = Mathf.Sin(Time.time * Mathf.Max(0.01f, swayFrequency)) * Mathf.Max(0f, swayAmplitude);

        // Drift back toward the vertical centre of MoveRange after a chase pulls
        // Vengefly away from its natural patrol height.
        float driftY = 0f;
        if (root != null && root.MoveRange != null)
        {
            float centerY = (root.MoveRange.MinY + root.MoveRange.MaxY) * 0.5f;
            driftY = Mathf.Clamp(centerY - rb.position.y, -Mathf.Max(0f, speed), Mathf.Max(0f, speed));
        }

        rb.velocity = new Vector2(dir * Mathf.Max(0f, speed), driftY + swayY);

        // (No position snap here — out-of-range case is handled by the smooth return block above.)

        return TaskStatus.Success;
    }

    public override void OnEnd()
    {
        // Intentionally do not zero velocity.
    }
}
