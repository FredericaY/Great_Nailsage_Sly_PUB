using System.Collections.Generic;
using UnityEngine;
using Game.Combat;
using Game.Player;

namespace Game.Systems.Hazard
{
    // ─────────────────────────────
    // HazardDamageZone
    // - Applies hazard damage with per-target cooldown (TriggerEnter/Stay).
    // - Optionally bounces player (or objects with PlayerRoot) away from hazard.
    // - Notifies PlayerSafeSpot to avoid recording while in hazard contact.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class HazardDamageZone : MonoBehaviour
    {
        // ─────────────────────────────
        // Damage
        // ─────────────────────────────
        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private LayerMask targetLayers;

        // ─────────────────────────────
        // Bounce (Player only)
        // ─────────────────────────────
        [Header("Bounce (Player Only)")]
        [SerializeField] private Vector2 bounceImpulse = new(18f, 10f);

        // ─────────────────────────────
        // Cooldown
        // ─────────────────────────────
        [Header("Cooldown")]
        [SerializeField] private float perTargetCooldown = 0.35f;

        // ─────────────────────────────
        // Runtime state
        // ─────────────────────────────
        private readonly Dictionary<Collider2D, float> _cooldowns = new();

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
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampSerializedValues();
        }
#endif

        private void ClampSerializedValues()
        {
            damage = Mathf.Max(0f, damage);

            bounceImpulse.x = Mathf.Max(0f, bounceImpulse.x);
            bounceImpulse.y = Mathf.Max(0f, bounceImpulse.y);

            perTargetCooldown = Mathf.Max(0.01f, perTargetCooldown);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Inform safe-spot system that we're in hazard contact
            var safe = other.GetComponentInParent<PlayerSafeSpot>();
            if (safe != null) safe.AddHazardContact(+1);

            TryHit(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryHit(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var safe = other.GetComponentInParent<PlayerSafeSpot>();
            if (safe != null) safe.AddHazardContact(-1);

            // Optional cleanup: avoid dictionary growth if colliders are destroyed frequently
            _cooldowns.Remove(other);
        }

        // ─────────────────────────────
        // Internal
        // ─────────────────────────────
        private void TryHit(Collider2D other)
        {
            // Layer filtering
            if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

            // Per-collider cooldown gate
            float now = Time.time;
            if (_cooldowns.TryGetValue(other, out float until) && now < until) return;
            _cooldowns[other] = now + perTargetCooldown;

            // Find damage receiver (collider object preferred, then parent)
            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            // Hit direction from hazard → target
            Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;

            // Build hazard damage info
            var info = new DamageInfo(
                damage: damage,
                hitDir: dir,
                hitPoint: other.ClosestPoint(transform.position),
                source: gameObject,
                type: DamageType.Hazard,
                hazardKind: HazardKind.Damage,
                bypassIFrames: false
            );

            bool applied = damageable.TakeDamage(info);
            if (!applied) return;

            // Bounce: only for player-like targets
            TryBouncePlayerLike(other, dir);
        }

        private void TryBouncePlayerLike(Collider2D other, Vector2 dir)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;

            // "Player-like" detection: tag Player OR has PlayerRoot in parent
            bool isPlayerLike =
                other.CompareTag("Player") ||
                other.GetComponentInParent<PlayerRoot>() != null;

            if (!isPlayerLike) return;

            // Cancel horizontal velocity, then apply impulse
            rb.velocity = new Vector2(0f, rb.velocity.y);

            float signX = (Mathf.Abs(dir.x) < 0.001f) ? 0f : Mathf.Sign(dir.x);
            Vector2 imp = new Vector2(signX * bounceImpulse.x, bounceImpulse.y);

            rb.AddForce(imp, ForceMode2D.Impulse);
        }
    }
}
