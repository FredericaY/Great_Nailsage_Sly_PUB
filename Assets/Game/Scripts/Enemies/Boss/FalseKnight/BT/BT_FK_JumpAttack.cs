using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/FalseKnight")]
public class BT_FK_JumpAttack : Action
{
    private FKAttackEmitter emitter;
    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private Rigidbody2D playerRb;

    [Header("Jump Motion")]
    public float jumpUpSpeed = 14f;
    public float maxChaseXDistance = 10f;
    public float minAbsVx = 1f;
    public float minDxToForceMinVx = 0.35f;
    public float maxAbsVx = 12f;
    public float leadTime = 0.1f;
    public float maxLeadX = 1.6f;

    [Header("Move Range")]
    public float edgePadding = 0.25f;
    public bool hardClampXInsideMoveRange = true;

    private bool jumped;
    private bool leftGround;
    private Vector2 lockedStartPos;
    private Vector2 lockedTargetPos;

    public override void OnAwake()
    {
        emitter = GetComponent<FKAttackEmitter>();
        if (emitter == null) emitter = gameObject.GetComponentInChildren<FKAttackEmitter>();
        if (emitter == null) emitter = gameObject.GetComponentInParent<FKAttackEmitter>();

        root = GetComponent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
        rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.GetComponentInChildren<Rigidbody2D>();
    }

    public override void OnStart()
    {
        jumped = false;
        leftGround = false;
        lockedStartPos = Vector2.zero;
        lockedTargetPos = Vector2.zero;

        playerRb = null;
        if (bb != null && bb.player != null)
            playerRb = bb.player.GetComponent<Rigidbody2D>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (bb.isHurtLocked) return TaskStatus.Running;
        if (emitter == null || rb == null || bb.player == null) return TaskStatus.Failure;

        bool grounded = (root != null && root.Ground != null) && root.Ground.IsGrounded;

        if (!jumped)
        {
            if (!grounded) return TaskStatus.Running;
            if (!emitter.RequestJumpAttack()) return TaskStatus.Running;

            lockedStartPos = rb.position;
            lockedTargetPos = GetLockedTargetPos();

            float vx = ComputeHorizontalSpeedForBallisticArc(
                lockedStartPos,
                lockedTargetPos,
                jumpUpSpeed,
                rb.gravityScale
            );

            rb.velocity = new Vector2(vx, jumpUpSpeed);
            jumped = true;
            bb.MarkOtherActionUsed();
            return TaskStatus.Running;
        }

        if (!leftGround)
        {
            if (!grounded) leftGround = true;
            ClampAirborneXIfNeeded(grounded);
            return TaskStatus.Running;
        }

        if (grounded) return TaskStatus.Success;

        ClampAirborneXIfNeeded(grounded);
        return TaskStatus.Running;
    }

    private Vector2 GetLockedTargetPos()
    {
        Vector2 p = bb.player.position;

        float leadX = 0f;
        if (leadTime > 0f && playerRb != null)
            leadX = Mathf.Clamp(playerRb.velocity.x * leadTime, -maxLeadX, maxLeadX);

        float startX = rb.position.x;
        float targetX = p.x + leadX;
        float dx = Mathf.Clamp(targetX - startX, -maxChaseXDistance, maxChaseXDistance);
        targetX = startX + dx;

        if (root != null && root.MoveRange != null)
            targetX = root.MoveRange.ClampX(targetX, edgePadding);

        return new Vector2(targetX, p.y);
    }

    private float ComputeHorizontalSpeedForBallisticArc(
        Vector2 start,
        Vector2 target,
        float initialUpSpeed,
        float gravityScale)
    {
        float g = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        g = Mathf.Max(0.0001f, g);

        float dx = target.x - start.x;
        float dy = target.y - start.y;

        float a = 0.5f * g;
        float b = -initialUpSpeed;
        float c = dy;

        float t = SolvePositiveTimePreferDescending(a, b, c);
        if (t <= 0f)
        {
            float T = (2f * initialUpSpeed) / g;
            t = Mathf.Max(0.05f, T);
        }

        float vx = dx / Mathf.Max(0.05f, t);
        float abs = Mathf.Abs(vx);

        if (abs < minAbsVx && Mathf.Abs(dx) >= minDxToForceMinVx)
        {
            float sign = Mathf.Sign(dx);
            if (sign == 0f) sign = 1f;
            vx = sign * minAbsVx;
        }
        else if (abs > maxAbsVx)
        {
            vx = Mathf.Sign(vx) * maxAbsVx;
        }

        return vx;
    }

    private float SolvePositiveTimePreferDescending(float a, float b, float c)
    {
        float disc = b * b - 4f * a * c;
        if (disc < 0f) return 0f;

        float sqrt = Mathf.Sqrt(disc);
        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);

        float best = 0f;
        if (t1 > 0f) best = t1;
        if (t2 > 0f) best = Mathf.Max(best, t2);
        return best;
    }

    private void ClampAirborneXIfNeeded(bool grounded)
    {
        if (!hardClampXInsideMoveRange) return;
        if (grounded) return;
        if (root == null || root.MoveRange == null || rb == null) return;

        float clampedX = root.MoveRange.ClampX(rb.position.x, edgePadding);
        if (Mathf.Abs(clampedX - rb.position.x) <= 0.0001f) return;

        rb.position = new Vector2(clampedX, rb.position.y);
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }
}
