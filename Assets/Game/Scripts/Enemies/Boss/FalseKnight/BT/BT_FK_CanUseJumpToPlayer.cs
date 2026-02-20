using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/FalseKnight")]
public class BT_FK_CanUseJumpToPlayer : Conditional
{
    private EnemyRoot root;
    private EnemyBlackboard bb;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null) return TaskStatus.Failure;
        return bb.lastMoveWasJumpToPlayer ? TaskStatus.Failure : TaskStatus.Success;
    }
}
