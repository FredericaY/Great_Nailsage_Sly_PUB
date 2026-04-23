using UnityEngine;
using Cinemachine;

namespace Game.Utils
{
    [DisallowMultipleComponent]
    public class EnemyAttackShakeEmitter : MonoBehaviour
    {
        [Header("Scene Shake Sources")]
        [SerializeField] private string shakeRootName = "CameraShake2D";
        [SerializeField] private string lightSourceName = "Light";
        [SerializeField] private string mediumSourceName = "Medium";
        [SerializeField] private string heavySourceName = "Heavy";

        [Header("Resolved Sources")]
        [SerializeField] private CinemachineImpulseSource lightSource;
        [SerializeField] private CinemachineImpulseSource medium;
        [SerializeField] private CinemachineImpulseSource heavy;

        private void Reset()
        {
            AutoWire();
        }

        private void Awake()
        {
            AutoWire();
        }

        private void AutoWire()
        {
            if (!lightSource) lightSource = FindNamedImpulseSource(lightSourceName);
            if (!medium) medium = FindNamedImpulseSource(mediumSourceName);
            if (!heavy) heavy = FindNamedImpulseSource(heavySourceName);
        }

        // ------------------------------
        // Public API
        // ------------------------------
        public void ShakeLight()
        {
            if (lightSource != null)
            {
                lightSource.GenerateImpulse();
            }
        }

        public void ShakeMedium()
        {
            if (medium != null)
            {
                medium.GenerateImpulse();
            }
        }

        public void ShakeHeavy()
        {
            if (heavy != null)
            {
                heavy.GenerateImpulse();
            }
        }

        // ------------------------------
        // Animation Events
        // ------------------------------
        public void AnimEvent_ShakeLight()
        {
            ShakeLight();
        }

        public void AnimEvent_ShakeMedium()
        {
            ShakeMedium();
        }

        public void AnimEvent_ShakeHeavy()
        {
            ShakeHeavy();
        }

        private CinemachineImpulseSource FindNamedImpulseSource(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                return null;

            Transform root = FindShakeRoot();
            if (root == null)
                return null;

            Transform child = root.Find(sourceName);
            if (child == null)
                return null;

            return child.GetComponent<CinemachineImpulseSource>();
        }

        private Transform FindShakeRoot()
        {
            if (string.IsNullOrWhiteSpace(shakeRootName))
                return null;

            GameObject rootObject = GameObject.Find(shakeRootName);
            return rootObject != null ? rootObject.transform : null;
        }
    }
}
