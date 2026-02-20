using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Rendering
{
    [DisallowMultipleComponent]
    public class ApplySpriteMaterial : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform graphicsRoot;

        [Header("Material To Apply")]
        [SerializeField] private Material depthMaterial;

        [Header("Options")]
        [SerializeField] private bool includeInactive = true;
        [SerializeField] private bool applyOnAwake = true;

        private void Reset()
        {
            var g = transform.Find("Graphics");
            if (g) graphicsRoot = g;
        }

        private void Awake()
        {
            if (applyOnAwake)
                Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif

        [ContextMenu("Apply Material Now")]
        public void Apply()
        {
            if (graphicsRoot == null || depthMaterial == null)
                return;

            var renderers = graphicsRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive);

            foreach (var r in renderers)
            {
                r.sharedMaterial = depthMaterial;
            }
        }
    }
}
