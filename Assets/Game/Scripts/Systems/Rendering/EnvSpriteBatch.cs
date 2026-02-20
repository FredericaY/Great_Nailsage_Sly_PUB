using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Rendering
{
    [DisallowMultipleComponent]
    public class EnvSpriteBatch : MonoBehaviour
    {
        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "Mid";
        [SerializeField] private int baseOrderInLayer = 0;
        [SerializeField] private bool includeInactive = true;
        [Header("Z Depth")]
        [SerializeField] private float localZ = 0f;
        [SerializeField] private bool applyZ = true;

        [ContextMenu("Apply Sorting Now")]
        public void Apply()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive);
            foreach (var r in renderers)
            {
                r.sortingLayerName = sortingLayerName;
                r.sortingOrder = baseOrderInLayer;
            }
            
            if (applyZ)
            {
                var p = transform.localPosition;
                p.z = localZ;
                transform.localPosition = p;
            }

        }

        private void Awake() => Apply();

#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif
    }
}

