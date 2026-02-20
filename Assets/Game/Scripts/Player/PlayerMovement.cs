using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // Handles player locomotion (e.g., horizontal move, facing, physics-based motion).
    // Gameplay decisions (when to move) come from higher-level logic/root.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerMovement : MonoBehaviour
    {
        // ─────────────────────────────
        // Movement Settings
        // ─────────────────────────────
        [Header("Movement Settings")]
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 80f;

        // [Header("Facing")]
        // [SerializeField] private bool startFacingRight = true;
        // ─────────────────────────────
        // Outlets
        // ─────────────────────────────
        private PlayerRoot _root;
        private float _moveInputX;
        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
            //ApplyInitialFacing();
        }
        
        public void SetMoveInput(float x)
        {
            _moveInputX = Mathf.Clamp(x, -1f, 1f);
        }

        private void FixedUpdate()
        {
            if (_root == null || _root.Rb == null) return;

            Vector2 v = _root.Rb.velocity;

            // target speed from input magnitude (supports analog stick later)
            float targetVx = _moveInputX * maxSpeed;

            // choose accel vs decel depending on whether player is trying to move
            float rate = (Mathf.Abs(targetVx) > 0.01f) ? acceleration : deceleration;

            // move towards target speed smoothly
            float newVx = Mathf.MoveTowards(v.x, targetVx, rate * Time.fixedDeltaTime);

            _root.Rb.velocity = new Vector2(newVx, v.y);


        }
        
    }

}
