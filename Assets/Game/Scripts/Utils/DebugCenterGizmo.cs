using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Utils
{
    [ExecuteAlways]
    public class DebugCenterGizmo : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        [SerializeField] private Color color = Color.magenta;
        [SerializeField] private float radius = 0.08f;
        [SerializeField] private bool drawAlways = true;

        private void OnDrawGizmos()
        {
            if (!drawAlways) return;

            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, radius);
        }

        private void OnDrawGizmosSelected()
        {
            if (drawAlways) return;

            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}

