using UnityEngine;

namespace Game.Player
{
    // PlayerMovement
    // - Handles horizontal locomotion using Rigidbody2D velocity.
    // - Applies acceleration/deceleration towards target speed.
    // - Receives move input from PlayerController.
    [DisallowMultipleComponent]
    public class PlayerMovement : MonoBehaviour
    {
        // ------------------------------
        // Config
        // ------------------------------
        [Header("Movement Settings")]
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 80f;

        // ------------------------------
        // Outlets
        // ------------------------------
        private PlayerRoot _root;

        // ------------------------------
        // Runtime state
        // ------------------------------
        private float _moveInputX;

        // ------------------------------
        // Methods
        // ------------------------------
        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
        }

        public void SetMoveInput(float x)
        {
            _moveInputX = Mathf.Clamp(x, -1f, 1f);
        }

        private void FixedUpdate()
        {
            if (_root == null || _root.Rb == null) return;

            Vector2 v = _root.Rb.velocity;
            float speedMultiplier = _root.CharmRuntime != null ? _root.CharmRuntime.GetMoveSpeedMultiplier() : 1f;

            // Target speed from input magnitude.
            float targetVx = _moveInputX * maxSpeed * speedMultiplier;

            // Choose accel vs decel by input intent.
            float rate = (Mathf.Abs(targetVx) > 0.01f) ? acceleration : deceleration;

            // Move towards target speed smoothly.
            float newVx = Mathf.MoveTowards(v.x, targetVx, rate * Time.fixedDeltaTime);

            _root.Rb.velocity = new Vector2(newVx, v.y);
        }
    }
}
