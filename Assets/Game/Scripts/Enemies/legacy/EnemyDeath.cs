using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyDeath : MonoBehaviour
    {
        [Header("Optional Visuals")]
        [SerializeField] private Animator animator;
        [SerializeField] private string deathTrigger = "Die";
        [Tooltip("If no Animator/clip, we just disable components and (optionally) destroy after delay.")]
        [SerializeField] private float destroyDelay = 0.8f;

        [Header("Disable On Death")]
        [SerializeField] private bool disableAI = true;
        [SerializeField] private bool disableKnockback = true;
        [SerializeField] private bool disableColliders = true;
        [SerializeField] private bool stopRigidbody = true;

        private HpHealth _hp;
        private Rigidbody2D _rb;
        private Collider2D[] _colliders;

        private EnemyAI _ai;
        private EnemyKnockback _knockback;

        private bool _deadHandled;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        private void Awake()
        {
            _hp = GetComponent<HpHealth>();
            _rb = GetComponent<Rigidbody2D>();
            _colliders = GetComponentsInChildren<Collider2D>(true);

            _ai = GetComponent<EnemyAI>();
            _knockback = GetComponent<EnemyKnockback>();

            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            if (_hp != null) _hp.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (_hp != null) _hp.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            if (_deadHandled) return;
            _deadHandled = true;

            // 1) stop gameplay logic
            if (disableAI && _ai != null) _ai.enabled = false;
            if (disableKnockback && _knockback != null) _knockback.enabled = false;

            // 2) physics cleanup
            if (stopRigidbody && _rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.simulated = false; // stops physics completely (2D)
            }

            if (disableColliders && _colliders != null)
            {
                foreach (var c in _colliders)
                    c.enabled = false;
            }

            // 3) play animation if exists
            if (animator != null && !string.IsNullOrEmpty(deathTrigger))
            {
                animator.ResetTrigger(deathTrigger);
                animator.SetTrigger(deathTrigger);
            }

            // 4) destroy (or you can disable GameObject if you prefer)
            if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
            else Destroy(gameObject);
        }
    }
}

