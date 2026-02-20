using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    // ─────────────────────────────
    // DamageInfo
    // - Immutable-like data packet describing a damage event.
    // - Passed to IDamageable.TakeDamage().
    // ─────────────────────────────
    public struct DamageInfo
    {
        // ─────────────────────────────
        // Core damage data
        // ─────────────────────────────
        public float damage;          // Raw damage value
        public Vector2 hitDir;        // Direction of the hit (normalized preferred)
        public Vector2 hitPoint;      // World-space hit position
        public GameObject source;     // Attacker / source object

        // ─────────────────────────────
        // Classification
        // ─────────────────────────────
        public DamageType type;       // Physical / Fire / etc.
        public HazardKind hazardKind; // Damage / Abyss / etc.

        // ─────────────────────────────
        // Flags
        // ─────────────────────────────
        public bool bypassIFrames;    // Ignore invincibility frames

        // ─────────────────────────────
        // Constructor (recommended usage)
        // ─────────────────────────────
        public DamageInfo(
            float damage,
            Vector2 hitDir,
            Vector2 hitPoint,
            GameObject source,
            DamageType type = default,
            HazardKind hazardKind = HazardKind.None,
            bool bypassIFrames = false)
        {
            this.damage = damage;
            this.hitDir = hitDir;
            this.hitPoint = hitPoint;
            this.source = source;

            this.type = type;
            this.hazardKind = hazardKind;
            this.bypassIFrames = bypassIFrames;
        }
    }

    // ─────────────────────────────
    // HazardKind
    // - Classifies environmental hazards
    // ─────────────────────────────
    public enum HazardKind
    {
        None = 0,
        Damage,   // Normal spikes / traps
        Abyss     // Instant death / void
    }
}



