using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Aspid")]
public class BT_ASP_Attack : Action
{
    [Header("Attack Distance Window")]
    public float minAttackDistance = 2.6f;
    public float maxAttackDistance = 7.5f;

    [Header("Attack Intention")]
    [Range(0f, 1f)] public float attackChancePerDecision = 0.65f;

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private ASPAttackEmitter emitter;
    private EnemyAggroSensor2D sensor;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        if (root == null) root = gameObject.GetComponentInParent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
        if (bb == null) bb = gameObject.GetComponentInParent<EnemyBlackboard>();

        emitter = GetComponent<ASPAttackEmitter>();
        if (emitter == null) emitter = gameObject.GetComponentInChildren<ASPAttackEmitter>();
        if (emitter == null) emitter = gameObject.GetComponentInParent<ASPAttackEmitter>();

        sensor = gameObject.GetComponentInChildren<EnemyAggroSensor2D>();
        if (sensor == null) sensor = gameObject.GetComponentInParent<EnemyAggroSensor2D>();
    }

    public override TaskStatus OnUpdate()
    {
        if (bb == null || bb.isDead) return TaskStatus.Failure;
        if (bb.isHurtLocked) return TaskStatus.Failure;
        if (emitter == null) return TaskStatus.Failure;

        if (Random.value > Mathf.Clamp01(attackChancePerDecision))
            return TaskStatus.Failure;

        Transform target = sensor != null && sensor.HasTarget ? sensor.Target : (bb != null ? bb.player : null);
        if (target == null) return TaskStatus.Failure;

        float distance = Vector2.Distance(transform.position, target.position);
        float minDist = Mathf.Max(0f, minAttackDistance);
        float maxDist = Mathf.Max(minDist, maxAttackDistance);
        if (distance < minDist || distance > maxDist)
            return TaskStatus.Failure;

        FaceTarget(target);

        if (!emitter.RequestAttack())
            return TaskStatus.Failure;

        bb.MarkOtherActionUsed();
        return TaskStatus.Success;
    }

    private void FaceTarget(Transform target)
    {
        if (target == null || bb == null) return;
        bool shouldFaceRight = target.position.x >= transform.position.x;
        if (bb.facingRight == shouldFaceRight) return;

        if (root != null)
            root.ApplyFacing(shouldFaceRight);
        else
        {
            bb.facingRight = shouldFaceRight;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (shouldFaceRight ? 1f : -1f);
            transform.localScale = s;
        }
    }
}
