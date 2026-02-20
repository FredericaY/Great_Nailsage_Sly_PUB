using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;
using Game.Player;

namespace Game.Systems.Hazard
{
    // ─────────────────────────────
    // HazardAbyssZone
    // - Triggers an abyss sequence: lock player → apply hazard damage → wait → teleport to safe spot.
    // - Uses re-trigger guard to avoid looping immediately after teleport.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class HazardAbyssZone : MonoBehaviour
    {
        // ─────────────────────────────
        // Damage
        // ─────────────────────────────
        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private LayerMask targetLayers;

        // ─────────────────────────────
        // Abyss sequence
        // ─────────────────────────────
        [Header("Abyss Sequence")]
        [SerializeField] private float abyssStunTime = 0.46f; // 14/30 ≈ 0.466

        // ─────────────────────────────
        // Safe Spot
        // ─────────────────────────────
        [Header("Safe Spot")]
        [SerializeField] private float suspendRecordAfterRespawn = 0.25f;

        // ─────────────────────────────
        // Re-trigger guard
        // ─────────────────────────────
        [Header("Re-trigger Guard")]
        [Tooltip("After teleport, ignore abyss triggers for this long to prevent re-enter looping.")]
        [SerializeField] private float postRespawnIgnoreTime = 0.25f;

        // ─────────────────────────────
        // Runtime state
        // ─────────────────────────────
        private readonly HashSet<PlayerRoot> _running = new();
        private readonly Dictionary<PlayerRoot, float> _ignoreUntil = new();

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
            abyssStunTime = Mathf.Max(0f, abyssStunTime);
            suspendRecordAfterRespawn = Mathf.Max(0f, suspendRecordAfterRespawn);
            postRespawnIgnoreTime = Mathf.Max(0f, postRespawnIgnoreTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Layer filtering
            if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

            // Find player root
            var root = other.GetComponentInParent<PlayerRoot>();
            if (root == null) return;

            // Ignore window after respawn to avoid immediate re-trigger loops
            if (_ignoreUntil.TryGetValue(root, out float until) && Time.time < until) return;

            // One sequence per player at a time
            if (_running.Contains(root)) return;

            // Track hazard contact for safe-spot recording guard
            var safe = other.GetComponentInParent<PlayerSafeSpot>();
            if (safe != null) safe.AddHazardContact(+1);

            StartCoroutine(AbyssSequence(other, root, safe));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var safe = other.GetComponentInParent<PlayerSafeSpot>();
            if (safe != null) safe.AddHazardContact(-1);
        }

        private IEnumerator AbyssSequence(Collider2D other, PlayerRoot root, PlayerSafeSpot safe)
        {
            _running.Add(root);

            var lockComp = root.GetComponent<PlayerLock>();
            if (lockComp == null)
            {
                Debug.LogError("[HazardAbyssZone] Missing PlayerLock on PlayerRoot.", root);
                _running.Remove(root);
                yield break;
            }

            // Find a damage receiver (collider object preferred, then parent)
            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();

            lockComp.Acquire();

            try
            {
                // 1) Apply abyss hazard damage (bypass i-frames)
                if (damageable != null && damage > 0f)
                {
                    // If you kept the DamageInfo constructor from earlier, this is the safest way:
                    var info = new DamageInfo(
                        damage: damage,
                        hitDir: Vector2.up,
                        hitPoint: other.ClosestPoint(transform.position),
                        source: gameObject,
                        type: DamageType.Hazard,
                        hazardKind: HazardKind.Abyss,
                        bypassIFrames: true
                    );

                    damageable.TakeDamage(info);
                }

                // 2) Stun time (allow hurt anim / pause)
                if (abyssStunTime > 0f)
                    yield return new WaitForSeconds(abyssStunTime);

                // 3) Teleport back to last safe spot & suspend recording briefly
                if (safe != null)
                {
                    safe.TeleportToSafe();
                    safe.SuspendRecording(suspendRecordAfterRespawn);
                }

                // 4) Re-trigger guard after teleport
                _ignoreUntil[root] = Time.time + postRespawnIgnoreTime;

                // 5) Safety: clear velocity next frame
                yield return null;
                if (root.Rb != null)
                    root.Rb.velocity = Vector2.zero;
            }
            finally
            {
                lockComp.Release();
                _running.Remove(root);
            }
        }
    }
}
