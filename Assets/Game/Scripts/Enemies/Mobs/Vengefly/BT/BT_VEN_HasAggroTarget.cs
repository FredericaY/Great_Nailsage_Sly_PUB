using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_HasAggroTarget : Conditional
{
    private EnemyAggroSensor2D sensor;
    private EnemyBlackboard bb;
    private EnemyRoot root;

    public override void OnAwake()
    {
        sensor = gameObject.GetComponentInChildren<EnemyAggroSensor2D>();
        if (sensor == null) sensor = gameObject.GetComponentInParent<EnemyAggroSensor2D>();

        root = GetComponent<EnemyRoot>();
        if (root == null) root = gameObject.GetComponentInParent<EnemyRoot>();

        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
        if (bb == null) bb = gameObject.GetComponentInParent<EnemyBlackboard>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb != null && bb.isDead) return TaskStatus.Failure;
        if (sensor == null || !sensor.HasTarget) return TaskStatus.Failure;

        // Option 3: player must also be inside MoveRange horizontally.
        // Only X is checked — MoveRange.MinY sits above ground level so a Y check
        // would prevent detection when the player is standing below Vengefly.
        if (root != null && root.MoveRange != null)
        {
            if (!root.MoveRange.ContainsX(sensor.Target.position.x))
                return TaskStatus.Failure;
        }

        return TaskStatus.Success;
    }
}
