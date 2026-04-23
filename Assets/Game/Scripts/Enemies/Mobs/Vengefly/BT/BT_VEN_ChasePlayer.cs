using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Vengefly Chase: aggressively flies straight toward the player when aggroed.
/// Unlike the Aspid which keeps a preferred distance and shoots projectiles, Vengefly is
/// a contact-damage dive enemy — it just barrels at the player. Adds a slight vertical sway
/// for organic motion. Respects the MoveRange so it doesn't fly through walls.
/// </summary>
[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_ChasePlayer : Action
{
    [Header("Movement")]
    public float chaseSpeed = 4.0f;
    public float xAccel = 22f;
    public float verticalFollowSpeed = 3.5f;

    [Header("Sway (organic motion)")]
    public float swayAmplitude = 0.35f;
    public float swayFrequency = 2.4f;

    [Header("Range Clamp")]
    public float edgePadding = 0.2f;

    [Header("Animator")]
    public string chaseBoolParam = "Chase";

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private EnemyAggroSensor2D sensor;
    private Animator anim;
    private VENAttackController attackController;

    private float swaySeed;
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

        attackController = GetComponent<VENAttackController>();
        if (attackController == null)
            attackController = gameObject.GetComponentInParent<VENAttackController>();

        chaseBoolHash = Animator.StringToHash(chaseBoolParam);
        swaySeed = Random.Range(0f, 1000f);
    }

    public override void OnStart()
    {
        if (anim != null)
            anim.SetBool(chaseBoolHash, true);
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (rb == null) return TaskStatus.Failure;

        // Dive is in progress — let the controller handle it.
        if (attackController != null)
        {
            attackController.EnsureFlyAnimation();
            if (attackController.IsAttackBusy) return TaskStatus.Success;
        }

        if (bb.isHurtLocked) return TaskStatus.Running;

        Transform player = sensor != null && sensor.HasTarget ? sensor.Target : bb.player;
        if (player == null) return TaskStatus.Failure;

        float dx = player.position.x - rb.position.x;
        FaceByX(dx);

        // Charge horizontally toward the player.
        float targetXVel = Mathf.Sign(dx) * Mathf.Max(0f, chaseSpeed);
        if (Mathf.Abs(dx) < 0.05f) targetXVel = 0f;
        float xVel = Mathf.MoveTowards(rb.velocity.x, targetXVel, Mathf.Max(0f, xAccel) * Time.deltaTime);

        // Track player vertically with a small sinusoidal sway for organic feel.
        float yTarget = player.position.y
                        + Mathf.Sin((Time.time + swaySeed) * Mathf.Max(0.01f, swayFrequency))
                          * Mathf.Max(0f, swayAmplitude);
        float yVel = Mathf.Clamp(yTarget - rb.position.y,
                                 -Mathf.Max(0f, verticalFollowSpeed),
                                  Mathf.Max(0f, verticalFollowSpeed));

        rb.velocity = new Vector2(xVel, yVel);

        // Stay inside the encounter MoveRange.
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
        // Don't drop the chase bool here — the BT may re-tick this action immediately.
        // FlyIdle.OnStart() handles clearing the Chase bool when the BT switches branches.
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
