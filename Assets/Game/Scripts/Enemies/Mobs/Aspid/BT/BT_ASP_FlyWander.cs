using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Aspid")]
public class BT_ASP_FlyWander : Action
{
    [Header("Move")]
    public float speed = 2.4f;
    public float reachDistance = 0.2f;

    [Header("Range")]
    public float yBand = 1.2f;
    public float edgePadding = 0.2f;

    [Header("Idle Between Targets")]
    public float minIdle = 0.1f;
    public float maxIdle = 0.45f;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;

    private Vector2 anchorPos;
    private Vector2 currentTarget;
    private float idleUntil;
    private bool hasTarget;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        if (root == null) root = gameObject.GetComponentInParent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
        if (bb == null) bb = gameObject.GetComponentInParent<EnemyBlackboard>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.GetComponentInChildren<Rigidbody2D>();
        if (rb == null) rb = gameObject.GetComponentInParent<Rigidbody2D>();

        anchorPos = transform.position;
        hasTarget = false;
        idleUntil = 0f;
    }

    public override void OnStart()
    {
        // Keep runtime state across selector re-entries.
    }

    public override TaskStatus OnUpdate()
    {
        if (bb != null && bb.isDead) return TaskStatus.Failure;
        if (bb != null && bb.isHurtLocked) return TaskStatus.Success;
        if (rb == null) return TaskStatus.Failure;

        if (Time.time < idleUntil)
        {
            rb.velocity = Vector2.zero;
            return TaskStatus.Success;
        }

        if (!hasTarget || Vector2.Distance(rb.position, currentTarget) <= Mathf.Max(0.01f, reachDistance))
        {
            currentTarget = PickRandomPoint();
            hasTarget = true;
            idleUntil = Time.time + Random.Range(minIdle, maxIdle);
            rb.velocity = Vector2.zero;
            return TaskStatus.Success;
        }

        Vector2 to = currentTarget - rb.position;
        Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.zero;
        rb.velocity = dir * Mathf.Max(0f, speed);

        if (bb != null && Mathf.Abs(to.x) > 0.01f)
            FaceByX(to.x);

        if (root != null && root.MoveRange != null)
        {
            float clampedX = root.MoveRange.ClampX(rb.position.x, edgePadding);
            float clampedY = root.MoveRange.ClampY(rb.position.y, edgePadding);
            bool outX = Mathf.Abs(clampedX - rb.position.x) > 0.0001f;
            bool outY = Mathf.Abs(clampedY - rb.position.y) > 0.0001f;
            if (outX || outY)
            {
                rb.position = new Vector2(clampedX, clampedY);
                rb.velocity = new Vector2(outX ? 0f : rb.velocity.x, outY ? 0f : rb.velocity.y);
            }
        }

        return TaskStatus.Success;
    }

    public override void OnEnd()
    {
        // Intentionally do not zero velocity here.
        // This action can complete every tick in selector mode.
    }

    private Vector2 PickRandomPoint()
    {
        float xMin;
        float xMax;
        float yMin;
        float yMax;
        if (root != null && root.MoveRange != null)
        {
            xMin = root.MoveRange.MinX + Mathf.Max(0f, edgePadding);
            xMax = root.MoveRange.MaxX - Mathf.Max(0f, edgePadding);
            if (xMin > xMax)
            {
                float c = (root.MoveRange.MinX + root.MoveRange.MaxX) * 0.5f;
                xMin = c;
                xMax = c;
            }

            yMin = root.MoveRange.MinY + Mathf.Max(0f, edgePadding);
            yMax = root.MoveRange.MaxY - Mathf.Max(0f, edgePadding);
            if (yMin > yMax)
            {
                float c = (root.MoveRange.MinY + root.MoveRange.MaxY) * 0.5f;
                yMin = c;
                yMax = c;
            }
        }
        else
        {
            xMin = anchorPos.x - 2f;
            xMax = anchorPos.x + 2f;
            yMin = anchorPos.y - Mathf.Abs(yBand);
            yMax = anchorPos.y + Mathf.Abs(yBand);
        }

        float y = anchorPos.y + Random.Range(-Mathf.Abs(yBand), Mathf.Abs(yBand));
        y = Mathf.Clamp(y, yMin, yMax);
        float x = Random.Range(xMin, xMax);
        return new Vector2(x, y);
    }

    private void FaceByX(float x)
    {
        if (bb == null) return;
        bool shouldFaceRight = x >= 0f;
        if (bb.facingRight == shouldFaceRight) return;
        if (root != null)
            root.ApplyFacing(shouldFaceRight);
        else
        {
            bb.facingRight = shouldFaceRight;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (shouldFaceRight ? 1f : -1f);
            transform.localScale = s;
        }
    }
}
