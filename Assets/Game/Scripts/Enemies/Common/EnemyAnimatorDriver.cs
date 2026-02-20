using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Utils.Physics2D;

namespace Game.Enemies
{
    // ─────────────────────────────
    // EnemyAnimatorDriver
    // - Writes shared gameplay state into Animator parameters
    // - Intended to be reused by all enemies
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Grounded Param")]
        [SerializeField] private string groundedParam = "Grounded";

        private EnemyRoot _root;
        private int groundedHash;

        private void Reset()
        {
            _root = GetComponent<EnemyRoot>();
            if (!animator) animator = GetComponentInChildren<Animator>(true);
        }

        private void Awake()
        {
            _root = GetComponent<EnemyRoot>();
            if (!animator && _root != null) animator = _root.Animator;
            if (!animator) animator = GetComponentInChildren<Animator>(true);

            groundedHash = Animator.StringToHash(groundedParam);
        }

        private void LateUpdate()
        {
            if (!animator || _root == null) return;

            bool grounded = _root.Ground != null && _root.Ground.IsGrounded;
            animator.SetBool(groundedHash, grounded);
        }
    }
}
