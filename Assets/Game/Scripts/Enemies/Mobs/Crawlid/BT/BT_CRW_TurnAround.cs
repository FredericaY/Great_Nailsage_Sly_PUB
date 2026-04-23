using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Crawlid")]
public class BT_CRW_TurnAround : Action
{
    public float turnDuration = 0.4f;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private float turnEndTime;
    private static readonly int T_Turn = Animator.StringToHash("Turn");

    public override void OnAwake()
    {
        root = gameObject.GetComponentInParent<EnemyRoot>();
        bb = root != null ? root.Blackboard : gameObject.GetComponentInParent<EnemyBlackboard>();
        rb = gameObject.GetComponentInParent<Rigidbody2D>();
    }

    public override void OnStart()
    {
        turnEndTime = Time.time + turnDuration;
        if (rb != null) rb.velocity = Vector2.zero;
        if (root != null && root.Animator != null)
            root.Animator.SetTrigger(T_Turn);
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (rb != null) rb.velocity = Vector2.zero;

        if (Time.time >= turnEndTime)
        {
            if (root != null)
                root.ApplyFacing(!bb.facingRight);
            else
            {
                bb.facingRight = !bb.facingRight;
                var s = root != null ? root.transform.localScale : bb.transform.localScale;
                s.x = Mathf.Abs(s.x) * (bb.facingRight ? 1f : -1f);
                if (root != null) root.transform.localScale = s;
            }
            return TaskStatus.Success;
        }
        return TaskStatus.Running;
    }
}
