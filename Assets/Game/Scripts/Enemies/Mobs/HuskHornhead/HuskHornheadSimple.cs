using UnityEngine;
using Game.Audio;
using Game.Combat;
using Game.Player;

namespace Game.Enemies
{
    /// <summary>
    /// Minimal Husk Hornhead controller.
    ///
    /// State flow:
    ///   Patrol     → walks back and forth around the spawn position.
    ///   Anticipate → windup when the player is detected.
    ///   Lunge      → dashes forward at charge speed.
    ///   Cooldown   → recovers, then returns to Patrol.
    ///   Dead       → plays death animation, then destroys the GameObject.
    ///
    /// All state is driven by Time.time and a few inspector-tunable knobs.
    /// No Behavior Designer, no blackboard, no aggro-sensor collider.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class HuskHornheadSimple : MonoBehaviour
    {
        // ─────────────────────────────
        // Inspector
        // ─────────────────────────────

        [Header("References (auto-found if left empty)")]
        public Rigidbody2D body;
        public Animator animator;
        public Transform spriteRoot;

        [Header("Sprite")]
        [Tooltip("Tick this if the idle sprite faces RIGHT by default. Untick if it faces LEFT.")]
        public bool spriteFacesRight = true;

        [Header("Patrol")]
        public float walkSpeed = 1.4f;
        [Tooltip("Patrols ± this many world units around the spawn point.")]
        public float patrolDistance = 3f;

        [Header("Detect")]
        public string playerTag = "Player";
        public float detectRadius = 6f;
        [Tooltip("How far up/down the player can be before Hornhead ignores them.")]
        public float detectVerticalTolerance = 2f;

        [Header("Charge Attack")]
        public float anticipateTime = 0.45f;
        public float lungeTime = 0.55f;
        public float chargeSpeed = 7f;
        public float cooldownTime = 0.8f;
        [Tooltip("Minimum delay between finishing one attack and starting another.")]
        public float attackCooldown = 1.2f;

        [Header("Animator Parameters")]
        public string isWalkingBool = "IsWalking";
        public string turnTrigger = "Turn";
        public string anticipateTrigger = "AttackAnticipate";
        public string lungeTrigger = "AttackLunge";
        public string cooldownTriggerParam = "AttackCooldown";
        public string deadTrigger = "Dead";

        [Header("Death")]
        [Tooltip("Seconds between HP hitting zero and the GameObject being destroyed.")]
        public float destroyDelay = 1.2f;

        [Header("Rewards")]
        [Tooltip("Amount of geo dropped on death.")]
        public int geoReward = 5;
        [Tooltip("If true, coins scatter in an arc (for flying enemies).")]
        public bool geoIsFlying = false;

        [Header("Audio")]
        [Tooltip("Audio key played each time the enemy takes damage.")]
        public string hurtSfxKey = AudioKeys.Enemy.Hurt;

        // ─────────────────────────────
        // State
        // ─────────────────────────────

        private enum State { Patrol, Anticipate, Lunge, Cooldown, Dead }

        private State _state = State.Patrol;
        private Vector2 _spawn;
        private int _facing = 1;            // 1 = right, -1 = left
        private float _stateEndsAt;         // Time.time when current attack phase ends
        private float _nextAttackAt;        // earliest Time.time we may start a new attack
        private Transform _player;
        private HpHealth _hp;

        // ─────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<Animator>();
            if (animator) spriteRoot = animator.transform;
        }

        private void Awake()
        {
            if (!body) body = GetComponent<Rigidbody2D>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (!spriteRoot) spriteRoot = animator ? animator.transform : transform;

            _spawn = transform.position;
            ApplyFacing();

            _hp = GetComponent<HpHealth>();
            if (_hp)
            {
                _hp.OnDeath += HandleDeath;
                _hp.OnHurt += HandleHurt;
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (_state == State.Dead) return;

            if (!_player) FindPlayer();

            switch (_state)
            {
                case State.Patrol:     TickPatrol();     break;
                case State.Anticipate:
                case State.Lunge:
                case State.Cooldown:   TickTimedPhase(); break;
            }
        }

        private void OnDestroy()
        {
            if (_hp)
            {
                _hp.OnDeath -= HandleDeath;
                _hp.OnHurt -= HandleHurt;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? (Vector3)_spawn : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                origin + Vector3.left  * patrolDistance + Vector3.down * 0.1f,
                origin + Vector3.right * patrolDistance + Vector3.down * 0.1f);

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, detectRadius);
        }

        // ─────────────────────────────
        // Patrol
        // ─────────────────────────────

