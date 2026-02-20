using UnityEngine;
using Game.Combat;
using Game.Utils;  // SpriteFlashUtil

namespace Game.Player
{
    [DisallowMultipleComponent]
    public class PlayerHurtVfx : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeartsHealth heartsHealth;

        [Header("Flash (Shader _Flash)")]
        [SerializeField] private SpriteRenderer[] flashTargets;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashHz = 12f;

        [SerializeField] private PlayerCombat combat;

        // hurt requests
        private bool _hasHurtRequest;
        private bool _hasBloodRequest;

        private Coroutine _flashCo;

        private void Reset()
        {
            AutoAssignRefs();
            AutoAssignTargets();
            Clamp();
            if (!combat) combat = GetComponentInParent<PlayerCombat>();

        }

        private void Awake()
        {
            AutoAssignRefs();
            AutoAssignTargets();
            Clamp();
            if (!combat) combat = GetComponentInParent<PlayerCombat>();

        }

        private void OnEnable()
        {
            if (heartsHealth != null)
                heartsHealth.OnHurt += OnHurt;
        }

        private void OnDisable()
        {
            if (heartsHealth != null)
                heartsHealth.OnHurt -= OnHurt;

            _flashCo = null;

            // 确保关闭 flash
            if (!CoroutineRunner.IsQuitting)
                SpriteFlashUtil.Flash(flashTargets, 0f, flashColor, 1);

        }

#if UNITY_EDITOR
        private void OnValidate() => Clamp();
#endif

        private void AutoAssignRefs()
        {
            if (heartsHealth == null)
                heartsHealth = GetComponent<HeartsHealth>();
        }

        private void AutoAssignTargets()
        {
            if (flashTargets == null || flashTargets.Length == 0)
                flashTargets = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void Clamp()
        {
            flashHz = Mathf.Max(1f, flashHz);
        }

        private void OnHurt(DamageInfo info)
        {
            _hasHurtRequest = true;
            _hasBloodRequest = true;
            if (combat != null) combat.ForceCancelAttack();

            float inv = (heartsHealth != null) ? heartsHealth.InvincibleTime : 0f;
            if (inv > 0f && flashTargets != null && flashTargets.Length > 0)
            {
                _flashCo = SpriteFlashUtil.FlashHz(flashTargets, inv, flashHz, flashColor);
            }
        }

        public bool TryConsumeHurtRequest(out bool playBody, out bool playBlood)
        {
            if (!_hasHurtRequest && !_hasBloodRequest)
            {
                playBody = false;
                playBlood = false;
                return false;
            }

            playBody = _hasHurtRequest;
            playBlood = _hasBloodRequest;

            _hasHurtRequest = false;
            _hasBloodRequest = false;

            return true;
        }
    }
}
