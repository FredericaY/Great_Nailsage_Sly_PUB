using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // PlayerCombat
    // - Owns attack gameplay logic (combo / buffer / hold-repeat).
    // - Sends ONE-shot animation requests to graphics layer (AnimatorDriver).
    // - Hitbox spawning is 100% driven by Animation Events (exact hit frames).
    //
    // Workflow per attack:
    //   1) TryAttack() decides which attack to perform.
    //   2) RaiseAnimRequest() asks AnimatorDriver to trigger the proper anim.
    //   3) Cache a PendingHitbox describing what should be spawned.
    //   4) Animation Event calls AnimEvent_SpawnAttackHitbox() at hit frame.
    //   5) Animation Event calls AnimEvent_AttackEnd() when the move ends.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerCombat : MonoBehaviour
    {
        // ─────────────────────────────
        // Config: Hitbox Prefabs
        // ─────────────────────────────
        [Header("Hitbox Prefabs")]
        [SerializeField] private AttackHitbox slashPrefab;
        [SerializeField] private AttackHitbox upperPrefab;
        [SerializeField] private AttackHitbox downAirPrefab;

        // ─────────────────────────────
        // Config: Spawn Offsets (local space)
        // ─────────────────────────────
        [Header("Spawn Offsets (Local)")]
        [SerializeField] private Vector2 slashOffset = Vector2.zero;
        [SerializeField] private Vector2 upperOffset = Vector2.zero;
        [SerializeField] private Vector2 downAirOffset = Vector2.zero;

        // ─────────────────────────────
        // Config: Damage
        // ─────────────────────────────
        [Header("Damage")]
        [SerializeField] private int baseDamage = 10;

        // ─────────────────────────────
        // Config: Combo & Buffer
        // ─────────────────────────────
        [Header("Combo")]
        [Tooltip("Time window AFTER last attack ends to continue combo.")]
        [SerializeField] private float comboWindowAfterEnd = 0.35f;

        [Header("Input Buffer")]
        [Tooltip("If attack is pressed shortly before attack end, queue next attack.")]
        [SerializeField] private float inputBufferTime = 0.18f;

        // ─────────────────────────────
        // Config: Hold Repeat
        // ─────────────────────────────
        [Header("Hold Repeat")]
        [SerializeField] private float holdToRepeatDelay = 0.18f;
        [SerializeField] private float repeatInterval = 0.10f;

        // ─────────────────────────────
        // Config: Safety
        // ─────────────────────────────
        [Header("Safety")]
        [Tooltip("If blocked (still attacking), retry a bit later to avoid spamming each frame.")]
        [SerializeField] private float minRetryIntervalWhenBlocked = 0.02f;

        // ─────────────────────────────
        // Public types
        // ─────────────────────────────
        public enum AttackAnim { Slash, Upper, DownAir }

        // ─────────────────────────────
        // Pending hitbox spawn (driven by Animation Event)
        // ─────────────────────────────
        private struct PendingHitbox
        {
            public AttackHitbox prefab;
            public Vector2 localOffset;
            public Vector2 dir;
            public int damage;
            public bool valid;
        }

        // ─────────────────────────────
        // Outlets
        // ─────────────────────────────
        private PlayerRoot _root;

        // ─────────────────────────────
        // Runtime: combo / gates
        // ─────────────────────────────
        private int _comboIndex;              // 0: slash, 1: upper, 2: slash...
        private bool _isAttacking;
        private float _lastAttackEndTime;

        // Runtime: input buffer
        private float _bufferedAttackUntil;
        private bool _attackEndedThisFrame;

        // Runtime: animation request handshake (Combat -> AnimatorDriver)
        private bool _hasAttackRequest;
        private AttackAnim _requestedAnim;

        // Runtime: hold-repeat
        private bool _holdActive;
        private float _nextRepeatTime;

        // Runtime: pending hitbox for anim event
        private PendingHitbox _pending;

        // ─────────────────────────────
        // Unity
        // ─────────────────────────────
        private void Reset() => ClampSerializedValues();

        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
            ClampSerializedValues();
        }

#if UNITY_EDITOR
        private void OnValidate() => ClampSerializedValues();
