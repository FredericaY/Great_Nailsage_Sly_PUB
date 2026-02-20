using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // Writes gameplay state into Animator parameters.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        // ─────────────────────────────
        // Outlets
        // ─────────────────────────────
        private PlayerRoot _root;
        private PlayerHurtVfx _hurtVfx; // cache
        // ─────────────────────────────
        // Animator Settings
        // ─────────────────────────────
        [Header("Graphics Animator")]
        [SerializeField] private Animator _anim;
        [Header("Bloodfx Animator")]
        [SerializeField] private Animator _bloodfxAnim;
        [Header("Damping")]
        [SerializeField] private float speedDampTime = 0.08f;
        
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int YVelHash = Animator.StringToHash("YVel");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
            
        //Attack
        // Attack triggers (must exist in Animator parameters)
        private static readonly int AttackSlashHash = Animator.StringToHash("AttackSlash");
        private static readonly int AttackUpperHash = Animator.StringToHash("AttackUpper");
        private static readonly int AttackDownAirHash = Animator.StringToHash("AttackDownAir");
        
        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
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

            _anim.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
            _anim.SetFloat(YVelHash, yVel);
            _anim.SetBool(GroundedHash, grounded);

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

            // Hurt（把你 PlayerHurtVfx 的请求也放这里消费）
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
    }
}

