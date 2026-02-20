using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

namespace Game.Enemies
{
    // ─────────────────────────────
    // EnemyContactDamage
    // - Put this on the enemy "ContactDamage" trigger collider object.
    // - Deals contact damage to the player with a short cooldown.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyContactDamage : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float hitCooldown = 0.35f;

        [Header("Target")]
        [SerializeField] private string playerTag = "Player";

        private Collider2D _col;
        private float _nextTime;

        private void Reset()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
        }

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < _nextTime) return;

            // Only damage player
            if (!other.CompareTag(playerTag) && other.GetComponentInParent<Transform>()?.CompareTag(playerTag) != true)
                return;

            // Try find IDamageable on player root (robust for child colliders)
            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            Vector2 dir = (other.transform.position - transform.position);
            if (dir.sqrMagnitude > 0f) dir.Normalize();
            else dir = Vector2.right;

            var info = new DamageInfo
            {
                damage = damage,
                type = DamageType.EnemyContact, 
                hitPoint = other.ClosestPoint(transform.position),
                hitDir = dir,
                source = transform.root.gameObject
            };

            if (damageable.TakeDamage(info))
                _nextTime = Time.time + hitCooldown;
        }

        // 给外部统一调参用
        public void SetDamage(int dmg) => damage = Mathf.Max(0, dmg);
    }
}
