using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemies/Hiveling")]
public class BT_HVL_HasAggroTarget : Conditional
{
    private EnemyAggroSensor2D sensor;
    private EnemyBlackboard bb;

    public override void OnAwake()
    {
        sensor = gameObject.GetComponentInChildren<EnemyAggroSensor2D>();
        if (sensor == null) sensor = gameObject.GetComponentInParent<EnemyAggroSensor2D>();

        bb = GetComponent<EnemyBlackboard>();
        if (bb == null)
        {
            EnemyRoot root = GetComponent<EnemyRoot>();
            bb = root != null ? root.Blackboard : null;
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (bb != null && bb.isDead) return TaskStatus.Failure;
        if (sensor == null) return TaskStatus.Failure;
        return sensor.HasTarget ? TaskStatus.Success : TaskStatus.Failure;
    }
}
