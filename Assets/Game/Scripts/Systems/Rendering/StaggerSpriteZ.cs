using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Rendering
{
    [DisallowMultipleComponent]
    public class StaggerSpriteZ : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform graphicsRoot;

        [Header("Z Stagger")]
        [Tooltip("How much to offset each sprite in local Z to avoid z-fighting. Keep tiny.")]
        [SerializeField] private float step = 0.001f;

        [Tooltip("If true, child index order decides z. Otherwise sort by name for stability.")]
        [SerializeField] private bool useHierarchyOrder = false;

        [SerializeField] private bool includeInactive = true;

        private void Reset()
        {
            var g = transform.Find("Graphics");
            if (g) graphicsRoot = g;
        }

        private void Awake() => Apply();

#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif

        [ContextMenu("Apply Z Stagger Now")]
        public void Apply()
        {
            if (graphicsRoot == null) return;

            var renderers = graphicsRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive);

            if (!useHierarchyOrder)
            {
                // stable order across edits (less “random”)
                System.Array.Sort(renderers, (a, b) => string.CompareOrdinal(a.name, b.name));
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                var t = renderers[i].transform;
                var p = t.localPosition;
                p.z = i * step;
                t.localPosition = p;
            }
        }
    }
}
