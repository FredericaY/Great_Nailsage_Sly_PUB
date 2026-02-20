using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/FalseKnight")]
public class BT_FK_WaveAttack : Action
{
    private FKAttackEmitter emitter;
    private EnemyRoot root;
    private EnemyBlackboard bb;

    public override void OnAwake()
    {
        emitter = GetComponent<FKAttackEmitter>();
        if (emitter == null) emitter = gameObject.GetComponentInChildren<FKAttackEmitter>();
        if (emitter == null) emitter = gameObject.GetComponentInParent<FKAttackEmitter>();
        root = GetComponent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (bb.isHurtLocked) return TaskStatus.Running;
        if (emitter == null) return TaskStatus.Failure;
        if (!emitter.RequestWaveAttack()) return TaskStatus.Running;

        bb.MarkOtherActionUsed();
        return TaskStatus.Success;
    }
}
