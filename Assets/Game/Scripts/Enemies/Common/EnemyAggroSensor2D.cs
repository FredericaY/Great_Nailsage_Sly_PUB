using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class EnemyAggroSensor2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyBlackboard blackboard;
    [SerializeField] private string playerTag = "Player";

    private Collider2D triggerCollider;
    private Transform target;

    public bool HasTarget => target != null;
    public Transform Target => target;

    private void Reset()
    {
        EnsureSetup();
    }

    private void Awake()
    {
        EnsureSetup();
    }

    private void Update()
    {
        if (target == null) return;
        if (!target.gameObject.activeInHierarchy) target = null;
    }

    private void EnsureSetup()
    {
        if (!triggerCollider) triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider && !triggerCollider.isTrigger) triggerCollider.isTrigger = true;

        if (!blackboard) blackboard = GetComponentInParent<EnemyBlackboard>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        target = other.transform;
        if (blackboard != null) blackboard.player = target;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (target == null) return;
        if (other.transform != target) return;
        target = null;
    }
}