#endif

        private void ClampSerializedValues()
        {
            baseDamage = Mathf.Max(0, baseDamage);

            comboWindowAfterEnd = Mathf.Max(0f, comboWindowAfterEnd);
            inputBufferTime = Mathf.Max(0f, inputBufferTime);

            holdToRepeatDelay = Mathf.Max(0f, holdToRepeatDelay);
            repeatInterval = Mathf.Max(0.01f, repeatInterval);
            minRetryIntervalWhenBlocked = Mathf.Max(0.01f, minRetryIntervalWhenBlocked);
        }

        // ─────────────────────────────
        // Animation Events (forwarded from Graphics)
        // ─────────────────────────────

        /// <summary>
        /// Called at the exact hit frame (per attack clip) to spawn the cached hitbox.
        /// </summary>
        public void AnimEvent_SpawnAttackHitbox()
        {
            if (!_pending.valid) return;

            var prefab = _pending.prefab;
            _pending.valid = false; // consume first (safe against double events)

            if (prefab == null) return;

            var hb = Instantiate(prefab, transform);
            hb.transform.localPosition = _pending.localOffset;
            hb.transform.localRotation = Quaternion.identity;
            hb.Init(gameObject, _pending.damage, _pending.dir);
        }

        /// <summary>
        /// Called at the end of the attack animation to reopen attack gate.
        /// </summary>
        public void AnimEvent_AttackEnd()
        {
            _isAttacking = false;
            _lastAttackEndTime = Time.time;
            _attackEndedThisFrame = true;
        }

        // ─────────────────────────────
        // Input hooks (called by PlayerInput/controller)
        // ─────────────────────────────

        public void OnAttackPressed()
        {
            // Always refresh buffer window
            _bufferedAttackUntil = Time.time + inputBufferTime;

            // Tap: try immediately if allowed
            if (!_isAttacking)
                TryAttack();

            // Hold tracking
            _holdActive = true;
            _nextRepeatTime = Time.time + holdToRepeatDelay;
        }

        public void OnAttackHeld(float dt)
        {
            if (!_holdActive) return;
            if (Time.time < _nextRepeatTime) return;

            if (TryAttack())
                _nextRepeatTime = Time.time + repeatInterval;
            else
                _nextRepeatTime = Time.time + minRetryIntervalWhenBlocked;
        }

        public void OnAttackReleased()
        {
            _holdActive = false;
        }

        private void Update()
        {
            // Handle buffered input right after attack ends (next frame safe)
            if (!_attackEndedThisFrame) return;
            _attackEndedThisFrame = false;

            if (Time.time <= _bufferedAttackUntil)
            {
                _bufferedAttackUntil = 0f;
                TryAttack();
            }
        }

        // ─────────────────────────────
        // Animation request API (Combat -> AnimatorDriver)
        // ─────────────────────────────
        public bool TryConsumeAttackRequest(out AttackAnim anim)
        {
            if (!_hasAttackRequest)
            {
                anim = default;
                return false;
            }

            anim = _requestedAnim;
            _hasAttackRequest = false;
            return true;
        }

        // ─────────────────────────────
        // Core attack attempt
        // ─────────────────────────────
        private bool TryAttack()
        {
            if (_isAttacking) return false;
            _isAttacking = true;

            // Reset combo if we waited too long after last end
            if (_lastAttackEndTime > 0f && Time.time - _lastAttackEndTime > comboWindowAfterEnd)
                _comboIndex = 0;

            bool grounded = _root != null && _root.Ground != null && _root.Ground.IsGrounded;


            int face = 1;
            if (_root != null && _root.Facing != null)
                face = (int)_root.Facing.Current;

            bool downHeld = _root != null && _root.Input != null && _root.Input.MoveY < -0.5f;
            bool wantDownAir = !grounded && downHeld;

            // Down-air has priority in air + down held
            if (wantDownAir)
            {
                RaiseAnimRequest(AttackAnim.DownAir);
                CachePending(downAirPrefab, downAirOffset, Vector2.down);
                _comboIndex = 0;
                return true;
            }

            // Slash <-> Upper combo chain
            if (_comboIndex == 0 || _comboIndex == 2)
            {
                RaiseAnimRequest(AttackAnim.Slash);
                CachePending(
                    slashPrefab,
                    new Vector2(slashOffset.x * face, slashOffset.y),
                    new Vector2(face, 0f)
                );
                _comboIndex = 1;
                return true;
            }

            RaiseAnimRequest(AttackAnim.Upper);
            CachePending(
                upperPrefab,
                new Vector2(upperOffset.x * face, upperOffset.y),
                new Vector2(face, 1f).normalized
            );
            _comboIndex = 2;
            return true;
        }

        private void CachePending(AttackHitbox prefab, Vector2 localOffset, Vector2 dir)
        {
            _pending = new PendingHitbox
            {
                prefab = prefab,
                localOffset = localOffset,
                dir = dir,
                damage = baseDamage,
                valid = true
            };
        }

        private void RaiseAnimRequest(AttackAnim anim)
        {
            _requestedAnim = anim;
            _hasAttackRequest = true;
        }

        /// <summary>
        /// Called by external systems (hurt/death/respawn) to hard-stop combat state.
        /// Prevents late animation events from spawning hitboxes.
        /// </summary>
        public void ForceCancelAttack()
        {
            _isAttacking = false;
            _holdActive = false;
            _hasAttackRequest = false;
            _comboIndex = 0;
            _pending.valid = false;
        }
    }
}
