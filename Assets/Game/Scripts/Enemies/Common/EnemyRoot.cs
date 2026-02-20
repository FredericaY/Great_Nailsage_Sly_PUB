using UnityEngine;
using BehaviorDesigner.Runtime;
using Game.Combat;
using Game.Enemies;
using Game.Utils.Physics2D;

[DisallowMultipleComponent]
public class EnemyRoot : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private EnemyBlackboard blackboard;
    [SerializeField] private HpHealth hpHealth;
    [SerializeField] private EnemyHurtVfx hurtVfx;
    [SerializeField] private EnemyDeath death;

    [Header("Presentation")]
    [SerializeField] private Animator animator;
    [Header("GroundSensor2D")]
    [SerializeField] private GroundSensor2D ground;
    [Header("Movement Range (optional)")]
    [SerializeField] private EnemyMoveRange2D moveRange;
    [Header("Optional")]
    [SerializeField] private BehaviorTree behaviorTree;


    // ─────────────────────────────
    // Public accessors (read-only)
    // ─────────────────────────────
    public EnemyBlackboard Blackboard => blackboard;
    public HpHealth HpHealth => hpHealth;
    public EnemyHurtVfx HurtVfx => hurtVfx;
    public EnemyDeath Death => death;
    public Animator Animator => animator;
    public BehaviorTree BehaviorTree => behaviorTree;
    public GroundSensor2D Ground => ground;
    public EnemyMoveRange2D MoveRange => moveRange;
    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
    }

    private void AutoWire()
    {
        if (!blackboard) blackboard = GetComponent<EnemyBlackboard>();
        if (!hpHealth) hpHealth = GetComponent<HpHealth>();
        if (!hurtVfx) hurtVfx = GetComponent<EnemyHurtVfx>();
        if (!death) death = GetComponent<EnemyDeath>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!behaviorTree) behaviorTree = GetComponent<BehaviorTree>();
        if (!Ground) ground = GetComponentInChildren<GroundSensor2D>();
        if (!moveRange) moveRange = GetComponentInChildren<EnemyMoveRange2D>();
        if (!moveRange && transform.parent != null)
            moveRange = transform.parent.GetComponentInChildren<EnemyMoveRange2D>();
        if (!moveRange)
            moveRange = FindBestMoveRangeInScene();
    }

    private EnemyMoveRange2D FindBestMoveRangeInScene()
    {
        var ranges = FindObjectsOfType<EnemyMoveRange2D>();
        if (ranges == null || ranges.Length == 0) return null;

        EnemyMoveRange2D bestContaining = null;
        float bestContainingDist = float.MaxValue;

        EnemyMoveRange2D bestAny = null;
        float bestAnyDist = float.MaxValue;

        float x = transform.position.x;
        foreach (var r in ranges)
        {
            float centerX = (r.MinX + r.MaxX) * 0.5f;
            float dist = Mathf.Abs(centerX - x);

            if (dist < bestAnyDist)
            {
                bestAnyDist = dist;
                bestAny = r;
            }

            if (r.ContainsX(x, 0f) && dist < bestContainingDist)
            {
                bestContainingDist = dist;
                bestContaining = r;
            }
        }

        return bestContaining != null ? bestContaining : bestAny;
    }
}
