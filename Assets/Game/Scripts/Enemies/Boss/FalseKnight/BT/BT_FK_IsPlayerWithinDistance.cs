using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/FalseKnight")]
public class BT_FK_IsPlayerWithinDistance : Conditional
{
    public float distance = 3f;

    private EnemyRoot root;
    private EnemyBlackboard bb;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.player == null) return TaskStatus.Failure;

        float d = Mathf.Abs(bb.player.position.x - transform.position.x);
        return d <= distance ? TaskStatus.Success : TaskStatus.Failure;
    }
}
