using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemies/Aspid")]
public class BT_ASP_RandomWait : Action
{
    [Header("Wait Range")]
    public float minWait = 0.4f;
    public float maxWait = 1.1f;

    private float endTime;

    public override void OnStart()
    {
        float min = Mathf.Max(0f, minWait);
        float max = Mathf.Max(min, maxWait);
        endTime = Time.time + Random.Range(min, max);
    }

    public override TaskStatus OnUpdate()
    {
        return Time.time >= endTime ? TaskStatus.Success : TaskStatus.Running;
    }
}
