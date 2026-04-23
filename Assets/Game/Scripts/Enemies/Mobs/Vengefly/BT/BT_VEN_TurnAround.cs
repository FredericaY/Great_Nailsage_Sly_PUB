using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Vengefly Turn: plays the Turn animation and flips facing direction.
/// Used to add variation during idle wandering, or as an optional step
/// before committing to a new direction. Similar to BT_CRW_TurnAround but
/// works for flying enemies (no velocity-zeroing requirement if used lightly).
/// </summary>
[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_TurnAround : Action
{
    [Header("Timing")]
    public float turnDuration = 0.35f;

    [Header("Animator")]
    public string turnTrigger = "Turn";

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private Animator anim;

    private float turnEndTime;
    private int turnHash;

    public override void OnAwake()
    {
        root = gameObject.GetComponentInParent<EnemyRoot>();
        if (root == null) root = GetComponent<EnemyRoot>();
        bb = root != null ? root.Blackboard : gameObject.GetComponentInParent<EnemyBlackboard>();
        rb = gameObject.GetComponentInParent<Rigidbody2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        anim = root != null ? root.Animator : gameObject.GetComponentInChildren<Animator>();

        turnHash = Animator.StringToHash(turnTrigger);
    }

    public override void OnStart()
    {
        turnEndTime = Time.time + Mathf.Max(0f, turnDuration);
        if (rb != null) rb.velocity = Vector2.zero;
        if (anim != null) anim.SetTrigger(turnHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (rb != null) rb.velocity = Vector2.zero;

        if (Time.time >= turnEndTime)
        {
            if (root != null && bb != null)
                root.ApplyFacing(!bb.facingRight);
            else if (bb != null)
            {
                bb.facingRight = !bb.facingRight;
                var s = bb.transform.localScale;
                s.x = Mathf.Abs(s.x) * (bb.facingRight ? 1f : -1f);
                bb.transform.localScale = s;
            }
            return TaskStatus.Success;
        }
        return TaskStatus.Running;
    }
}
