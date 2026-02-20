using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // Handles facing direction by flipping a visual root
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerFacing : MonoBehaviour
    {
        // ─────────────────────────────
        // Facing Settings
        // ─────────────────────────────
        public enum FacingDir { Left = -1, Right = 1 }
        
        [Header("Visual Root (Graphics)")]
        [SerializeField] private Transform graphicsRoot;

        [Header("Initial Facing")]
        [SerializeField] private FacingDir startFacing = FacingDir.Right;
        
        public FacingDir Current { get; private set; }
        
        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Reset()
        {
            // Try to auto-find a child named "Graphics"
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

            // default RIGHT
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

