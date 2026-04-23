using UnityEngine;

namespace Game.Utils.Physics2D
{
    [DisallowMultipleComponent]
    public class WallSensor2D : MonoBehaviour
    {
        // Wall probe settings.
        [Header("Wall Check")]
        [SerializeField] private Transform wallCheckL;
        [SerializeField] private Transform wallCheckR;
        [SerializeField] private float rayLength = 0.2f;
        [SerializeField] private LayerMask wallMask;

        public bool IsOnWall { get; private set; }
        /// <summary> -1 = left, 1 = right, 0 = none </summary>
        public int WallSide { get; private set; }
        private bool _warnedMissingMask;

#if UNITY_EDITOR
        private void Reset()
        {
            EnsureWallChecks();
        }
#endif

        private void Awake()
        {
            EnsureWallChecks();
        }

        private void EnsureWallChecks()
        {
            if (wallCheckL == null)
            {
                var l = new GameObject("WallCheckL");
                l.transform.SetParent(transform);
                l.transform.localPosition = new Vector3(-0.25f, 0.5f, 0f);
                wallCheckL = l.transform;
            }
            if (wallCheckR == null)
            {
                var r = new GameObject("WallCheckR");
                r.transform.SetParent(transform);
                r.transform.localPosition = new Vector3(0.25f, 0.5f, 0f);
                wallCheckR = r.transform;
            }
        }

        private void Update()
        {
            IsOnWall = false;
            WallSide = 0;
            if (wallMask.value == 0)
            {
                if (!_warnedMissingMask)
                {
                    Debug.LogWarning(
                        $"[{nameof(WallSensor2D)}] wallMask is empty on '{name}'. " +
                        "Assign a wall layer to enable wall detection.",
                        this);
                    _warnedMissingMask = true;
                }
                return;
            }

            _warnedMissingMask = false;

            if (wallCheckL != null)
            {
                var hit = UnityEngine.Physics2D.Raycast(wallCheckL.position, Vector2.left, rayLength, wallMask);
                if (hit.collider != null)
                {
                    IsOnWall = true;
                    WallSide = -1;
                    return;
                }
            }

            if (wallCheckR != null)
            {
                var hit = UnityEngine.Physics2D.Raycast(wallCheckR.position, Vector2.right, rayLength, wallMask);
                if (hit.collider != null)
                {
                    IsOnWall = true;
                    WallSide = 1;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            rayLength = Mathf.Max(0.01f, rayLength);
        }

        private void OnDrawGizmosSelected()
        {
            if (wallCheckL != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(wallCheckL.position, wallCheckL.position + Vector3.left * rayLength);
            }
            if (wallCheckR != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(wallCheckR.position, wallCheckR.position + Vector3.right * rayLength);
            }
        }
#endif
    }
}

