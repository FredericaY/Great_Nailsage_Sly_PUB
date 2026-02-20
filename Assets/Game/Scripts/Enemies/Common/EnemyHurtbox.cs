using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

namespace Game.Enemies
{
    // ─────────────────────────────
    // EnemyHurtbox
    // - Attach to the enemy Hurtbox trigger collider object.
    // - Implements IDamageable so Player AttackHitbox can hit it.
    // - Forwards DamageInfo to HpHealth on the root.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyHurtbox : MonoBehaviour, IDamageable
    {
        [Header("Forward Target (Root)")]
        [SerializeField] private HpHealth hpHealth;

        private Collider2D _col;

        private void Reset()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;

            hpHealth = GetComponentInParent<HpHealth>();
        }

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;

            if (!hpHealth)
                hpHealth = GetComponentInParent<HpHealth>();
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (!hpHealth) return false;
            return hpHealth.TakeDamage(info);
        }
    }
}
