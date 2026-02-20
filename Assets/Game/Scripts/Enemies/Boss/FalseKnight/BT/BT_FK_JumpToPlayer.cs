using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/FalseKnight")]
public class BT_FK_JumpToPlayer : Action
{
    [Header("Jump Params")]
    public float jumpUpSpeed = 14f;

    [UnityEngine.Tooltip("Clamp target dx to avoid absurd long jumps.")]
    public float maxChaseXDistance = 12f;

    [Header("Horizontal Speed Clamp")]
    [UnityEngine.Tooltip("Avoid tiny vx causing near-vertical jumps.")]
    public float minAbsVx = 1.0f;
    [UnityEngine.Tooltip("Do not force minAbsVx when horizontal target distance is too small.")]
    public float minDxToForceMinVx = 0.35f;

    [UnityEngine.Tooltip("Avoid huge vx like a cannonball.")]
    public float maxAbsVx = 12.0f;

    [Header("Prediction")]
    [UnityEngine.Tooltip("Lead time (seconds). 0.1~0.25 usually feels good.")]
    public float leadTime = 0.15f;

    [UnityEngine.Tooltip("Max lead distance on x to prevent over-predict.")]
    public float maxLeadX = 2.0f;

    [Header("Move Range Clamp (optional)")]
    [UnityEngine.Tooltip("Keeps jump landing target away from movement-range edges.")]
    public float edgePadding = 0.25f;
    [UnityEngine.Tooltip("Hard-clamp x inside MoveRange while airborne to prevent rare overshoot.")]
    public bool hardClampXInsideMoveRange = true;

    [Header("Animation (optional)")]
    [UnityEngine.Tooltip("Animator Trigger fired when the jump starts. Leave empty to disable.")]
    public string jumpTrigger = "Jump";

    [UnityEngine.Tooltip("If true, will try to trigger animations via EnemyRoot.Animator.")]
    public bool driveAnimator = true;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private Rigidbody2D playerRb; // for prediction

    private bool jumped;
    private bool leftGround;

    private Vector2 lockedStartPos;
    private Vector2 lockedTargetPos;

    public override void OnAwake()
    {
        root = gameObject.GetComponent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();

        rb = gameObject.GetComponent<Rigidbody2D>();
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
        //BB
        if (bb.isDead) return TaskStatus.Failure;
        if (bb.isHurtLocked) return TaskStatus.Running;

        if (!bb.player) return TaskStatus.Failure;

        bool grounded = (root != null && root.Ground != null) && root.Ground.IsGrounded;

        // ---- Jump start (lock target + compute velocity) ----
        if (!jumped)
        {
            if (!grounded) return TaskStatus.Running;

            lockedStartPos = rb.position;
            lockedTargetPos = GetLockedTargetPos();

            float vx = ComputeHorizontalSpeedForBallisticArc(
                start: lockedStartPos,
                target: lockedTargetPos,
                initialUpSpeed: jumpUpSpeed,
                gravityScale: rb.gravityScale
            );

            rb.velocity = new Vector2(vx, jumpUpSpeed);
            jumped = true;
            bb.MarkJumpToPlayerUsed();

            TryTrigger(jumpTrigger);
            return TaskStatus.Running;
        }

        // ---- wait until left ground at least once ----
        if (!leftGround)
        {
            if (!grounded) leftGround = true;
            ClampAirborneXIfNeeded(grounded);
            return TaskStatus.Running;
        }

        // ---- landed (Animator will transition to FK-Land via Grounded bool) ----
        if (grounded) return TaskStatus.Success;

        ClampAirborneXIfNeeded(grounded);

        return TaskStatus.Running;
    }

    private Vector2 GetLockedTargetPos()
    {
        Vector2 p = bb.player.position;

        // lead prediction (x only)
        float leadX = 0f;
        if (leadTime > 0f)
        {
            float pxv = 0f;
            if (playerRb != null) pxv = playerRb.velocity.x;
            leadX = Mathf.Clamp(pxv * leadTime, -maxLeadX, maxLeadX);
        }

        float startX = rb.position.x;
        float targetX = p.x + leadX;

        // clamp dx
        float dx = Mathf.Clamp(targetX - startX, -maxChaseXDistance, maxChaseXDistance);
        targetX = startX + dx;

        if (root != null && root.MoveRange != null)
            targetX = root.MoveRange.ClampX(targetX, edgePadding);

        return new Vector2(targetX, p.y);
    }

    /// <summary>
    /// Computes the required horizontal speed vx so that a projectile-like jump arc
    /// (with fixed initial upward speed) reaches the target Y at time t, then vx = dx/t.
    /// If no valid positive solution exists, fall back to same-height flight time.
    /// </summary>
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

        // dy = vy*t - 0.5*g*t^2  ->  0.5*g*t^2 - vy*t + dy = 0
        float a = 0.5f * g;
        float b = -initialUpSpeed;
        float c = dy;

        float t = SolvePositiveTimePreferDescending(a, b, c);

        if (t <= 0f)
        {
            // fallback: assume landing height ~= start height
            float T = (2f * initialUpSpeed) / g;
            t = Mathf.Max(0.05f, T);
        }

        float vx = dx / Mathf.Max(0.05f, t);

        // clamp vx
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

    /// <summary>
    /// Prefer the larger positive root (usually the "descending" intersection).
    /// Returns 0 if no positive root.
    /// </summary>
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

    private void TryTrigger(string trigger)
    {
        if (!driveAnimator) return;
        if (string.IsNullOrEmpty(trigger)) return;
        if (root == null || root.Animator == null) return;

        root.Animator.SetTrigger(trigger);
    }
}
