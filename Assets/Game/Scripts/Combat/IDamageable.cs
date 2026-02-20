using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    // ─────────────────────────────
    // IDamageable
    // - Implemented by objects that can receive damage.
    // - Return true if damage was successfully applied.
    // ─────────────────────────────
    public interface IDamageable
    {
        bool TakeDamage(DamageInfo info);
    }
}


