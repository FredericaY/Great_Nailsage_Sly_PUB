using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Aspid")]
public class BT_ASP_CombatFly : Action
{
    [Header("Distance Control")]
    public float preferredDistance = 4.5f;
    public float distanceTolerance = 0.8f;
    public float distanceBufferTime = 0.35f;
    public float moveSpeed = 3.2f;
    public float xAccel = 18f;

    [Header("Distance Wobble")]
    public float preferredDistanceWobble = 0.25f;
    public float preferredDistanceWobbleFrequency = 0.7f;

    [Header("Near Distance Drift")]
    public float nearDriftSpeed = 0.35f;
    public float nearDriftFrequency = 1.4f;

    [Header("Vertical Sway")]
    public float swayAmplitude = 0.6f;
    public float swayFrequency = 1.8f;
    public float verticalFollowSpeed = 3.2f;

    [Header("Range Clamp")]
    public float edgePadding = 0.2f;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private EnemyAggroSensor2D sensor;
    private float outOfBandSince;
    private int outOfBandSign;
    private float wobbleSeed;

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
        wobbleSeed = Random.Range(0f, 1000f);
        outOfBandSince = -1f;
        outOfBandSign = 0;
    }

    public override void OnStart()
    {
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (bb.isHurtLocked) return TaskStatus.Running;
        if (rb == null) return TaskStatus.Failure;

        Transform player = sensor != null && sensor.HasTarget ? sensor.Target : bb.player;
        if (player == null) return TaskStatus.Failure;

        float dx = player.position.x - rb.position.x;
        FaceByX(dx);

        float absDx = Mathf.Abs(dx);
        float wobble = Mathf.Sin((Time.time + wobbleSeed) * Mathf.Max(0.01f, preferredDistanceWobbleFrequency))
                       * Mathf.Max(0f, preferredDistanceWobble);
        float dynamicPreferredDistance = Mathf.Max(0.3f, preferredDistance + wobble);

        float outDistance = absDx - dynamicPreferredDistance;
        int currentOutOfBandSign = 0;
        if (Mathf.Abs(outDistance) > Mathf.Max(0.01f, distanceTolerance))
            currentOutOfBandSign = outDistance > 0f ? 1 : -1;

        float targetXVel;
        if (currentOutOfBandSign == 0)
        {
            outOfBandSign = 0;
            outOfBandSince = -1f;
            targetXVel = Mathf.Sin((Time.time + wobbleSeed * 0.37f) * Mathf.Max(0.01f, nearDriftFrequency))
                         * Mathf.Max(0f, nearDriftSpeed);
        }
        else
        {
            if (outOfBandSign != currentOutOfBandSign)
            {
                outOfBandSign = currentOutOfBandSign;
                outOfBandSince = Time.time;
            }
            else if (outOfBandSince < 0f)
            {
                outOfBandSince = Time.time;
            }

            bool canRespond = Time.time - outOfBandSince >= Mathf.Max(0f, distanceBufferTime);
            if (!canRespond)
            {
                targetXVel = 0f;
            }
            else
            {
                // out sign:  1 => too far, move toward player; -1 => too close, move away from player
                targetXVel = Mathf.Sign(dx) * currentOutOfBandSign * Mathf.Max(0f, moveSpeed);
            }
        }

        float xVel = Mathf.MoveTowards(rb.velocity.x, targetXVel, Mathf.Max(0f, xAccel) * Time.deltaTime);

        float yTarget = player.position.y + Mathf.Sin(Time.time * Mathf.Max(0.01f, swayFrequency)) * Mathf.Max(0f, swayAmplitude);
        float yVel = Mathf.Clamp(yTarget - rb.position.y, -Mathf.Max(0f, verticalFollowSpeed), Mathf.Max(0f, verticalFollowSpeed));

        rb.velocity = new Vector2(xVel, yVel);

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
