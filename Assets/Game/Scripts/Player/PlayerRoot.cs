using UnityEngine;
using Game.Utils.Physics2D;
    
namespace Game.Player
{
    // ─────────────────────────────
    // PlayerRoot is the structural entry point of the Player entity.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerRoot : MonoBehaviour
    {
        // ─────────────────────────────
        // References
        // ─────────────────────────────
        [Header("Core Components")]
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Collider2D _collider;

        [Header("Visual Roots")]
        [SerializeField] private Transform graphicsRoot;      // Player/Graphics
        [SerializeField] private Transform vfxRoot;           // Player/VFX
        [SerializeField] private Animator graphicsAnimator;   // Graphics - Animator
        [SerializeField] private Animator bloodFxAnimator;    // VFX/HitFX - Animator

        // ─────────────────────────────
        // Modules
        // ─────────────────────────────
        public PlayerInput Input { get; private set; }
        public PlayerMovement Movement { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public PlayerFacing Facing { get; private set; }
        public PlayerJump Jump { get; private set; }
        public GroundSensor2D Ground { get; private set; }

        // ─────────────────────────────
        // Public accessors (read-only)
        // ─────────────────────────────
        public Rigidbody2D Rb => _rb;
        public Collider2D Collider => _collider;

        public Transform GraphicsRoot => graphicsRoot;
        public Transform VfxRoot => vfxRoot;

        public Animator GraphicsAnimator => graphicsAnimator;
        public Animator BloodFxAnimator => bloodFxAnimator;

        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Reset()
        {
            AutoAssignCore();
            AutoAssignVisuals();
        }

        private void Awake()
        {
            AutoAssignCore();
            AutoAssignVisuals();
            CacheModules();
        }

        private void AutoAssignCore()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_collider == null) _collider = GetComponent<Collider2D>();
        }

        private void AutoAssignVisuals()
        {
            if (graphicsRoot == null) graphicsRoot = transform.Find("Graphics");
            if (vfxRoot == null) vfxRoot = transform.Find("VFX");

            if (graphicsAnimator == null && graphicsRoot != null)
                graphicsAnimator = graphicsRoot.GetComponent<Animator>();

            // Hierarchy: VFX/HitFX
            if (bloodFxAnimator == null && vfxRoot != null)
            {
                var hitFx = vfxRoot.Find("HitFX");
                if (hitFx != null) bloodFxAnimator = hitFx.GetComponent<Animator>();
            }
        }

        private void CacheModules()
        {
            Input = GetComponent<PlayerInput>();
            Movement = GetComponent<PlayerMovement>();
            Combat = GetComponent<PlayerCombat>();
            Facing = GetComponent<PlayerFacing>();
            Jump = GetComponent<PlayerJump>();
            Ground = GetComponentInChildren<GroundSensor2D>();
        }
    }
}
