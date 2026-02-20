using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // PlayerSafeSpot
    // - Records the last safe position while grounded and not in hazard contact.
    // - Used by hazards/respawn to teleport player back to a safe location.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerSafeSpot : MonoBehaviour
    {
        // ─────────────────────────────
        // Record settings
        // ─────────────────────────────
        [Header("Record")]
        [SerializeField] private float recordInterval = 0.10f;   // How often to record safe spot
        [SerializeField] private float minGroundedTime = 0.05f;  // Debounce: grounded must last this long

        // ─────────────────────────────
        // Debug
        // ─────────────────────────────
        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;

        // ─────────────────────────────
        // Public state
        // ─────────────────────────────
        public Vector3 LastSafePosition { get; private set; }

        // ─────────────────────────────
        // Outlets
        // ─────────────────────────────
        private PlayerRoot _root;

        // ─────────────────────────────
        // Runtime state
        // ─────────────────────────────
        private float _nextRecordTime;
        private float _groundedTimer;

        private int _hazardContactCount;
        private float _suspendUntil;

        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Reset()
        {
            ClampSerializedValues();
        }

        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
            ClampSerializedValues();

            // Initialize with current position as a reasonable default
            LastSafePosition = transform.position;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampSerializedValues();
        }
#endif

        private void ClampSerializedValues()
        {
            recordInterval = Mathf.Max(0.01f, recordInterval);
            minGroundedTime = Mathf.Max(0f, minGroundedTime);
        }

        private void Update()
        {
            if (_root == null || _root.Jump == null) return;

            // Track grounded stability
            if (_root.Jump.IsGrounded) _groundedTimer += Time.deltaTime;
            else _groundedTimer = 0f;

            // Rate limit recording
            if (Time.time < _nextRecordTime) return;
            _nextRecordTime = Time.time + recordInterval;

            // Recording guards
            if (Time.time < _suspendUntil) return;
            if (_hazardContactCount > 0) return;
            if (_groundedTimer < minGroundedTime) return;

            LastSafePosition = transform.position;
        }

        // ─────────────────────────────
        // Public API
        // ─────────────────────────────
        public void TeleportToSafe()
        {
            transform.position = LastSafePosition;

            // Clear velocity so we don't immediately drift back into hazards
            if (_root != null && _root.Rb != null)
                _root.Rb.velocity = Vector2.zero;
        }

        /// <summary>
        /// Hazards call this when entering/exiting hazard volumes.
        /// delta: +1 on enter, -1 on exit.
        /// </summary>
        public void AddHazardContact(int delta)
        {
            _hazardContactCount = Mathf.Max(0, _hazardContactCount + delta);
        }

        /// <summary>
        /// Temporarily suspends safe spot recording (e.g. right after respawn/teleport).
        /// </summary>
        public void SuspendRecording(float seconds)
        {
            _suspendUntil = Mathf.Max(_suspendUntil, Time.time + Mathf.Max(0f, seconds));
        }

        // ─────────────────────────────
        // Gizmos
        // ─────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(LastSafePosition, 0.08f);
        }
    }
}
