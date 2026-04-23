using UnityEngine;

namespace Game.Player
{
    // PlayerAnimatorDriver
    // - Writes gameplay state into Animator parameters.
    // - Consumes one-shot attack/hurt animation requests.
    // - Exposes wall-jump trigger API for PlayerJump.
    [DisallowMultipleComponent]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        // ------------------------------
        // Outlets
        // ------------------------------
        private PlayerRoot _root;

        // ------------------------------
        // Config
        // ------------------------------
        [Header("Graphics Animator")]
        [SerializeField] private Animator _anim;
        [Header("Bloodfx Animator")]
        [SerializeField] private Animator _bloodfxAnim;
        [Header("Damping")]
        [SerializeField] private float speedDampTime = 0.08f;

        // ------------------------------
        // Animator parameter hashes
        // ------------------------------
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int YVelHash = Animator.StringToHash("YVel");
        private static readonly int OnWallHash = Animator.StringToHash("OnWall");
        private static readonly int WallSlidingHash = Animator.StringToHash("WallSliding");
        private static readonly int WallSideHash = Animator.StringToHash("WallSide");
        private static readonly int WallJumpTriggerHash = Animator.StringToHash("WallJumpTrigger");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");

        // ------------------------------
        // Attack triggers (must exist in Animator parameters).
        // ------------------------------
        private static readonly int AttackSlashHash = Animator.StringToHash("AttackSlash");
        private static readonly int AttackUpperHash = Animator.StringToHash("AttackUpper");
        private static readonly int AttackDownAirHash = Animator.StringToHash("AttackDownAir");

        // ------------------------------
        // Methods
        // ------------------------------
        private void Reset()
        {
            _root = GetComponent<PlayerRoot>();
            if (_anim == null) _anim = GetComponentInChildren<Animator>(true);
            if (_bloodfxAnim == null)
            {
                var t = transform.Find("VFX/HitFX");
                if (t != null) _bloodfxAnim = t.GetComponent<Animator>();
            }
        }

        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
            
            if (_anim == null) _anim = _root != null ? _root.GraphicsAnimator : null;
            if (_bloodfxAnim == null) _bloodfxAnim = _root != null ? _root.BloodFxAnimator : null;
            if (_anim == null)
            {
                var g = transform.Find("Graphics");
                if (g != null) _anim = g.GetComponent<Animator>();
            }
            if (_bloodfxAnim == null)
            {
                var t = transform.Find("VFX/HitFX");
                if (t != null) _bloodfxAnim = t.GetComponent<Animator>();
            }
        }

        private void LateUpdate()
        {
            if (_root == null || _root.Rb == null || _anim == null) return;

            float speed = Mathf.Abs(_root.Rb.velocity.x);
            float yVel = _root.Rb.velocity.y;
            bool grounded = _root.Ground != null && _root.Ground.IsGrounded;
            bool onWall = !grounded && _root.Wall != null && _root.Wall.IsOnWall;
            bool wallSliding = _root.Jump != null && _root.Jump.IsWallSliding;
            float wallSide = onWall && _root.Wall != null ? _root.Wall.WallSide : 0f;

            _anim.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
            _anim.SetFloat(YVelHash, yVel);
            _anim.SetBool(GroundedHash, grounded);
            _anim.SetBool(OnWallHash, onWall);
            _anim.SetBool(WallSlidingHash, wallSliding);
            _anim.SetFloat(WallSideHash, wallSide);

            // Attack
            if (_root.Combat != null && _root.Combat.TryConsumeAttackRequest(out var atk))
            {
                switch (atk)
                {
                    case PlayerCombat.AttackAnim.Slash: _anim.SetTrigger(AttackSlashHash); break;
                    case PlayerCombat.AttackAnim.Upper: _anim.SetTrigger(AttackUpperHash); break;
                    case PlayerCombat.AttackAnim.DownAir: _anim.SetTrigger(AttackDownAirHash); break;
                }
            }

            if (_root.TryGetComponent<PlayerHurtVfx>(out var hurtVfx) &&
                hurtVfx.TryConsumeHurtRequest(out bool playBody, out bool playBlood))
            {
                if (playBody) _anim.SetTrigger(HurtHash);

                if (playBlood && _bloodfxAnim != null)
                {
                    if (!_bloodfxAnim.gameObject.activeSelf) _bloodfxAnim.gameObject.SetActive(true);
                    _bloodfxAnim.SetTrigger(HurtHash);
                }
            }
        }

        public void NotifyWallJumpTriggered()
        {
            if (_anim == null) return;
            _anim.SetTrigger(WallJumpTriggerHash);
        }
    }
}

