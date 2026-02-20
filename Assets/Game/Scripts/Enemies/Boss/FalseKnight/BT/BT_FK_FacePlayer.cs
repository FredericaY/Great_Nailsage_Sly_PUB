using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/FalseKnight")]
public class BT_FK_FacePlayer : Action
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
        if (bb.isDead) return TaskStatus.Failure;
        if (bb.isHurtLocked) return TaskStatus.Running;

        bb.FacePlayerByFlippingRoot();
        return TaskStatus.Success;
    }
}
