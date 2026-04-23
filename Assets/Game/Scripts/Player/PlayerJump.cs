using UnityEngine;

namespace Game.Player
{
    // PlayerJump
    // - Handles jump, wall-jump and variable jump height.
    // - Exposes grounded/wall-slide state for other modules.
    // - Triggers wall-jump animation request when needed.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerJump : MonoBehaviour
    {
        // ------------------------------
        // Config: Jump
        // ------------------------------
        [Header("Jump Settings")]
        [SerializeField] private float jumpVelocity = 10f;

        // ------------------------------
        // Config: Wall Jump
        // ------------------------------
        [Header("Wall Jump")]
        [SerializeField] private float wallJumpVelocityX = 8f;
        [SerializeField] private float wallJumpVelocityY = 10f;
        [SerializeField] private float wallJumpCooldown = 0.15f;

        // ------------------------------
        // Config: Variable Jump
        // ------------------------------
        [Header("Variable Jump")]
        [SerializeField, Range(0.1f, 1f)]
        private float jumpCutMultiplier = 0.5f;

        // ------------------------------
        // Config: Wall Slide
        // ------------------------------
        [Header("Wall Slide")]
        [SerializeField] private bool enableWallSlide = true;
        [SerializeField, Min(0f)] private float wallSlideMinDownSpeed = 0.35f;
        [SerializeField, Min(0f)] private float wallSlideMaxFallSpeed = 2.5f;
        [SerializeField, Range(0f, 1f)] private float wallInputThreshold = 0.1f;

        // ------------------------------
        // Public state (read-only)
        // ------------------------------
        public bool IsGrounded => _root != null && _root.Ground != null && _root.Ground.IsGrounded;
        public bool IsWallSliding { get; private set; }

        // True when wall jump is on cooldown (prevents immediate re-stick).
        public bool IsWallJumpCooldown => _wallJumpCooldownUntil > 0f && Time.time < _wallJumpCooldownUntil;

        // ------------------------------
        // Outlets / Runtime state
        // ------------------------------
        private PlayerRoot _root;
        private float _wallJumpCooldownUntil;
        private int _remainingExtraJumps;

        // ------------------------------
        // Methods
        // ------------------------------
        private void Reset()
        {
            _root = GetComponent<PlayerRoot>();
        }

        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
        }

        private void FixedUpdate()
        {
            RefreshExtraJumpState();
            HandleWallSlide();
        }

        public bool TryJump()
        {
            IsWallSliding = false;

            if (IsGrounded)
            {
                Vector2 v = _root.Rb.velocity;
                v.y = jumpVelocity;
                _root.Rb.velocity = v;
                RefreshExtraJumpCharges();
                return true;
            }

            if (_root.Wall != null && _root.Wall.IsOnWall && !IsWallJumpCooldown)
            {
                int side = _root.Wall.WallSide;
                if (side == 0) return false;

                Vector2 v = _root.Rb.velocity;
                v.x = wallJumpVelocityX * -side;
                v.y = wallJumpVelocityY;
                _root.Rb.velocity = v;
                _wallJumpCooldownUntil = Time.time + wallJumpCooldown;
                _root.AnimDriver?.NotifyWallJumpTriggered();
                return true;
            }

            if (CanUseExtraJump())
            {
                Vector2 v = _root.Rb.velocity;
                v.y = jumpVelocity;
                _root.Rb.velocity = v;
                _remainingExtraJumps--;
                return true;
            }

            return false;
        }

        public void OnJumpReleased()
        {
            // Only cut jump if still moving upward.
            if (_root.Rb.velocity.y > 0.01f)
            {
                Vector2 v = _root.Rb.velocity;
                v.y *= jumpCutMultiplier;
                _root.Rb.velocity = v;
            }
        }

        private void HandleWallSlide()
        {
            IsWallSliding = false;

            if (!enableWallSlide || _root == null || _root.Rb == null || _root.Wall == null || _root.Input == null)
                return;

            if (IsGrounded || !_root.Wall.IsOnWall || IsWallJumpCooldown)
                return;

            int side = _root.Wall.WallSide;
            if (side == 0) return;

            float moveX = _root.Input.MoveX;
            bool pressingIntoWall =
                (side < 0 && moveX <= -wallInputThreshold) ||
                (side > 0 && moveX >= wallInputThreshold);
            if (!pressingIntoWall) return;

            Vector2 v = _root.Rb.velocity;

            // Force a controllable downward drift instead of "sticking" to the wall.
            if (v.y > -wallSlideMinDownSpeed) v.y = -wallSlideMinDownSpeed;
            if (v.y < -wallSlideMaxFallSpeed) v.y = -wallSlideMaxFallSpeed;

            _root.Rb.velocity = v;
            IsWallSliding = true;
        }

        private bool CanUseExtraJump()
        {
            return _root != null
                   && _root.CharmRuntime != null
                   && _root.CharmRuntime.HasDoubleJumpAbility()
                   && _remainingExtraJumps > 0;
        }

        private void RefreshExtraJumpState()
        {
            if (_root == null)
                return;

            if (_root.CharmRuntime == null || !_root.CharmRuntime.HasDoubleJumpAbility())
            {
                _remainingExtraJumps = 0;
                return;
            }

            if (IsGrounded || (_root.Wall != null && _root.Wall.IsOnWall))
                _remainingExtraJumps = 1;
        }

        private void RefreshExtraJumpCharges()
        {
            _remainingExtraJumps = _root != null && _root.CharmRuntime != null && _root.CharmRuntime.HasDoubleJumpAbility() ? 1 : 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            wallSlideMinDownSpeed = Mathf.Max(0f, wallSlideMinDownSpeed);
            wallSlideMaxFallSpeed = Mathf.Max(0f, wallSlideMaxFallSpeed);
            if (wallSlideMaxFallSpeed < wallSlideMinDownSpeed)
                wallSlideMaxFallSpeed = wallSlideMinDownSpeed;
        }
#endif
    }
}
