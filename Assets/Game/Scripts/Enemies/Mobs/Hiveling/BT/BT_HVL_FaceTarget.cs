using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Hiveling")]
public class BT_HVL_FaceTarget : Action
{
    private EnemyBlackboard bb;
    private EnemyAggroSensor2D sensor;
    private HVLAttackController attackController;

    public override void OnAwake()
    {
        bb = GetComponent<EnemyBlackboard>();
        if (bb == null)
        {
            EnemyRoot root = GetComponent<EnemyRoot>();
            bb = root != null ? root.Blackboard : null;
        }

        sensor = gameObject.GetComponentInChildren<EnemyAggroSensor2D>();
        if (sensor == null) sensor = gameObject.GetComponentInParent<EnemyAggroSensor2D>();

        attackController = GetComponent<HVLAttackController>();
        if (attackController == null) attackController = gameObject.GetComponentInParent<HVLAttackController>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (attackController == null) return TaskStatus.Failure;
        if (attackController.IsAttackBusy) return TaskStatus.Success;

        Transform target = sensor != null && sensor.HasTarget ? sensor.Target : bb.player;
        if (target == null) return TaskStatus.Failure;

        attackController.FaceToward(target);
        return TaskStatus.Success;
    }
}
