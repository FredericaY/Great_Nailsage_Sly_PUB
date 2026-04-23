using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Audio
{
    [DisallowMultipleComponent]
    public class AudioService : MonoBehaviour
    {
        [Header("Bus Volume")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float bgmBusVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxBusVolume = 1f;

        [Header("SFX Pool")]
        [SerializeField] private int initialSfxVoices = 8;
        [SerializeField] private int maxSfxVoices = 24;

        [Header("3D SFX Attenuation")]
        [SerializeField] private AudioRolloffMode default3dRolloff = AudioRolloffMode.Logarithmic;
        [SerializeField, Min(0.01f)] private float default3dMinDistance = 1.5f;
        [SerializeField, Min(0.02f)] private float default3dMaxDistance = 18f;
        [SerializeField, Range(0f, 5f)] private float defaultDopplerLevel = 0f;

        public static AudioService Instance { get; private set; }

        private readonly List<AudioSource> _sfxPool = new();
        private readonly Dictionary<string, AudioSource> _loopByKey = new();
        private readonly Dictionary<string, float> _lastPlayTimeByKey = new();
        private readonly Dictionary<AudioSource, float> _baseSfxVolumeBySource = new();
        private readonly List<SoundBank> _banks = new();

        private AudioSource _bgmSource;
        private Coroutine _bgmFadeRoutine;
        private float _bgmBaseVolume = 1f;

        public float MasterVolume => masterVolume;
        public float BgmBusVolume => bgmBusVolume;
        public float SfxBusVolume => sfxBusVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureBgmSource();
            EnsurePool(initialSfxVoices);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnValidate()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            bgmBusVolume = Mathf.Clamp01(bgmBusVolume);
            sfxBusVolume = Mathf.Clamp01(sfxBusVolume);
            default3dMinDistance = Mathf.Max(0.01f, default3dMinDistance);
            default3dMaxDistance = Mathf.Max(default3dMinDistance + 0.01f, default3dMaxDistance);
            defaultDopplerLevel = Mathf.Clamp(defaultDopplerLevel, 0f, 5f);

            if (!Application.isPlaying) return;
            ReapplyBusVolumes();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this) Instance = null;
        }

        public static AudioService Ensure()
        {
            if (Instance != null) return Instance;

            var existing = FindObjectOfType<AudioService>();
            if (existing != null) return existing;

            var go = new GameObject("AudioService");
            return go.AddComponent<AudioService>();
        }

        public void ApplySceneProfile(SceneAudioProfile profile)
        {
            if (profile == null) return;

            _banks.Clear();
            var banks = profile.SoundBanks;
            if (banks != null)
            {
                for (int i = 0; i < banks.Length; i++)
                {
                    if (banks[i] != null) _banks.Add(banks[i]);
                }
            }

            if (profile.DefaultBgm != null)
            {
                PlayBgm(profile.DefaultBgm, profile.DefaultBgmVolume, profile.DefaultBgmLoop, profile.DefaultBgmFadeIn);
            }
        }

        public void PlayBgm(AudioClip clip, float volume = 1f, bool loop = true, float fadeIn = 0.25f)
        {
            if (clip == null) return;
            EnsureBgmSource();

            if (_bgmFadeRoutine != null)
            {
                StopCoroutine(_bgmFadeRoutine);
                _bgmFadeRoutine = null;
            }

            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            _bgmBaseVolume = Mathf.Clamp01(volume);
            float target = EvalBgmVolume(_bgmBaseVolume);
            if (fadeIn <= 0f)
            {
                _bgmSource.volume = target;
                return;
            }

            _bgmFadeRoutine = StartCoroutine(FadeVolume(_bgmSource, 0f, target, fadeIn));
        }

        public void SwitchBgm(AudioClip clip, float volume = 1f, bool loop = true, float fadeOut = 0.2f, float fadeIn = 0.25f)
        {
            if (clip == null) return;
            EnsureBgmSource();

            if (_bgmFadeRoutine != null)
            {
                StopCoroutine(_bgmFadeRoutine);
                _bgmFadeRoutine = null;
            }

            bool sameClip = _bgmSource.clip == clip && _bgmSource.isPlaying;
            if (sameClip)
            {
                _bgmBaseVolume = Mathf.Clamp01(volume);
                _bgmSource.loop = loop;
                float target = EvalBgmVolume(_bgmBaseVolume);
                if (fadeIn <= 0f)
                {
                    _bgmSource.volume = target;
                    return;
                }

                _bgmFadeRoutine = StartCoroutine(FadeVolume(_bgmSource, _bgmSource.volume, target, fadeIn));
                return;
            }

            _bgmFadeRoutine = StartCoroutine(SwitchBgmRoutine(clip, volume, loop, fadeOut, fadeIn));
        }

        public void StopBgm(float fadeOut = 0.2f)
        {
            if (_bgmSource == null || !_bgmSource.isPlaying) return;
            if (_bgmFadeRoutine != null)
            {
                StopCoroutine(_bgmFadeRoutine);
                _bgmFadeRoutine = null;
            }

            if (fadeOut <= 0f)
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
                return;
            }

            _bgmFadeRoutine = StartCoroutine(StopBgmWithFade(fadeOut));
        }

        public void SetMasterVolume(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            ReapplyBusVolumes();
        }

        public void SetBgmBusVolume(float v)
        {
            bgmBusVolume = Mathf.Clamp01(v);
            ReapplyBusVolumes();
        }

        public void SetSfxBusVolume(float v)
        {
            sfxBusVolume = Mathf.Clamp01(v);
            ReapplyBusVolumes();
        }

        public bool PlaySfxByKey(string key, Vector3 worldPos, Transform follow = null)
        {
            PruneInvalidVoices();
            if (!TryResolveCue(key, out var cue)) return false;
            if (!cue.TryPick(out var clip, out var volume, out var pitch, out var spatialBlend)) return false;
            if (!CanPassInterval(key, cue.MinInterval)) return false;

            AudioSource src = AcquireVoice();
            if (src == null) return false;

            PrepareSource(src, clip, volume, pitch, spatialBlend, false, worldPos, follow, key);
            src.Play();
            return true;
        }

        public bool SetLoopSfxByKey(string key, bool play, Vector3 worldPos, Transform follow = null)
        {
            return SetLoopSfxByKey(key, key, play, worldPos, follow);
        }

        public bool SetLoopSfxByKey(string runtimeKey, string cueKey, bool play, Vector3 worldPos, Transform follow = null)
        {
            PruneInvalidVoices();
            if (!play)
            {
                StopLoopByKey(runtimeKey);
                return true;
            }

            if (_loopByKey.TryGetValue(runtimeKey, out var existing) && existing != null)
            {
                if (follow != null)
                {
                    existing.transform.SetParent(follow, false);
                    existing.transform.localPosition = Vector3.zero;
                }
                else
                {
                    existing.transform.SetParent(transform, true);
                    existing.transform.position = worldPos;
                }
                return true;
            }

            if (!TryResolveCue(cueKey, out var cue)) return false;
            if (!cue.TryPick(out var clip, out var volume, out var pitch, out var spatialBlend)) return false;

            AudioSource src = AcquireVoice();
            if (src == null) return false;

            PrepareSource(src, clip, volume, pitch, spatialBlend, true, worldPos, follow, runtimeKey);
            src.Play();
            _loopByKey[runtimeKey] = src;
            return true;
        }

        public void StopLoopByKey(string key)
        {
            PruneInvalidVoices();
            if (!_loopByKey.TryGetValue(key, out var src) || src == null)
                return;

            src.Stop();
            src.clip = null;
            src.loop = false;
            src.transform.SetParent(transform, false);
            _loopByKey.Remove(key);
            _baseSfxVolumeBySource.Remove(src);
        }

        private bool TryResolveCue(string key, out SoundCue cue)
        {
            cue = null;
            if (string.IsNullOrWhiteSpace(key)) return false;

            for (int i = 0; i < _banks.Count; i++)
            {
                if (_banks[i] == null) continue;
                if (_banks[i].TryGetCue(key, out cue)) return cue != null;
            }

            return false;
        }

        private bool CanPassInterval(string key, float minInterval)
        {
            if (minInterval <= 0f) return true;
            if (!_lastPlayTimeByKey.TryGetValue(key, out float last))
            {
                _lastPlayTimeByKey[key] = Time.time;
                return true;
            }

            if (Time.time - last < minInterval) return false;
            _lastPlayTimeByKey[key] = Time.time;
            return true;
        }

        private void EnsureBgmSource()
        {
            if (_bgmSource != null) return;
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.priority = 64;
        }

        private void EnsurePool(int count)
        {
            int target = Mathf.Clamp(count, 0, Mathf.Max(1, maxSfxVoices));
            while (_sfxPool.Count < target)
            {
                _sfxPool.Add(CreateVoice(_sfxPool.Count));
            }
        }

        private AudioSource AcquireVoice()
        {
            PruneInvalidVoices();
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                if (!_sfxPool[i].isPlaying)
                    return _sfxPool[i];
            }

            if (_sfxPool.Count >= Mathf.Max(1, maxSfxVoices))
                return null;

            var src = CreateVoice(_sfxPool.Count);
            _sfxPool.Add(src);
            return src;
        }

        private AudioSource CreateVoice(int index)
        {
            var go = new GameObject($"SFXVoice_{index}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = default3dMinDistance;
            src.maxDistance = default3dMaxDistance;
            src.dopplerLevel = defaultDopplerLevel;
            src.priority = 128;
            return src;
        }

        private void PrepareSource(
            AudioSource src,
            AudioClip clip,
            float volume,
            float pitch,
            float spatialBlend,
            bool loop,
            Vector3 worldPos,
            Transform follow,
            string key)
        {
            src.Stop();
            src.clip = clip;
            float baseVol = Mathf.Clamp01(volume);
            _baseSfxVolumeBySource[src] = baseVol;
            src.volume = EvalSfxVolume(baseVol);
            src.pitch = Mathf.Max(0.01f, pitch);
            src.spatialBlend = Mathf.Clamp01(spatialBlend);
            src.rolloffMode = default3dRolloff;
            src.minDistance = default3dMinDistance;
            src.maxDistance = default3dMaxDistance;
            src.dopplerLevel = defaultDopplerLevel;
            src.loop = loop;
            src.name = $"SFXVoice_{key}";

            // Keep pooled voices under AudioService to survive scene object destruction safely.
            src.transform.SetParent(transform, false);
            src.transform.position = follow != null ? follow.position : worldPos;
        }

        private IEnumerator FadeVolume(AudioSource src, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                src.volume = Mathf.Lerp(from, to, k);
                yield return null;
            }
            src.volume = to;
            _bgmFadeRoutine = null;
        }

        private IEnumerator StopBgmWithFade(float duration)
        {
            float from = _bgmSource.volume;
            yield return FadeVolume(_bgmSource, from, 0f, duration);
            _bgmSource.Stop();
            _bgmSource.clip = null;
            _bgmFadeRoutine = null;
        }

        private IEnumerator SwitchBgmRoutine(AudioClip clip, float volume, bool loop, float fadeOut, float fadeIn)
        {
            fadeOut = Mathf.Max(0f, fadeOut);
            fadeIn = Mathf.Max(0f, fadeIn);

            if (_bgmSource.isPlaying && _bgmSource.clip != null)
            {
                float from = _bgmSource.volume;
                if (fadeOut > 0f)
                {
                    float t = 0f;
                    while (t < fadeOut)
                    {
                        t += Time.unscaledDeltaTime;
                        float k = Mathf.Clamp01(t / fadeOut);
                        _bgmSource.volume = Mathf.Lerp(from, 0f, k);
                        yield return null;
                    }
                }

                _bgmSource.volume = 0f;
                _bgmSource.Stop();
            }

            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmBaseVolume = Mathf.Clamp01(volume);
            float target = EvalBgmVolume(_bgmBaseVolume);
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            if (fadeIn > 0f)
            {
                float t = 0f;
                while (t < fadeIn)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / fadeIn);
                    _bgmSource.volume = Mathf.Lerp(0f, target, k);
                    yield return null;
                }
            }

            _bgmSource.volume = target;
            _bgmFadeRoutine = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Scene reload can destroy followed objects; clear transient loop routing.
            StopAllLoopSfx();
            PruneInvalidVoices();
            ReapplyBusVolumes();
        }

        private void StopAllLoopSfx()
        {
            if (_loopByKey.Count == 0) return;
            var keys = new List<string>(_loopByKey.Keys);
            for (int i = 0; i < keys.Count; i++)
                StopLoopByKey(keys[i]);
        }

        private void PruneInvalidVoices()
        {
            _sfxPool.RemoveAll(v => v == null);
            var deadSources = new List<AudioSource>();
            foreach (var kv in _baseSfxVolumeBySource)
            {
                if (kv.Key == null) deadSources.Add(kv.Key);
            }
            for (int i = 0; i < deadSources.Count; i++)
                _baseSfxVolumeBySource.Remove(deadSources[i]);

            if (_loopByKey.Count == 0) return;
            var removeKeys = new List<string>();
            foreach (var kv in _loopByKey)
            {
                if (kv.Value == null) removeKeys.Add(kv.Key);
            }

            for (int i = 0; i < removeKeys.Count; i++)
                _loopByKey.Remove(removeKeys[i]);
        }

        private float EvalBgmVolume(float baseVolume)
        {
            return Mathf.Clamp01(baseVolume) * Mathf.Clamp01(masterVolume) * Mathf.Clamp01(bgmBusVolume);
        }

        private float EvalSfxVolume(float baseVolume)
        {
            return Mathf.Clamp01(baseVolume) * Mathf.Clamp01(masterVolume) * Mathf.Clamp01(sfxBusVolume);
        }

        private void ReapplyBusVolumes()
        {
            if (_bgmSource != null)
                _bgmSource.volume = EvalBgmVolume(_bgmBaseVolume);

            foreach (var kv in _baseSfxVolumeBySource)
            {
                AudioSource src = kv.Key;
                if (src == null) continue;
                src.volume = EvalSfxVolume(kv.Value);
            }
        }
    }
}
