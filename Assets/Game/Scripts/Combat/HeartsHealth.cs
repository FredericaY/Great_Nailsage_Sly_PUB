using System;
using UnityEngine;

namespace Game.Combat
{
    // ─────────────────────────────
    // HeartsHealth
    // - A heart-based health system implementing IDamageable
    // - Supports i-frames (invincibility) and event callbacks
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class HeartsHealth : MonoBehaviour, IDamageable
    {
        // ─────────────────────────────
        // Hearts Settings
        // ─────────────────────────────
        [Header("Hearts")]
        [SerializeField] private int maxHearts = 5;
        [SerializeField] private int hearts = 5;

        // ─────────────────────────────
        // Damage Mapping
        // ─────────────────────────────
        [Header("Damage Mapping")]
        [Tooltip("How much damage equals one heart.")]
        [SerializeField] private float damagePerHeart = 10f;

        // ─────────────────────────────
        // I-Frames
        // ─────────────────────────────
        [Header("I-Frames")]
        [SerializeField] private float invincibleTime = 0.6f;

        // ─────────────────────────────
        // Runtime state
        // ─────────────────────────────
        private float _invUntil;

        // ─────────────────────────────
        // Public read-only accessors
        // ─────────────────────────────
        public int MaxHearts => maxHearts;
        public int Hearts => hearts;

        public float DamagePerHeart => damagePerHeart;
        public float InvincibleTime => invincibleTime;

        public bool IsInvincible => Time.time < _invUntil;
        public bool IsDead => hearts <= 0;

        // ─────────────────────────────
        // Events
        // ─────────────────────────────
        public event Action<DamageInfo> OnHurt;
        public event Action<int> OnHeartsChanged;
        public event Action OnDeath;

        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Reset()
        {
            // Editor convenience: keep serialized values reasonable
            ClampSerializedValues();
        }

        private void Awake()
        {
            ClampSerializedValues();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep values sane while editing
            ClampSerializedValues();
        }
#endif

        private void ClampSerializedValues()
        {
            maxHearts = Mathf.Max(1, maxHearts);
            hearts = Mathf.Clamp(hearts, 0, maxHearts);

            // Avoid division by zero / negative mapping
            damagePerHeart = Mathf.Max(0.0001f, damagePerHeart);

            invincibleTime = Mathf.Max(0f, invincibleTime);
        }

        // ─────────────────────────────
        // IDamageable
        // ─────────────────────────────
        public bool TakeDamage(DamageInfo info)
        {
            // Respect i-frames unless bypass is requested
            if (IsInvincible && !info.bypassIFrames) return false;

            float dmg = Mathf.Max(0f, info.damage);
            if (dmg <= 0f) return false;

            if (IsDead) return false;

            // Convert damage to heart loss (at least 1 heart per non-zero hit)
            int heartLoss = Mathf.Max(1, Mathf.CeilToInt(dmg / damagePerHeart));

            int before = hearts;
            hearts = Mathf.Max(0, hearts - heartLoss);

            // Start i-frames if not bypassed
            if (!info.bypassIFrames)
                _invUntil = Time.time + invincibleTime;

            // Fire events
            if (hearts != before)
                OnHeartsChanged?.Invoke(hearts);

            OnHurt?.Invoke(info);

            if (hearts <= 0)
                OnDeath?.Invoke();

            return true;
        }

        // ─────────────────────────────
        // Utilities
        // ─────────────────────────────
        public void SetInvincible(float duration)
        {
            // Extend i-frames to the later time
            float t = Time.time + Mathf.Max(0f, duration);
            _invUntil = Mathf.Max(_invUntil, t);
        }

        public void RestoreFull()
        {
            hearts = maxHearts;
            _invUntil = 0f;
            OnHeartsChanged?.Invoke(hearts);
        }

        public void Restore(int amount)
        {
            int before = hearts;
            hearts = Mathf.Clamp(hearts + Mathf.Max(0, amount), 0, maxHearts);

            if (hearts != before)
                OnHeartsChanged?.Invoke(hearts);
        }
    }
}
