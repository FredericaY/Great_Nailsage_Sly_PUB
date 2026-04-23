using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    [CreateAssetMenu(menuName = "Game/Audio/Sound Bank", fileName = "SoundBank_")]
    public class SoundBank : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string key;
            public SoundCue cue;
        }

        [SerializeField] private Entry[] entries;

        private Dictionary<string, SoundCue> _map;

        public bool TryGetCue(string key, out SoundCue cue)
        {
            EnsureMap();
            return _map.TryGetValue(key, out cue) && cue != null;
        }

        private void EnsureMap()
        {
            if (_map != null) return;
            _map = new Dictionary<string, SoundCue>();
            if (entries == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                string key = entries[i].key;
                if (string.IsNullOrWhiteSpace(key)) continue;
                _map[key] = entries[i].cue;
            }
        }
    }
}