        private void TickPatrol()
        {
            if (CanStartAttack())
            {
                FacePlayer();
                EnterAnticipate();
                return;
            }

            float dx = transform.position.x - _spawn.x;
            if (dx >= patrolDistance && _facing > 0) Flip();
            else if (dx <= -patrolDistance && _facing < 0) Flip();

            SetHorizontalVelocity(_facing * walkSpeed);
            SetBool(isWalkingBool, true);
        }

        private bool CanStartAttack()
        {
            if (!_player) return false;
            if (Time.time < _nextAttackAt) return false;

            Vector2 delta = (Vector2)_player.position - (Vector2)transform.position;
            if (Mathf.Abs(delta.y) > detectVerticalTolerance) return false;
            return delta.sqrMagnitude <= detectRadius * detectRadius;
        }

        // ─────────────────────────────
        // Attack phases
        // ─────────────────────────────

        private void EnterAnticipate()
        {
            _state = State.Anticipate;
            _stateEndsAt = Time.time + anticipateTime;
            SetHorizontalVelocity(0f);
            SetBool(isWalkingBool, false);
            TriggerAnim(anticipateTrigger);
        }

        private void TickTimedPhase()
        {
            if (Time.time < _stateEndsAt) return;

            switch (_state)
            {
                case State.Anticipate:
                    _state = State.Lunge;
                    _stateEndsAt = Time.time + lungeTime;
                    SetHorizontalVelocity(_facing * chargeSpeed);
                    TriggerAnim(lungeTrigger);
                    break;

                case State.Lunge:
                    _state = State.Cooldown;
                    _stateEndsAt = Time.time + cooldownTime;
                    SetHorizontalVelocity(0f);
                    TriggerAnim(cooldownTriggerParam);
                    break;

                case State.Cooldown:
                    _state = State.Patrol;
                    _nextAttackAt = Time.time + attackCooldown;
                    break;
            }
        }

        // ─────────────────────────────
        // Death
        // ─────────────────────────────

        private void HandleHurt(DamageInfo info)
        {
            if (_hp != null && _hp.IsDead) return;
            PlayHurtSfx();
        }

        private void HandleDeath()
        {
            if (_state == State.Dead) return;
            _state = State.Dead;

            SpawnGeoReward();

            SetHorizontalVelocity(0f);
            SetBool(isWalkingBool, false);
            TriggerAnim(deadTrigger);

            foreach (var c in GetComponentsInChildren<Collider2D>(true))
                c.enabled = false;

            if (body)
            {
                body.velocity = Vector2.zero;
                body.simulated = false;
            }

            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }

        private void PlayHurtSfx()
        {
            if (string.IsNullOrEmpty(hurtSfxKey)) return;
            var audio = AudioService.Ensure();
            if (audio == null) return;
            audio.PlaySfxByKey(hurtSfxKey, transform.position, transform);
        }

        private void SpawnGeoReward()
        {
            if (geoReward <= 0) return;

            var spawner = GeoPickupSpawner.Instance;
            if (spawner != null)
            {
                spawner.Spawn(geoReward, transform.position, geoIsFlying);
                return;
            }

            // Fallback: credit player directly when no spawner is present in the scene.
            var playerGo = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGo == null) return;
            var currency = playerGo.GetComponent<PlayerCurrency>()
                           ?? playerGo.GetComponentInChildren<PlayerCurrency>();
            if (currency != null)
                currency.Add(geoReward);
        }

        // ─────────────────────────────
        // Facing
        // ─────────────────────────────

        private void FacePlayer()
        {
            int desired = _player.position.x > transform.position.x ? 1 : -1;
            if (desired != _facing)
            {
                _facing = desired;
                ApplyFacing();
            }
        }

        private void Flip()
        {
            _facing = -_facing;
            ApplyFacing();
            TriggerAnim(turnTrigger);
        }

        private void ApplyFacing()
        {
            if (!spriteRoot) return;

            int scaleSign = spriteFacesRight ? _facing : -_facing;
            Vector3 s = spriteRoot.localScale;
            s.x = Mathf.Abs(s.x) * scaleSign;
            spriteRoot.localScale = s;
        }

        // ─────────────────────────────
        // Helpers
        // ─────────────────────────────

        private void SetHorizontalVelocity(float vx)
        {
            if (!body) return;
            Vector2 v = body.velocity;
            v.x = vx;
            body.velocity = v;
        }

        private void SetBool(string paramName, bool value)
        {
            if (animator && !string.IsNullOrEmpty(paramName))
                animator.SetBool(paramName, value);
        }

        private void TriggerAnim(string paramName)
        {
            if (animator && !string.IsNullOrEmpty(paramName))
                animator.SetTrigger(paramName);
        }

        private void FindPlayer()
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) _player = go.transform;
        }
    }
}
