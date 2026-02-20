using UnityEngine;

namespace Game.Utils
{
    [DisallowMultipleComponent]
    public sealed class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        private static bool _quitting;

        public static bool IsQuitting => _quitting;

        public static CoroutineRunner Instance
        {
            get
            {
                if (_quitting) return null;
                if (_instance != null) return _instance;
                
                _instance = FindFirstObjectByType<CoroutineRunner>();
                if (_instance != null) return _instance;
                
                var go = new GameObject("[CoroutineRunner]");
                go.hideFlags = HideFlags.HideAndDontSave;
                _instance = go.AddComponent<CoroutineRunner>();
                return _instance;
            }
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}