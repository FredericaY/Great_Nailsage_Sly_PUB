using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Hiveling")]
public class BT_HVL_FlyPatrol : Action
{
    public float speed = 2.1f;
    public float reachDistance = 0.2f;
    public float yBand = 1.1f;
    public float edgePadding = 0.2f;
    public float minIdle = 0.1f;
    public float maxIdle = 0.4f;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private HVLAttackController attackController;

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
        if (rb == null) rb = gameObject.GetComponentInParent<Rigidbody2D>();
        attackController = GetComponent<HVLAttackController>();
        if (attackController == null) attackController = gameObject.GetComponentInParent<HVLAttackController>();

        anchorPos = transform.position;
    }

    public override TaskStatus OnUpdate()
    {
        if (bb != null && bb.isDead) return TaskStatus.Failure;
        if (rb == null) return TaskStatus.Failure;

        if (attackController != null)
        {
            attackController.EnsureFlyAnimation();
            if (attackController.IsAttackBusy) return TaskStatus.Success;
        }

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

        if (attackController != null && Mathf.Abs(to.x) > 0.01f)
            attackController.FaceByX(to.x);

        ClampInsideRange();
        return TaskStatus.Success;
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
                float centerX = (root.MoveRange.MinX + root.MoveRange.MaxX) * 0.5f;
                xMin = centerX;
                xMax = centerX;
            }

            yMin = root.MoveRange.MinY + Mathf.Max(0f, edgePadding);
            yMax = root.MoveRange.MaxY - Mathf.Max(0f, edgePadding);
            if (yMin > yMax)
            {
                float centerY = (root.MoveRange.MinY + root.MoveRange.MaxY) * 0.5f;
                yMin = centerY;
                yMax = centerY;
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

    private void ClampInsideRange()
    {
        if (rb == null || root == null || root.MoveRange == null) return;

        float clampedX = root.MoveRange.ClampX(rb.position.x, edgePadding);
        float clampedY = root.MoveRange.ClampY(rb.position.y, edgePadding);
        bool outX = Mathf.Abs(clampedX - rb.position.x) > 0.0001f;
        bool outY = Mathf.Abs(clampedY - rb.position.y) > 0.0001f;
        if (!outX && !outY) return;

        rb.position = new Vector2(clampedX, clampedY);
        rb.velocity = new Vector2(outX ? 0f : rb.velocity.x, outY ? 0f : rb.velocity.y);
    }
}
