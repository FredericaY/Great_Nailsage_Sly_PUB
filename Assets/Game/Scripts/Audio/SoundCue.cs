using UnityEngine;

namespace Game.Audio
{
    [CreateAssetMenu(menuName = "Game/Audio/Sound Cue", fileName = "SoundCue_")]
    public class SoundCue : ScriptableObject
    {
        [Header("Clips")]
        [SerializeField] private AudioClip[] clips;

        [Header("Playback")]
        [SerializeField] private bool loop;
        [SerializeField] private float volumeMin = 1f;
        [SerializeField] private float volumeMax = 1f;
        [SerializeField] private float pitchMin = 1f;
        [SerializeField] private float pitchMax = 1f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;

        [Header("Limits")]
        [SerializeField] private float minInterval = 0f;

        public bool Loop => loop;
        public float MinInterval => Mathf.Max(0f, minInterval);

        public bool TryPick(out AudioClip clip, out float volume, out float pitch, out float blend)
        {
            clip = null;
            volume = 1f;
            pitch = 1f;
            blend = Mathf.Clamp01(spatialBlend);

            if (clips == null || clips.Length == 0)
                return false;

            int idx = Random.Range(0, clips.Length);
            clip = clips[idx];
            if (clip == null) return false;

            float vMin = Mathf.Max(0f, volumeMin);
            float vMax = Mathf.Max(vMin, volumeMax);
            float pMin = Mathf.Max(0.01f, pitchMin);
            float pMax = Mathf.Max(pMin, pitchMax);

            volume = Random.Range(vMin, vMax);
            pitch = Random.Range(pMin, pMax);
            return true;
        }
    }
}
