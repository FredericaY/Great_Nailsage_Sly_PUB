using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpwardForce = 2f;
    [Tooltip("Time in seconds the enemy stays in knockback (AI won't overwrite velocity).")]
    [SerializeField] private float knockbackDuration = 0.3f;
    [Tooltip("Linear drag on the Rigidbody2D so the enemy slows down after knockback instead of sliding off the map. 0 = slides forever.")]
    [SerializeField] private float linearDrag = 6f;

    private Rigidbody2D rb;
    private float _knockbackEndTime;
    private HpHealth _health;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.drag = linearDrag;
            rb.freezeRotation = true;
        }

        _health = GetComponent<HpHealth>();
        if (_health != null)
            _health.OnHurt += ApplyKnockback;
    }
    private void OnDestroy()
    {
        if (_health != null)
            _health.OnHurt -= ApplyKnockback;
    }

    /// <summary>True while knockback is active; AI should not drive velocity during this time.</summary>
    public bool IsInKnockback => Time.time < _knockbackEndTime;

    private void LateUpdate()
    {
        // keep upright in case something else tries to rotate (e.g. prefab state, physics glitch)
        transform.rotation = Quaternion.identity;
    }

    public void ApplyKnockback(DamageInfo info)
    {
        if (rb == null) return;

        Vector2 force = info.hitDir.normalized * knockbackForce;
        force.y += knockbackUpwardForce;
        // never push into the ground (e.g. from down-air or low hit)
        force.y = Mathf.Max(force.y, 0f);

        rb.velocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
        _knockbackEndTime = Time.time + knockbackDuration;
    }
}
