using UnityEngine;
using Game.Combat;
using Game.Utils;

namespace Game.Enemies
{
    // ─────────────────────────────
    // EnemyHurtVfx
    // - Pure visual & light gameplay feedback on hurt.
    // - Does NOT control animation (handled by BT).
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class EnemyHurtVfx : MonoBehaviour
    {
        // ─────────────────────────────
        // References
        // ─────────────────────────────
        [Header("References")]
        [SerializeField] private EnemyBlackboard blackboard;
        [SerializeField] private HpHealth hpHealth;

        // ─────────────────────────────
        // Optional gameplay lock
        // ─────────────────────────────
        [Header("Hurt Lock (optional)")]
        [SerializeField] private float hurtLockTime = 0.25f;

        // ─────────────────────────────
        // Flash (Shader _Flash)
        // ─────────────────────────────
        [Header("Flash")]
        [SerializeField] private SpriteRenderer[] flashTargets;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.06f;
        [SerializeField] private int flashTimes = 1;

        private void Reset()
        {
            AutoAssign();
            Clamp();
        }

        private void Awake()
        {
            AutoAssign();
            Clamp();
        }

        private void OnEnable()
        {
            if (hpHealth != null)
                hpHealth.OnHurt += OnHurt;
        }

        private void OnDisable()
        {
            if (hpHealth != null)
                hpHealth.OnHurt -= OnHurt;

            // safety: ensure flash is cleared
            if (flashTargets != null && flashTargets.Length > 0)
                SpriteFlashUtil.Flash(flashTargets, 0f, flashColor, 1);
        }

        // ─────────────────────────────
        // Setup helpers
        // ─────────────────────────────
        private void AutoAssign()
        {
            if (!blackboard)
                blackboard = GetComponent<EnemyBlackboard>();

            if (!hpHealth)
                hpHealth = GetComponent<HpHealth>();

            if (flashTargets == null || flashTargets.Length == 0)
                flashTargets = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void Clamp()
        {
            hurtLockTime = Mathf.Max(0f, hurtLockTime);
            flashDuration = Mathf.Max(0f, flashDuration);
            flashTimes = Mathf.Max(1, flashTimes);
        }

        // ─────────────────────────────
        // Event
        // ─────────────────────────────
        private void OnHurt(DamageInfo info)
        {
            if (hpHealth != null && hpHealth.IsDead) return;

            // Optional: lock BT logic
            if (blackboard != null && hurtLockTime > 0f)
            {
                blackboard.isHurtLocked = true;
                CancelInvoke(nameof(Unlock));
                Invoke(nameof(Unlock), hurtLockTime);
            }

            // Flash effect
            if (flashDuration > 0f && flashTargets != null && flashTargets.Length > 0)
            {
                SpriteFlashUtil.Flash(
                    flashTargets,
                    flashDuration,
                    flashColor,
                    flashTimes
                );
            }
        }

        private void Unlock()
        {
            if (blackboard != null)
                blackboard.isHurtLocked = false;
        }
    }
}
