using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Hiveling")]
public class BT_HVL_CanDiveAttack : Conditional
{
    public float maxAttackXDistance = 1.25f;
    public float minHeightAboveTarget = 1f;

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
        if (attackController == null || !attackController.CanAttackNow) return TaskStatus.Failure;

        Transform target = sensor != null && sensor.HasTarget ? sensor.Target : bb.player;
        if (target == null) return TaskStatus.Failure;

        float dx = Mathf.Abs(target.position.x - transform.position.x);
        if (dx > Mathf.Max(0f, maxAttackXDistance)) return TaskStatus.Failure;

        float dy = transform.position.y - target.position.y;
        if (dy < Mathf.Max(0f, minHeightAboveTarget)) return TaskStatus.Failure;

        return TaskStatus.Success;
    }
}
