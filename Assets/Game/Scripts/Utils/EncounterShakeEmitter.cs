using Cinemachine;
using UnityEngine;
using System.Collections;

namespace Game.Utils
{
    [DisallowMultipleComponent]
    public class EncounterShakeEmitter : MonoBehaviour
    {
        [Header("Scene Shake Source")]
        [SerializeField] private string shakeRootName = "CameraShake2D";
        [SerializeField] private string encounterSourceName = "Encounter";

        [Header("Resolved Source")]
        [SerializeField] private CinemachineImpulseSource encounterSource;

        [Header("Playback")]
        [SerializeField] private int repeatCount = 1;
        [SerializeField] private float repeatInterval = 0.08f;

        private Coroutine _shakeRoutine;

        private void Reset()
        {
            AutoWire();
        }

        private void Awake()
        {
            AutoWire();
        }

        public void ShakeEncounter()
        {
            if (encounterSource == null)
                AutoWire();

            if (encounterSource == null)
                return;

            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);

            _shakeRoutine = StartCoroutine(PlayEncounterShakeRoutine());
        }

        private void AutoWire()
        {
            if (encounterSource != null)
                return;

            if (string.IsNullOrWhiteSpace(encounterSourceName))
                return;

            Transform root = FindShakeRoot();
            if (root == null)
                return;

            Transform child = root.Find(encounterSourceName);
            if (child == null)
                return;

            encounterSource = child.GetComponent<CinemachineImpulseSource>();
        }

        private Transform FindShakeRoot()
        {
            if (string.IsNullOrWhiteSpace(shakeRootName))
                return null;

            GameObject rootObject = GameObject.Find(shakeRootName);
            return rootObject != null ? rootObject.transform : null;
        }

        private IEnumerator PlayEncounterShakeRoutine()
        {
            int count = Mathf.Max(1, repeatCount);
            float interval = Mathf.Max(0f, repeatInterval);

            for (int i = 0; i < count; i++)
            {
                if (encounterSource != null)
                    encounterSource.GenerateImpulse();

                if (i < count - 1 && interval > 0f)
                    yield return new WaitForSecondsRealtime(interval);
            }

            _shakeRoutine = null;
        }
    }
}
