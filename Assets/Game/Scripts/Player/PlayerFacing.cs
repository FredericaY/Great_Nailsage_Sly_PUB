using UnityEngine;

namespace Game.Player
{
    // PlayerFacing
    // - Tracks logical facing direction.
    // - Flips graphics root on X axis.
    // - Accepts facing input from movement/wall logic.
    [DisallowMultipleComponent]
    public class PlayerFacing : MonoBehaviour
    {
        // ------------------------------
        // Types
        // ------------------------------
        public enum FacingDir { Left = -1, Right = 1 }

        // ------------------------------
        // Config
        // ------------------------------
        [Header("Visual Root (Graphics)")]
        [SerializeField] private Transform graphicsRoot;

        [Header("Initial Facing")]
        [SerializeField] private FacingDir startFacing = FacingDir.Right;

        // ------------------------------
        // Public state
        // ------------------------------
        public FacingDir Current { get; private set; }

        // ------------------------------
        // Methods
        // ------------------------------
        private void Reset()
        {
            // Try to auto-find a child named "Graphics".
            var t = transform.Find("Graphics");
            if (t != null) graphicsRoot = t;
        }

        private void Awake()
        {
            if (graphicsRoot == null)
            {
                Debug.LogError("[PlayerFacing] Missing graphicsRoot. Create a child named 'Graphics' and assign it.", this);
                return;
            }

            SetFacing(startFacing, force: true);
        }

        public void SetFacing(FacingDir dir, bool force = false)
        {
            if (!force && dir == Current) return;
            Current = dir;

            Vector3 s = graphicsRoot.localScale;

            // Default orientation is facing right.
            s.x = (dir == FacingDir.Right)
                ? Mathf.Abs(s.x)
                : -Mathf.Abs(s.x);

            graphicsRoot.localScale = s;
        }

        public void FaceByMoveX(float moveX, float deadZone = 0.01f)
        {
            if (Mathf.Abs(moveX) < deadZone) return;
            SetFacing(moveX > 0 ? FacingDir.Right : FacingDir.Left);
        }
    }
}

