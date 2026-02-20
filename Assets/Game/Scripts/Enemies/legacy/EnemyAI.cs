using UnityEngine;
using Game.Combat;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("State")]
    public EnemyState currentState = EnemyState.Patrol;
    [Tooltip("Seconds to stay Idle at game start before patrolling.")]
    [SerializeField] private float idleDurationAtStart = 0.5f;
    [Tooltip("Brief pause when first spotting the player (telegraph).")]
    [SerializeField] private float alertPauseOnSpot = 0.15f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [Tooltip("Stop moving when this close to the player (avoids running into them).")]
    [SerializeField] private float chaseStopDistance = 0.5f;
    [Tooltip("Turn after walking this far (0 = only turn at edges/walls). Increase so the enemy patrols a longer stretch.")]
    [SerializeField] private float maxPatrolDistance = 6f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [Tooltip("Leave empty to auto-find by 'Player' tag.")]
    public Transform player;
    [Tooltip("Layers that count as ground for edge detection.")]
    [SerializeField] private LayerMask groundLayerMask = ~0;
    [Tooltip("How far in front of the enemy to check for ground (edge detection).")]
    [SerializeField] private float edgeCheckOffset = 0.35f;
    [Tooltip("How far down to raycast for ground ahead.")]
    [SerializeField] private float edgeCheckDepth = 0.5f;
    [Tooltip("Vertical offset from pivot for foot position (edge check).")]
    [SerializeField] private float footOffset = 0.2f;

    [Header("Facing")]
    [Tooltip("Tick only if your sprite faces RIGHT when scale.x is positive.")]
    [SerializeField] private bool spriteFacesRightWhenScalePositive = false;
    [SerializeField] private float faceDirectionDeadZone = 0.25f;

    private Rigidbody2D rb;
    private HpHealth hpHealth;
    private EnemyKnockback knockback;

    private bool movingRight = true;
    private float _lastFacingDir = 1f;
    private float _stateTime;
    private bool _wasChasing;
    private Vector2 _lastTurnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hpHealth = GetComponent<HpHealth>();
        knockback = GetComponent<EnemyKnockback>();
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    private void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
        }

        currentState = EnemyState.Idle;
        _stateTime = 0f;
        _wasChasing = false;
        _lastTurnPosition = transform.position;
    }

    private void Update()
    {
        if (hpHealth != null && hpHealth.IsDead)
            return;
        if (knockback != null && knockback.IsInKnockback)
            return;

        _stateTime += Time.deltaTime;

        // Idle at start
        if (currentState == EnemyState.Idle)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            if (_stateTime >= idleDurationAtStart)
            {
                currentState = EnemyState.Patrol;
                _stateTime = 0f;
            }
            return;
        }

        if (player == null)
        {
            Patrol();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        bool inRange = dist <= detectionRange;

        if (inRange)
        {
            if (currentState != EnemyState.Chase)
            {
                currentState = EnemyState.Chase;
                _stateTime = 0f;
            }

            if (_stateTime < alertPauseOnSpot && !_wasChasing)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                SetFacing(player.position.x > transform.position.x);
                return;
            }

            _wasChasing = true;
            Chase();
        }
        else
        {
            _wasChasing = false;
            currentState = EnemyState.Patrol;
            Patrol();
        }
    }

    private void Patrol()
    {
        bool atEdge = !HasGroundAhead();
        bool reachedPatrolDistance = maxPatrolDistance > 0f &&
                                     Vector2.Distance(transform.position, _lastTurnPosition) >= maxPatrolDistance;

        if (atEdge || reachedPatrolDistance)
            FlipPatrol();

        float xVel = movingRight ? patrolSpeed : -patrolSpeed;
        rb.velocity = new Vector2(xVel, rb.velocity.y);
        SetFacing(movingRight);
    }

    private void Chase()
    {
        float deltaX = player.position.x - transform.position.x;
        float dir = Mathf.Abs(deltaX) >= faceDirectionDeadZone ? Mathf.Sign(deltaX) : _lastFacingDir;
        _lastFacingDir = dir;

        if (!HasGroundAhead())
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            SetFacing(dir > 0);
            return;
        }

        float distX = Mathf.Abs(deltaX);
        float speed = distX <= chaseStopDistance ? 0f : (dir * chaseSpeed);
        rb.velocity = new Vector2(speed, rb.velocity.y);
        SetFacing(dir > 0);
    }

    private bool HasGroundAhead()
    {
        float faceDir = (currentState == EnemyState.Chase) ? _lastFacingDir : (movingRight ? 1f : -1f);
        Vector2 origin = (Vector2)transform.position
                         + Vector2.down * footOffset
                         + Vector2.right * (faceDir * edgeCheckOffset);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDepth, groundLayerMask);
        return hit.collider != null;
    }

    private void SetFacing(bool faceRight)
    {
        float abs = Mathf.Abs(transform.localScale.x);
        float sign = faceRight == spriteFacesRightWhenScalePositive ? 1f : -1f;
        transform.localScale = new Vector3(sign * abs, transform.localScale.y, transform.localScale.z);
    }

    private void FlipPatrol()
    {
        movingRight = !movingRight;
        _lastFacingDir = movingRight ? 1f : -1f;
        _lastTurnPosition = transform.position;
        SetFacing(movingRight);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;
        FlipPatrol();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        float faceDir = Application.isPlaying ? _lastFacingDir : (movingRight ? 1f : -1f);
        Vector2 origin = (Vector2)transform.position + Vector2.down * footOffset + Vector2.right * (faceDir * edgeCheckOffset);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector2.down * edgeCheckDepth);
    }
}