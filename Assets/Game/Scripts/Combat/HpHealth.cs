using System;
using UnityEngine;

namespace Game.Combat
{
    // ─────────────────────────────
    // HpHealth
    // - Continuous HP-based health system implementing IDamageable.
    // - Supports hazard rules (instant kill on specific hazard types).
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class HpHealth : MonoBehaviour, IDamageable
    {
        // ─────────────────────────────
        // Hazard behavior
        // ─────────────────────────────
        public enum HazardAffectMode
        {
            KillOnAnyHazard,
            KillOnAbyssOnly
        }

        // ─────────────────────────────
        // HP Settings
        // ─────────────────────────────
        [Header("HP")]
        [SerializeField] private float maxHP = 50f;
        [SerializeField] private float hp = 50f;

        // ─────────────────────────────
        // Hazard Rules
        // ─────────────────────────────
        [Header("Hazard Rules")]
        [SerializeField] private HazardAffectMode hazardMode = HazardAffectMode.KillOnAnyHazard;

        // ─────────────────────────────
        // Public read-only
        // ─────────────────────────────
        public float MaxHP => maxHP;
        public float HP => hp;
        public bool IsDead => hp <= 0f;

        // ─────────────────────────────
        // Events
        // ─────────────────────────────
        public event Action<DamageInfo> OnHurt;
        public event Action OnDeath;

        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Reset()
        {
            ClampSerializedValues();
        }

        private void Awake()
        {
            ClampSerializedValues();
            hp = maxHP;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampSerializedValues();
        }
#endif

        private void ClampSerializedValues()
        {
            maxHP = Mathf.Max(1f, maxHP);
            hp = Mathf.Clamp(hp, 0f, maxHP);
        }

        // ─────────────────────────────
        // IDamageable
        // ─────────────────────────────
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead) return false;

            // ─────────────────────────────
            // Hazard logic
            // ─────────────────────────────
            if (info.type == DamageType.Hazard)
            {
                bool shouldDie =
                    hazardMode == HazardAffectMode.KillOnAnyHazard ||
                    (hazardMode == HazardAffectMode.KillOnAbyssOnly &&
                     info.hazardKind == HazardKind.Abyss);

                if (shouldDie)
                {
                    hp = 0f;
                    OnHurt?.Invoke(info);
                    OnDeath?.Invoke();
                    return true;
                }

                // Hazard exists but does not apply under current rule
                return false;
            }

            // ─────────────────────────────
            // Normal damage
            // ─────────────────────────────
            float dmg = Mathf.Max(0f, info.damage);
            if (dmg <= 0f) return false;

            hp = Mathf.Max(0f, hp - dmg);

            OnHurt?.Invoke(info);

            if (hp <= 0f)
                OnDeath?.Invoke();

            return true;
        }
    }
}
