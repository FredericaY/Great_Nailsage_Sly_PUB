using UnityEngine;
using Cinemachine;

namespace Game.Utils
{
    [DisallowMultipleComponent]
    public class AnimEventCameraShake : MonoBehaviour
    {
        [Header("Cinemachine Impulse Sources")]
        [SerializeField] private CinemachineImpulseSource light;
        [SerializeField] private CinemachineImpulseSource medium;
        [SerializeField] private CinemachineImpulseSource heavy;

        [Header("Fallback")]
        [SerializeField] private CameraShake2D shake;

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
            if (!light) light = GetComponent<CinemachineImpulseSource>();
            if (!medium) medium = GetComponent<CinemachineImpulseSource>();
            if (!heavy) heavy = GetComponent<CinemachineImpulseSource>();

            if (!shake) shake = CameraShake2D.Instance;
        }

        // Animation Events
        public void AnimEvent_ShakeLight()
        {
            if (light != null)
            {
                light.GenerateImpulse();
                return;
            }
            if (shake != null) shake.ShakeLight();
        }

        public void AnimEvent_ShakeMedium()
        {
            if (medium != null)
            {
                medium.GenerateImpulse();
                return;
            }
            if (shake != null) shake.ShakeMedium();
        }

        public void AnimEvent_ShakeHeavy()
        {
            if (heavy != null)
            {
                heavy.GenerateImpulse();
                return;
            }
            if (shake != null) shake.ShakeHeavy();
        }
    }
}
