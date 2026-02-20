using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/FalseKnight")]
public class BT_FK_BackstepToFarthestSide : Action
{
    [Header("Jump Params")]
    public float jumpUpSpeed = 13f;

    [UnityEngine.Tooltip("Max horizontal travel for this retreat jump.")]
    public float maxRetreatXDistance = 8f;

    [Header("Horizontal Speed Clamp")]
    public float minAbsVx = 1f;
    [UnityEngine.Tooltip("Do not force minAbsVx when horizontal target distance is too small.")]
    public float minDxToForceMinVx = 0.35f;
    public float maxAbsVx = 11f;

    [Header("Move Range")]
    [UnityEngine.Tooltip("Keep landing point away from range edge.")]
    public float edgePadding = 0.4f;
    [UnityEngine.Tooltip("Hard-clamp x inside MoveRange while airborne to prevent rare overshoot.")]
    public bool hardClampXInsideMoveRange = true;

    [Header("Animation (optional)")]
    public string jumpTrigger = "Jump";
    public bool driveAnimator = true;

    [Header("Facing")]
    [UnityEngine.Tooltip("If jump direction is forward, face player after landing.")]
    public bool facePlayerAfterForwardJump = true;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;

    private bool jumped;
    private bool leftGround;
    private bool forwardJump;
    private bool startFacingRight;

    private Vector2 lockedStartPos;
    private Vector2 lockedTargetPos;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();

        rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.GetComponentInChildren<Rigidbody2D>();
    }

    public override void OnStart()
    {
        jumped = false;
        leftGround = false;
        forwardJump = false;
        startFacingRight = bb != null && bb.facingRight;

        lockedStartPos = Vector2.zero;
        lockedTargetPos = Vector2.zero;
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || rb == null) return TaskStatus.Failure;
        if (bb.isDead) return TaskStatus.Failure;
        if (bb.isHurtLocked) return TaskStatus.Running;
        if (!bb.player) return TaskStatus.Failure;

        bool grounded = (root != null && root.Ground != null) && root.Ground.IsGrounded;

        if (!jumped)
        {
            if (!grounded) return TaskStatus.Running;

            lockedStartPos = rb.position;
            lockedTargetPos = GetRetreatTargetPos();

            float vx = ComputeHorizontalSpeedForBallisticArc(
                lockedStartPos,
                lockedTargetPos,
                jumpUpSpeed,
                rb.gravityScale
            );

            rb.velocity = new Vector2(vx, jumpUpSpeed);
            jumped = true;
            bb.MarkBackstepUsed();

            forwardJump = (vx > 0f && startFacingRight) || (vx < 0f && !startFacingRight);

            TryTrigger(jumpTrigger);
            return TaskStatus.Running;
        }

        if (!leftGround)
        {
            if (!grounded) leftGround = true;
            ClampAirborneXIfNeeded(grounded);
            return TaskStatus.Running;
        }

        if (grounded)
        {
            if (facePlayerAfterForwardJump && forwardJump)
                bb.FacePlayerByFlippingRoot();
            return TaskStatus.Success;
        }

        ClampAirborneXIfNeeded(grounded);

        return TaskStatus.Running;
    }

    private Vector2 GetRetreatTargetPos()
    {
        float startX = rb.position.x;
        float targetX = startX;

        if (root != null && root.MoveRange != null)
        {
            float minX = root.MoveRange.MinX;
            float maxX = root.MoveRange.MaxX;
            float leftDistance = Mathf.Abs(startX - minX);
            float rightDistance = Mathf.Abs(maxX - startX);

            bool goRight = rightDistance >= leftDistance;
            targetX = goRight ? (maxX - edgePadding) : (minX + edgePadding);
            targetX = root.MoveRange.ClampX(targetX, edgePadding);
        }
        else
        {
            // Fallback without explicit range: retreat away from player.
            float dir = Mathf.Sign(startX - bb.player.position.x);
            if (dir == 0f) dir = (bb.facingRight ? -1f : 1f);
            targetX = startX + dir * Mathf.Max(1f, maxRetreatXDistance * 0.7f);
        }

        float dx = Mathf.Clamp(targetX - startX, -maxRetreatXDistance, maxRetreatXDistance);
        targetX = startX + dx;

        if (root != null && root.MoveRange != null)
            targetX = root.MoveRange.ClampX(targetX, edgePadding);

        return new Vector2(targetX, bb.player.position.y);
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
            if (sign == 0f) sign = startFacingRight ? 1f : -1f;
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

    private void TryTrigger(string trigger)
    {
        if (!driveAnimator) return;
        if (string.IsNullOrEmpty(trigger)) return;
        if (root == null || root.Animator == null) return;
        root.Animator.SetTrigger(trigger);
    }
}
