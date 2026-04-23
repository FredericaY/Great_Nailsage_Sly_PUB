using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemies/Hiveling")]
public class BT_HVL_PlayTurnIfFlipped : Action
{
    private HVLAttackController attackController;

    public override void OnAwake()
    {
        attackController = GetComponent<HVLAttackController>();
        if (attackController == null) attackController = gameObject.GetComponentInParent<HVLAttackController>();
    }

    public override TaskStatus OnUpdate()
    {
        if (attackController == null) return TaskStatus.Failure;
        if (attackController.IsAttackBusy) return TaskStatus.Success;
        attackController.PlayQueuedTurnAnimation();
        return TaskStatus.Success;
    }
}
