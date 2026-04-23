using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Vengefly dive readiness check — mirrors BT_HVL_CanDiveAttack.
/// Returns Success only when:
///   • VENAttackController.CanAttackNow (not busy + cooldown expired)
///   • Vengefly is within maxAttackXDistance horizontally of the target
///   • Vengefly is at least minHeightAboveTarget units above the target
/// </summary>
[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_CanDive : Conditional
{
    public float maxAttackXDistance = 2f;
    public float minHeightAboveTarget = 1f;

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
