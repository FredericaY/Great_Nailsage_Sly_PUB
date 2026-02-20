using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Utils.Physics2D
{
    [DisallowMultipleComponent]
    public class GroundSensor2D : MonoBehaviour
    {
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float radius = 0.12f;
        [SerializeField] private LayerMask groundMask;

        public bool IsGrounded { get; private set; }

        // 可选：需要的话给外部读配置
        public Transform GroundCheck => groundCheck;
        public float Radius => radius;
        public LayerMask GroundMask => groundMask;

        private void Reset()
        {
            if (groundCheck == null)
            {
                var gc = new GameObject("GroundCheck");
                gc.transform.SetParent(transform);
                gc.transform.localPosition = Vector3.zero; // 之后在 Inspector 调到脚底
                groundCheck = gc.transform;
            }
        }

        private void Update()
        {
            UpdateGrounded();
        }

        public void UpdateGrounded()
        {
            if (!groundCheck)
            {
                IsGrounded = false;
                return;
            }

            IsGrounded = UnityEngine.Physics2D.OverlapCircle(
                groundCheck.position,
                radius,
                groundMask
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!groundCheck) return;
            Gizmos.DrawWireSphere(groundCheck.position, radius);
        }
#endif
    }
}

