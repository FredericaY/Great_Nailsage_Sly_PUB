using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Hiveling")]
public class BT_HVL_ChaseHover : Action
{
    public float moveSpeedX = 3.1f;
    public float accelX = 16f;
    public float hoverHeight = 2.3f;
    public float verticalFollowSpeed = 4f;
    public float desiredXOffset = 0f;
    public float xStopDistance = 0.15f;
    public float edgePadding = 0.2f;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private EnemyAggroSensor2D sensor;
    private HVLAttackController attackController;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        if (root == null) root = gameObject.GetComponentInParent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
        if (bb == null) bb = gameObject.GetComponentInParent<EnemyBlackboard>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.GetComponentInParent<Rigidbody2D>();
        sensor = gameObject.GetComponentInChildren<EnemyAggroSensor2D>();
        if (sensor == null) sensor = gameObject.GetComponentInParent<EnemyAggroSensor2D>();
        attackController = GetComponent<HVLAttackController>();
        if (attackController == null) attackController = gameObject.GetComponentInParent<HVLAttackController>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (rb == null) return TaskStatus.Failure;

        Transform target = sensor != null && sensor.HasTarget ? sensor.Target : bb.player;
        if (target == null) return TaskStatus.Failure;

        if (attackController != null)
        {
            attackController.EnsureFlyAnimation();
            if (attackController.IsAttackBusy) return TaskStatus.Success;
        }

        float desiredX = target.position.x + Mathf.Sign(transform.position.x - target.position.x) * desiredXOffset;
        float dx = desiredX - rb.position.x;
        float targetXVel = Mathf.Abs(dx) <= Mathf.Max(0.01f, xStopDistance)
            ? 0f
            : Mathf.Sign(dx) * Mathf.Max(0f, moveSpeedX);
        float xVel = Mathf.MoveTowards(rb.velocity.x, targetXVel, Mathf.Max(0f, accelX) * Time.deltaTime);

        float desiredY = target.position.y + hoverHeight;
        float yVel = Mathf.Clamp(desiredY - rb.position.y, -Mathf.Max(0f, verticalFollowSpeed), Mathf.Max(0f, verticalFollowSpeed));

        rb.velocity = new Vector2(xVel, yVel);

        if (attackController != null)
            attackController.FaceToward(target);

        ClampInsideRange();
        return TaskStatus.Success;
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
