using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Triggers a Vengefly dive via VENAttackController and returns Success immediately.
/// The actual dive physics are driven by the controller in Update/FixedUpdate —
/// the BT does not stay in this task during the dive, mirroring BT_HVL_StartDiveAttack.
/// </summary>
[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_StartDive : Action
{
    private EnemyBlackboard bb;
    private EnemyAggroSensor2D sensor;
    private VENAttackController attackController;

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

        attackController = GetComponent<VENAttackController>();
        if (attackController == null)
            attackController = gameObject.GetComponentInParent<VENAttackController>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (attackController == null) return TaskStatus.Failure;

        Transform target = sensor != null && sensor.HasTarget ? sensor.Target : bb.player;
        if (target == null) return TaskStatus.Failure;

        if (!attackController.RequestDiveAttack(target))
            return TaskStatus.Failure;

        bb.MarkOtherActionUsed();
        return TaskStatus.Success;
    }
}
