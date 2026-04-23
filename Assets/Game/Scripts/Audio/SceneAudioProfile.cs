using UnityEngine;

namespace Game.Audio
{
    [CreateAssetMenu(menuName = "Game/Audio/Scene Audio Profile", fileName = "SceneAudioProfile_")]
    public class SceneAudioProfile : ScriptableObject
    {
        [Header("BGM")]
        [SerializeField] private AudioClip defaultBgm;
        [SerializeField, Range(0f, 1f)] private float defaultBgmVolume = 1f;
        [SerializeField] private bool defaultBgmLoop = true;
        [SerializeField] private float defaultBgmFadeIn = 0.4f;

        [Header("Boss BGM")]
        [SerializeField] private AudioClip bossBgm;
        [SerializeField, Range(0f, 1f)] private float bossBgmVolume = 1f;
        [SerializeField] private bool bossBgmLoop = true;
        [SerializeField] private float bossBgmFadeIn = 0.4f;

        [Header("Transitions")]
        [SerializeField] private float bgmFadeOut = 0.2f;

        [Header("Banks")]
        [SerializeField] private SoundBank[] soundBanks;

        public AudioClip DefaultBgm => defaultBgm;
        public float DefaultBgmVolume => Mathf.Clamp01(defaultBgmVolume);
        public bool DefaultBgmLoop => defaultBgmLoop;
        public float DefaultBgmFadeIn => Mathf.Max(0f, defaultBgmFadeIn);
        public AudioClip BossBgm => bossBgm;
        public float BossBgmVolume => Mathf.Clamp01(bossBgmVolume);
        public bool BossBgmLoop => bossBgmLoop;
        public float BossBgmFadeIn => Mathf.Max(0f, bossBgmFadeIn);
        public float BgmFadeOut => Mathf.Max(0f, bgmFadeOut);
        public SoundBank[] SoundBanks => soundBanks;
    }
}
