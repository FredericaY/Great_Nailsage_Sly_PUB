using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Vengefly Startle: plays the Startle animation when the enemy first spots the player.
/// Uses a time-based cooldown so that:
///   - During the same chase session (Repeater cycling fast), Startle is skipped instantly.
///   - After the player leaves and re-enters aggro range (cooldown elapsed), Startle plays again.
/// Velocity is only zeroed during the actual animation window, NOT in instant-skip mode,
/// so ChasePlayer is free to build up speed without being interrupted.
/// </summary>
[TaskCategory("Enemies/Vengefly")]
public class BT_VEN_Startle : Action
{
    [Header("Timing")]
    public float startleDuration = 0.45f;

    [Header("Re-arm Cooldown")]
    public float startleCooldown = 4f;

    [Header("Animator")]
    public string startleTrigger = "Startle";

    private EnemyRoot root;
    private EnemyBlackboard bb;
    private Rigidbody2D rb;
    private Animator anim;

    private float endTime;
    private float nextStartleAllowedTime = 0f;  // time-based re-arm; no cross-task flag needed
    private bool isActiveStartle;               // true only during the animation window
    private int startleHash;

    public override void OnAwake()
    {
        root = GetComponent<EnemyRoot>();
        if (root == null) root = gameObject.GetComponentInParent<EnemyRoot>();
        bb = root != null ? root.Blackboard : GetComponent<EnemyBlackboard>();
        if (bb == null) bb = gameObject.GetComponentInParent<EnemyBlackboard>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.GetComponentInChildren<Rigidbody2D>();
        if (rb == null) rb = gameObject.GetComponentInParent<Rigidbody2D>();
        anim = root != null ? root.Animator : gameObject.GetComponentInChildren<Animator>();

        startleHash = Animator.StringToHash(startleTrigger);
    }

    public override void OnStart()
    {
        bool canStartle = Time.time >= nextStartleAllowedTime;

        if (!canStartle)
        {
            // Cooldown not elapsed: instant-skip so ChasePlayer begins immediately.
            endTime = Time.time;
            isActiveStartle = false;
            return;
        }

        // Play the startle animation and freeze movement.
        if (rb != null) rb.velocity = Vector2.zero;
        if (anim != null) anim.SetTrigger(startleHash);
        endTime = Time.time + Mathf.Max(0f, startleDuration);
        // Block re-startle for the duration of the animation plus the cooldown period.
        nextStartleAllowedTime = endTime + Mathf.Max(0f, startleCooldown);
        isActiveStartle = true;
    }

    public override TaskStatus OnUpdate()
    {
        if (bb != null && bb.isDead) return TaskStatus.Failure;

        if (Time.time >= endTime)
            return TaskStatus.Success;

        // Only zero velocity during the actual animation window.
        // In instant-skip mode (isActiveStartle == false), endTime == Time.time so
        // we never reach here — velocity is left untouched for ChasePlayer to use.
        if (isActiveStartle && rb != null)
            rb.velocity = Vector2.zero;

        return TaskStatus.Running;
    }
}
