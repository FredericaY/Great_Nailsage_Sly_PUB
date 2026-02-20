using UnityEngine;

namespace Game.Utils
{
    [DisallowMultipleComponent]
    public class CameraShake2D : MonoBehaviour
    {
        [System.Serializable]
        public struct Preset
        {
            public float duration;
            public float strength;
            public float frequency;
        }

        private static CameraShake2D _instance;
        public static CameraShake2D Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindFirstObjectByType<CameraShake2D>();
                return _instance;
            }
        }

        private Vector3 baseLocalPos;
        private float timeRemaining;
        private float strength;
        private float frequency;
        private float nextSampleTime;

        [Header("Presets")]
        [SerializeField] private Preset light = new Preset { duration = 0.08f, strength = 0.15f, frequency = 40f };
        [SerializeField] private Preset medium = new Preset { duration = 0.12f, strength = 0.25f, frequency = 35f };
        [SerializeField] private Preset heavy = new Preset { duration = 0.18f, strength = 0.35f, frequency = 28f };

        private void Awake()
        {
            if (_instance == null) _instance = this;
            baseLocalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            baseLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (timeRemaining <= 0f) return;

            timeRemaining -= Time.unscaledDeltaTime;
            if (Time.unscaledTime >= nextSampleTime)
            {
                nextSampleTime = Time.unscaledTime + (frequency > 0f ? 1f / frequency : 0.05f);
                Vector2 offset = Random.insideUnitCircle * strength;
                transform.localPosition = baseLocalPos + new Vector3(offset.x, offset.y, 0f);
            }

            if (timeRemaining <= 0f)
                transform.localPosition = baseLocalPos;
        }

        public void Shake(float duration, float intensity, float freq)
        {
            duration = Mathf.Max(0f, duration);
            intensity = Mathf.Max(0f, intensity);
            frequency = Mathf.Max(0f, freq);

            if (duration <= 0f || intensity <= 0f) return;

            timeRemaining = duration;
            strength = intensity;
            nextSampleTime = 0f;
        }

        public void ShakeLight() => Shake(light.duration, light.strength, light.frequency);
        public void ShakeMedium() => Shake(medium.duration, medium.strength, medium.frequency);
        public void ShakeHeavy() => Shake(heavy.duration, heavy.strength, heavy.frequency);
    }
}
