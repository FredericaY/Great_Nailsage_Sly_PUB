using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemies/Crawlid")]
public class BT_CRW_WalkPatrol : Action
{
    public float speed = 1.8f;
    public float edgePadding = 0.1f;

    [Header("Stuck Detection")]
    public float stuckCheckInterval = 0.25f;
    public float stuckMoveThreshold = 0.05f;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;

    private float lastCheckedX;
    private float nextStuckCheckTime;

    public override void OnAwake()
    {
        root = gameObject.GetComponentInParent<EnemyRoot>();
        bb = root != null ? root.Blackboard : gameObject.GetComponentInParent<EnemyBlackboard>();
        rb = gameObject.GetComponentInParent<Rigidbody2D>();
    }

    public override void OnStart()
    {
        // Sync sprite facing with movement direction at the start of each patrol
        if (root != null && bb != null)
            root.ApplyFacing(bb.facingRight);

        // Reset stuck detection
        if (rb != null)
            lastCheckedX = rb.position.x;
        nextStuckCheckTime = Time.time + stuckCheckInterval;
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (rb == null) return TaskStatus.Failure;

        float dir = bb.facingRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * speed, rb.velocity.y);

        // MoveRange boundary detection
        if (root != null && root.MoveRange != null)
        {
            float x = rb.position.x;
            bool hitRight = bb.facingRight && !root.MoveRange.ContainsX(x + edgePadding, 0f);
            bool hitLeft  = !bb.facingRight && !root.MoveRange.ContainsX(x - edgePadding, 0f);
            if (hitRight || hitLeft)
                return TaskStatus.Failure;
        }

        // Stuck detection: if position barely changed, we hit a physical wall
        if (Time.time >= nextStuckCheckTime)
        {
            float moved = Mathf.Abs(rb.position.x - lastCheckedX);
            if (moved < stuckMoveThreshold)
                return TaskStatus.Failure;

            lastCheckedX = rb.position.x;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }

        return TaskStatus.Running;
    }
}