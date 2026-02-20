using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // Handles vertical jumping and ground detection.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerJump : MonoBehaviour
    {
        // ─────────────────────────────
        // Jump Settings
        // ─────────────────────────────
        [Header("Jump Settings")]
        [SerializeField] private float jumpVelocity = 10f;
        
        [Header("Variable Jump")]
        [SerializeField, Range(0.1f, 1f)]
        private float jumpCutMultiplier = 0.5f;

        public bool IsGrounded => _root != null && _root.Ground != null && _root.Ground.IsGrounded;
        
     
        // ─────────────────────────────
        // Outlets
        // ─────────────────────────────
        private PlayerRoot _root;
        
        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Reset()
        {
            _root = GetComponent<PlayerRoot>();
        }
        
        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
        }
        
        

        public bool TryJump()
        {
            if (!IsGrounded) return false;

            Vector2 v = _root.Rb.velocity;
            v.y = jumpVelocity;
            _root.Rb.velocity = v;

            return true;
        }
        
        public void OnJumpReleased()
        {
            // Only cut jump if we are still moving upward
            if (_root.Rb.velocity.y > 0.01f)
            {
                Vector2 v = _root.Rb.velocity;
                v.y *= jumpCutMultiplier;  // cut the upward speed
                _root.Rb.velocity = v;
            }
        }

        
    }

}
